namespace LlamaLogic.TuningArchive;

partial class TuningArchiveFile
{
#if IS_NET_7_0_OR_GREATER
    [GeneratedRegex(@"(?<type>[0-9A-Fa-f]{8})[_!-](?<group>[0-9A-Fa-f]{8})[_!-](?<instance>[0-9A-Fa-f]{16})", RegexOptions.CultureInvariant)]
    private static partial Regex GetFileNameKeyPattern();
#else
    static readonly Regex fileNameKeyPattern = new(@"(?<type>[0-9A-Fa-f]{8})[_!-](?<group>[0-9A-Fa-f]{8})[_!-](?<instance>[0-9A-Fa-f]{16})", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    static Regex GetFileNameKeyPattern() =>
        fileNameKeyPattern;
#endif

#if IS_NET_7_0_OR_GREATER
    [GeneratedRegex(@"^(BG|(EP|GP|SP|FP)\d+)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex GetPackSegmentPattern();
#else
    static readonly Regex packSegmentPattern = new(@"^(BG|(EP|GP|SP|FP)\d+)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static Regex GetPackSegmentPattern() =>
        packSegmentPattern;
#endif

#if IS_NET_7_0_OR_GREATER
    [GeneratedRegex(@"^0x(?<key>[0-9A-Fa-f]{8})\t(?<text>.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex GetStringReferenceLinePattern();
#else
    static readonly Regex stringReferenceLinePattern = new(@"^0x(?<key>[0-9A-Fa-f]{8})\t(?<text>.*)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    static Regex GetStringReferenceLinePattern() =>
        stringReferenceLinePattern;
#endif

    /// <summary>
    /// Imports a folder of tuning files extracted by another tool (such as Scumbumbo's XML Extractor or Sims 4 Studio) as a snapshot of the specified <paramref name="gameVersion"/> (💤)
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every <c>.xml</c> file under the folder whose root is an instance (<c>I</c>) or module (<c>M</c>) tuning element is imported as tuning, every one whose root is <c>SimData</c> as SimData, and every other one whose file name has a resource key of type <see cref="ResourceType.AnimationStateMachine"/> (as Scumbumbo's XML Extractor names the files in its <c>AsmJazz</c> folders) as an animation state machine.
    /// A tuning's type is derived from its <c>i</c> attribute the way the game derives it, falling back to a resource key in its file name, and its group is always 0 (the game identifies tuning by type and instance); comments are discarded.
    /// A file is attributed to the pack named by a folder in its path (such as <c>EP01</c>), or to <see cref="TuningArchiveImportOptions.DefaultPack"/>.
    /// </para>
    /// <para>
    /// A file named <c>Reference - All Strings.txt</c> in the folder, as Scumbumbo's XML Extractor writes it, is imported as the snapshot's strings.
    /// </para>
    /// </remarks>
    public async Task<TuningArchiveCaptureResult> ImportFolderAsync(string folder, string gameVersion, TuningArchiveImportOptions? options = null, IProgress<TuningArchiveProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(gameVersion);
        if (string.IsNullOrWhiteSpace(gameVersion))
            throw new ArgumentException("A game version is required", nameof(gameVersion));
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException($"There is no folder at {folder}");
        options ??= new();
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(() =>
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var problems = new List<string>();
                progress?.Report(new("Finding files", 0, 0));
                var files = new List<string>();
                foreach (var file in Directory.EnumerateFiles(folder, "*.xml", SearchOption.AllDirectories))
                {
                    files.Add(file);
                    if (files.Count % 1000 == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        progress?.Report(new("Finding files", files.Count, 0));
                    }
                }
                var completed = 0;
                var parsed = files.AsParallel().WithCancellation(cancellationToken).Select(file =>
                {
                    var done = Interlocked.Increment(ref completed);
                    if (done % 100 == 0)
                        progress?.Report(new("Reading files", done, files.Count));
                    try
                    {
                        var document = XDocument.Load(file);
                        if (document.Root is not { } root)
                            return default;
                        var relative = System.IO.Path.GetRelativePath(folder, file);
                        var pack = relative.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar).FirstOrDefault(segment => GetPackSegmentPattern().IsMatch(segment))?.ToUpperInvariant() ?? options.DefaultPack;
                        var fileNameKey = GetFileNameKeyPattern().Match(System.IO.Path.GetFileName(file));
                        var fileType = fileNameKey.Success ? (ResourceType?)uint.Parse(fileNameKey.Groups["type"].Value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture) : null;
                        var fileGroup = fileNameKey.Success ? uint.Parse(fileNameKey.Groups["group"].Value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture) : 0u;
                        var fileInstance = fileNameKey.Success ? ulong.Parse(fileNameKey.Groups["instance"].Value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture) : (ulong?)null;
                        if (root.Name.LocalName is "I" or "M")
                        {
                            var typeName = root.Name.LocalName == "M" ? "tun" : root.Attribute("i")?.Value;
                            var type = typeName is not null ? TuningXml.GetResourceType(typeName) : fileType;
                            ulong? instance = ulong.TryParse(root.Attribute("s")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInstance) ? parsedInstance : fileInstance;
                            if (type is not { } resolvedType || instance is not { } resolvedInstance)
                                return (TuningArchiveResourceKind.Tuning, (IngestDocument?)null, $"{relative}: its type or instance could not be determined");
                            return (TuningArchiveResourceKind.Tuning, CreateTuningDocument(resolvedType, 0, resolvedInstance, pack, typeName, root), (string?)null);
                        }
                        if (root.Name.LocalName == "SimData")
                        {
                            if (fileInstance is not { } simDataInstance)
                                return (TuningArchiveResourceKind.SimData, null, $"{relative}: SimData files must have a resource key in their names");
                            return (TuningArchiveResourceKind.SimData, CreateSimDataDocument(new ResourceKey(ResourceType.SimData, fileGroup, simDataInstance), pack, SimDataModel.FromXml(document)), null);
                        }
                        if (fileType is ResourceType.AnimationStateMachine && fileInstance is { } animationStateMachineInstance)
                            return (TuningArchiveResourceKind.AnimationStateMachine, CreateAnimationStateMachineDocument(new ResourceKey(ResourceType.AnimationStateMachine, fileGroup, animationStateMachineInstance), pack, new AnimationStateMachineModel(document)), null);
                        return default;
                    }
                    catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException or InvalidDataException or FormatException or ArgumentException)
                    {
                        return (TuningArchiveResourceKind.Tuning, null, $"{System.IO.Path.GetRelativePath(folder, file)}: {ex.Message}");
                    }
                }).ToList();
                cancellationToken.ThrowIfCancellationRequested();
                problems.AddRange(parsed.Select(entry => entry.Item3).OfType<string>().OrderBy(problem => problem, StringComparer.Ordinal));
                var packOrder = parsed.Where(entry => entry.Item2 is not null).Select(entry => entry.Item2!.Pack).Distinct().OrderBy(pack => pack == "BG" ? 0 : 1).ThenBy(pack => pack, StringComparer.Ordinal).ToList();
                using var connection = OpenConnection();
                var ingestor = new Ingestor(this, connection);
                var tuningManifest = new Dictionary<long, ResourceContent>();
                var simDataManifest = new Dictionary<long, ResourceContent>();
                var tuningDocuments = parsed.Where(entry => entry.Item1 is TuningArchiveResourceKind.Tuning && entry.Item2 is not null).Select(entry => entry.Item2!).OrderBy(document => packOrder.IndexOf(document.Pack)).ToList();
                ingestor.Ingest(TuningArchiveResourceKind.Tuning, tuningDocuments, tuningManifest, problems, progress, "Storing tuning");
                var simDataDocuments = parsed.Where(entry => entry.Item1 is TuningArchiveResourceKind.SimData && entry.Item2 is not null).Select(entry => entry.Item2!).OrderBy(document => packOrder.IndexOf(document.Pack)).ToList();
                if (simDataDocuments.Count > 0)
                    ingestor.Ingest(TuningArchiveResourceKind.SimData, simDataDocuments, simDataManifest, problems, progress, "Storing SimData");
                var animationStateMachineManifest = new Dictionary<long, ResourceContent>();
                var animationStateMachineDocuments = parsed.Where(entry => entry.Item1 is TuningArchiveResourceKind.AnimationStateMachine && entry.Item2 is not null).Select(entry => entry.Item2!).OrderBy(document => packOrder.IndexOf(document.Pack)).ToList();
                if (animationStateMachineDocuments.Count > 0)
                    ingestor.Ingest(TuningArchiveResourceKind.AnimationStateMachine, animationStateMachineDocuments, animationStateMachineManifest, problems, progress, "Storing animation state machines");
                var stringManifest = new Dictionary<long, StringContent>();
                var locale = options.Locale.ToUpperInvariant();
                var stringReference = System.IO.Path.Combine(folder, "Reference - All Strings.txt");
                var hasStrings = File.Exists(stringReference);
                if (hasStrings)
                {
                    progress?.Report(new("Storing strings", 0, 0));
                    var entries = new Dictionary<uint, string>();
                    foreach (var line in File.ReadLines(stringReference))
                        if (GetStringReferenceLinePattern().Match(line) is { Success: true } match)
                            entries.TryAdd(uint.Parse(match.Groups["key"].Value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture), match.Groups["text"].Value);
                    ingestor.IngestStrings(locale, options.DefaultPack, entries, stringManifest);
                }
                progress?.Report(new("Recording the snapshot", 0, 0));
                long snapshotId;
                using (var transaction = connection.BeginTransaction())
                {
                    var (id, ordinal) = InsertSnapshot(connection, transaction, gameVersion, options.Label, options.Notes, TuningArchiveSnapshotSource.TuningFolder, folder, DateTimeOffset.UtcNow, hasStrings ? locale : null, packOrder.Select(pack => new TuningArchiveSnapshotPack(pack, null)));
                    ApplyResourceScope(connection, transaction, id, ordinal, TuningArchiveResourceKind.Tuning, tuningManifest);
                    if (simDataDocuments.Count > 0)
                        ApplyResourceScope(connection, transaction, id, ordinal, TuningArchiveResourceKind.SimData, simDataManifest);
                    if (animationStateMachineDocuments.Count > 0)
                        ApplyResourceScope(connection, transaction, id, ordinal, TuningArchiveResourceKind.AnimationStateMachine, animationStateMachineManifest);
                    if (hasStrings)
                        ApplyStringScope(connection, transaction, id, ordinal, locale, stringManifest);
                    transaction.Commit();
                    snapshotId = id;
                }
                var snapshot = GetSnapshots(connection, null).First(snapshot => snapshot.Id == snapshotId);
                return new TuningArchiveCaptureResult(snapshot, tuningManifest.Count, simDataManifest.Count, animationStateMachineManifest.Count, stringManifest.Count, ingestor.NewContent, ingestor.NewContentBytes, problems, stopwatch.Elapsed);
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }
}