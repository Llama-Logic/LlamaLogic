namespace LlamaLogic.TuningArchive;

/// <summary>
/// Finds the overrides of game tuning in every package in a folder and its subfolders, works out what a game version means for each, checks the SimData of the tuning the packages add against the game version's tuning descriptions, and writes updated copies of the packages with the changes that need no judgment from the creator, with a report of everything
/// </summary>
/// <remarks>
/// <para>
/// The changes that need no judgment are merges without conflicts whose base is not uncertain (see <see cref="TuningPackageOverride.BaseIsUncertain"/>), the SimData changes and fixes the options allow, and, if the options say so, removing overrides that do nothing.
/// Everything else is left as the package has it and marked in the report as needing the creator.
/// </para>
/// <para>
/// The packages in the folder are never changed, and updated copies are never written inside it, where the game would load them beside the originals.
/// </para>
/// </remarks>
public sealed class TuningFolderUpdate
{
    TuningFolderUpdate(string folder, TuningArchiveSnapshot target, TuningArchiveSnapshot? declaredBase, IReadOnlyList<TuningFolderUpdatePackage> packages, IReadOnlyDictionary<ResourceType, string> typeNames)
    {
        Folder = folder;
        Target = target;
        DeclaredBase = declaredBase;
        Packages = packages;
        this.typeNames = typeNames;
    }

    readonly IReadOnlyDictionary<ResourceType, string> typeNames;

    /// <summary>
    /// Gets the game version the creator said the packages were last updated for, if they said
    /// </summary>
    public TuningArchiveSnapshot? DeclaredBase { get; }

    /// <summary>
    /// Gets the folder
    /// </summary>
    public string Folder { get; }

    /// <summary>
    /// Gets the packages in the folder that override game tuning, add tuning whose SimData isn't what the tuning descriptions give, or couldn't be read, in order of their paths
    /// </summary>
    public IReadOnlyList<TuningFolderUpdatePackage> Packages { get; }

    /// <summary>
    /// Gets the snapshot of the game version the packages are being updated for
    /// </summary>
    public TuningArchiveSnapshot Target { get; }

