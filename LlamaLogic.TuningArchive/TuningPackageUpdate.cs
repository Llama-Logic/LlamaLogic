namespace LlamaLogic.TuningArchive;

/// <summary>
/// Finds a mod package's overrides of game tuning, works out what a game version means for each, and writes an updated copy of the package
/// </summary>
/// <remarks>
/// <para>
/// For each override, the base (the game version the mod was most likely made from) is the archive snapshot, up to the target, whose version of the tuning has the fewest differences from the override.
/// When versions with different content are as close, the base is the oldest of them, because they tie when the mod changed what the game later changed, and the older base shows that as a conflict rather than taking the mod's version as up to date.
/// If the game changed the tuning between the base and the target, the mod's changes are merged onto the target's version (see <see cref="TuningMerge"/>).
/// </para>
/// <para>
/// The package is never changed: <see cref="WriteAsync"/> writes a new one, copying every resource it is not told to change.
/// </para>
/// </remarks>
public sealed class TuningPackageUpdate
{
    TuningPackageUpdate(string packagePath, TuningArchiveSnapshot target, TuningSimDataDrafter? simData, IReadOnlyList<TuningPackageOverride> overrides)
    {
        PackagePath = packagePath;
        Target = target;
        this.simData = simData;
        Overrides = overrides;
    }

    readonly TuningSimDataDrafter? simData;

    /// <summary>
    /// Gets whether the package's SimData was checked against what its overrides need, and can be made what they need when the updated package is written
    /// </summary>
    public bool ChecksSimData =>
        simData is not null;

    /// <summary>
    /// Gets the package's overrides of game tuning
    /// </summary>
    public IReadOnlyList<TuningPackageOverride> Overrides { get; }

    /// <summary>
    /// Gets the path of the package
    /// </summary>
    public string PackagePath { get; }

    /// <summary>
    /// Gets the snapshot of the game version the package is being updated for
    /// </summary>
    public TuningArchiveSnapshot Target { get; }

