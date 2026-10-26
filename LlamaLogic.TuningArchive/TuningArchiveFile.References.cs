namespace LlamaLogic.TuningArchive;

partial class TuningArchiveFile
{
    /// <summary>
    /// Indexes the references in every tuning that has not yet been indexed with the tuning descriptions best suited to it, returning how many tunings were indexed (💤)
    /// </summary>
    /// <remarks>
    /// The tuning descriptions best suited to a tuning are those best suited to the earliest snapshot containing it (see <see cref="FindTuningDescriptions(TuningArchiveSnapshot)"/>).
    /// Indexing is needed after capturing or importing a snapshot, after importing tuning descriptions, and after opening the archive with a version of this library that indexes differently (which discards the old index); tunings already indexed with the right descriptions are skipped.
    /// </remarks>
    public async Task<int> IndexReferencesAsync(IProgress<TuningArchiveProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(() =>
            {
                using var connection = OpenConnection();
                progress?.Report(new("Finding tunings to index", 0, 0));
                var allDescriptions = GetTuningDescriptions(connection);
                if (allDescriptions.Count == 0)
                    return 0;
                var snapshots = GetSnapshots(connection, null);
                var work = new List<(long blobId, long descriptionsId, string className)>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        """
                        SELECT b.id, b.references_indexed_by, MIN(v.first_ord), MIN(c.class)
                        FROM blob b
                        JOIN resource_version v ON v.blob_id = b.id
                        JOIN resource r ON r.id = v.resource_id
                        LEFT JOIN tuning_class c ON c.id = v.class_id
                        WHERE r.kind = 0
                        GROUP BY b.id
                        """;
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var firstOrdinal = reader.GetInt64(2);
                        var firstSnapshot = snapshots.FirstOrDefault(snapshot => snapshot.Ordinal >= firstOrdinal);
                        if (firstSnapshot is null || FindTuningDescriptions(allDescriptions, firstSnapshot.GameVersion) is not { } descriptions)
                            continue;
                        if (reader.IsDBNull(1) || reader.GetInt64(1) != descriptions.Id)
                            work.Add((reader.GetInt64(0), descriptions.Id, reader.IsDBNull(3) ? string.Empty : reader.GetString(3)));
                    }
                }
                if (work.Count == 0)
                    return 0;
                progress?.Report(new("Learning tuning names", 0, 0));
                var namedInstances = new Dictionary<(uint type, string name), ulong>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT DISTINCT r.type, v.name, r.instance FROM resource_version v JOIN resource r ON r.id = v.resource_id WHERE r.kind = 0 AND v.name IS NOT NULL";
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                        namedInstances.TryAdd(((uint)reader.GetInt64(0), reader.GetString(1)), unchecked((ulong)reader.GetInt64(2)));
                }
                const int batchSize = 2000;
                const int progressInterval = 100;
                var completed = 0;
                TuningDescriptionCache? cache = null;
                long cacheDescriptionsId = 0;
                var cacheClassName = string.Empty;
                var batches = work
                    .OrderBy(item => item.descriptionsId)
                    .ThenBy(item => item.className, StringComparer.Ordinal)
                    .GroupBy(item => (item.descriptionsId, item.className))
                    .SelectMany(group => group.Chunk(batchSize));
                foreach (var batch in batches)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var batchDescriptionsId = batch[0].descriptionsId;
                    if (cache is null || cacheDescriptionsId != batchDescriptionsId)
                    {
                        progress?.Report(new($"Opening the {allDescriptions.First(descriptions => descriptions.Id == batchDescriptionsId).GameVersion} tuning descriptions", completed, work.Count));
                        cache?.Clear();
                        cache = new TuningDescriptionCache(OpenTuningDescriptions(allDescriptions.First(descriptions => descriptions.Id == batchDescriptionsId)));
                        cacheDescriptionsId = batchDescriptionsId;
                    }
                    else if (cacheClassName != batch[0].className)
                        cache.ClearInstances();
                    cacheClassName = batch[0].className;
                    var batchCache = cache;
                    progress?.Report(new("Following references", completed, work.Count));
                    var contents = batch.Select(item => (item.blobId, item.descriptionsId, content: GetBlobContent(connection, item.blobId))).ToList();
                    var results = contents.AsParallel().AsOrdered().WithCancellation(cancellationToken).Select(item =>
                    {
                        var analyzed = Interlocked.Increment(ref completed);
                        if (analyzed % progressInterval == 0)
                            progress?.Report(new("Following references", analyzed, work.Count));
                        try
                        {
                            var analysis = TuningAnalysis.Analyze(Encoding.UTF8.GetString(item.content), batchCache);
                            var strings = analysis.Nodes
                                .Where(node => node.ValueKind is TuningValueKind.LocalizedString && node.StringKey is not null and not 0)
                                .Select(node => (type: (uint)ResourceType.StringTable, instance: (ulong)node.StringKey!.Value));
                            var animationStateMachines = analysis.Nodes
                                .Where(node => node.ValueKind is TuningValueKind.ResourceKey && node.ResourceKey is { Type: ResourceType.AnimationStateMachine, FullInstance: not 0 })
                                .Select(node => (type: (uint)ResourceType.AnimationStateMachine, instance: node.ResourceKey!.Value.FullInstance));
                            var media = analysis.Nodes
                                .Where(node => node.ValueKind is TuningValueKind.ResourceKey && node.ResourceKey is { FullInstance: not 0 } key && GetMediaKind(key.Type) is not null)
                                .Select(node => (type: (uint)node.ResourceKey!.Value.Type, instance: node.ResourceKey!.Value.FullInstance));
                            return (item.blobId, item.descriptionsId, references: analysis.References.Select(node =>
                            {
                                var instance = node.ReferenceInstance ?? (node.ReferenceName is { } name && namedInstances.TryGetValue(((uint)node.ReferenceType!.Value, name), out var named) ? named : (ulong?)null);
                                return instance is { } resolved ? (type: (uint)node.ReferenceType!.Value, instance: resolved) : ((uint type, ulong instance)?)null;
                            }).OfType<(uint type, ulong instance)>().Where(reference => reference.instance != 0).Concat(strings).Concat(animationStateMachines).Concat(media).Distinct().ToList());
                        }
                        catch (XmlException)
                        {
                            return (item.blobId, item.descriptionsId, references: new List<(uint type, ulong instance)>());
                        }
                    }).ToList();
                    using var transaction = connection.BeginTransaction();
                    using var delete = connection.CreateCommand();
                    delete.Transaction = transaction;
                    delete.CommandText = "DELETE FROM reference WHERE blob_id = $blob";
                    var deleteBlob = delete.Parameters.Add("$blob", SqliteType.Integer);
                    using var insert = connection.CreateCommand();
                    insert.Transaction = transaction;
                    insert.CommandText = "INSERT INTO reference (target_type, target_instance, blob_id) VALUES ($type, $instance, $blob) ON CONFLICT DO NOTHING";
                    var insertBlob = insert.Parameters.Add("$blob", SqliteType.Integer);
                    var insertType = insert.Parameters.Add("$type", SqliteType.Integer);
                    var insertInstance = insert.Parameters.Add("$instance", SqliteType.Integer);
                    using var mark = connection.CreateCommand();
                    mark.Transaction = transaction;
                    mark.CommandText = "UPDATE blob SET references_indexed_by = $descriptions WHERE id = $blob";
                    var markBlob = mark.Parameters.Add("$blob", SqliteType.Integer);
                    var markDescriptions = mark.Parameters.Add("$descriptions", SqliteType.Integer);
                    foreach (var (blobId, descriptionsId, references) in results)
                    {
                        deleteBlob.Value = blobId;
                        delete.ExecuteNonQuery();
                        insertBlob.Value = blobId;
                        foreach (var (type, instance) in references)
                        {
                            insertType.Value = (long)type;
                            insertInstance.Value = unchecked((long)instance);
                            insert.ExecuteNonQuery();
                        }
                        markBlob.Value = blobId;
                        markDescriptions.Value = descriptionsId;
                        mark.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
                cache?.Clear();
                progress?.Report(new("Following references", completed, work.Count));
                return completed;
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    /// <summary>
    /// Gets how many tunings have had their references indexed, and how many tunings there are
    /// </summary>
    public (int indexed, int total) GetReferenceIndexStatus()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(b.references_indexed_by), COUNT(*) FROM blob b WHERE b.id IN (SELECT v.blob_id FROM resource_version v JOIN resource r ON r.id = v.resource_id WHERE r.kind = 0)";
        using var reader = command.ExecuteReader();
        reader.Read();
        return (reader.GetInt32(0), reader.GetInt32(1));
    }

