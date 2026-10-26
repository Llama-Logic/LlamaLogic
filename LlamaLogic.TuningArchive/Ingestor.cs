namespace LlamaLogic.TuningArchive;

sealed class Ingestor
{
    public Ingestor(TuningArchiveFile archive, SqliteConnection connection)
    {
        this.archive = archive;
        this.connection = connection;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT id, kind, type, grp, instance FROM resource";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                resources[((TuningArchiveResourceKind)reader.GetInt32(1), (uint)reader.GetInt64(2), (uint)reader.GetInt64(3), unchecked((ulong)reader.GetInt64(4)))] = reader.GetInt64(0);
        }
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT id, hash FROM blob";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                blobs[BlobHash.FromBytes((byte[])reader.GetValue(1))] = reader.GetInt64(0);
        }
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT id, class, module FROM tuning_class";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                classes[(reader.GetString(1), reader.GetString(2))] = reader.GetInt64(0);
        }
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT type, name FROM tuning_type";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                tuningTypes[(uint)reader.GetInt64(0)] = reader.GetString(1);
        }
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT id, locale, key FROM string_key";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                stringKeys[(reader.GetString(1), (uint)reader.GetInt64(2))] = reader.GetInt64(0);
        }
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT kind, MAX(id) FROM dictionary GROUP BY kind";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                dictionaries[(TuningArchiveResourceKind)reader.GetInt32(0)] = reader.GetInt64(1);
        }
    }

    readonly TuningArchiveFile archive;
    readonly Dictionary<BlobHash, long> blobs = [];
    readonly Dictionary<(string @class, string module), long> classes = [];
    readonly SqliteConnection connection;
    readonly Dictionary<TuningArchiveResourceKind, long> dictionaries = [];
    readonly Dictionary<long, BlobHash> packNeutralHashes = [];
    readonly Dictionary<(TuningArchiveResourceKind kind, uint type, uint group, ulong instance), long> resources = [];
    readonly Dictionary<(string locale, uint key), long> stringKeys = [];
    readonly Dictionary<uint, string> tuningTypes = [];

    public int NewContent { get; private set; }

    static string GetKindName(TuningArchiveResourceKind kind) =>
        kind switch
        {
            TuningArchiveResourceKind.Tuning => "tuning",
            TuningArchiveResourceKind.SimData => "SimData",
            TuningArchiveResourceKind.AnimationStateMachine => "animation state machine",
            TuningArchiveResourceKind.Image => "image",
            TuningArchiveResourceKind.Sound => "sound",
            TuningArchiveResourceKind.Video => "video",
            _ => "Python module"
        };

    static BlobHash GetPackNeutralHash(byte[] content)
    {
        const string packMarker = "<SimData u=\"0x";
        var text = Encoding.UTF8.GetString(content);
        var packIndex = text.IndexOf(packMarker, StringComparison.Ordinal);
        if (packIndex >= 0 && packIndex + packMarker.Length + 8 <= text.Length)
            text = string.Concat(text.AsSpan(0, packIndex + packMarker.Length), text.AsSpan(packIndex + packMarker.Length + 8));
        return BlobHash.Compute(Encoding.UTF8.GetBytes(text));
    }

    public long NewContentBytes { get; private set; }

    public void Ingest(TuningArchiveResourceKind kind, IReadOnlyList<IngestDocument> documents, Dictionary<long, ResourceContent> manifest, ICollection<string> problems, IProgress<TuningArchiveProgress>? progress = null, string? stage = null)
    {
        const int progressInterval = 100;
        if (stage is not null)
            progress?.Report(new(stage, 0, 0));
        var isMedia = kind is TuningArchiveResourceKind.Image or TuningArchiveResourceKind.Sound or TuningArchiveResourceKind.Video;
        if (!isMedia && !dictionaries.ContainsKey(kind) && BlobCodec.TrainDictionary([.. documents.Select(document => document.Content)]) is { } trained)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO dictionary (kind, data) VALUES ($kind, $data) RETURNING id";
            command.Parameters.AddWithValue("$kind", (int)kind);
            command.Parameters.AddWithValue("$data", trained);
            dictionaries[kind] = (long)command.ExecuteScalar()!;
        }
        long? dictionaryId = !isMedia && dictionaries.TryGetValue(kind, out var id) ? id : null;
        using var mediaCodec = isMedia ? BlobCodec.CreateForMedia() : null;
        var codec = mediaCodec ?? archive.GetCodec(connection, dictionaryId);
        var seen = new HashSet<BlobHash>();
        var pending = documents.Where(document => !blobs.ContainsKey(document.Hash) && seen.Add(document.Hash)).ToList();
        var compressedCount = 0;
        var compressed = pending.AsParallel().AsOrdered().Select(document =>
        {
            var compressedContent = codec.Compress(document.Content);
            var done = Interlocked.Increment(ref compressedCount);
            if (stage is not null && done % progressInterval == 0)
                progress?.Report(new(stage, done, pending.Count));
            return compressedContent;
        }).ToList();
        if (stage is not null)
            progress?.Report(new(stage, pending.Count, pending.Count));
        using var transaction = connection.BeginTransaction();
        using (var insertBlob = connection.CreateCommand())
        {
            insertBlob.Transaction = transaction;
            insertBlob.CommandText = "INSERT INTO blob (hash, dictionary_id, size, data) VALUES ($hash, $dictionary, $size, $data) RETURNING id";
            var hashParameter = insertBlob.Parameters.Add("$hash", SqliteType.Blob);
            var dictionaryParameter = insertBlob.Parameters.Add("$dictionary", SqliteType.Integer);
            var sizeParameter = insertBlob.Parameters.Add("$size", SqliteType.Integer);
            var dataParameter = insertBlob.Parameters.Add("$data", SqliteType.Blob);
            dictionaryParameter.Value = dictionaryId is { } existingDictionaryId ? existingDictionaryId : DBNull.Value;
            for (var i = 0; i < pending.Count; ++i)
            {
                hashParameter.Value = pending[i].Hash.ToBytes();
                sizeParameter.Value = pending[i].Content.Length;
                dataParameter.Value = compressed[i];
                blobs[pending[i].Hash] = (long)insertBlob.ExecuteScalar()!;
                NewContentBytes += compressed[i].Length;
            }
            NewContent += pending.Count;
        }
        using var insertResource = connection.CreateCommand();
        insertResource.Transaction = transaction;
        insertResource.CommandText = "INSERT INTO resource (kind, type, grp, instance) VALUES ($kind, $type, $group, $instance) RETURNING id";
        var kindParameter = insertResource.Parameters.Add("$kind", SqliteType.Integer);
        var typeParameter = insertResource.Parameters.Add("$type", SqliteType.Integer);
        var groupParameter = insertResource.Parameters.Add("$group", SqliteType.Integer);
        var instanceParameter = insertResource.Parameters.Add("$instance", SqliteType.Integer);
        using var insertClass = connection.CreateCommand();
        insertClass.Transaction = transaction;
        insertClass.CommandText = "INSERT INTO tuning_class (class, module) VALUES ($class, $module) RETURNING id";
        var classParameter = insertClass.Parameters.Add("$class", SqliteType.Text);
        var moduleParameter = insertClass.Parameters.Add("$module", SqliteType.Text);
        using var insertType = connection.CreateCommand();
        insertType.Transaction = transaction;
        insertType.CommandText = "INSERT INTO tuning_type (type, name) VALUES ($type, $name) ON CONFLICT (type) DO UPDATE SET name = excluded.name";
        var tuningTypeParameter = insertType.Parameters.Add("$type", SqliteType.Integer);
        var tuningTypeNameParameter = insertType.Parameters.Add("$name", SqliteType.Text);
        foreach (var document in documents)
        {
            var key = (kind, (uint)document.Type, document.Group, document.Instance);
            if (!resources.TryGetValue(key, out var resourceId))
            {
                kindParameter.Value = (int)kind;
                typeParameter.Value = (long)(uint)document.Type;
                groupParameter.Value = (long)document.Group;
                instanceParameter.Value = unchecked((long)document.Instance);
                resourceId = (long)insertResource.ExecuteScalar()!;
                resources[key] = resourceId;
            }
            long? classId = null;
            if (document.Class is not null || document.Module is not null)
            {
                var classKey = (document.Class ?? string.Empty, document.Module ?? string.Empty);
                if (!classes.TryGetValue(classKey, out var existingClassId))
                {
                    classParameter.Value = classKey.Item1;
                    moduleParameter.Value = classKey.Item2;
                    existingClassId = (long)insertClass.ExecuteScalar()!;
                    classes[classKey] = existingClassId;
                }
                classId = existingClassId;
            }
            if (document.TypeName is { } typeName && (!tuningTypes.TryGetValue((uint)document.Type, out var knownName) || knownName != typeName))
            {
                tuningTypeParameter.Value = (long)(uint)document.Type;
                tuningTypeNameParameter.Value = typeName;
                insertType.ExecuteNonQuery();
                tuningTypes[(uint)document.Type] = typeName;
            }
            var content = new ResourceContent(blobs[document.Hash], document.Pack, document.Name, classId);
            var packNeutralHash = kind is TuningArchiveResourceKind.SimData ? GetPackNeutralHash(document.Content) : document.Hash;
            var differsOnlyInPack = packNeutralHashes.TryGetValue(resourceId, out var existingPackNeutralHash) && existingPackNeutralHash.Equals(packNeutralHash);
            packNeutralHashes[resourceId] = packNeutralHash;
            if (manifest.TryGetValue(resourceId, out var existing))
            {
                if (existing.BlobId != content.BlobId && !differsOnlyInPack)
                    problems.Add($"{document.Pack} and {existing.Pack} both contain {GetKindName(kind)} {new ResourceKey(document.Type, document.Group, document.Instance)} ({document.Name}) with different content; the archive keeps {document.Pack}'s");
                manifest[resourceId] = content;
            }
            else
                manifest.Add(resourceId, content);
        }
        transaction.Commit();
    }

    public void IngestStrings(string locale, string pack, IEnumerable<KeyValuePair<uint, string>> entries, Dictionary<long, StringContent> manifest)
    {
        using var transaction = connection.BeginTransaction();
        using var insertKey = connection.CreateCommand();
        insertKey.Transaction = transaction;
        insertKey.CommandText = "INSERT INTO string_key (locale, key) VALUES ($locale, $key) RETURNING id";
        var localeParameter = insertKey.Parameters.Add("$locale", SqliteType.Text);
        var keyParameter = insertKey.Parameters.Add("$key", SqliteType.Integer);
        foreach (var (key, text) in entries)
        {
            if (!stringKeys.TryGetValue((locale, key), out var stringKeyId))
            {
                localeParameter.Value = locale;
                keyParameter.Value = (long)key;
                stringKeyId = (long)insertKey.ExecuteScalar()!;
                stringKeys[(locale, key)] = stringKeyId;
            }
            manifest[stringKeyId] = new(text, pack);
        }
        transaction.Commit();
    }
}