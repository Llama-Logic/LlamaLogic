namespace LlamaLogic.EpicTuningTime.Components.State;

/// <summary>
/// Represents what Compare is showing, so that it can be shown again when the creator returns to it
/// </summary>
sealed class CompareState
{
    public CompareState(TuningArchiveSession session) =>
        session.ArchiveChanged += Reset;

    static readonly IReadOnlyCollection<TuningArchiveChangeKind> defaultKinds = [TuningArchiveChangeKind.Modified, TuningArchiveChangeKind.Added, TuningArchiveChangeKind.Removed];

    /// <summary>
    /// Gets where the diff was scrolled to
    /// </summary>
    public EditorViewState Diff { get; } = new();

    /// <summary>
    /// Gets or sets the kinds of change shown
    /// </summary>
    public IReadOnlyCollection<TuningArchiveChangeKind> Kinds { get; set; } = defaultKinds;

    /// <summary>
    /// Gets how far the list of changes is scrolled
    /// </summary>
    public ScrollState List { get; } = new();

    /// <summary>
    /// Gets or sets what is compared: <c>tuning</c>, <c>simdata</c>, <c>strings</c> or <c>python</c>
    /// </summary>
    public string Mode { get; set; } = "tuning";

    /// <summary>
    /// Gets or sets the identifier of the newer snapshot, if any
    /// </summary>
    public long? NewerId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the older snapshot, if any
    /// </summary>
    public long? OlderId { get; set; }

    /// <summary>
    /// Gets or sets what the list is filtered by
    /// </summary>
    public string Search { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the kind and key of the selected change, if any
    /// </summary>
    public (TuningArchiveChangeKind kind, ResourceKey key)? Selected { get; set; }

    /// <summary>
    /// Gets or sets the key of the selected string change, if any
    /// </summary>
    public uint? SelectedStringKey { get; set; }

    /// <summary>
    /// Gets or sets the type of tuning the list is filtered to, if any
    /// </summary>
    public ResourceType? TypeFilter { get; set; }

    /// <summary>
    /// Asks Compare to compare the snapshots with the specified identifiers
    /// </summary>
    public void Request(long olderId, long newerId)
    {
        OlderId = olderId;
        NewerId = newerId;
        Selected = null;
        SelectedStringKey = null;
    }

    /// <summary>
    /// Forgets everything
    /// </summary>
    public void Reset()
    {
        Diff.Key = null;
        Diff.ViewState = null;
        Kinds = defaultKinds;
        List.Key = null;
        List.Top = 0;
        Mode = "tuning";
        NewerId = null;
        OlderId = null;
        Search = string.Empty;
        Selected = null;
        SelectedStringKey = null;
        TypeFilter = null;
    }
}