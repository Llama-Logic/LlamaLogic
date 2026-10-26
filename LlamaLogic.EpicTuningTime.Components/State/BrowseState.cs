namespace LlamaLogic.EpicTuningTime.Components.State;

/// <summary>
/// Represents what Browse is showing, so that it can be shown again when the creator returns to it
/// </summary>
sealed class BrowseState
{
    public BrowseState(TuningArchiveSession session) =>
        session.ArchiveChanged += Reset;

    /// <summary>
    /// Gets the places to go back to, the most recent last
    /// </summary>
    public List<BrowseLocation> Back { get; } = [];

    /// <summary>
    /// Gets the places to go forward to, the most recent last
    /// </summary>
    public List<BrowseLocation> Forward { get; } = [];

    /// <summary>
    /// Gets or sets the kind of resource shown when strings are not
    /// </summary>
    public TuningArchiveResourceKind Kind { get; set; } = TuningArchiveResourceKind.Tuning;

    /// <summary>
    /// Gets how far the list of resources is scrolled
    /// </summary>
    public ScrollState List { get; } = new();

    /// <summary>
    /// Gets or sets a request from elsewhere that Browse has yet to act on
    /// </summary>
    public BrowseRequest? PendingRequest { get; set; }

    /// <summary>
    /// Gets or sets the type and instance of a resource whose reference in the selected tuning should be revealed, if any
    /// </summary>
    public (ResourceType type, ulong instance)? RevealReferenceTo { get; set; }

    /// <summary>
    /// Gets or sets what the list is filtered by
    /// </summary>
    public string Search { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type and instance of the selected resource, if any
    /// </summary>
    public (ResourceType type, ulong instance)? Selected { get; set; }

    /// <summary>
    /// Gets or sets the key of the selected string, if any
    /// </summary>
    public uint? SelectedStringKey { get; set; }

    /// <summary>
    /// Gets or sets whether strings are shown rather than resources
    /// </summary>
    public bool ShowingStrings { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the snapshot shown, if any
    /// </summary>
    public long? SnapshotId { get; set; }

    /// <summary>
    /// Gets how far the list of strings is scrolled
    /// </summary>
    public ScrollState StringList { get; } = new();

    /// <summary>
    /// Gets how far the Used by list of the selected string is scrolled
    /// </summary>
    public ScrollState StringReferrers { get; } = new();

    /// <summary>
    /// Gets where the editor showing the selected SimData or Python script was scrolled to
    /// </summary>
    public EditorViewState Text { get; } = new();

    /// <summary>
    /// Gets or sets the type of tuning the list is filtered to, if any
    /// </summary>
    public ResourceType? TypeFilter { get; set; }

    /// <summary>
    /// Gets the state of the tuning viewer
    /// </summary>
    public TuningViewerState Viewer { get; } = new();

    /// <summary>
    /// Asks Browse to show the specified <paramref name="target"/> in the snapshot with the specified <paramref name="snapshotId"/> (or just the snapshot), revealing its reference to the specified resource
    /// </summary>
    public void Request(long snapshotId, (ResourceType type, ulong instance)? target = null, (ResourceType type, ulong instance)? revealReferenceTo = null) =>
        PendingRequest = new(snapshotId, target, revealReferenceTo);

    /// <summary>
    /// Forgets everything
    /// </summary>
    public void Reset()
    {
        Back.Clear();
        Forward.Clear();
        Kind = TuningArchiveResourceKind.Tuning;
        List.Key = null;
        List.Top = 0;
        PendingRequest = null;
        RevealReferenceTo = null;
        Search = string.Empty;
        Selected = null;
        SelectedStringKey = null;
        ShowingStrings = false;
        SnapshotId = null;
        StringList.Key = null;
        StringList.Top = 0;
        StringReferrers.Key = null;
        StringReferrers.Top = 0;
        Text.Key = null;
        Text.ViewState = null;
        TypeFilter = null;
        Viewer.Reset();
    }
}