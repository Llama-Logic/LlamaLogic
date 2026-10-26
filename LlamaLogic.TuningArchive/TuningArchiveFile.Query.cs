namespace LlamaLogic.TuningArchive;

partial class TuningArchiveFile
{
    const string entrySelect =
        """
        SELECT r.id, v.id, r.kind, r.type, r.grp, r.instance, v.name, v.pack, c.class, c.module, v.blob_id, v.first_ord, v.last_ord
        FROM resource_version v
        JOIN resource r ON r.id = v.resource_id
        LEFT JOIN tuning_class c ON c.id = v.class_id
        """;

    const string coversA = "(v.first_ord <= $a AND (v.last_ord IS NULL OR v.last_ord >= $a))";
    const string coversB = "(v.first_ord <= $b AND (v.last_ord IS NULL OR v.last_ord >= $b))";

    static TuningArchiveEntry ReadEntry(SqliteDataReader reader) =>
        new
        (
            reader.GetInt64(0),
            reader.GetInt64(1),
            (TuningArchiveResourceKind)reader.GetInt32(2),
            (ResourceType)(uint)reader.GetInt64(3),
            (uint)reader.GetInt64(4),
            unchecked((ulong)reader.GetInt64(5)),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.GetString(7),
            reader.IsDBNull(8) || reader.GetString(8).Length == 0 ? null : reader.GetString(8),
            reader.IsDBNull(9) || reader.GetString(9).Length == 0 ? null : reader.GetString(9),
            reader.GetInt64(10),
            reader.GetInt64(11),
            reader.IsDBNull(12) ? null : reader.GetInt64(12)
        );

    static List<TuningArchiveEntry> ReadEntries(SqliteCommand command)
    {
        var entries = new List<TuningArchiveEntry>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            entries.Add(ReadEntry(reader));
        return entries;
    }

