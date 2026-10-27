namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents an archive of The Sims 4's tuning across versions of the game, stored in a SQLite database file (🔒)
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="TuningArchiveSnapshot"/> records the tuning, SimData and strings of a version of the game, as captured from an installation or imported from extracted files.
/// Content is stored once however many snapshots contain it, and each resource's history is kept as ranges of snapshots, so a snapshot adds only what changed.
/// </para>
/// <para>
/// An archive's members are safe to use from multiple threads. Operations that change the archive are serialized.
/// </para>
/// </remarks>
public sealed partial class TuningArchiveFile :
    IDisposable
{
    const long ordinalSpacing = 1024;
    const int referenceIndexVersion = 4;
    const int schemaVersion = 2;

    internal const string AnimationStateMachineScope = "asm";
    internal const string ImagesScope = "images";
    internal const string PythonScope = "python";
    internal const string SimDataScope = "simdata";
    internal const string SoundsScope = "sounds";
    internal const string StringsScopePrefix = "strings:";
    internal const string TuningScope = "tuning";
    internal const string VideosScope = "videos";

    /// <summary>
    /// Opens the archive at the specified <paramref name="path"/>, creating it if it does not exist (🗑️)
    /// </summary>
    /// <exception cref="InvalidDataException">The file is an archive created by a newer version of this library</exception>
    public static TuningArchiveFile Open(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var archive = new TuningArchiveFile(System.IO.Path.GetFullPath(path));
        try
        {
            archive.Initialize();
            return archive;
        }
        catch
        {
            archive.Dispose();
            throw;
        }
    }

    internal static string GetStringsScope(string locale) =>
        $"{StringsScopePrefix}{locale}";

    static int CompareSnapshotOrder((string gameVersion, DateTimeOffset capturedAt) x, (string gameVersion, DateTimeOffset capturedAt) y)
    {
        var xParsed = Version.TryParse(x.gameVersion, out var xVersion);
        var yParsed = Version.TryParse(y.gameVersion, out var yVersion);
        var comparison = xParsed && yParsed
            ? xVersion!.CompareTo(yVersion)
            : xParsed != yParsed
            ? (xParsed ? 1 : -1)
            : string.CompareOrdinal(x.gameVersion, y.gameVersion);
        return comparison != 0 ? comparison : x.capturedAt.CompareTo(y.capturedAt);
    }

    TuningArchiveFile(string path)
    {
        Path = path;
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true,
            DefaultTimeout = 60
        }.ToString();
    }

    readonly ConcurrentDictionary<long, BlobCodec> codecs = new();
    readonly ConcurrentDictionary<long, TuningDescriptionCache> descriptionCaches = new();
    readonly string connectionString;
    readonly ConcurrentDictionary<long, Lazy<TuningDescriptionSet?>> descriptionSets = new();
    int isDisposed;
    readonly SemaphoreSlim writeLock = new(1, 1);

    /// <summary>
    /// Gets the path of the archive's database file
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Releases the archive's resources
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref isDisposed, 1) != 0)
            return;
        foreach (var codec in codecs.Values)
            codec.Dispose();
        foreach (var lazySet in descriptionSets.Values)
            if (lazySet.IsValueCreated)
                lazySet.Value?.Dispose();
        writeLock.Dispose();
        using var connection = new SqliteConnection(connectionString);
        SqliteConnection.ClearPool(connection);
    }

    internal SqliteConnection OpenConnection()
    {
        if (isDisposed != 0)
            throw new ObjectDisposedException(nameof(TuningArchiveFile));
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA synchronous = NORMAL; PRAGMA temp_store = MEMORY; PRAGMA cache_size = -65536;";
        command.ExecuteNonQuery();
        return connection;
    }

    void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = WAL;";
        command.ExecuteNonQuery();
        command.CommandText = "CREATE TABLE IF NOT EXISTS meta (key TEXT PRIMARY KEY, value TEXT NOT NULL) WITHOUT ROWID; SELECT value FROM meta WHERE key = 'schema_version';";
        var existingVersion = command.ExecuteScalar() is string text ? int.Parse(text, CultureInfo.InvariantCulture) : 0;
        if (existingVersion > schemaVersion)
            throw new InvalidDataException($"The archive at {Path} was created by a newer version of this library (schema {existingVersion})");
        if (existingVersion < 1)
            CreateSchema(connection);
        if (existingVersion < 2)
            MigrateSchemaToVersion2(connection);
        command.CommandText = "SELECT value FROM meta WHERE key = 'reference_index_version'";
        if (command.ExecuteScalar() is string indexVersionText && int.Parse(indexVersionText, CultureInfo.InvariantCulture) == referenceIndexVersion)
            return;
        using var transaction = connection.BeginTransaction();
        command.Transaction = transaction;
        command.CommandText =
            """
            DELETE FROM reference;
            UPDATE blob SET references_indexed_by = NULL WHERE references_indexed_by IS NOT NULL;
            INSERT INTO meta (key, value) VALUES ('reference_index_version', $version) ON CONFLICT (key) DO UPDATE SET value = excluded.value;
            """;
        command.Parameters.AddWithValue("$version", referenceIndexVersion.ToString(CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    static void CreateSchema(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            CREATE TABLE snapshot (
                id INTEGER PRIMARY KEY,
                ord INTEGER NOT NULL UNIQUE,
                game_version TEXT NOT NULL,
                label TEXT,
                notes TEXT,
                source INTEGER NOT NULL,
                source_path TEXT,
                captured_utc TEXT NOT NULL,
                locale TEXT
            );
            CREATE TABLE snapshot_pack (
                snapshot_id INTEGER NOT NULL,
                pack TEXT NOT NULL,
                version TEXT,
                PRIMARY KEY (snapshot_id, pack)
            ) WITHOUT ROWID;
            CREATE TABLE snapshot_scope (
                snapshot_id INTEGER NOT NULL,
                scope TEXT NOT NULL,
                PRIMARY KEY (snapshot_id, scope)
            ) WITHOUT ROWID;
            CREATE INDEX snapshot_scope_scope ON snapshot_scope (scope);
            CREATE TABLE dictionary (
                id INTEGER PRIMARY KEY,
                kind INTEGER NOT NULL,
                data BLOB NOT NULL
            );
            CREATE TABLE blob (
                id INTEGER PRIMARY KEY,
                hash BLOB NOT NULL UNIQUE,
                dictionary_id INTEGER,
                size INTEGER NOT NULL,
                data BLOB NOT NULL,
                references_indexed_by INTEGER
            );
            CREATE TABLE resource (
                id INTEGER PRIMARY KEY,
                kind INTEGER NOT NULL,
                type INTEGER NOT NULL,
                grp INTEGER NOT NULL,
                instance INTEGER NOT NULL,
                UNIQUE (kind, type, grp, instance)
            );
            CREATE TABLE tuning_class (
                id INTEGER PRIMARY KEY,
                class TEXT NOT NULL,
                module TEXT NOT NULL,
                UNIQUE (class, module)
            );
            CREATE TABLE tuning_type (
                type INTEGER PRIMARY KEY,
                name TEXT NOT NULL
            );
            CREATE TABLE resource_version (
                id INTEGER PRIMARY KEY,
                resource_id INTEGER NOT NULL,
                first_ord INTEGER NOT NULL,
                last_ord INTEGER,
                blob_id INTEGER NOT NULL,
                pack TEXT NOT NULL,
                name TEXT,
                class_id INTEGER
            );
            CREATE INDEX resource_version_resource ON resource_version (resource_id, first_ord);
            CREATE INDEX resource_version_first ON resource_version (first_ord);
            CREATE INDEX resource_version_last ON resource_version (last_ord);
            CREATE INDEX resource_version_blob ON resource_version (blob_id);
            CREATE TABLE string_key (
                id INTEGER PRIMARY KEY,
                locale TEXT NOT NULL,
                key INTEGER NOT NULL,
                UNIQUE (locale, key)
            );
            CREATE TABLE string_version (
                id INTEGER PRIMARY KEY,
                string_key_id INTEGER NOT NULL,
                first_ord INTEGER NOT NULL,
                last_ord INTEGER,
                text TEXT NOT NULL,
                pack TEXT NOT NULL
            );
            CREATE INDEX string_version_key ON string_version (string_key_id, first_ord);
            CREATE INDEX string_version_first ON string_version (first_ord);
            CREATE INDEX string_version_last ON string_version (last_ord);
            CREATE TABLE tuning_description (
                id INTEGER PRIMARY KEY,
                game_version TEXT NOT NULL UNIQUE,
                file_name TEXT,
                imported_utc TEXT NOT NULL,
                data BLOB NOT NULL
            );
            CREATE TABLE reference (
                target_type INTEGER NOT NULL,
                target_instance INTEGER NOT NULL,
                blob_id INTEGER NOT NULL,
                PRIMARY KEY (target_type, target_instance, blob_id)
            ) WITHOUT ROWID;
            CREATE INDEX reference_blob ON reference (blob_id);
            INSERT INTO meta (key, value) VALUES ('schema_version', '1');
            """;
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    static void MigrateSchemaToVersion2(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            CREATE TABLE snapshot_uninstalled_tuning (
                snapshot_id INTEGER NOT NULL,
                type INTEGER NOT NULL,
                instance INTEGER NOT NULL,
                name TEXT NOT NULL,
                class TEXT,
                pack TEXT NOT NULL,
                PRIMARY KEY (snapshot_id, type, instance)
            ) WITHOUT ROWID;
            UPDATE meta SET value = '2' WHERE key = 'schema_version';
            """;
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    internal BlobCodec GetCodec(SqliteConnection connection, long? dictionaryId)
    {
        if (dictionaryId is not { } id)
            return codecs.GetOrAdd(0, _ => new BlobCodec(null, null));
        return codecs.GetOrAdd(id, _ =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT data FROM dictionary WHERE id = $id";
            command.Parameters.AddWithValue("$id", id);
            return new BlobCodec(id, (byte[])command.ExecuteScalar()!);
        });
    }

    /// <summary>
    /// Gets the snapshots in the archive, in order of game version (and then of when they were captured)
    /// </summary>
    public IReadOnlyList<TuningArchiveSnapshot> GetSnapshots()
    {
        using var connection = OpenConnection();
        return GetSnapshots(connection, null);
    }

    internal static List<TuningArchiveSnapshot> GetSnapshots(SqliteConnection connection, SqliteTransaction? transaction)
    {
        var packs = new Dictionary<long, List<TuningArchiveSnapshotPack>>();
        using (var packCommand = connection.CreateCommand())
        {
            packCommand.Transaction = transaction;
            packCommand.CommandText = "SELECT snapshot_id, pack, version FROM snapshot_pack ORDER BY snapshot_id, CASE pack WHEN 'BG' THEN 0 ELSE 1 END, pack";
            using var reader = packCommand.ExecuteReader();
            while (reader.Read())
            {
                if (!packs.TryGetValue(reader.GetInt64(0), out var list))
                {
                    list = [];
                    packs.Add(reader.GetInt64(0), list);
                }
                list.Add(new(reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2)));
            }
        }
        var kinds = new Dictionary<long, HashSet<TuningArchiveResourceKind>>();
        using (var scopeCommand = connection.CreateCommand())
        {
            scopeCommand.Transaction = transaction;
            scopeCommand.CommandText = "SELECT snapshot_id, scope FROM snapshot_scope";
            using var reader = scopeCommand.ExecuteReader();
            while (reader.Read())
            {
                var kind = GetKind(reader.GetString(1));
                if (kind is null)
                    continue;
                if (!kinds.TryGetValue(reader.GetInt64(0), out var set))
                {
                    set = [];
                    kinds.Add(reader.GetInt64(0), set);
                }
                set.Add(kind.Value);
            }
        }
        var snapshots = new List<TuningArchiveSnapshot>();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT id, ord, game_version, label, notes, source, source_path, captured_utc, locale FROM snapshot ORDER BY ord";
        using var snapshotReader = command.ExecuteReader();
        while (snapshotReader.Read())
            snapshots.Add(new
            (
                snapshotReader.GetInt64(0),
                snapshotReader.GetInt64(1),
                snapshotReader.GetString(2),
                snapshotReader.IsDBNull(3) ? null : snapshotReader.GetString(3),
                snapshotReader.IsDBNull(4) ? null : snapshotReader.GetString(4),
                (TuningArchiveSnapshotSource)snapshotReader.GetInt32(5),
                snapshotReader.IsDBNull(6) ? null : snapshotReader.GetString(6),
                DateTimeOffset.Parse(snapshotReader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                snapshotReader.IsDBNull(8) ? null : snapshotReader.GetString(8),
                packs.TryGetValue(snapshotReader.GetInt64(0), out var snapshotPacks) ? snapshotPacks : [],
                kinds.TryGetValue(snapshotReader.GetInt64(0), out var snapshotKinds) ? snapshotKinds : new HashSet<TuningArchiveResourceKind>()
            ));
        return snapshots;
    }

    /// <summary>
    /// Gets whether the specified <paramref name="snapshot"/> contains resources of the specified <paramref name="kind"/>
    /// </summary>
    public bool HasKind(TuningArchiveSnapshot snapshot, TuningArchiveResourceKind kind)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        using var connection = OpenConnection();
        return HasScope(connection, null, snapshot.Id, GetScope(kind));
    }

    static TuningArchiveResourceKind? GetKind(string scope) =>
        scope switch
        {
            TuningScope => TuningArchiveResourceKind.Tuning,
            SimDataScope => TuningArchiveResourceKind.SimData,
            PythonScope => TuningArchiveResourceKind.PythonModule,
            AnimationStateMachineScope => TuningArchiveResourceKind.AnimationStateMachine,
            ImagesScope => TuningArchiveResourceKind.Image,
            SoundsScope => TuningArchiveResourceKind.Sound,
            VideosScope => TuningArchiveResourceKind.Video,
            _ => null
        };

    internal static string GetScope(TuningArchiveResourceKind kind) =>
        kind switch
        {
            TuningArchiveResourceKind.Tuning => TuningScope,
            TuningArchiveResourceKind.SimData => SimDataScope,
            TuningArchiveResourceKind.PythonModule => PythonScope,
            TuningArchiveResourceKind.AnimationStateMachine => AnimationStateMachineScope,
            TuningArchiveResourceKind.Image => ImagesScope,
            TuningArchiveResourceKind.Sound => SoundsScope,
            TuningArchiveResourceKind.Video => VideosScope,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    internal static bool HasScope(SqliteConnection connection, SqliteTransaction? transaction, long snapshotId, string scope)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1 FROM snapshot_scope WHERE snapshot_id = $snapshot AND scope = $scope";
        command.Parameters.AddWithValue("$snapshot", snapshotId);
        command.Parameters.AddWithValue("$scope", scope);
        return command.ExecuteScalar() is not null;
    }

    static (long? previous, long? next) GetScopeNeighbors(SqliteConnection connection, SqliteTransaction transaction, string scope, long ordinal)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT
                (SELECT MAX(s.ord) FROM snapshot s JOIN snapshot_scope sc ON sc.snapshot_id = s.id WHERE sc.scope = $scope AND s.ord < $ord),
                (SELECT MIN(s.ord) FROM snapshot s JOIN snapshot_scope sc ON sc.snapshot_id = s.id WHERE sc.scope = $scope AND s.ord > $ord)
            """;
        command.Parameters.AddWithValue("$scope", scope);
        command.Parameters.AddWithValue("$ord", ordinal);
        using var reader = command.ExecuteReader();
        reader.Read();
        return (reader.IsDBNull(0) ? null : reader.GetInt64(0), reader.IsDBNull(1) ? null : reader.GetInt64(1));
    }

    static RangeTable<ResourceContent> ResourceRanges { get; } = new
    (
        "resource_version",
        "resource_id",
        "resource_id IN (SELECT id FROM resource WHERE kind = $kind)",
        ["blob_id", "pack", "name", "class_id"],
        (reader, offset) => new(reader.GetInt64(offset), reader.GetString(offset + 1), reader.IsDBNull(offset + 2) ? null : reader.GetString(offset + 2), reader.IsDBNull(offset + 3) ? null : reader.GetInt64(offset + 3)),
        (parameters, content) =>
        {
            parameters.AddWithValue("$c0", content.BlobId);
            parameters.AddWithValue("$c1", content.Pack);
            parameters.AddWithValue("$c2", (object?)content.Name ?? DBNull.Value);
            parameters.AddWithValue("$c3", (object?)content.ClassId ?? DBNull.Value);
        }
    );

    static RangeTable<StringContent> StringRanges { get; } = new
    (
        "string_version",
        "string_key_id",
        "string_key_id IN (SELECT id FROM string_key WHERE locale = $locale)",
        ["text", "pack"],
        (reader, offset) => new(reader.GetString(offset), reader.GetString(offset + 1)),
        (parameters, content) =>
        {
            parameters.AddWithValue("$c0", content.Text);
            parameters.AddWithValue("$c1", content.Pack);
        }
    );

    internal static void ApplyResourceScope(SqliteConnection connection, SqliteTransaction transaction, long snapshotId, long ordinal, TuningArchiveResourceKind kind, IReadOnlyDictionary<long, ResourceContent> manifest)
    {
        var scope = GetScope(kind);
        var (previous, next) = GetScopeNeighbors(connection, transaction, scope, ordinal);
        ResourceRanges.Apply(connection, transaction, parameters => parameters.AddWithValue("$kind", (int)kind), previous, ordinal, next, manifest);
        AddScope(connection, transaction, snapshotId, scope);
    }

    internal static void ApplyStringScope(SqliteConnection connection, SqliteTransaction transaction, long snapshotId, long ordinal, string locale, IReadOnlyDictionary<long, StringContent> manifest)
    {
        var scope = GetStringsScope(locale);
        var (previous, next) = GetScopeNeighbors(connection, transaction, scope, ordinal);
        StringRanges.Apply(connection, transaction, parameters => parameters.AddWithValue("$locale", locale), previous, ordinal, next, manifest);
        AddScope(connection, transaction, snapshotId, scope);
    }

    static void AddScope(SqliteConnection connection, SqliteTransaction transaction, long snapshotId, string scope)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO snapshot_scope (snapshot_id, scope) VALUES ($snapshot, $scope) ON CONFLICT DO NOTHING";
        command.Parameters.AddWithValue("$snapshot", snapshotId);
        command.Parameters.AddWithValue("$scope", scope);
        command.ExecuteNonQuery();
    }

    internal static (long id, long ordinal) InsertSnapshot(SqliteConnection connection, SqliteTransaction transaction, string gameVersion, string? label, string? notes, TuningArchiveSnapshotSource source, string? sourcePath, DateTimeOffset capturedAt, string? locale, IEnumerable<TuningArchiveSnapshotPack> packs)
    {
        var snapshots = GetSnapshots(connection, transaction);
        var position = 0;
        while (position < snapshots.Count && CompareSnapshotOrder((snapshots[position].GameVersion, snapshots[position].CapturedAt), (gameVersion, capturedAt)) <= 0)
            ++position;
        long? previous = position > 0 ? snapshots[position - 1].Ordinal : null;
        long? next = position < snapshots.Count ? snapshots[position].Ordinal : null;
        if (previous is { } p && next is { } n && n - p < 2)
        {
            Renumber(connection, transaction, snapshots);
            snapshots = GetSnapshots(connection, transaction);
            previous = snapshots[position - 1].Ordinal;
            next = snapshots[position].Ordinal;
        }
        var ordinal = (previous, next) switch
        {
            (null, null) => ordinalSpacing,
            ({ } a, null) => a + ordinalSpacing,
            (null, { } b) => b - ordinalSpacing,
            ({ } a, { } b) => a + (b - a) / 2
        };
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO snapshot (ord, game_version, label, notes, source, source_path, captured_utc, locale) VALUES ($ord, $version, $label, $notes, $source, $path, $captured, $locale) RETURNING id";
        command.Parameters.AddWithValue("$ord", ordinal);
        command.Parameters.AddWithValue("$version", gameVersion);
        command.Parameters.AddWithValue("$label", (object?)label ?? DBNull.Value);
        command.Parameters.AddWithValue("$notes", (object?)notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$source", (int)source);
        command.Parameters.AddWithValue("$path", (object?)sourcePath ?? DBNull.Value);
        command.Parameters.AddWithValue("$captured", capturedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$locale", (object?)locale ?? DBNull.Value);
        var id = (long)command.ExecuteScalar()!;
        using var packCommand = connection.CreateCommand();
        packCommand.Transaction = transaction;
        packCommand.CommandText = "INSERT INTO snapshot_pack (snapshot_id, pack, version) VALUES ($snapshot, $pack, $version)";
        foreach (var pack in packs)
        {
            packCommand.Parameters.Clear();
            packCommand.Parameters.AddWithValue("$snapshot", id);
            packCommand.Parameters.AddWithValue("$pack", pack.Code);
            packCommand.Parameters.AddWithValue("$version", (object?)pack.Version ?? DBNull.Value);
            packCommand.ExecuteNonQuery();
        }
        return (id, ordinal);
    }

    static void Renumber(SqliteConnection connection, SqliteTransaction transaction, IReadOnlyList<TuningArchiveSnapshot> snapshots)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "CREATE TEMP TABLE IF NOT EXISTS ordinal_map (old INTEGER PRIMARY KEY, new INTEGER NOT NULL); DELETE FROM ordinal_map;";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO ordinal_map (old, new) VALUES ($old, $new)";
        var oldParameter = command.Parameters.Add("$old", SqliteType.Integer);
        var newParameter = command.Parameters.Add("$new", SqliteType.Integer);
        for (var i = 0; i < snapshots.Count; ++i)
        {
            oldParameter.Value = snapshots[i].Ordinal;
            newParameter.Value = -(i + 1) * ordinalSpacing;
            command.ExecuteNonQuery();
        }
        command.Parameters.Clear();
        command.CommandText =
            """
            UPDATE snapshot SET ord = (SELECT new FROM ordinal_map WHERE old = snapshot.ord);
            UPDATE resource_version SET first_ord = (SELECT new FROM ordinal_map WHERE old = first_ord), last_ord = (SELECT new FROM ordinal_map WHERE old = last_ord);
            UPDATE string_version SET first_ord = (SELECT new FROM ordinal_map WHERE old = first_ord), last_ord = (SELECT new FROM ordinal_map WHERE old = last_ord);
            UPDATE snapshot SET ord = -ord;
            UPDATE resource_version SET first_ord = -first_ord, last_ord = -last_ord;
            UPDATE string_version SET first_ord = -first_ord, last_ord = -last_ord;
            DROP TABLE ordinal_map;
            """;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Changes the label and notes of the specified <paramref name="snapshot"/>, returning the updated snapshot (💤)
    /// </summary>
    public async Task<TuningArchiveSnapshot> UpdateSnapshotAsync(TuningArchiveSnapshot snapshot, string? label, string? notes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE snapshot SET label = $label, notes = $notes WHERE id = $id";
            command.Parameters.AddWithValue("$id", snapshot.Id);
            command.Parameters.AddWithValue("$label", string.IsNullOrWhiteSpace(label) ? DBNull.Value : label);
            command.Parameters.AddWithValue("$notes", string.IsNullOrWhiteSpace(notes) ? DBNull.Value : notes);
            command.ExecuteNonQuery();
            return GetSnapshots(connection, null).First(updated => updated.Id == snapshot.Id);
        }
        finally
        {
            writeLock.Release();
        }
    }

    /// <summary>
    /// Deletes the specified <paramref name="snapshot"/> and any content no other snapshot shares (💤)
    /// </summary>
    public async Task DeleteSnapshotAsync(TuningArchiveSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await Task.Run(() =>
            {
                using var connection = OpenConnection();
                using var transaction = connection.BeginTransaction();
                var scopes = new List<string>();
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "SELECT scope FROM snapshot_scope WHERE snapshot_id = $snapshot";
                    command.Parameters.AddWithValue("$snapshot", snapshot.Id);
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                        scopes.Add(reader.GetString(0));
                }
                foreach (var scope in scopes)
                {
                    var (previous, next) = GetScopeNeighbors(connection, transaction, scope, snapshot.Ordinal);
                    if (scope.StartsWith(StringsScopePrefix, StringComparison.Ordinal))
                        StringRanges.Remove(connection, transaction, parameters => parameters.AddWithValue("$locale", scope[StringsScopePrefix.Length..]), previous, snapshot.Ordinal, next);
                    else
                    {
                        var kind = GetKind(scope) ?? TuningArchiveResourceKind.PythonModule;
                        ResourceRanges.Remove(connection, transaction, parameters => parameters.AddWithValue("$kind", (int)kind), previous, snapshot.Ordinal, next);
                    }
                }
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText =
                        """
                        DELETE FROM snapshot_scope WHERE snapshot_id = $snapshot;
                        DELETE FROM snapshot_pack WHERE snapshot_id = $snapshot;
                        DELETE FROM snapshot_uninstalled_tuning WHERE snapshot_id = $snapshot;
                        DELETE FROM snapshot WHERE id = $snapshot;
                        """;
                    command.Parameters.AddWithValue("$snapshot", snapshot.Id);
                    command.ExecuteNonQuery();
                }
                CollectGarbage(connection, transaction);
                transaction.Commit();
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    static void CollectGarbage(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            DELETE FROM reference WHERE blob_id NOT IN (SELECT blob_id FROM resource_version);
            DELETE FROM blob WHERE id NOT IN (SELECT blob_id FROM resource_version);
            DELETE FROM resource WHERE id NOT IN (SELECT resource_id FROM resource_version);
            DELETE FROM string_key WHERE id NOT IN (SELECT string_key_id FROM string_version);
            """;
        command.ExecuteNonQuery();
    }
}