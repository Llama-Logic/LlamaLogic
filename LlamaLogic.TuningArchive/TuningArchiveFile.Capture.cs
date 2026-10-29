namespace LlamaLogic.TuningArchive;

partial class TuningArchiveFile
{
    static IReadOnlyList<(ResourceKey key, GameInstallationPackage package)> ClaimResources(IReadOnlyList<GameInstallationPackage> packages, Func<ResourceKey, bool> predicate, ICollection<string> problems, string packCode)
    {
        var claimed = new Dictionary<ResourceKey, GameInstallationPackage?>();
        foreach (var package in packages.OrderByDescending(package => package.IsPatch))
        {
            try
            {
                using var dbpf = new DataBasePackedFile(package.Path, true);
                foreach (var key in dbpf.Keys)
                    if (predicate(key) && !claimed.ContainsKey(key))
                        claimed.Add(key, dbpf.GetExplicitCompressionMode(key) is CompressionMode.SetDeletedFlag ? null : package);
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or NotSupportedException)
            {
                problems.Add($"{packCode}: {System.IO.Path.GetFileName(package.Path)} could not be opened ({ex.Message})");
            }
        }
        return [.. claimed.Where(entry => entry.Value is not null).Select(entry => (entry.Key, entry.Value!))];
    }

    static string? GetModuleName(XElement root) =>
        root.Name.LocalName == "M" ? root.Attribute("n")?.Value : root.Attribute("m")?.Value;

    internal static IngestDocument CreateTuningDocument(ResourceType type, uint group, ulong instance, string pack, string? typeName, XElement root)
    {
        var text = TuningXml.GetCanonicalText(root);
        var content = Encoding.UTF8.GetBytes(text);
        return new(type, group, instance, root.Attribute("n")?.Value, root.Name.LocalName == "I" ? root.Attribute("c")?.Value : null, GetModuleName(root), pack, typeName, content, BlobHash.Compute(content));
    }

    internal static IngestDocument CreateAnimationStateMachineDocument(ResourceKey key, string pack, AnimationStateMachineModel model)
    {
        var text = TuningXml.GetCanonicalText(model.Document);
        var content = Encoding.UTF8.GetBytes(text);
        return new(key.Type, key.Group, key.FullInstance, model.ResourceName, null, null, pack, null, content, BlobHash.Compute(content));
    }

    internal static IngestDocument CreateSimDataDocument(ResourceKey key, string pack, SimDataModel model)
    {
        var text = TuningXml.GetCanonicalText(model.ToXml());
        var content = Encoding.UTF8.GetBytes(text);
        return new(key.Type, key.Group, key.FullInstance, model.ResourceName, null, null, pack, null, content, BlobHash.Compute(content));
    }