    /// <summary>
    /// Gets the resources of the specified <paramref name="kind"/> in the specified <paramref name="snapshot"/>
    /// </summary>
    public IReadOnlyList<TuningArchiveEntry> GetEntries(TuningArchiveSnapshot snapshot, TuningArchiveResourceKind kind = TuningArchiveResourceKind.Tuning)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        using var connection = OpenConnection();
        if (!HasScope(connection, null, snapshot.Id, GetScope(kind)))
            return [];
        using var command = connection.CreateCommand();
        command.CommandText = $"{entrySelect} WHERE r.kind = $kind AND {coversA}";
        command.Parameters.AddWithValue("$kind", (int)kind);
        command.Parameters.AddWithValue("$a", snapshot.Ordinal);
        return ReadEntries(command);
    }

    /// <summary>
    /// Gets the resource of the specified <paramref name="kind"/>, <paramref name="type"/> and <paramref name="instance"/> in the specified <paramref name="snapshot"/>, if it is there
    /// </summary>
    /// <remarks>
    /// When <paramref name="group"/> is <see langword="null"/>, the resource with the lowest group is returned.
    /// </remarks>
    public TuningArchiveEntry? GetEntry(TuningArchiveSnapshot snapshot, TuningArchiveResourceKind kind, ResourceType type, ulong instance, uint? group = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        using var connection = OpenConnection();
        if (!HasScope(connection, null, snapshot.Id, GetScope(kind)))
            return null;
        using var command = connection.CreateCommand();
        command.CommandText = $"{entrySelect} WHERE r.kind = $kind AND r.type = $type AND r.instance = $instance AND ($group IS NULL OR r.grp = $group) AND {coversA} ORDER BY r.grp LIMIT 1";
        command.Parameters.AddWithValue("$kind", (int)kind);
        command.Parameters.AddWithValue("$type", (long)(uint)type);
        command.Parameters.AddWithValue("$instance", unchecked((long)instance));
        command.Parameters.AddWithValue("$group", group is { } g ? (long)g : DBNull.Value);
        command.Parameters.AddWithValue("$a", snapshot.Ordinal);
        return ReadEntries(command).FirstOrDefault();
    }

    /// <summary>
    /// Gets the resources of the specified <paramref name="kind"/> with the specified <paramref name="instance"/> in the specified <paramref name="snapshot"/>, whatever their type
    /// </summary>
    public IReadOnlyList<TuningArchiveEntry> GetEntriesByInstance(TuningArchiveSnapshot snapshot, TuningArchiveResourceKind kind, ulong instance)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        using var connection = OpenConnection();
        if (!HasScope(connection, null, snapshot.Id, GetScope(kind)))
            return [];
        using var command = connection.CreateCommand();
        command.CommandText = $"{entrySelect} WHERE r.kind = $kind AND r.instance = $instance AND {coversA}";
        command.Parameters.AddWithValue("$kind", (int)kind);
        command.Parameters.AddWithValue("$instance", unchecked((long)instance));
        command.Parameters.AddWithValue("$a", snapshot.Ordinal);
        return ReadEntries(command);
    }

    /// <summary>
    /// Gets the content of the specified <paramref name="entry"/>, as canonical text (see <see cref="TuningXml"/>) or Python source
    /// </summary>
    public string GetText(TuningArchiveEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        using var connection = OpenConnection();
        return Encoding.UTF8.GetString(GetBlobContent(connection, entry.BlobId));
    }

    internal byte[] GetBlobContent(SqliteConnection connection, long blobId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT dictionary_id, data FROM blob WHERE id = $id";
        command.Parameters.AddWithValue("$id", blobId);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
            throw new InvalidDataException($"The archive has no content {blobId}");
        long? dictionaryId = reader.IsDBNull(0) ? null : reader.GetInt64(0);
        var data = (byte[])reader.GetValue(1);
        return GetCodec(connection, dictionaryId).Decompress(data);
    }

    /// <summary>
    /// Gets the resources of the specified <paramref name="kind"/> that differ between the <paramref name="older"/> and <paramref name="newer"/> snapshots, and, if <paramref name="includeUnchanged"/> is <see langword="true"/>, those that do not (as <see cref="TuningArchiveChangeKind.Unchanged"/>)
    /// </summary>
    public IReadOnlyList<TuningArchiveChange> Compare(TuningArchiveSnapshot older, TuningArchiveSnapshot newer, TuningArchiveResourceKind kind = TuningArchiveResourceKind.Tuning, bool includeUnchanged = false)
    {
        ArgumentNullException.ThrowIfNull(older);
        ArgumentNullException.ThrowIfNull(newer);
        using var connection = OpenConnection();
        var scope = GetScope(kind);
        if (!HasScope(connection, null, older.Id, scope) || !HasScope(connection, null, newer.Id, scope))
            return [];
        using var command = connection.CreateCommand();
        command.CommandText = $"{entrySelect} WHERE r.kind = $kind AND ({coversA} {(includeUnchanged ? "OR" : "<>")} {coversB})";
        command.Parameters.AddWithValue("$kind", (int)kind);
        command.Parameters.AddWithValue("$a", older.Ordinal);
        command.Parameters.AddWithValue("$b", newer.Ordinal);
        var olderEntries = new Dictionary<long, TuningArchiveEntry>();
        var newerEntries = new Dictionary<long, TuningArchiveEntry>();
        var changes = new List<TuningArchiveChange>();
        foreach (var entry in ReadEntries(command))
        {
            var coversOlder = entry.FirstOrdinal <= older.Ordinal && (entry.LastOrdinal is null || entry.LastOrdinal >= older.Ordinal);
            var coversNewer = entry.FirstOrdinal <= newer.Ordinal && (entry.LastOrdinal is null || entry.LastOrdinal >= newer.Ordinal);
            if (coversOlder && coversNewer)
                changes.Add(new(TuningArchiveChangeKind.Unchanged, entry, entry));
            else
                (coversOlder ? olderEntries : newerEntries)[entry.ResourceId] = entry;
        }
        foreach (var (resourceId, olderEntry) in olderEntries)
        {
            if (!newerEntries.Remove(resourceId, out var newerEntry))
                changes.Add(new(TuningArchiveChangeKind.Removed, olderEntry, null));
            else if (olderEntry.BlobId != newerEntry.BlobId)
                changes.Add(new(TuningArchiveChangeKind.Modified, olderEntry, newerEntry));
            else if (olderEntry.Pack != newerEntry.Pack || olderEntry.Name != newerEntry.Name)
                changes.Add(new(TuningArchiveChangeKind.Moved, olderEntry, newerEntry));
            else if (includeUnchanged)
                changes.Add(new(TuningArchiveChangeKind.Unchanged, olderEntry, newerEntry));
        }
        changes.AddRange(newerEntries.Values.Select(newerEntry => new TuningArchiveChange(TuningArchiveChangeKind.Added, null, newerEntry)));
        return changes;
    }

    /// <summary>
    /// Gets the history of the resource of the specified <paramref name="entry"/>: each version it has had and the snapshots containing it
    /// </summary>
    public IReadOnlyList<TuningArchiveHistoryItem> GetHistory(TuningArchiveEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        using var connection = OpenConnection();
        var scope = GetScope(entry.Kind);
        var snapshots = GetSnapshots(connection, null);
        var inScope = new HashSet<long>();
        using (var scopeCommand = connection.CreateCommand())
        {
            scopeCommand.CommandText = "SELECT snapshot_id FROM snapshot_scope WHERE scope = $scope";
            scopeCommand.Parameters.AddWithValue("$scope", scope);
            using var reader = scopeCommand.ExecuteReader();
            while (reader.Read())
                inScope.Add(reader.GetInt64(0));
        }
        var scoped = snapshots.Where(snapshot => inScope.Contains(snapshot.Id)).ToList();
        using var command = connection.CreateCommand();
        command.CommandText = $"{entrySelect} WHERE v.resource_id = $resource ORDER BY v.first_ord";
        command.Parameters.AddWithValue("$resource", entry.ResourceId);
        return [.. ReadEntries(command).Select(version => new TuningArchiveHistoryItem(version, [.. scoped.Where(snapshot => snapshot.Ordinal >= version.FirstOrdinal && (version.LastOrdinal is null || snapshot.Ordinal <= version.LastOrdinal))]))];
    }

    /// <summary>
    /// Gets the names of the tuning types the archive has seen, by resource type
    /// </summary>
    public IReadOnlyDictionary<ResourceType, string> GetTuningTypeNames()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT type, name FROM tuning_type";
        var names = new Dictionary<ResourceType, string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            names[(ResourceType)(uint)reader.GetInt64(0)] = reader.GetString(1);
        return names;
    }

    /// <summary>
    /// Gets the strings of the specified <paramref name="snapshot"/>, by key, in its <see cref="TuningArchiveSnapshot.Locale"/>
    /// </summary>
    public IReadOnlyDictionary<uint, string> GetStrings(TuningArchiveSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Locale is not { } locale)
            return ImmutableDictionary<uint, string>.Empty;
        using var connection = OpenConnection();
        if (!HasScope(connection, null, snapshot.Id, GetStringsScope(locale)))
            return ImmutableDictionary<uint, string>.Empty;
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT k.key, v.text FROM string_version v JOIN string_key k ON k.id = v.string_key_id WHERE k.locale = $locale AND {coversA}";
        command.Parameters.AddWithValue("$locale", locale);
        command.Parameters.AddWithValue("$a", snapshot.Ordinal);
        var strings = new Dictionary<uint, string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            strings[(uint)reader.GetInt64(0)] = reader.GetString(1);
        return strings;
    }

    /// <summary>
    /// Gets the strings that differ between the <paramref name="older"/> and <paramref name="newer"/> snapshots, and, if <paramref name="includeUnchanged"/> is <see langword="true"/>, those that do not (as <see cref="TuningArchiveChangeKind.Unchanged"/>), in the <paramref name="newer"/> snapshot's locale
    /// </summary>
    /// <remarks>
    /// A string whose text is the same in both snapshots is reported as <see cref="TuningArchiveChangeKind.Moved"/> only when both snapshots were captured from the game, because an extracted strings file does not say which pack each string comes from.
    /// </remarks>
    public IReadOnlyList<TuningArchiveStringChange> CompareStrings(TuningArchiveSnapshot older, TuningArchiveSnapshot newer, bool includeUnchanged = false)
    {
        ArgumentNullException.ThrowIfNull(older);
        ArgumentNullException.ThrowIfNull(newer);
        if (newer.Locale is not { } locale)
            return [];
        using var connection = OpenConnection();
        var scope = GetStringsScope(locale);
        if (!HasScope(connection, null, older.Id, scope) || !HasScope(connection, null, newer.Id, scope))
            return [];
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT v.string_key_id, k.key, v.text, v.pack, {coversA}, {coversB} FROM string_version v JOIN string_key k ON k.id = v.string_key_id WHERE k.locale = $locale AND ({coversA} {(includeUnchanged ? "OR" : "<>")} {coversB})";
        command.Parameters.AddWithValue("$locale", locale);
        command.Parameters.AddWithValue("$a", older.Ordinal);
        command.Parameters.AddWithValue("$b", newer.Ordinal);
        var olderStrings = new Dictionary<long, (uint key, string text, string pack)>();
        var newerStrings = new Dictionary<long, (uint key, string text, string pack)>();
        var changes = new List<TuningArchiveStringChange>();
        using (var reader = command.ExecuteReader())
            while (reader.Read())
            {
                var coversOlder = reader.GetBoolean(4);
                if (coversOlder && reader.GetBoolean(5))
                {
                    var text = reader.GetString(2);
                    changes.Add(new(TuningArchiveChangeKind.Unchanged, (uint)reader.GetInt64(1), text, text));
                }
                else
                    (coversOlder ? olderStrings : newerStrings)[reader.GetInt64(0)] = ((uint)reader.GetInt64(1), reader.GetString(2), reader.GetString(3));
            }
        var packsAreComparable = older.Source is TuningArchiveSnapshotSource.GameInstallation && newer.Source is TuningArchiveSnapshotSource.GameInstallation;
        foreach (var (id, (key, text, pack)) in olderStrings)
        {
            if (!newerStrings.Remove(id, out var newerString))
                changes.Add(new(TuningArchiveChangeKind.Removed, key, text, null));
            else if (newerString.text != text)
                changes.Add(new(TuningArchiveChangeKind.Modified, key, text, newerString.text));
            else if (newerString.pack != pack && packsAreComparable)
                changes.Add(new(TuningArchiveChangeKind.Moved, key, text, newerString.text));
            else if (includeUnchanged)
                changes.Add(new(TuningArchiveChangeKind.Unchanged, key, text, newerString.text));
        }
        changes.AddRange(newerStrings.Values.Select(newerString => new TuningArchiveStringChange(TuningArchiveChangeKind.Added, newerString.key, null, newerString.text)));
        return changes;
    }
}