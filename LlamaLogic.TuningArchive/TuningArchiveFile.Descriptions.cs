namespace LlamaLogic.TuningArchive;

partial class TuningArchiveFile
{
#if IS_NET_7_0_OR_GREATER
    [GeneratedRegex(@"(?<version>\d+\.\d+\.\d+\.\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex GetVersionInFileNamePattern();
#else
    static readonly Regex versionInFileNamePattern = new(@"(?<version>\d+\.\d+\.\d+\.\d+)", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    static Regex GetVersionInFileNamePattern() =>
        versionInFileNamePattern;
#endif

    /// <summary>
    /// Imports the ZIP archive of tuning descriptions at the specified <paramref name="path"/>, as Maxis distributes them, replacing any set already imported for the same game version (💤)
    /// </summary>
    /// <remarks>
    /// When <paramref name="gameVersion"/> is <see langword="null"/>, it is taken from the name of the file (such as <c>1.128.90.1030-TDESC.zip</c>).
    /// </remarks>
    /// <exception cref="InvalidOperationException">No game version was specified and the file's name does not contain one</exception>
    /// <exception cref="NotSupportedException">The archive was opened for reading only</exception>
    public async Task<TuningArchiveDescriptions> ImportTuningDescriptionsAsync(string path, string? gameVersion = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        ThrowIfReadOnly();
        var fileName = System.IO.Path.GetFileName(path);
        gameVersion ??= GetVersionInFileNamePattern().Match(fileName) is { Success: true } match
            ? match.Groups["version"].Value
            : throw new InvalidOperationException($"The name of {fileName} does not contain a game version; specify it");
        var data = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        using (var stream = new MemoryStream(data, false))
        using (var set = TuningDescriptionSet.FromZip(stream))
            if (set.InstanceClasses.Count == 0)
                throw new InvalidDataException($"{fileName} does not contain tuning descriptions");
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM tuning_description WHERE game_version = $version; INSERT INTO tuning_description (game_version, file_name, imported_utc, data) VALUES ($version, $file, $imported, $data) RETURNING id";
            command.Parameters.AddWithValue("$version", gameVersion);
            command.Parameters.AddWithValue("$file", fileName);
            var importedAt = DateTimeOffset.UtcNow;
            command.Parameters.AddWithValue("$imported", importedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$data", data);
            var id = (long)command.ExecuteScalar()!;
            return new(id, gameVersion, fileName, importedAt);
        }
        finally
        {
            writeLock.Release();
        }
    }

    /// <summary>
    /// Gets the sets of tuning descriptions imported into the archive, in order of game version
    /// </summary>
    public IReadOnlyList<TuningArchiveDescriptions> GetTuningDescriptions()
    {
        using var connection = OpenConnection();
        return GetTuningDescriptions(connection);
    }

    static List<TuningArchiveDescriptions> GetTuningDescriptions(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, game_version, file_name, imported_utc FROM tuning_description";
        var list = new List<TuningArchiveDescriptions>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            list.Add(new(reader.GetInt64(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)));
        list.Sort((x, y) => CompareSnapshotOrder((x.GameVersion, default), (y.GameVersion, default)));
        return list;
    }

    /// <summary>
    /// Deletes the specified set of tuning <paramref name="descriptions"/> and the references indexed with it (💤)
    /// </summary>
    /// <exception cref="NotSupportedException">The archive was opened for reading only</exception>
    public async Task DeleteTuningDescriptionsAsync(TuningArchiveDescriptions descriptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptions);
        ThrowIfReadOnly();
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                DELETE FROM reference WHERE blob_id IN (SELECT id FROM blob WHERE references_indexed_by = $id);
                UPDATE blob SET references_indexed_by = NULL WHERE references_indexed_by = $id;
                DELETE FROM tuning_description WHERE id = $id;
                """;
            command.Parameters.AddWithValue("$id", descriptions.Id);
            command.ExecuteNonQuery();
            transaction.Commit();
            descriptionCaches.TryRemove(descriptions.Id, out _);
            if (descriptionSets.TryRemove(descriptions.Id, out var lazySet) && lazySet.IsValueCreated)
                lazySet.Value?.Dispose();
        }
        finally
        {
            writeLock.Release();
        }
    }

    /// <summary>
    /// Gets the set of tuning descriptions best suited to the specified <paramref name="snapshot"/>: the one for its game version, otherwise the nearest earlier one, otherwise the nearest later one
    /// </summary>
    public TuningArchiveDescriptions? FindTuningDescriptions(TuningArchiveSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        using var connection = OpenConnection();
        return FindTuningDescriptions(GetTuningDescriptions(connection), snapshot.GameVersion);
    }

    /// <summary>
    /// Gets the set of tuning descriptions for the game version of the specified <paramref name="snapshot"/>: when <paramref name="exactGameVersion"/> is <see langword="true"/>, only the one for that game version (or <see langword="null"/> if the archive has none), otherwise the one best suited to it (see <see cref="FindTuningDescriptions(TuningArchiveSnapshot)"/>)
    /// </summary>
    public TuningArchiveDescriptions? FindTuningDescriptions(TuningArchiveSnapshot snapshot, bool exactGameVersion)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!exactGameVersion)
            return FindTuningDescriptions(snapshot);
        using var connection = OpenConnection();
        return GetTuningDescriptions(connection).FirstOrDefault(descriptions => descriptions.GameVersion == snapshot.GameVersion);
    }

    static TuningArchiveDescriptions? FindTuningDescriptions(IReadOnlyList<TuningArchiveDescriptions> all, string gameVersion)
    {
        if (all.FirstOrDefault(descriptions => descriptions.GameVersion == gameVersion) is { } exact)
            return exact;
        return all.LastOrDefault(descriptions => CompareSnapshotOrder((descriptions.GameVersion, default), (gameVersion, default)) < 0)
            ?? all.FirstOrDefault();
    }

    /// <summary>
    /// Opens the specified set of tuning <paramref name="descriptions"/>; the archive owns the set, so it must not be disposed
    /// </summary>
    public TuningDescriptionSet OpenTuningDescriptions(TuningArchiveDescriptions descriptions)
    {
        ArgumentNullException.ThrowIfNull(descriptions);
        return descriptionSets.GetOrAdd(descriptions.Id, id => new Lazy<TuningDescriptionSet?>(() =>
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT data FROM tuning_description WHERE id = $id";
            command.Parameters.AddWithValue("$id", id);
            if (command.ExecuteScalar() is not byte[] data)
                return null;
            return TuningDescriptionSet.FromZip(new MemoryStream(data, false));
        })).Value ?? throw new InvalidOperationException("The tuning descriptions are no longer in the archive");
    }

    /// <summary>
    /// Gets the cache the archive keeps for the specified set of tuning <paramref name="descriptions"/>, so that repeated analyses are fast
    /// </summary>
    public TuningDescriptionCache GetTuningDescriptionCache(TuningArchiveDescriptions descriptions)
    {
        ArgumentNullException.ThrowIfNull(descriptions);
        return descriptionCaches.GetOrAdd(descriptions.Id, _ => new TuningDescriptionCache(OpenTuningDescriptions(descriptions)));
    }

    /// <summary>
    /// Analyzes the specified tuning <paramref name="entry"/> with the tuning descriptions best suited to the specified <paramref name="snapshot"/>, or returns <see langword="null"/> if the archive has no tuning descriptions
    /// </summary>
    public TuningAnalysis? Analyze(TuningArchiveSnapshot snapshot, TuningArchiveEntry entry)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.Kind is not TuningArchiveResourceKind.Tuning || FindTuningDescriptions(snapshot) is not { } descriptions)
            return null;
        return TuningAnalysis.Analyze(GetText(entry), GetTuningDescriptionCache(descriptions));
    }
}