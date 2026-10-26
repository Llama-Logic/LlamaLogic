namespace LlamaLogic.EpicTuningTime.Components.State;

/// <summary>
/// Represents what a tuning viewer is showing of the tuning it shows
/// </summary>
public sealed class TuningViewerState
{
    /// <summary>
    /// Gets or sets the index of the tab the viewer shows
    /// </summary>
    public int ActiveTab { get; set; }

    /// <summary>
    /// Gets where the tuning's editor was scrolled to
    /// </summary>
    public EditorViewState Editor { get; } = new();

    /// <summary>
    /// Gets or sets what identifies the tuning the rest of this state belongs to
    /// </summary>
    public string? EntryKey { get; set; }

    /// <summary>
    /// Gets where the History tab's diff was scrolled to
    /// </summary>
    public EditorViewState HistoryDiff { get; } = new();

    /// <summary>
    /// Gets or sets the index of the version the History tab compares with the one before it, if any
    /// </summary>
    public int? HistoryIndex { get; set; }

    /// <summary>
    /// Gets the state of the properties panel
    /// </summary>
    public PropertyPanelState Panel { get; } = new();

    /// <summary>
    /// Gets how far the Referenced by list is scrolled
    /// </summary>
    public ScrollState Referrers { get; } = new();

    /// <summary>
    /// Gets or sets what identifies the reference the viewer last revealed, so that it is revealed only once
    /// </summary>
    public string? RevealedKey { get; set; }

    /// <summary>
    /// Gets or sets the path of the property the viewer has selected, if any
    /// </summary>
    public string? SelectedPropertyPath { get; set; }

    /// <summary>
    /// Forgets everything
    /// </summary>
    public void Reset()
    {
        ActiveTab = 0;
        EntryKey = null;
        HistoryIndex = null;
        RevealedKey = null;
        SelectedPropertyPath = null;
        Editor.Key = null;
        Editor.ViewState = null;
        HistoryDiff.Key = null;
        HistoryDiff.ViewState = null;
        Referrers.Key = null;
        Referrers.Top = 0;
        Panel.Reset();
    }
}