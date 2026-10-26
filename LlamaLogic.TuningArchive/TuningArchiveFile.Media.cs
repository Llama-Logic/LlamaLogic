namespace LlamaLogic.TuningArchive;

partial class TuningArchiveFile
{
    const int mediaBatchBytes = 256 * 1024 * 1024;
    const string mediaReferenceTypes = "796721156, 796721158, 11720834, 968010314, 4244956094, 27600859, 32437818, 929579223";

    static readonly ResourceType[] imageTypes = [ResourceType.DSTImage, ResourceType.PNGImage, ResourceType.PNGImage2];
    static readonly ResourceType[] soundTypes = [ResourceType.AudioConfiguration, ResourceType.AudioVocals, ResourceType.AudioEffects];
    static readonly ResourceType[] videoTypes = [ResourceType.AVI];

#if IS_NET_7_0_OR_GREATER
    [GeneratedRegex(@"(?<![0-9A-Fa-f])(?<type>[0-9A-Fa-f]{8})(?<separator>[:-])(?<group>[0-9A-Fa-f]{8})\k<separator>(?<instance>[0-9A-Fa-f]{16})(?![0-9A-Fa-f])", RegexOptions.CultureInvariant)]
    private static partial Regex GetMediaKeyPattern();
#else
    static readonly Regex mediaKeyPattern = new(@"(?<![0-9A-Fa-f])(?<type>[0-9A-Fa-f]{8})(?<separator>[:-])(?<group>[0-9A-Fa-f]{8})\k<separator>(?<instance>[0-9A-Fa-f]{16})(?![0-9A-Fa-f])", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    static Regex GetMediaKeyPattern() =>
        mediaKeyPattern;
#endif

#if IS_NET_7_0_OR_GREATER
    [GeneratedRegex(@"<T\b[^>]*?\bp=""(?<name>[^""]*)""[^>]*>\s*(?<type>[0-9A-Fa-f]{8}):[0-9A-Fa-f]{8}:(?<instance>[0-9A-Fa-f]{16})\s*</T>", RegexOptions.CultureInvariant)]
    private static partial Regex GetNamedMediaKeyPattern();
#else
    static readonly Regex namedMediaKeyPattern = new(@"<T\b[^>]*?\bp=""(?<name>[^""]*)""[^>]*>\s*(?<type>[0-9A-Fa-f]{8}):[0-9A-Fa-f]{8}:(?<instance>[0-9A-Fa-f]{16})\s*</T>", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    static Regex GetNamedMediaKeyPattern() =>
        namedMediaKeyPattern;
#endif

    /// <summary>
    /// Gets the kind of media to which tuning or SimData refers with a resource key of the specified <paramref name="type"/>, or <see langword="null"/> if the type is not one of media
    /// </summary>
    /// <remarks>
    /// Tuning refers to images with <see cref="ResourceType.PNGImage"/> and <see cref="ResourceType.PNGImage2"/> keys, which the game resolves to the <see cref="ResourceType.DSTImage"/> with the same instance, and to sounds with <see cref="ResourceType.AudioConfigurationReference"/> keys, which it resolves to the <see cref="ResourceType.AudioConfiguration"/> with the same instance.
    /// </remarks>
    public static TuningArchiveResourceKind? GetMediaKind(ResourceType type) =>
        type switch
        {
            ResourceType.PNGImage or ResourceType.PNGImage2 or ResourceType.DSTImage => TuningArchiveResourceKind.Image,
            ResourceType.AudioConfigurationReference or ResourceType.AudioConfiguration or ResourceType.AudioVocals or ResourceType.AudioEffects => TuningArchiveResourceKind.Sound,
            ResourceType.AVI => TuningArchiveResourceKind.Video,
            _ => null
        };

    /// <summary>
    /// Gets the resource types to which the game resolves a reference to media with the specified <paramref name="type"/>, in the order it tries them (see <see cref="GetMediaKind(ResourceType)"/>), or only the type itself if it is not a type tuning uses for media
    /// </summary>
    public static IReadOnlyList<ResourceType> GetMediaResourceTypes(ResourceType type) =>
        type switch
        {
            ResourceType.PNGImage or ResourceType.PNGImage2 => [ResourceType.DSTImage, type],
            ResourceType.AudioConfigurationReference => [ResourceType.AudioConfiguration, ResourceType.AudioVocals, ResourceType.AudioEffects],
            _ => [type]
        };

    static IEnumerable<ResourceType> GetMediaReferenceTypes(ResourceType type) =>
        type switch
        {
            ResourceType.DSTImage => [ResourceType.DSTImage, ResourceType.PNGImage, ResourceType.PNGImage2],
            ResourceType.AudioConfiguration or ResourceType.AudioVocals or ResourceType.AudioEffects => [type, ResourceType.AudioConfigurationReference],
            _ => [type]
        };

    internal static void CollectMediaReferences(IEnumerable<IngestDocument> documents, Dictionary<(ResourceType type, ulong instance), (uint group, string? name)> references)
    {
        var found = documents.AsParallel().SelectMany(document =>
        {
            var text = Encoding.UTF8.GetString(document.Content);
            var keys = new List<(ResourceType type, uint group, ulong instance, string? name)>();
            foreach (Match match in GetMediaKeyPattern().Matches(text))
            {
                var type = (ResourceType)uint.Parse(match.Groups["type"].ValueSpan, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var instance = ulong.Parse(match.Groups["instance"].ValueSpan, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (GetMediaKind(type) is not null && instance is not 0)
                    keys.Add((type, uint.Parse(match.Groups["group"].ValueSpan, NumberStyles.HexNumber, CultureInfo.InvariantCulture), instance, null));
            }
            foreach (Match match in GetNamedMediaKeyPattern().Matches(text))
            {
                var type = (ResourceType)uint.Parse(match.Groups["type"].ValueSpan, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var instance = ulong.Parse(match.Groups["instance"].ValueSpan, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (GetMediaKind(type) is not null && instance is not 0 && match.Groups["name"].Value is { Length: > 0 } name)
                    keys.Add((type, 0, instance, name));
            }
            return keys;
        }).ToList();
        foreach (var (type, group, instance, name) in found)
        {
            if (references.TryGetValue((type, instance), out var existing))
            {
                if (existing.name is null && name is not null)
                    references[(type, instance)] = (existing.group, name);
            }
            else
                references.Add((type, instance), (group, name));
        }
    }

    static (ulong parent, IReadOnlyList<ulong> samples) ReadAudioConfiguration(ReadOnlySpan<byte> content)
    {
        if (content.Length < 14)
            return (0, []);
        var parent = BinaryPrimitives.ReadUInt64LittleEndian(content[2..]);
        var count = BinaryPrimitives.ReadUInt32LittleEndian(content[10..]);
        var samples = new List<ulong>();
        for (var i = 0L; i < count && 14 + (i + 1) * 8 <= content.Length; ++i)
            samples.Add(BinaryPrimitives.ReadUInt64LittleEndian(content[(int)(14 + i * 8)..]));
        return (parent, samples);
    }

    static void CaptureMedia(IReadOnlyList<GameInstallationPack> packs, TuningArchiveCaptureOptions options, Dictionary<(ResourceType type, ulong instance), (uint group, string? name)> references, Ingestor ingestor, IReadOnlyDictionary<TuningArchiveResourceKind, Dictionary<long, ResourceContent>> manifests, List<string> problems, IProgress<TuningArchiveProgress>? progress, CancellationToken cancellationToken)
    {
        var wanted = new HashSet<ResourceType>();
        if (options.IncludeImages)
            wanted.UnionWith(imageTypes);
        if (options.IncludeSounds)
            wanted.UnionWith(soundTypes);
        if (options.IncludeVideos)
            wanted.UnionWith(videoTypes);
        if (wanted.Count is 0)
            return;
        var claims = new Dictionary<(ResourceType type, ulong instance), List<(ResourceKey key, string path, string pack)>>();
        for (var packIndex = 0; packIndex < packs.Count; ++packIndex)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pack = packs[packIndex];
            progress?.Report(new($"Finding media in {pack.Code}", packIndex, packs.Count));
            foreach (var (key, package) in ClaimResources(pack.ClientPackages, key => wanted.Contains(key.Type), problems, pack.Code))
            {
                if (!claims.TryGetValue((key.Type, key.FullInstance), out var list))
                {
                    list = [];
                    claims.Add((key.Type, key.FullInstance), list);
                }
                list.RemoveAll(claim => claim.key == key);
                list.Insert(0, (key, package.Path, pack.Code));
            }
        }
        (ResourceKey key, string path, string pack)? Claim(ResourceType type, ulong instance, uint group) =>
            claims.TryGetValue((type, instance), out var list)
                ? list.FirstOrDefault(claim => claim.key.Group == group) is { path: not null } exact ? exact : list[0]
                : null;
        var targets = new Dictionary<ResourceKey, (TuningArchiveResourceKind kind, string path, string pack, string? name)>();
        void Target(TuningArchiveResourceKind kind, (ResourceKey key, string path, string pack) claim, string? name)
        {
            if (!targets.TryGetValue(claim.key, out var existing))
                targets.Add(claim.key, (kind, claim.path, claim.pack, name));
            else if (existing.name is null && name is not null)
                targets[claim.key] = existing with { name = name };
        }
        var audioConfigurations = new Queue<(ulong instance, uint group, string? name)>();
        foreach (var ((type, instance), (group, name)) in references)
        {
            if (GetMediaKind(type) is not { } kind || !GetMediaResourceTypes(type).Any(wanted.Contains))
                continue;
            if (type is ResourceType.AudioConfigurationReference or ResourceType.AudioConfiguration)
            {
                if (Claim(ResourceType.AudioConfiguration, instance, group) is null)
                    foreach (var audioType in new[] { ResourceType.AudioVocals, ResourceType.AudioEffects })
                        if (Claim(audioType, instance, group) is { } audio)
                            Target(kind, audio, name);
                audioConfigurations.Enqueue((instance, group, name));
                continue;
            }
            if (GetMediaResourceTypes(type).Select(candidate => Claim(candidate, instance, group)).FirstOrDefault(claim => claim is not null) is { } claimed)
                Target(kind, claimed, name);
        }
        var openPackages = new Dictionary<string, DataBasePackedFile>(StringComparer.OrdinalIgnoreCase);
        try
        {
            DataBasePackedFile Open(string path)
            {
                if (!openPackages.TryGetValue(path, out var dbpf))
                {
                    dbpf = new DataBasePackedFile(path, true);
                    openPackages.Add(path, dbpf);
                }
                return dbpf;
            }
            var seenConfigurations = new HashSet<ulong>();
            while (audioConfigurations.TryDequeue(out var configuration))
            {
                if (configuration.instance is 0 || !seenConfigurations.Add(configuration.instance) || Claim(ResourceType.AudioConfiguration, configuration.instance, configuration.group) is not { } claim)
                    continue;
                Target(TuningArchiveResourceKind.Sound, claim, configuration.name);
                ReadOnlyMemory<byte> content;
                try
                {
                    content = Open(claim.path).Get(claim.key);
                }
                catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException)
                {
                    problems.Add($"{claim.pack}: audio configuration {claim.key} could not be read ({ex.Message})");
                    continue;
                }
                var (parent, samples) = ReadAudioConfiguration(content.Span);
                audioConfigurations.Enqueue((parent, 0, null));
                foreach (var sample in samples)
                    foreach (var audioType in new[] { ResourceType.AudioVocals, ResourceType.AudioEffects })
                        if (Claim(audioType, sample, 0) is { } audio)
                            Target(TuningArchiveResourceKind.Sound, audio, null);
            }
            var stages = new Dictionary<TuningArchiveResourceKind, string>
            {
                [TuningArchiveResourceKind.Image] = "Storing images",
                [TuningArchiveResourceKind.Sound] = "Storing sounds",
                [TuningArchiveResourceKind.Video] = "Storing videos"
            };
            foreach (var kindTargets in targets.GroupBy(target => target.Value.kind))
            {
                var kind = kindTargets.Key;
                var total = kindTargets.Count();
                var done = 0;
                var batch = new List<IngestDocument>();
                var batchBytes = 0L;
                void Flush()
                {
                    if (batch.Count is 0)
                        return;
                    ingestor.Ingest(kind, batch, manifests[kind], problems);
                    batch.Clear();
                    batchBytes = 0;
                }
                foreach (var byPath in kindTargets.GroupBy(target => target.Value.path, StringComparer.OrdinalIgnoreCase))
                {
                    var dbpf = Open(byPath.Key);
                    foreach (var (key, (_, _, pack, name)) in byPath)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            var content = dbpf.Get(key).ToArray();
                            batch.Add(new(key.Type, key.Group, key.FullInstance, name, null, null, pack, null, content, BlobHash.Compute(content)));
                            batchBytes += content.Length;
                        }
                        catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException)
                        {
                            problems.Add($"{pack}: {(kind switch { TuningArchiveResourceKind.Image => "image", TuningArchiveResourceKind.Sound => "sound", _ => "video" })} {key} could not be read ({ex.Message})");
                        }
                        if (++done % 100 is 0)
                            progress?.Report(new(stages[kind], done, total));
                        if (batchBytes >= mediaBatchBytes)
                            Flush();
                    }
                    dbpf.Dispose();
                    openPackages.Remove(byPath.Key);
                }
                Flush();
                progress?.Report(new(stages[kind], total, total));
            }
        }
        finally
        {
            foreach (var dbpf in openPackages.Values)
                dbpf.Dispose();
        }
    }

    /// <summary>
    /// Gets the content of the specified <paramref name="entry"/> as the archive stores it
    /// </summary>
    /// <remarks>
    /// Media (see <see cref="TuningArchiveResourceKind.Image"/>, <see cref="TuningArchiveResourceKind.Sound"/> and <see cref="TuningArchiveResourceKind.Video"/>) are stored as the game stores them; other kinds are stored as UTF-8 text (see <see cref="GetText(TuningArchiveEntry)"/>).
    /// </remarks>
    public byte[] GetContent(TuningArchiveEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        using var connection = OpenConnection();
        return GetBlobContent(connection, entry.BlobId);
    }

    /// <summary>
    /// Gets the medium in the specified <paramref name="snapshot"/> to which tuning or SimData refers with the specified <paramref name="reference"/>, or <see langword="null"/> if the snapshot does not contain it
    /// </summary>
    /// <remarks>
    /// References are resolved as the game resolves them (see <see cref="GetMediaKind(ResourceType)"/>); a reference to an audio configuration resolves to the configuration rather than to the audio it uses.
    /// </remarks>
    public TuningArchiveEntry? GetMediaEntry(TuningArchiveSnapshot snapshot, ResourceKey reference)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (GetMediaKind(reference.Type) is not { } kind)
            return null;
        foreach (var type in GetMediaResourceTypes(reference.Type))
            if (GetEntry(snapshot, kind, type, reference.FullInstance) is { } entry)
                return entry;
        return null;
    }