    /// <summary>
    /// Finds the overrides of game tuning in every package in the specified <paramref name="folder"/> and its subfolders and works out what the <paramref name="target"/> game version in the specified <paramref name="archive"/> means for each (🏃)
    /// </summary>
    /// <param name="archive">The archive with the game versions</param>
    /// <param name="target">The game version to update the packages for</param>
    /// <param name="folder">The folder</param>
    /// <param name="simData">A drafter for the <paramref name="target"/> snapshot, to check the packages' SimData against what their overrides need and the SimData of the tuning they add against the tuning descriptions, or <see langword="null"/> not to</param>
    /// <param name="declaredBase">The game version the creator says the packages were last updated for, to use as the base instead of inferring it, or <see langword="null"/> to infer it</param>
    /// <param name="progress">Receives progress</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public static Task<TuningFolderUpdate> AnalyzeAsync(TuningArchiveFile archive, TuningArchiveSnapshot target, string folder, TuningSimDataDrafter? simData = null, TuningArchiveSnapshot? declaredBase = null, IProgress<TuningArchiveProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(folder);
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException($"There is no folder at {folder}");
        return Task.Run(() =>
        {
            progress?.Report(new("Finding packages", 0, 0));
            var paths = Directory.EnumerateFiles(folder, "*.package", SearchOption.AllDirectories).Select(path => System.IO.Path.GetRelativePath(folder, path)).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
            var indexes = new Dictionary<long, Dictionary<(ResourceType type, ulong instance), TuningArchiveEntry>>();
            var context = new TuningPackageUpdate.Context(archive, target, declaredBase, (snapshot, type, instance) => indexes[snapshot.Id].TryGetValue((type, instance), out var entry) ? entry : null);
            for (var index = 0; index < context.Candidates.Count; ++index)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new("Reading the game's tuning", index, context.Candidates.Count));
                var snapshot = context.Candidates[index];
                var byKey = new Dictionary<(ResourceType type, ulong instance), TuningArchiveEntry>();
                foreach (var entry in archive.GetEntries(snapshot, TuningArchiveResourceKind.Tuning))
                    if (!byKey.TryGetValue((entry.Type, entry.Instance), out var existing) || entry.Group < existing.Group)
                        byKey[(entry.Type, entry.Instance)] = entry;
                indexes[snapshot.Id] = byKey;
            }
            var results = new (TuningPackageUpdate? update, string? problem, List<TuningPackageUpdate.AddedTuning>? added)[paths.Count];
            var completed = 0;
            Parallel.For(0, paths.Count, new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = Environment.ProcessorCount }, index =>
            {
                var relativePath = paths[index];
                try
                {
                    var added = simData is null ? null : new List<TuningPackageUpdate.AddedTuning>();
                    results[index] = (TuningPackageUpdate.Analyze(context, System.IO.Path.Combine(folder, relativePath), simData, added, null, cancellationToken), null, added);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or NotSupportedException or ArgumentException or FormatException)
                {
                    results[index] = (null, exception.Message, null);
                }
                var done = Interlocked.Increment(ref completed);
                if (done % 25 == 0)
                    progress?.Report(new("Comparing packages", done, paths.Count));
            });
            progress?.Report(new("Comparing packages", paths.Count, paths.Count));
            var addedSimData = new IReadOnlyList<TuningPackageAddedSimData>[paths.Count];
            if (simData is not null && results.Any(result => result.added?.Any(tuning => tuning.SimDataKey is not null) is true))
                lock (simData)
                {
                    var isReliable = simData.GetReliableColumns(progress, cancellationToken);
                    progress?.Report(new("Checking the SimData of the mods' own tuning", 0, paths.Count));
                    var generator = simData.CreateAddedTuningGenerator();
                    foreach (var result in results)
                        foreach (var tuning in result.added ?? [])
                            generator.AddTuning(tuning.Document);
                    var scriptFolders = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                    for (var index = 0; index < paths.Count; ++index)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (index % 25 == 0)
                            progress?.Report(new("Checking the SimData of the mods' own tuning", index, paths.Count));
                        var items = new List<TuningPackageAddedSimData>();
                        foreach (var group in (results[index].added ?? []).Where(tuning => tuning.SimDataKey is not null).GroupBy(tuning => tuning.SimDataKey!.Value))
                        {
                            var tuning = group.First();
                            var simDataKey = group.Key;
                            var directory = System.IO.Path.GetDirectoryName(System.IO.Path.Combine(folder, paths[index]))!;
                            if (!scriptFolders.TryGetValue(directory, out var hasScript))
                                scriptFolders[directory] = hasScript = Directory.EnumerateFiles(directory, "*.ts4script").Any();
                            SimDataModel? generated = null;
                            var problem = group.Skip(1).Any() ? "more than one of the package's tunings has its instance, so which one it belongs to isn't clear" : tuning.SimDataProblem;
                            if (tuning.SimData is { } existing && problem is null)
                                try
                                {
                                    generated = generator.Generate(tuning.Document);
                                    generated.Pack = existing.Pack;
                                }
                                catch (Exception exception) when (exception is SimDataGenerationException or InvalidDataException or InvalidOperationException or XmlException or ArgumentException)
                                {
                                    problem = $"SimData couldn't be generated from its tuning ({exception.Message})";
                                }
                            IReadOnlyList<(string name, string value)> leftOut = [];
                            ReadOnlyMemory<byte>? amendedTuning = null;
                            SimDataModel? amendedGenerated = null;
                            if (generated is not null && tuning.SimData is { } modSimData && tuning.Text is { } text && tuning.Document.Root is { } root && root.Attribute("c")?.Value is { } instanceClass)
                            {
                                var candidates = TuningSimDataRepair.GetLeftOutTunables(modSimData, generated, root, name => simData.IsScalar(instanceClass, name), simData.IsSet).ToList();
                                while (candidates.Count > 0 && TuningSimDataRepair.AddTunables(text, candidates) is { } amendedText)
                                {
                                    SimDataModel regenerated;
                                    try
                                    {
                                        regenerated = generator.Generate(TuningXml.Parse(amendedText.TrimStart('\uFEFF')));
                                        regenerated.Pack = modSimData.Pack;
                                    }
                                    catch (Exception exception) when (exception is SimDataGenerationException or InvalidDataException or InvalidOperationException or XmlException or ArgumentException or FormatException)
                                    {
                                        break;
                                    }
                                    var stillDiffering = TuningSimDataRepair.GetDifferingColumns(modSimData, regenerated, simData.IsSet);
                                    var verified = candidates.Where(candidate => !stillDiffering.Contains(candidate.name)).ToList();
                                    if (verified.Count < candidates.Count)
                                    {
                                        candidates = verified;
                                        continue;
                                    }
                                    if (TuningSimDataRepair.GetDifferingColumns(generated, regenerated, simData.IsSet).SetEquals(candidates.Select(candidate => candidate.name)))
                                    {
                                        leftOut = candidates;
                                        amendedGenerated = regenerated;
                                        amendedTuning = (tuning.HasByteOrderMark ? (byte[])[0xEF, 0xBB, 0xBF] : []).Concat(Encoding.UTF8.GetBytes(amendedText)).ToArray();
                                    }
                                    break;
                                }
                            }
                            var item = new TuningPackageAddedSimData(simDataKey, tuning.Key, tuning.Name, hasScript, problem, tuning.SimData, generated, isReliable, simData.IsSet, leftOut, amendedTuning, amendedGenerated);
                            if (item.Problems is not TuningPackageAddedSimDataProblems.None)
                                items.Add(item);
                        }
                        addedSimData[index] = items;
                    }
                    progress?.Report(new("Checking the SimData of the mods' own tuning", paths.Count, paths.Count));
                }
            var packages = new List<TuningFolderUpdatePackage>();
            for (var index = 0; index < paths.Count; ++index)
            {
                var (update, problem, _) = results[index];
                var items = addedSimData[index] ?? [];
                if (problem is not null || update!.Overrides.Count > 0 || items.Count > 0)
                    packages.Add(new(paths[index], update, problem, items));
            }
            return new TuningFolderUpdate(folder, target, declaredBase, packages, archive.GetTuningTypeNames());
        }, cancellationToken);
    }

    /// <summary>
    /// Gets whether the update makes the change to the specified <paramref name="item"/> on its own: the merge of a mod's changes without conflicts, whose base is not uncertain
    /// </summary>
    public static bool IsAutomatic(TuningPackageOverride item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Outcome is TuningPackageOverrideOutcome.Merged && !item.BaseIsUncertain;
    }

    /// <summary>
    /// Gets the tuning the update writes in place of the specified <paramref name="update"/>'s overrides with the specified <paramref name="options"/>
    /// </summary>
    public static IReadOnlyDictionary<ResourceKey, string?> GetTuning(TuningPackageUpdate update, TuningFolderUpdateOptions options)
    {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(options);
        var tuning = new Dictionary<ResourceKey, string?>();
        foreach (var item in update.Overrides)
            if (IsAutomatic(item))
                tuning[item.Key] = item.Merge!.Text;
            else if (options.RemoveOverridesThatDoNothing && item.Outcome is TuningPackageOverrideOutcome.MatchesGame)
                tuning[item.Key] = null;
        return tuning;
    }

    static bool ChangesSimData(TuningPackageOverride item, TuningPackageSimDataChanges changes) =>
        item.SimData switch
        {
            TuningPackageSimDataState.Stale => changes.HasFlag(TuningPackageSimDataChanges.Replace),
            TuningPackageSimDataState.Unneeded => changes.HasFlag(TuningPackageSimDataChanges.Remove),
            TuningPackageSimDataState.Missing => changes.HasFlag(TuningPackageSimDataChanges.Add),
            _ => false
        };

    /// <summary>
    /// Gets whether the specified <paramref name="item"/> needs the creator's judgment after the update makes the changes the specified <paramref name="options"/> allow
    /// </summary>
    /// <remarks>
    /// An override whose declared base disagrees always does, even if it looks up to date: when the creator's declared version is wrong for it, it may be undoing the game's changes since the version it was really made from.
    /// </remarks>
    public static bool NeedsCreator(TuningPackageOverride item, TuningFolderUpdateOptions options)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(options);
        return item.Outcome switch
        {
            TuningPackageOverrideOutcome.UpToDate => false,
            TuningPackageOverrideOutcome.Merged => !IsAutomatic(item),
            TuningPackageOverrideOutcome.MatchesGame => !options.RemoveOverridesThatDoNothing,
            _ => true
        } || item.BaseDisagrees || item.SimData is TuningPackageSimDataState.Unverifiable || (item.SimData is TuningPackageSimDataState.Missing && !options.SimDataChanges.HasFlag(TuningPackageSimDataChanges.Add));
    }

    /// <summary>
    /// Gets whether the specified <paramref name="item"/> needs the creator's judgment after the update makes the fixes the specified <paramref name="options"/> allow
    /// </summary>
    public static bool NeedsCreator(TuningPackageAddedSimData item, TuningFolderUpdateOptions options)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(options);
        return item.Plan(options.AddedSimDataFixes).Remaining is not TuningPackageAddedSimDataProblems.None;
    }

    static string GetProblemsText(TuningPackageAddedSimDataProblems problems)
    {
        var parts = new List<string>();
        if (problems.HasFlag(TuningPackageAddedSimDataProblems.Unchecked))
            parts.Add("not checked");
        if (problems.HasFlag(TuningPackageAddedSimDataProblems.MissingColumns))
            parts.Add("missing columns");
        if (problems.HasFlag(TuningPackageAddedSimDataProblems.MistypedColumns))
            parts.Add("column types");
        if (problems.HasFlag(TuningPackageAddedSimDataProblems.ExtraColumns))
            parts.Add("extra columns");
        if (problems.HasFlag(TuningPackageAddedSimDataProblems.MismatchedHashes))
            parts.Add("schema hashes");
        if (problems.HasFlag(TuningPackageAddedSimDataProblems.Values))
            parts.Add("values");
        return string.Join("; ", parts);
    }

    static string GetOutcomeText(TuningPackageOverrideOutcome outcome) =>
        outcome switch
        {
            TuningPackageOverrideOutcome.UpToDate => "Up to date",
            TuningPackageOverrideOutcome.Merged => "Merges",
            TuningPackageOverrideOutcome.Conflicted => "Conflicts",
            TuningPackageOverrideOutcome.MatchesGame => "Does nothing",
            TuningPackageOverrideOutcome.MatchesOlderGame => "Old copy",
            TuningPackageOverrideOutcome.RemovedFromGame => "Gone from game",
            _ => "Unreadable"
        };

    static string GetBaseNote(TuningPackageOverride item) =>
        item.BaseDisagrees ? "another version is closer to the override than the declared one, so it may have been made from that version"
        : item.BaseIsAmbiguous ? "another version is as close; the older was assumed"
        : item.BaseMayBeOlder ? "may be older than the archive"
        : string.Empty;

    static string GetDoneText(TuningPackageUpdateChangeKind kind) =>
        kind switch
        {
            TuningPackageUpdateChangeKind.TuningReplaced => "merged",
            TuningPackageUpdateChangeKind.TuningRemoved => "removed",
            TuningPackageUpdateChangeKind.SimDataReplaced => "SimData replaced",
            TuningPackageUpdateChangeKind.SimDataAdded => "SimData added",
            TuningPackageUpdateChangeKind.SimDataRemoved => "SimData removed",
            TuningPackageUpdateChangeKind.SimDataRepaired => "SimData fixed",
            TuningPackageUpdateChangeKind.TuningAmended => "tuning amended",
            _ => "SimData left as it was"
        };

    static string Quote(string value) =>
        value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;

    /// <summary>
    /// Gets a row for every override in the folder, for the SimData of every tuning the packages add that isn't what the tuning descriptions give, and for every package that couldn't be read, for the specified <paramref name="options"/> and, if the updated copies were written, what writing them did
    /// </summary>
    public IReadOnlyList<TuningFolderUpdateReportRow> GetReportRows(TuningFolderUpdateOptions options, IReadOnlyList<TuningFolderUpdateWrite>? written = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var writes = (written ?? []).ToDictionary(write => write.RelativePath, StringComparer.OrdinalIgnoreCase);
        var rows = new List<TuningFolderUpdateReportRow>();
        foreach (var package in Packages)
        {
            if (package.Update is not { } update)
            {
                rows.Add(new(package.RelativePath, null, null, string.Empty, string.Empty, string.Empty, "Unreadable package", true, string.Empty, string.Empty, string.Empty, string.Empty, 0, string.Empty, package.Problem ?? string.Empty, string.Empty));
                continue;
            }
            writes.TryGetValue(package.RelativePath, out var write);
            foreach (var item in update.Overrides)
                rows.Add(new(
                    package.RelativePath,
                    item,
                    null,
                    item.Name ?? string.Empty,
                    typeNames.TryGetValue(item.Key.Type, out var typeName) ? typeName : $"0x{(uint)item.Key.Type:X8}",
                    item.Key.FullInstance.ToString(CultureInfo.InvariantCulture),
                    GetOutcomeText(item.Outcome),
                    NeedsCreator(item, options),
                    write is null ? string.Empty
                        : write.Skipped is { } skipped ? $"not written: {skipped}"
                        : string.Join("; ", write.Changes.Where(change => change.Kind is not (TuningPackageUpdateChangeKind.SimDataRepaired or TuningPackageUpdateChangeKind.TuningAmended) && change.Name == item.Name && (change.Key == item.Key || change.Key.FullInstance == item.Key.FullInstance)).Select(change => GetDoneText(change.Kind)).Distinct()),
                    item.Base?.GameVersion ?? string.Empty,
                    item.Base is null ? string.Empty : item.BaseIsDeclared ? "declared" : "inferred",
                    GetBaseNote(item),
                    item.Merge?.Conflicts.Count ?? 0,
                    item.SimData is TuningPackageSimDataState.NotChecked ? string.Empty : item.SimData.ToString(),
                    item.Message,
                    item.SimData is TuningPackageSimDataState.NotChecked or TuningPackageSimDataState.Current ? string.Empty : item.SimDataMessage ?? string.Empty));
            foreach (var item in package.AddedSimData)
                rows.Add(new(
                    package.RelativePath,
                    null,
                    item,
                    item.Name ?? string.Empty,
                    typeNames.TryGetValue(item.TuningKey.Type, out var typeName) ? typeName : $"0x{(uint)item.TuningKey.Type:X8}",
                    item.TuningKey.FullInstance.ToString(CultureInfo.InvariantCulture),
                    "Mod's own tuning",
                    NeedsCreator(item, options),
                    write is null ? string.Empty
                        : write.Skipped is { } skipped ? $"not written: {skipped}"
                        : string.Join("; ", write.Changes.Where(change => (change.Kind is TuningPackageUpdateChangeKind.SimDataRepaired && change.Key == item.Key) || (change.Kind is TuningPackageUpdateChangeKind.TuningAmended && change.Key == item.TuningKey)).Select(change => GetDoneText(change.Kind)).Distinct()),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    0,
                    GetProblemsText(item.Problems),
                    item.Message,
                    item.Plan(options.AddedSimDataFixes).Message));
        }
        return rows;
    }

    /// <summary>
    /// Gets a report of every override in the folder and the SimData of the tuning the packages add, as comma-separated values with a header row (see <see cref="GetReportRows"/>)
    /// </summary>
    public string GetReport(TuningFolderUpdateOptions options, IReadOnlyList<TuningFolderUpdateWrite>? written = null)
    {
        var builder = new StringBuilder();
        void row(params string[] values) =>
            builder.Append(string.Join(',', values.Select(Quote))).Append("\r\n");
        row("Package", "Tuning", "Type", "Instance", "Outcome", "Needs you", "Done", "Base", "Base source", "Base note", "Conflicts", "SimData", "Message", "SimData message");
        foreach (var report in GetReportRows(options, written))
            row(report.Package, report.Tuning, report.Type, report.Instance, report.Outcome, report.NeedsCreator ? "yes" : string.Empty, report.Done, report.Base, report.BaseSource, report.BaseNote, report.Conflicts > 0 ? report.Conflicts.ToString(CultureInfo.InvariantCulture) : string.Empty, report.SimData, report.Message, report.SimDataMessage);
        return builder.ToString();
    }

    /// <summary>
    /// Writes the report (see <see cref="GetReport"/>) to the specified <paramref name="path"/>, as UTF-8 with a byte order mark so that Excel reads it as UTF-8 (🏃)
    /// </summary>
    public Task WriteReportAsync(string path, TuningFolderUpdateOptions options, IReadOnlyList<TuningFolderUpdateWrite>? written = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(options);
        return File.WriteAllTextAsync(path, GetReport(options, written), new UTF8Encoding(true), cancellationToken);
    }

    /// <summary>
    /// Writes an updated copy of each package the specified <paramref name="options"/> change to the same relative path in the specified <paramref name="outputFolder"/>, skipping any whose path there already has a file, and, unless told not to, the report beside them (🏃)
    /// </summary>
    /// <param name="outputFolder">Where to write the updated copies, which must not be in the folder</param>
    /// <param name="options">Which changes to make</param>
    /// <param name="writeReport">Whether to write the report (see <see cref="GetReport"/>) beside the copies, as <c>Update report for</c> the game version, with <c>.csv</c></param>
    /// <param name="progress">Receives progress</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    /// <exception cref="ArgumentException"><paramref name="outputFolder"/> is the folder or in it</exception>
    public Task<TuningFolderUpdateResult> WriteAsync(string outputFolder, TuningFolderUpdateOptions options, bool writeReport = true, IProgress<TuningArchiveProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outputFolder);
        ArgumentNullException.ThrowIfNull(options);
        var output = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(outputFolder));
        var source = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(Folder));
        if (string.Equals(output, source, StringComparison.OrdinalIgnoreCase) || output.StartsWith(source + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || output.StartsWith(source + System.IO.Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The updated copies must be written outside the folder, or the game would load them beside the originals", nameof(outputFolder));
        return Task.Run(async () =>
        {
            Directory.CreateDirectory(output);
            var writes = new List<TuningFolderUpdateWrite>();
            var planned = Packages.Where(package => package.Update is not null).Select(package => (package, tuning: GetTuning(package.Update!, options), added: package.AddedSimData.Select(item => (item, plan: item.Plan(options.AddedSimDataFixes))).Where(fix => fix.plan.ChangesSimData || fix.plan.ChangesTuning).ToList())).Where(plan => plan.tuning.Count > 0 || plan.added.Count > 0 || plan.package.Update!.Overrides.Any(item => ChangesSimData(item, options.SimDataChanges))).ToList();
            for (var index = 0; index < planned.Count; ++index)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new("Writing updated packages", index, planned.Count));
                var (package, tuning, added) = planned[index];
                var path = System.IO.Path.Combine(output, package.RelativePath);
                if (File.Exists(path))
                {
                    writes.Add(new(package.RelativePath, null, [], "a file is already there"));
                    continue;
                }
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
                var changes = await package.Update!.WriteCoreAsync(path, tuning, options.SimDataChanges, added, cancellationToken).ConfigureAwait(false);
                if (!changes.Any(change => change.Kind is not TuningPackageUpdateChangeKind.SimDataUnverifiable))
                {
                    File.Delete(path);
                    writes.Add(new(package.RelativePath, null, changes, null));
                    continue;
                }
                writes.Add(new(package.RelativePath, path, changes, null));
            }
            progress?.Report(new("Writing updated packages", planned.Count, planned.Count));
            if (!writeReport)
                return new TuningFolderUpdateResult(writes, null);
            var reportPath = System.IO.Path.Combine(output, $"Update report for {Target.GameVersion}.csv");
            for (var attempt = 2; File.Exists(reportPath); ++attempt)
                reportPath = System.IO.Path.Combine(output, $"Update report for {Target.GameVersion} ({attempt}).csv");
            await WriteReportAsync(reportPath, options, writes, cancellationToken).ConfigureAwait(false);
            return new TuningFolderUpdateResult(writes, reportPath);
        }, cancellationToken);
    }
}