    /// <summary>
    /// Gets the tunings in the specified <paramref name="snapshot"/> that refer to the resource with the specified <paramref name="type"/> and <paramref name="instance"/>
    /// </summary>
    /// <remarks>
    /// Tunings refer to other tunings and to non-tuning resources (such as object definitions) by tuning reference, and to animation state machines and media by resource key (see <see cref="GetMediaReferrers(TuningArchiveSnapshot, TuningArchiveEntry)"/> to find a medium's referrers by any of the types tuning uses for it).
    /// Only tunings whose references have been indexed (see <see cref="IndexReferencesAsync"/>) are found.
    /// </remarks>
    public IReadOnlyList<TuningArchiveReference> GetReferrers(TuningArchiveSnapshot snapshot, ResourceType type, ulong instance)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        using var connection = OpenConnection();
        if (!HasScope(connection, null, snapshot.Id, TuningScope))
            return [];
        using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT r.id, v.id, r.kind, r.type, r.grp, r.instance, v.name, v.pack, c.class, c.module, v.blob_id, v.first_ord, v.last_ord
            FROM reference ref
            JOIN resource_version v ON v.blob_id = ref.blob_id
            JOIN resource r ON r.id = v.resource_id
            LEFT JOIN tuning_class c ON c.id = v.class_id
            WHERE ref.target_type = $type AND ref.target_instance = $instance AND r.kind = 0 AND {coversA}
            ORDER BY v.name
            """;
        command.Parameters.AddWithValue("$type", (long)(uint)type);
        command.Parameters.AddWithValue("$instance", unchecked((long)instance));
        command.Parameters.AddWithValue("$a", snapshot.Ordinal);
        var references = new List<TuningArchiveReference>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            references.Add(new(ReadEntry(reader), type, instance));
        return references;
    }

    /// <summary>
    /// Gets the tunings in the specified <paramref name="snapshot"/> that use any of the strings with the specified <paramref name="keys"/>
    /// </summary>
    /// <remarks>
    /// Each reference's <see cref="TuningArchiveReference.TargetType"/> is <see cref="ResourceType.StringTable"/> and its <see cref="TuningArchiveReference.TargetInstance"/> is the key of the string used; a tuning using several of the strings appears once for each.
    /// Only tunings whose references have been indexed (see <see cref="IndexReferencesAsync"/>) are found.
    /// </remarks>
    public IReadOnlyList<TuningArchiveReference> GetStringReferrers(TuningArchiveSnapshot snapshot, IEnumerable<uint> keys)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(keys);
        using var connection = OpenConnection();
        if (!HasScope(connection, null, snapshot.Id, TuningScope))
            return [];
        using (var prepare = connection.CreateCommand())
        {
            prepare.CommandText =
                """
                DROP TABLE IF EXISTS temp.string_key;
                CREATE TEMP TABLE string_key (key INTEGER PRIMARY KEY);
                """;
            prepare.ExecuteNonQuery();
        }
        using (var transaction = connection.BeginTransaction())
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO temp.string_key (key) VALUES ($key) ON CONFLICT DO NOTHING";
            var key = insert.Parameters.Add("$key", SqliteType.Integer);
            foreach (var value in keys)
            {
                key.Value = (long)value;
                insert.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT r.id, v.id, r.kind, r.type, r.grp, r.instance, v.name, v.pack, c.class, c.module, v.blob_id, v.first_ord, v.last_ord, ref.target_instance
            FROM temp.string_key k
            JOIN reference ref ON ref.target_type = $type AND ref.target_instance = k.key
            JOIN resource_version v ON v.blob_id = ref.blob_id
            JOIN resource r ON r.id = v.resource_id
            LEFT JOIN tuning_class c ON c.id = v.class_id
            WHERE r.kind = 0 AND {coversA}
            ORDER BY v.name
            """;
        command.Parameters.AddWithValue("$type", (long)(uint)ResourceType.StringTable);
        command.Parameters.AddWithValue("$a", snapshot.Ordinal);
        var references = new List<TuningArchiveReference>();
        using (var reader = command.ExecuteReader())
            while (reader.Read())
                references.Add(new(ReadEntry(reader), ResourceType.StringTable, unchecked((ulong)reader.GetInt64(13))));
        using var cleanup = connection.CreateCommand();
        cleanup.CommandText = "DROP TABLE IF EXISTS temp.string_key";
        cleanup.ExecuteNonQuery();
        return references;
    }

    /// <summary>
    /// Gets the references in the specified <paramref name="snapshot"/> to tunings that are not in it
    /// </summary>
    /// <remarks>
    /// Only references to tuning types the archive has seen are considered, and only tunings whose references have been indexed (see <see cref="IndexReferencesAsync"/>).
    /// </remarks>
    public IReadOnlyList<TuningArchiveReference> GetBrokenReferences(TuningArchiveSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        using var connection = OpenConnection();
        if (!HasScope(connection, null, snapshot.Id, TuningScope))
            return [];
        using (var prepare = connection.CreateCommand())
        {
            prepare.CommandText =
                $"""
                DROP TABLE IF EXISTS temp.live_tuning;
                CREATE TEMP TABLE live_tuning (type INTEGER NOT NULL, instance INTEGER NOT NULL, PRIMARY KEY (type, instance)) WITHOUT ROWID;
                INSERT INTO live_tuning (type, instance) SELECT r.type, r.instance FROM resource_version v JOIN resource r ON r.id = v.resource_id WHERE r.kind = 0 AND {coversA} ON CONFLICT DO NOTHING;
                """;
            prepare.Parameters.AddWithValue("$a", snapshot.Ordinal);
            prepare.ExecuteNonQuery();
        }
        using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT r.id, v.id, r.kind, r.type, r.grp, r.instance, v.name, v.pack, c.class, c.module, v.blob_id, v.first_ord, v.last_ord, ref.target_type, ref.target_instance
            FROM resource_version v
            JOIN resource r ON r.id = v.resource_id
            LEFT JOIN tuning_class c ON c.id = v.class_id
            JOIN reference ref ON ref.blob_id = v.blob_id
            WHERE r.kind = 0 AND {coversA}
            AND ref.target_type IN (SELECT type FROM tuning_type)
            AND NOT EXISTS (SELECT 1 FROM live_tuning l WHERE l.type = ref.target_type AND l.instance = ref.target_instance)
            ORDER BY v.name
            """;
        command.Parameters.AddWithValue("$a", snapshot.Ordinal);
        var references = new List<TuningArchiveReference>();
        using (var reader = command.ExecuteReader())
            while (reader.Read())
                references.Add(new(ReadEntry(reader), (ResourceType)(uint)reader.GetInt64(13), unchecked((ulong)reader.GetInt64(14))));
        using var cleanup = connection.CreateCommand();
        cleanup.CommandText = "DROP TABLE IF EXISTS temp.live_tuning";
        cleanup.ExecuteNonQuery();
        return references;
    }
}