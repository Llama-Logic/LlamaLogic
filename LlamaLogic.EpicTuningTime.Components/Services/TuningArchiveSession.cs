namespace LlamaLogic.EpicTuningTime.Components.Services;

/// <summary>
/// Represents the archive a creator has open and the state the components share about it
/// </summary>
sealed class TuningArchiveSession :
    IDisposable
{
    const string recentArchivesSetting = "RecentArchives";

    public TuningArchiveSession(IEpicTuningTimePlatform platform) =>
        Platform = platform;

    readonly object cacheLock = new();
    readonly Dictionary<(long snapshotId, TuningArchiveResourceKind kind), Task<SnapshotEntries>> entries = [];
    readonly Dictionary<long, Task<IReadOnlyDictionary<uint, string>>> strings = [];
    readonly Dictionary<long, Task<TuningSimDataDrafter?>> simDataDrafters = [];
    readonly List<TuningDraft> drafts = [];

    /// <summary>
    /// Gets the open archive, if there is one
    /// </summary>
    public TuningArchiveFile? Archive { get; private set; }

    /// <summary>
    /// Occurs when the archive is closed, including before another is opened
    /// </summary>
    public event Action? ArchiveChanged;

    /// <summary>
    /// Occurs when the archive, its snapshots, or an operation's progress changes
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets the tuning descriptions imported into the archive
    /// </summary>
    public IReadOnlyList<TuningArchiveDescriptions> Descriptions { get; private set; } = [];

    /// <summary>
    /// Gets a number that changes whenever the archive or what is known about its contents changes (but not when only an operation's progress does)
    /// </summary>
    public int Generation { get; private set; }

    /// <summary>
    /// Gets the operation in progress, if there is one
    /// </summary>
    public TuningArchiveOperation? Operation { get; private set; }

    /// <summary>
    /// Gets the host of the components
    /// </summary>
    public IEpicTuningTimePlatform Platform { get; }

    /// <summary>
    /// Gets how many tunings have had their references indexed, and how many there are
    /// </summary>
    public (int indexed, int total) ReferenceStatus { get; private set; }

    /// <summary>
    /// Gets the snapshots in the archive
    /// </summary>
    public IReadOnlyList<TuningArchiveSnapshot> Snapshots { get; private set; } = [];

    /// <summary>
    /// Gets the names of the tuning types the archive has seen
    /// </summary>
    public IReadOnlyDictionary<ResourceType, string> TypeNames { get; private set; } = new Dictionary<ResourceType, string>();

    /// <summary>
    /// Gets the archives the creator opened most recently
    /// </summary>
    public IReadOnlyList<string> RecentArchives =>
        (Platform.GetSetting(recentArchivesSetting) ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);

    public void Close()
    {
        ClearCaches();
        drafts.Clear();
        Archive?.Dispose();
        Archive = null;
        Snapshots = [];
        Descriptions = [];
        TypeNames = new Dictionary<ResourceType, string>();
        ReferenceStatus = default;
        ++Generation;
        ArchiveChanged?.Invoke();
        NotifyChanged();
    }

    void ClearCaches()
    {
        lock (cacheLock)
        {
            entries.Clear();
            strings.Clear();
            simDataDrafters.Clear();
        }
    }

    public void Dispose() =>
        Archive?.Dispose();

    /// <summary>
    /// Gets the tunings the creator is drafting changes to
    /// </summary>
    public IReadOnlyList<TuningDraft> Drafts =>
        drafts;

    /// <summary>
    /// Stops drafting changes to the specified <paramref name="draft"/>'s tuning
    /// </summary>
    public void DiscardDraft(TuningDraft draft)
    {
        drafts.Remove(draft);
        NotifyChanged();
    }

    /// <summary>
    /// Gets the draft of the specified <paramref name="entry"/> in the specified <paramref name="snapshot"/>, starting one if there is none, or returns <see langword="null"/> if the archive has no tuning descriptions for the snapshot
    /// </summary>
    public async Task<TuningDraft?> GetDraftAsync(TuningArchiveSnapshot snapshot, TuningArchiveEntry entry)
    {
        var archive = Archive ?? throw new InvalidOperationException("No archive is open");
        if (drafts.FirstOrDefault(draft => draft.Snapshot.Id == snapshot.Id && draft.Entry.Key == entry.Key) is { } existing)
            return existing;
        if (archive.FindTuningDescriptions(snapshot) is not { } descriptions)
            return null;
        var draft = await Task.Run(() => new TuningDraft(snapshot, entry, archive.GetText(entry), archive.GetTuningDescriptionCache(descriptions)));
        drafts.Add(draft);
        NotifyChanged();
        return draft;
    }

    public string GetTypeName(ResourceType type) =>
        TypeNames.TryGetValue(type, out var name) ? name : Enum.IsDefined(type) ? type.ToString() : $"0x{(uint)type:X8}";

    public Task<SnapshotEntries> GetEntriesAsync(TuningArchiveSnapshot snapshot, TuningArchiveResourceKind kind = TuningArchiveResourceKind.Tuning)
    {
        var archive = Archive ?? throw new InvalidOperationException("No archive is open");
        lock (cacheLock)
        {
            if (!entries.TryGetValue((snapshot.Id, kind), out var task))
            {
                task = Task.Run(() => new SnapshotEntries(archive.GetEntries(snapshot, kind)));
                entries[(snapshot.Id, kind)] = task;
            }
            return task;
        }
    }

    /// <summary>
    /// Gets the media of the specified <paramref name="snapshot"/>, for resolving the references tuning makes to them
    /// </summary>
    public async Task<MediaEntries> GetMediaAsync(TuningArchiveSnapshot snapshot) =>
        new(snapshot, await GetEntriesAsync(snapshot, TuningArchiveResourceKind.Image).ConfigureAwait(false), await GetEntriesAsync(snapshot, TuningArchiveResourceKind.Sound).ConfigureAwait(false), await GetEntriesAsync(snapshot, TuningArchiveResourceKind.Video).ConfigureAwait(false));

    public Task<IReadOnlyDictionary<uint, string>> GetStringsAsync(TuningArchiveSnapshot snapshot)
    {
        var archive = Archive ?? throw new InvalidOperationException("No archive is open");
        lock (cacheLock)
        {
            if (!strings.TryGetValue(snapshot.Id, out var task))
            {
                task = Task.Run(() => archive.GetStrings(snapshot));
                strings[snapshot.Id] = task;
            }
            return task;
        }
    }

    /// <summary>
    /// Gets a drafter of the SimData that overrides of the specified <paramref name="snapshot"/>'s tuning need, or <see langword="null"/> if the snapshot has no SimData or no tuning descriptions
    /// </summary>
    public Task<TuningSimDataDrafter?> GetSimDataDrafterAsync(TuningArchiveSnapshot snapshot)
    {
        var archive = Archive ?? throw new InvalidOperationException("No archive is open");
        lock (cacheLock)
        {
            if (!simDataDrafters.TryGetValue(snapshot.Id, out var task))
            {
                task = Task.Run(() => archive.HasKind(snapshot, TuningArchiveResourceKind.SimData) && archive.FindTuningDescriptions(snapshot) is { } descriptions
                    ? TuningSimDataDrafter.Create(archive, snapshot, archive.GetTuningDescriptionCache(descriptions).Set)
                    : null);
                simDataDrafters[snapshot.Id] = task;
            }
            return task;
        }
    }

    public TuningArchiveSnapshot? GetSnapshot(long? id) =>
        id is { } snapshotId ? Snapshots.FirstOrDefault(snapshot => snapshot.Id == snapshotId) : null;

    void NotifyChanged() =>
        Changed?.Invoke();

    public async Task OpenAsync(string path)
    {
        Close();
        Archive = await Task.Run(() => TuningArchiveFile.Open(path)).ConfigureAwait(false);
        var recent = RecentArchives.Where(existing => !string.Equals(existing, Archive.Path, StringComparison.OrdinalIgnoreCase)).Prepend(Archive.Path).Take(8);
        Platform.SetSetting(recentArchivesSetting, string.Join('\n', recent));
        await RefreshAsync().ConfigureAwait(false);
    }

    public async Task RefreshAsync()
    {
        if (Archive is not { } archive)
            return;
        var (snapshots, descriptions, typeNames, referenceStatus) = await Task.Run(() => (archive.GetSnapshots(), archive.GetTuningDescriptions(), archive.GetTuningTypeNames(), archive.GetReferenceIndexStatus())).ConfigureAwait(false);
        Snapshots = snapshots;
        Descriptions = descriptions;
        TypeNames = typeNames;
        ReferenceStatus = referenceStatus;
        ++Generation;
        NotifyChanged();
    }

    public void ForgetRecentArchive(string path)
    {
        Platform.SetSetting(recentArchivesSetting, string.Join('\n', RecentArchives.Where(existing => !string.Equals(existing, path, StringComparison.OrdinalIgnoreCase))));
        NotifyChanged();
    }

    public async Task<T?> RunAsync<T>(string name, Func<IProgress<TuningArchiveProgress>, CancellationToken, Task<T>> operation, bool contentChanges = true)
    {
        if (Operation is not null)
            throw new InvalidOperationException("Another operation is in progress");
        using var cancellation = new CancellationTokenSource();
        var current = new TuningArchiveOperation(name, cancellation);
        Operation = current;
        NotifyChanged();
        var lastReport = DateTime.MinValue;
        var progress = new Progress<TuningArchiveProgress>(report =>
        {
            current.Report(report);
            if ((DateTime.UtcNow - lastReport).TotalMilliseconds > 150)
            {
                lastReport = DateTime.UtcNow;
                NotifyChanged();
            }
        });
        try
        {
            return await operation(progress, cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return default;
        }
        finally
        {
            Operation = null;
            if (contentChanges)
                ClearCaches();
            await RefreshAsync().ConfigureAwait(false);
        }
    }
}