    /// <summary>
    /// Gets the audio that the audio configuration in the specified <paramref name="entry"/> uses (its samples, and those of the configurations it inherits from), as the specified <paramref name="snapshot"/> contains them
    /// </summary>
    public IReadOnlyList<TuningArchiveEntry> GetSoundSamples(TuningArchiveSnapshot snapshot, TuningArchiveEntry entry)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.Type is not ResourceType.AudioConfiguration)
            return [];
        var samples = new List<TuningArchiveEntry>();
        var seen = new HashSet<ulong>();
        var current = entry;
        while (current is not null && seen.Add(current.Instance))
        {
            var (parent, instances) = ReadAudioConfiguration(GetContent(current));
            foreach (var instance in instances)
                foreach (var audioType in new[] { ResourceType.AudioVocals, ResourceType.AudioEffects })
                    if (GetEntry(snapshot, TuningArchiveResourceKind.Sound, audioType, instance) is { } sample && !samples.Any(existing => existing.Key == sample.Key))
                        samples.Add(sample);
            current = parent is 0 ? null : GetEntry(snapshot, TuningArchiveResourceKind.Sound, ResourceType.AudioConfiguration, parent);
        }
        return samples;
    }

    /// <summary>
    /// Gets the tunings in the specified <paramref name="snapshot"/> that refer to the medium in the specified <paramref name="entry"/>
    /// </summary>
    /// <remarks>
    /// References with any of the resource types tuning uses for the medium are found (for an image, <see cref="ResourceType.PNGImage"/>, <see cref="ResourceType.PNGImage2"/> and <see cref="ResourceType.DSTImage"/>).
    /// Only tunings whose references have been indexed (see <see cref="IndexReferencesAsync"/>) are found.
    /// </remarks>
    public IReadOnlyList<TuningArchiveReference> GetMediaReferrers(TuningArchiveSnapshot snapshot, TuningArchiveEntry entry)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(entry);
        return [.. GetMediaReferenceTypes(entry.Type).SelectMany(type => GetReferrers(snapshot, type, entry.Instance)).DistinctBy(reference => (reference.Source.VersionId, reference.TargetType)).OrderBy(reference => reference.Source.Name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Gets the references in the specified <paramref name="snapshot"/> to media it captured that are not in it
    /// </summary>
    /// <remarks>
    /// Only references to kinds of media the snapshot captured are considered (see <see cref="HasKind(TuningArchiveSnapshot, TuningArchiveResourceKind)"/>), and only tunings whose references have been indexed (see <see cref="IndexReferencesAsync"/>).
    /// </remarks>
    public IReadOnlyList<TuningArchiveReference> GetBrokenMediaReferences(TuningArchiveSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var kinds = new[] { TuningArchiveResourceKind.Image, TuningArchiveResourceKind.Sound, TuningArchiveResourceKind.Video }.Where(kind => HasKind(snapshot, kind)).ToHashSet();
        if (kinds.Count is 0)
            return [];
        var present = kinds.ToDictionary(kind => kind, kind => GetEntries(snapshot, kind).Select(entry => entry.Instance).ToHashSet());
        var broken = new List<TuningArchiveReference>();
        using var connection = OpenConnection();
        if (!HasScope(connection, null, snapshot.Id, TuningScope))
            return [];
        using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT r.id, v.id, r.kind, r.type, r.grp, r.instance, v.name, v.pack, c.class, c.module, v.blob_id, v.first_ord, v.last_ord, ref.target_type, ref.target_instance
            FROM resource_version v
            JOIN resource r ON r.id = v.resource_id
            LEFT JOIN tuning_class c ON c.id = v.class_id
            JOIN reference ref ON ref.blob_id = v.blob_id
            WHERE r.kind = 0 AND {coversA}
            AND ref.target_type IN ({mediaReferenceTypes})
            ORDER BY v.name
            """;
        command.Parameters.AddWithValue("$a", snapshot.Ordinal);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var type = (ResourceType)(uint)reader.GetInt64(13);
            var instance = unchecked((ulong)reader.GetInt64(14));
            if (GetMediaKind(type) is { } kind && present.TryGetValue(kind, out var instances) && !instances.Contains(instance))
                broken.Add(new(ReadEntry(reader), type, instance));
        }
        return broken;
    }
}