namespace LlamaLogic.EpicTuningTime.Components.State;

/// <summary>
/// Represents what Drafts is showing, so that it can be shown again when the creator returns to it
/// </summary>
sealed class DraftsState
{
    public DraftsState(TuningArchiveSession session) =>
        session.ArchiveChanged += Reset;

    /// <summary>
    /// Gets or sets the index of the tab shown
    /// </summary>
    public int ActiveTab { get; set; }

    /// <summary>
    /// Gets where the diff of the selected draft was scrolled to
    /// </summary>
    public EditorViewState Diff { get; } = new();

    /// <summary>
    /// Gets the state of the properties panel
    /// </summary>
    public PropertyPanelState Panel { get; } = new();

    /// <summary>
    /// Gets or sets the draft shown, if any
    /// </summary>
    public TuningDraft? Selected { get; set; }

    /// <summary>
    /// Gets or sets the path of the selected property, if any
    /// </summary>
    public string? SelectedPropertyPath { get; set; }

    /// <summary>
    /// Forgets everything
    /// </summary>
    public void Reset()
    {
        ActiveTab = 0;
        Diff.Key = null;
        Diff.ViewState = null;
        Panel.Reset();
        Selected = null;
        SelectedPropertyPath = null;
    }
}