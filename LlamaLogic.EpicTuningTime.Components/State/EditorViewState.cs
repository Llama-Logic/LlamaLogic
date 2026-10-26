namespace LlamaLogic.EpicTuningTime.Components.State;

/// <summary>
/// Represents where an editor was scrolled to, and what it had selected, when it last showed a particular text
/// </summary>
public sealed class EditorViewState
{
    /// <summary>
    /// Gets or sets what identifies the text the view state belongs to
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the editor's view state, as Monaco serializes it
    /// </summary>
    public string? ViewState { get; set; }
}