    /// <summary>
    /// Captures a snapshot of the tuning, SimData, animation state machines, strings and media of the specified <paramref name="installation"/> (💤)
    /// </summary>
    /// <remarks>
    /// Media are the images, sounds and videos to which the captured tuning and SimData refer (with the audio the sounds use), taken from the client packages of the packs with the same rules as tuning; which kinds are captured is up to the <paramref name="options"/>.
    /// Strings are merged from every string table of every pack the way the game merges them: a string in a package of higher priority replaces the same string in a package of lower priority (packages of the same priority in pack order, the base game first), so a pack's <c>Delta</c> strings add to and correct its installed strings rather than replacing them.
    /// Within a pack, tuning, SimData and animation state machines are taken from the pack's <c>Delta</c> packages before its installed packages, because the <c>Delta</c> folder patches the installed one: its resources replace theirs and its delete records retire theirs, whatever priorities the two folders' configurations assign.
    /// The game does not load the patches of packs that are not installed, so their tuning is not captured; only the names of the tunings they patch are recorded (see <see cref="GetUninstalledTunings"/>), so that what refers to them can still be explained.
    /// </remarks>
    /// <exception cref="InvalidOperationException">No game version was specified and the installation's version could not be found</exception>
    /// <exception cref="NotSupportedException">The archive was opened for reading only</exception>
    public async Task<TuningArchiveCaptureResult> CaptureAsync(GameInstallation installation, TuningArchiveCaptureOptions? options = null, IProgress<TuningArchiveProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(installation);
        ThrowIfReadOnly();
        options ??= new();
        var gameVersion = options.GameVersion ?? installation.GameVersion ?? throw new InvalidOperationException("The installation's game version could not be found; specify it in the options");
        var packs = installation.Packs.Where(pack => options.Packs is null || options.Packs.Contains(pack.Code, StringComparer.OrdinalIgnoreCase)).ToList();
        var uninstalledPacks = installation.UninstalledPacks.Where(pack => options.Packs is null || options.Packs.Contains(pack.Code, StringComparer.OrdinalIgnoreCase)).ToList();
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(() =>
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var problems = new List<string>();
                using var connection = OpenConnection();
                var ingestor = new Ingestor(this, connection);
                var tuningManifest = new Dictionary<long, ResourceContent>();
                var simDataManifest = new Dictionary<long, ResourceContent>();
                var animationStateMachineManifest = new Dictionary<long, ResourceContent>();
                var stringManifest = new Dictionary<long, StringContent>();
                var mediaManifests = new Dictionary<TuningArchiveResourceKind, Dictionary<long, ResourceContent>>
                {
                    [TuningArchiveResourceKind.Image] = [],
                    [TuningArchiveResourceKind.Sound] = [],
                    [TuningArchiveResourceKind.Video] = []
                };
                var mediaReferences = new Dictionary<(ResourceType type, ulong instance), (uint group, string? name)>();
                var capturedTunings = new HashSet<(ResourceType type, ulong instance)>();
                var includeMedia = options.IncludeImages || options.IncludeSounds || options.IncludeVideos;
                var locale = options.Locale?.ToUpperInvariant();
                for (var packIndex = 0; packIndex < packs.Count; ++packIndex)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var pack = packs[packIndex];
                    progress?.Report(new($"Reading tuning from {pack.Code}", packIndex, packs.Count));
                    var documents = new List<IngestDocument>();
                    foreach (var (key, package) in ClaimResources(pack.SimulationPackages, key => key.Type is ResourceType.CombinedTuning, problems, pack.Code))
                    {
                        try
                        {
                            using var dbpf = new DataBasePackedFile(package.Path, true);
                            var tunings = dbpf.GetCombinedTuning(key).GetTunings().ToList();
                            documents.AddRange(tunings.AsParallel().AsOrdered().WithCancellation(cancellationToken).Select(tuning => CreateTuningDocument(tuning.ResourceType, 0, tuning.Instance, pack.Code, tuning.TypeName, tuning.Document.Root!)));
                        }
                        catch (Exception ex) when (ex is InvalidDataException or XmlException or FormatException or NotSupportedException or IOException)
                        {
                            problems.Add($"{pack.Code}: the combined tuning in {System.IO.Path.GetFileName(package.Path)} could not be read ({ex.Message})");
                        }
                    }
                    ingestor.Ingest(TuningArchiveResourceKind.Tuning, documents, tuningManifest, problems);
                    foreach (var document in documents)
                        capturedTunings.Add((document.Type, document.Instance));
                    if (includeMedia)
                        CollectMediaReferences(documents, mediaReferences);
                    if (options.IncludeSimData)
                    {
                        progress?.Report(new($"Reading SimData from {pack.Code}", packIndex, packs.Count));
                        var simDataDocuments = new List<IngestDocument>();
                        foreach (var group in ClaimResources(pack.SimulationPackages, key => key.Type is ResourceType.SimData, problems, pack.Code).GroupBy(claim => claim.package.Path))
                        {
                            using var dbpf = new DataBasePackedFile(group.Key, true);
                            var raw = group.Select(claim => (claim.key, data: dbpf.Get(claim.key))).ToList();
                            simDataDocuments.AddRange(raw.AsParallel().AsOrdered().WithCancellation(cancellationToken).Select(entry =>
                            {
                                try
                                {
                                    return CreateSimDataDocument(entry.key, pack.Code, SimDataModel.Decode(entry.data));
                                }
                                catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or FormatException or ArgumentException or IndexOutOfRangeException)
                                {
                                    lock (problems)
                                        problems.Add($"{pack.Code}: SimData {entry.key} could not be read ({ex.Message})");
                                    return null;
                                }
                            }).OfType<IngestDocument>());
                        }
                        ingestor.Ingest(TuningArchiveResourceKind.SimData, simDataDocuments, simDataManifest, problems);
                        if (includeMedia)
                            CollectMediaReferences(simDataDocuments, mediaReferences);
                    }
                    if (options.IncludeAnimationStateMachines)
                    {
                        progress?.Report(new($"Reading animation state machines from {pack.Code}", packIndex, packs.Count));
                        var animationStateMachineDocuments = new List<IngestDocument>();
                        foreach (var group in ClaimResources(pack.SimulationPackages, key => key.Type is ResourceType.AnimationStateMachine, problems, pack.Code).GroupBy(claim => claim.package.Path))
                        {
                            using var dbpf = new DataBasePackedFile(group.Key, true);
                            var raw = group.Select(claim => (claim.key, data: dbpf.Get(claim.key))).ToList();
                            animationStateMachineDocuments.AddRange(raw.AsParallel().AsOrdered().WithCancellation(cancellationToken).Select(entry =>
                            {
                                try
                                {
                                    return CreateAnimationStateMachineDocument(entry.key, pack.Code, AnimationStateMachineModel.Decode(entry.data));
                                }
                                catch (InvalidDataException ex)
                                {
                                    lock (problems)
                                        problems.Add($"{pack.Code}: animation state machine {entry.key} could not be read ({ex.Message})");
                                    return null;
                                }
                            }).OfType<IngestDocument>());
                        }
                        ingestor.Ingest(TuningArchiveResourceKind.AnimationStateMachine, animationStateMachineDocuments, animationStateMachineManifest, problems);
                    }
                }
                var uninstalledTunings = new Dictionary<(ResourceType type, ulong instance), TuningArchiveUninstalledTuning>();
                for (var packIndex = 0; packIndex < uninstalledPacks.Count; ++packIndex)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var pack = uninstalledPacks[packIndex];
                    progress?.Report(new($"Reading the names of tuning patched for {pack.Code}, which is not installed", packIndex, uninstalledPacks.Count));
                    foreach (var (key, package) in ClaimResources(pack.SimulationPackages, key => key.Type is ResourceType.CombinedTuning, problems, pack.Code))
                    {
                        try
                        {
                            using var dbpf = new DataBasePackedFile(package.Path, true);
                            foreach (var tuning in dbpf.GetCombinedTuning(key).GetTunings())
                                if (tuning.Document.Root is { } root
                                    && root.Attribute("n")?.Value is { Length: > 0 } name
                                    && !capturedTunings.Contains((tuning.ResourceType, tuning.Instance)))
                                    uninstalledTunings.TryAdd((tuning.ResourceType, tuning.Instance), new(tuning.ResourceType, tuning.Instance, name, root.Name.LocalName == "I" ? root.Attribute("c")?.Value : null, pack.Code));
                        }
                        catch (Exception ex) when (ex is InvalidDataException or XmlException or FormatException or NotSupportedException or IOException)
                        {
                            problems.Add($"{pack.Code}: the combined tuning in {System.IO.Path.GetFileName(package.Path)} could not be read ({ex.Message})");
                        }
                    }
                }
                if (locale is not null)
                {
                    var stringPackages = packs
                        .SelectMany((pack, packIndex) => pack.GetStringTablePackages(locale).Select(package => (pack, packIndex, package)))
                        .OrderByDescending(entry => entry.package.Priority)
                        .ThenBy(entry => entry.packIndex)
                        .ToList();
                    var entries = new Dictionary<uint, (string text, string pack)>();
                    for (var packageIndex = 0; packageIndex < stringPackages.Count; ++packageIndex)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var (pack, _, package) = stringPackages[packageIndex];
                        progress?.Report(new($"Reading strings from {pack.Code}", packageIndex, stringPackages.Count));
                        try
                        {
                            using var dbpf = new DataBasePackedFile(package.Path, true);
                            foreach (var key in dbpf.Keys.Where(key => key.Type is ResourceType.StringTable && dbpf.GetExplicitCompressionMode(key) is not CompressionMode.SetDeletedFlag))
                            {
                                try
                                {
                                    var table = dbpf.GetStringTable(key);
                                    foreach (var keyHash in table.KeyHashes)
                                        entries.TryAdd(keyHash, (table[keyHash], pack.Code));
                                }
                                catch (Exception ex) when (ex is InvalidDataException or NotSupportedException)
                                {
                                    problems.Add($"{pack.Code}: string table {key} in {System.IO.Path.GetFileName(package.Path)} could not be read ({ex.Message})");
                                }
                            }
                        }
                        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or NotSupportedException)
                        {
                            problems.Add($"{pack.Code}: {System.IO.Path.GetFileName(package.Path)} could not be opened ({ex.Message})");
                        }
                    }
                    foreach (var packEntries in entries.GroupBy(entry => entry.Value.pack, entry => new KeyValuePair<uint, string>(entry.Key, entry.Value.text)))
                        ingestor.IngestStrings(locale, packEntries.Key, packEntries, stringManifest);
                }
                if (includeMedia)
                    CaptureMedia(packs, options, mediaReferences, ingestor, mediaManifests, problems, progress, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new("Recording the snapshot", packs.Count, packs.Count));
                long snapshotId;
                using (var transaction = connection.BeginTransaction())
                {
                    var (id, ordinal) = InsertSnapshot(connection, transaction, gameVersion, options.Label, options.Notes, TuningArchiveSnapshotSource.GameInstallation, installation.Path, DateTimeOffset.UtcNow, locale, packs.Select(pack => new TuningArchiveSnapshotPack(pack.Code, pack.Version)));
                    ApplyResourceScope(connection, transaction, id, ordinal, TuningArchiveResourceKind.Tuning, tuningManifest);
                    if (options.IncludeSimData)
                        ApplyResourceScope(connection, transaction, id, ordinal, TuningArchiveResourceKind.SimData, simDataManifest);
                    if (options.IncludeAnimationStateMachines)
                        ApplyResourceScope(connection, transaction, id, ordinal, TuningArchiveResourceKind.AnimationStateMachine, animationStateMachineManifest);
                    if (locale is not null)
                        ApplyStringScope(connection, transaction, id, ordinal, locale, stringManifest);
                    InsertUninstalledTunings(connection, transaction, id, uninstalledTunings.Values);
                    if (options.IncludeImages)
                        ApplyResourceScope(connection, transaction, id, ordinal, TuningArchiveResourceKind.Image, mediaManifests[TuningArchiveResourceKind.Image]);
                    if (options.IncludeSounds)
                        ApplyResourceScope(connection, transaction, id, ordinal, TuningArchiveResourceKind.Sound, mediaManifests[TuningArchiveResourceKind.Sound]);
                    if (options.IncludeVideos)
                        ApplyResourceScope(connection, transaction, id, ordinal, TuningArchiveResourceKind.Video, mediaManifests[TuningArchiveResourceKind.Video]);
                    transaction.Commit();
                    snapshotId = id;
                }
                var snapshot = GetSnapshots(connection, null).First(snapshot => snapshot.Id == snapshotId);
                return new TuningArchiveCaptureResult(snapshot, tuningManifest.Count, simDataManifest.Count, animationStateMachineManifest.Count, stringManifest.Count, ingestor.NewContent, ingestor.NewContentBytes, problems, stopwatch.Elapsed, mediaManifests[TuningArchiveResourceKind.Image].Count, mediaManifests[TuningArchiveResourceKind.Sound].Count, mediaManifests[TuningArchiveResourceKind.Video].Count);
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }
}