    /// <summary>
    /// Finds the overrides of game tuning in the package at the specified <paramref name="packagePath"/> and works out what the <paramref name="target"/> game version in the specified <paramref name="archive"/> means for each (🏃)
    /// </summary>
    /// <param name="archive">The archive with the game versions</param>
    /// <param name="target">The game version to update the package for</param>
    /// <param name="packagePath">The path of the package</param>
    /// <param name="simData">A drafter for the <paramref name="target"/> snapshot, to check the package's SimData against what its overrides need, or <see langword="null"/> not to</param>
    /// <param name="declaredBase">The game version the creator says the package was last updated for, to use as the base instead of inferring it (for each tuning that version has), or <see langword="null"/> to infer it</param>
    /// <param name="progress">Receives progress</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public static Task<TuningPackageUpdate> AnalyzeAsync(TuningArchiveFile archive, TuningArchiveSnapshot target, string packagePath, TuningSimDataDrafter? simData = null, TuningArchiveSnapshot? declaredBase = null, IProgress<TuningArchiveProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(packagePath);
        return Task.Run(() =>
        {
            var context = new Context(archive, target, declaredBase, null);
            return Analyze(context, packagePath, simData, null, progress, cancellationToken);
        }, cancellationToken);
    }

    internal sealed class Context
    {
        public Context(TuningArchiveFile archive, TuningArchiveSnapshot target, TuningArchiveSnapshot? declaredBase, Func<TuningArchiveSnapshot, ResourceType, ulong, TuningArchiveEntry?>? lookup)
        {
            Archive = archive;
            Target = target;
            var snapshots = archive.GetSnapshots();
            var targetIndex = snapshots.ToList().FindIndex(snapshot => snapshot.Id == target.Id);
            if (targetIndex < 0)
                throw new ArgumentException("The target is not a snapshot in the archive", nameof(target));
            Oldest = snapshots[0];
            Candidates = [.. snapshots.Take(targetIndex + 1)];
            if (declaredBase is not null && !Candidates.Any(snapshot => snapshot.Id == declaredBase.Id))
                throw new ArgumentException("The declared base must be a snapshot in the archive no newer than the target", nameof(declaredBase));
            DeclaredBase = declaredBase;
            TuningTypes = archive.GetTuningTypeNames().Keys.ToHashSet();
            TuningTypes.Add(ResourceType.Tuning);
            Lookup = lookup ?? ((snapshot, type, instance) => archive.GetEntry(snapshot, TuningArchiveResourceKind.Tuning, type, instance));
        }

        readonly ConcurrentDictionary<long, string> texts = new();

        public TuningArchiveFile Archive { get; }

        public IReadOnlyList<TuningArchiveSnapshot> Candidates { get; }

        public TuningArchiveSnapshot? DeclaredBase { get; }

        public Func<TuningArchiveSnapshot, ResourceType, ulong, TuningArchiveEntry?> Lookup { get; }

        public TuningArchiveSnapshot Oldest { get; }

        public TuningArchiveSnapshot Target { get; }

        public HashSet<ResourceType> TuningTypes { get; }

        public string GetText(TuningArchiveEntry entry) =>
            texts.GetOrAdd(entry.BlobId, _ => Archive.GetText(entry));
    }

    internal sealed record AddedTuning(ResourceKey Key, string? Name, XDocument Document, ResourceKey? SimDataKey, SimDataModel? SimData, string? SimDataProblem, string? Text = null, bool HasByteOrderMark = false);

    internal static TuningPackageUpdate Analyze(Context context, string packagePath, TuningSimDataDrafter? simData, List<AddedTuning>? added, IProgress<TuningArchiveProgress>? progress, CancellationToken cancellationToken)
    {
        var target = context.Target;
        using var package = new DataBasePackedFile(packagePath, true);
        var keys = package.Keys.Where(key => context.TuningTypes.Contains(key.Type) && package.GetExplicitCompressionMode(key) is not CompressionMode.SetDeletedFlag).ToList();
        var getText = context.GetText;
        var overrides = new List<TuningPackageOverride>();
        for (var index = 0; index < keys.Count; ++index)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new("Comparing overrides", index, keys.Count));
            var key = keys[index];
            var versions = context.Candidates.Select(snapshot => (snapshot, entry: context.Lookup(snapshot, key.Type, key.FullInstance))).Where(version => version.entry is not null).Select(version => (version.snapshot, entry: version.entry!)).ToList();
            if (versions.Count == 0)
            {
                if (added is not null)
                    CollectAddedTuning(package, key, added);
                continue;
            }
            var gameEntry = versions[^1].snapshot.Id == target.Id ? versions[^1].entry : null;
            var name = versions[^1].entry.Name;
            string modText;
            try
            {
                var text = Encoding.UTF8.GetString(package.Get(key).Span).TrimStart('\uFEFF');
                if (!text.TrimStart().StartsWith('<'))
                    throw new XmlException("The resource is not XML");
                var document = TuningXml.Parse(text);
                if (document.Root?.Attribute("n")?.Value is { } documentName)
                    name = documentName;
                modText = TuningXml.GetCanonicalText(document);
            }
            catch (Exception exception) when (exception is XmlException or InvalidDataException or ArgumentException or DecoderFallbackException)
            {
                overrides.Add(new(key, name, TuningPackageOverrideOutcome.Unreadable, $"The override couldn't be read as tuning ({exception.Message}).", null, null, 0, false, false, false, false, null, gameEntry, null, null));
                continue;
            }
            var distances = new Dictionary<long, int>();
            var scored = versions.Select(version =>
            {
                if (!distances.TryGetValue(version.entry.BlobId, out var distance))
                    distances[version.entry.BlobId] = distance = TuningSemanticDiff.Compare(getText(version.entry), modText).Count;
                return (version.snapshot, version.entry, distance);
            }).ToList();
            var baseDistance = scored.Min(version => version.distance);
            var closest = scored.Where(version => version.distance == baseDistance).ToList();
            var baseBlobId = closest[0].entry.BlobId;
            var best = closest.Last(version => version.entry.BlobId == baseBlobId);
            var baseIsAmbiguous = closest.Any(version => version.entry.BlobId != baseBlobId);
            var baseMayBeOlder = baseDistance > 0 && scored[0].snapshot.Id == context.Oldest.Id && scored[0].entry.BlobId == baseBlobId;
            var baseIsDeclared = false;
            var baseDisagrees = false;
            if (context.DeclaredBase is { } declaredBase && scored.FirstOrDefault(version => version.snapshot.Id == declaredBase.Id) is { entry: not null } declared)
            {
                baseIsDeclared = true;
                baseDisagrees = declared.distance > baseDistance;
                best = declared;
                baseDistance = declared.distance;
                baseIsAmbiguous = false;
                baseMayBeOlder = false;
            }
            var baseText = getText(best.entry);
            var madeFrom = baseIsDeclared ? $"{best.snapshot.DisplayName}, which the package was last updated for" : $"{best.snapshot.DisplayName}, which the override was most likely made from";
            TuningPackageOverride create(TuningPackageOverrideOutcome outcome, string message, string? gameText = null, TuningMergeResult? merge = null) =>
                new(key, name, outcome, message, modText, best.snapshot, baseDistance, baseIsAmbiguous, baseMayBeOlder, baseIsDeclared, baseDisagrees, baseText, gameEntry, gameText, merge);
            TuningPackageOverride item;
            if (gameEntry is null)
                item = create(TuningPackageOverrideOutcome.RemovedFromGame, $"The game no longer has this tuning in {target.DisplayName}, so the override has nothing to override.");
            else
            {
                var gameText = getText(gameEntry);
                if (modText == gameText)
                    item = create(TuningPackageOverrideOutcome.MatchesGame, $"The override is the same as the game's tuning in {target.DisplayName}, so it does nothing.", gameText);
                else if (baseDistance == 0)
                    item = create(TuningPackageOverrideOutcome.MatchesOlderGame, $"The override is the same as the game's tuning in {best.snapshot.DisplayName}, so all it does is undo the game's changes since then.", gameText);
                else if (gameText == baseText)
                    item = create(TuningPackageOverrideOutcome.UpToDate, $"The game hasn't changed this tuning since {madeFrom}, so it needs nothing.", gameText);
                else
                {
                    var merge = TuningMerge.Merge(baseText, modText, gameText);
                    item = merge.Conflicts.Count == 0 && merge.Text == modText
                        ? create(TuningPackageOverrideOutcome.UpToDate, $"The game changed this tuning since {best.snapshot.DisplayName}, but the override already has the game's changes, so it needs nothing.", gameText)
                        : merge.Conflicts.Count > 0
                        ? create(TuningPackageOverrideOutcome.Conflicted, $"The game changed this tuning since {best.snapshot.DisplayName}, and {(merge.Conflicts.Count == 1 ? "one of the mod's changes conflicts" : $"{merge.Conflicts.Count} of the mod's changes conflict")} with the game's.", gameText, merge)
                        : merge.Text == gameText
                        ? create(TuningPackageOverrideOutcome.Merged, $"The game changed this tuning since {best.snapshot.DisplayName}, and its new version already makes the mod's changes, so the override could be removed.", gameText, merge)
                        : create(TuningPackageOverrideOutcome.Merged, $"The game changed this tuning since {best.snapshot.DisplayName}, and the mod's changes merge onto its new version.", gameText, merge);
                }
            }
            if (simData is not null && item.GameEntry is not null && GetRecommendedText(item) is { } recommendedText)
            {
                SimDataPlan plan;
                lock (simData)
                    plan = PlanSimData(package, simData, item.GameEntry, recommendedText);
                item.SimData = plan.State;
                item.SimDataMessage = plan.Message;
            }
            overrides.Add(item);
        }
        progress?.Report(new("Comparing overrides", keys.Count, keys.Count));
        return new(packagePath, target, simData, overrides);
    }

    static void CollectAddedTuning(DataBasePackedFile package, ResourceKey key, List<AddedTuning> added)
    {
        XDocument document;
        string text;
        bool hasByteOrderMark;
        try
        {
            var bytes = package.Get(key);
            hasByteOrderMark = bytes.Span.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]);
            text = Encoding.UTF8.GetString(bytes.Span[(hasByteOrderMark ? 3 : 0)..]);
            if (!text.TrimStart('\uFEFF').TrimStart().StartsWith('<'))
                return;
            document = TuningXml.Parse(text.TrimStart('\uFEFF'));
        }
        catch (Exception exception) when (exception is XmlException or InvalidDataException or ArgumentException or DecoderFallbackException)
        {
            return;
        }
        var name = document.Root?.Attribute("n")?.Value;
        var simDataKey = package.Keys.Where(candidate => candidate.Type is ResourceType.SimData && candidate.FullInstance == key.FullInstance && package.GetExplicitCompressionMode(candidate) is not CompressionMode.SetDeletedFlag).Select(candidate => (ResourceKey?)candidate).FirstOrDefault();
        if (simDataKey is not { } existingKey)
        {
            if (key.Type is not ResourceType.Tuning)
                document = new XDocument(new XElement("I", new XAttribute("n", name ?? string.Empty), new XAttribute("s", key.FullInstance.ToString(CultureInfo.InvariantCulture))));
            added.Add(new(key, name, document, null, null, null));
            return;
        }
        try
        {
            added.Add(new(key, name, document, existingKey, package.GetSimData(existingKey), null, text, hasByteOrderMark));
        }
        catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or ArgumentException or NotSupportedException or IndexOutOfRangeException or ArgumentOutOfRangeException)
        {
            added.Add(new(key, name, document, existingKey, null, $"it couldn't be read ({exception.Message})"));
        }
    }

    static string? GetRecommendedText(TuningPackageOverride item) =>
        item.Outcome is TuningPackageOverrideOutcome.Merged ? item.Merge!.Text : item.ModText;

    static ResourceKey? FindSimData(DataBasePackedFile package, ResourceKey key)
    {
        if (package.ContainsKey(key) && package.GetExplicitCompressionMode(key) is not CompressionMode.SetDeletedFlag)
            return key;
        foreach (var candidate in package.Keys)
            if (candidate.Type is ResourceType.SimData && candidate.FullInstance == key.FullInstance && package.GetExplicitCompressionMode(candidate) is not CompressionMode.SetDeletedFlag)
                return candidate;
        return null;
    }

    readonly record struct SimDataPlan(TuningPackageSimDataState State, string Message, ResourceKey? ModKey, ResourceKey? GameKey, SimDataModel? Generated);

    static SimDataPlan PlanSimData(DataBasePackedFile package, TuningSimDataDrafter simData, TuningArchiveEntry gameEntry, string text)
    {
        var result = simData.Draft(gameEntry, text);
        var modKey = result.Key is { } gameKey ? FindSimData(package, gameKey) : null;
        switch (result.Kind)
        {
            case TuningSimDataDraftKind.Failed:
                return new(TuningPackageSimDataState.Unverifiable, result.Message, modKey, result.Key, null);
            case TuningSimDataDraftKind.Unaffected:
                if (result.Key is null || modKey is null)
                    return new(TuningPackageSimDataState.Current, result.Message, modKey, result.Key, null);
                return simData.CanReproduce(gameEntry)
                    ? new(TuningPackageSimDataState.Unneeded, "The mod has SimData for this tuning, but the tuning's changes don't reach SimData, so the game's SimData is what it needs.", modKey, result.Key, null)
                    : new(TuningPackageSimDataState.Unverifiable, "The mod has SimData for this tuning, and the tuning's changes don't seem to reach SimData, but this tuning's SimData can't be reproduced from the tuning descriptions, so whether the mod's SimData is needed couldn't be checked; it stays as it was.", modKey, result.Key, null);
            default:
                var generated = result.SimData!;
                if (modKey is not { } existingKey)
                    return new(TuningPackageSimDataState.Missing, "The tuning's changes reach its SimData, but the mod has no SimData for it, so the game uses its own SimData without them.", null, result.Key, generated);
                SimDataModel modSimData;
                try
                {
                    modSimData = package.GetSimData(existingKey);
                }
                catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or ArgumentException or NotSupportedException)
                {
                    return new(TuningPackageSimDataState.Stale, $"The mod's SimData for this tuning couldn't be read ({exception.Message}), and SimData the tuning needs was generated.", existingKey, result.Key, generated);
                }
                var (pack, version) = (generated.Pack, generated.Version);
                generated.Pack = modSimData.Pack;
                generated.Version = modSimData.Version;
                var same = TuningSimDataDrafter.GetComparable(generated) == TuningSimDataDrafter.GetComparable(modSimData);
                generated.Pack = pack;
                generated.Version = version;
                return same
                    ? new(TuningPackageSimDataState.Current, "The mod's SimData for this tuning is what the tuning needs.", existingKey, result.Key, null)
                    : new(TuningPackageSimDataState.Stale, "The mod's SimData for this tuning isn't what the tuning needs, and SimData that is was generated.", existingKey, result.Key, generated);
        }
    }

    static CompressionMode GetCompressionMode(DataBasePackedFile package, ResourceKey key) =>
        package.ContainsKey(key) && package.GetExplicitCompressionMode(key) is var mode && mode is not (CompressionMode.CallerSuppliedInternal or CompressionMode.CallerSuppliedStreamable or CompressionMode.CallerSuppliedZLib or CompressionMode.SetDeletedFlag)
            ? mode
            : CompressionMode.Auto;

    /// <summary>
    /// Gets the tuning the update recommends writing: the merge, for each override whose changes merge onto the game's new version without conflicts
    /// </summary>
    /// <remarks>
    /// Nothing else is recommended, because only the creator knows whether an override that undoes the game's changes, or does nothing, is meant to.
    /// </remarks>
    public IReadOnlyDictionary<ResourceKey, string?> GetRecommendedTuning() =>
        Overrides.Where(item => item.Outcome is TuningPackageOverrideOutcome.Merged).ToDictionary(item => item.Key, item => (string?)item.Merge!.Text);

    /// <summary>
    /// Writes a copy of the package to the specified <paramref name="path"/> with the specified <paramref name="tuning"/> in place of the overrides it has keys for (<see langword="null"/> to remove an override), and, if the update has a SimData drafter, the specified <paramref name="simDataChanges"/> made to each override's SimData (🏃)
    /// </summary>
    /// <param name="path">Where to write the updated package, which must not be the package's own path</param>
    /// <param name="tuning">The tuning to write in place of overrides, by key, such as from <see cref="GetRecommendedTuning"/></param>
    /// <param name="simDataChanges">Which changes to make to the SimData, to make it what each override's tuning needs</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    /// <returns>What was changed</returns>
    public Task<IReadOnlyList<TuningPackageUpdateChange>> WriteAsync(string path, IReadOnlyDictionary<ResourceKey, string?> tuning, TuningPackageSimDataChanges simDataChanges = TuningPackageSimDataChanges.All, CancellationToken cancellationToken = default) =>
        WriteCoreAsync(path, tuning, simDataChanges, [], cancellationToken);

    internal Task<IReadOnlyList<TuningPackageUpdateChange>> WriteCoreAsync(string path, IReadOnlyDictionary<ResourceKey, string?> tuning, TuningPackageSimDataChanges simDataChanges, IReadOnlyList<(TuningPackageAddedSimData item, TuningPackageAddedSimDataPlan plan)> addedSimData, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(tuning);
        if (string.Equals(System.IO.Path.GetFullPath(path), System.IO.Path.GetFullPath(PackagePath), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The updated package must be written somewhere other than the package itself", nameof(path));
        var byKey = Overrides.ToDictionary(item => item.Key);
        foreach (var key in tuning.Keys)
            if (!byKey.TryGetValue(key, out var item) || item.Outcome is TuningPackageOverrideOutcome.Unreadable)
                throw new ArgumentException($"{key} is not an override that can be replaced", nameof(tuning));
        return Task.Run(async () =>
        {
            var changes = new List<TuningPackageUpdateChange>();
            using var package = new DataBasePackedFile(PackagePath, true);
            foreach (var item in Overrides)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (item.Outcome is TuningPackageOverrideOutcome.Unreadable)
                    continue;
                var text = item.ModText;
                var removed = false;
                if (tuning.TryGetValue(item.Key, out var replacement))
                {
                    if (replacement is null)
                    {
                        package.Delete(item.Key);
                        removed = true;
                        changes.Add(new(TuningPackageUpdateChangeKind.TuningRemoved, item.Key, item.Name, "The override was removed."));
                    }
                    else
                    {
                        text = TuningXml.GetCanonicalText(TuningXml.Parse(replacement));
                        package.Set(item.Key, text, GetCompressionMode(package, item.Key));
                        changes.Add(new(TuningPackageUpdateChangeKind.TuningReplaced, item.Key, item.Name, "The override was replaced."));
                    }
                }
                if (simDataChanges is TuningPackageSimDataChanges.None || simData is null || item.GameEntry is null || text is null)
                    continue;
                SimDataPlan plan;
                lock (simData)
                    plan = PlanSimData(package, simData, item.GameEntry, removed ? item.GameText! : text);
                if (removed)
                {
                    if (simDataChanges.HasFlag(TuningPackageSimDataChanges.Remove) && plan.ModKey is { } orphan)
                    {
                        package.Delete(orphan);
                        changes.Add(new(TuningPackageUpdateChangeKind.SimDataRemoved, orphan, item.Name, "The override's SimData was removed with it."));
                    }
                    continue;
                }
                switch (plan.State)
                {
                    case TuningPackageSimDataState.Stale when simDataChanges.HasFlag(TuningPackageSimDataChanges.Replace):
                        package.Set(plan.ModKey!.Value, plan.Generated!, GetCompressionMode(package, plan.ModKey!.Value));
                        changes.Add(new(TuningPackageUpdateChangeKind.SimDataReplaced, plan.ModKey!.Value, item.Name, plan.Message));
                        break;
                    case TuningPackageSimDataState.Missing when simDataChanges.HasFlag(TuningPackageSimDataChanges.Add):
                        package.Set(plan.GameKey!.Value, plan.Generated!, CompressionMode.Auto);
                        changes.Add(new(TuningPackageUpdateChangeKind.SimDataAdded, plan.GameKey!.Value, item.Name, plan.Message));
                        break;
                    case TuningPackageSimDataState.Unneeded when simDataChanges.HasFlag(TuningPackageSimDataChanges.Remove):
                        package.Delete(plan.ModKey!.Value);
                        changes.Add(new(TuningPackageUpdateChangeKind.SimDataRemoved, plan.ModKey!.Value, item.Name, plan.Message));
                        break;
                    case TuningPackageSimDataState.Unverifiable when plan.ModKey is not null || tuning.ContainsKey(item.Key):
                        changes.Add(new(TuningPackageUpdateChangeKind.SimDataUnverifiable, plan.ModKey ?? plan.GameKey ?? item.Key, item.Name, plan.Message));
                        break;
                }
            }
            foreach (var (item, plan) in addedSimData)
            {
                if (plan.Tuning is { } amended)
                {
                    package.Set(item.TuningKey, amended, GetCompressionMode(package, item.TuningKey));
                    changes.Add(new(TuningPackageUpdateChangeKind.TuningAmended, item.TuningKey, item.Name, plan.Message));
                }
                if (plan.SimData is { } repaired)
                {
                    package.Set(item.Key, repaired, GetCompressionMode(package, item.Key));
                    changes.Add(new(TuningPackageUpdateChangeKind.SimDataRepaired, item.Key, item.Name, plan.Message));
                }
            }
            await package.SaveAsAsync(path, cancellationToken: cancellationToken).ConfigureAwait(false);
            return (IReadOnlyList<TuningPackageUpdateChange>)changes;
        }, cancellationToken);
    }
}