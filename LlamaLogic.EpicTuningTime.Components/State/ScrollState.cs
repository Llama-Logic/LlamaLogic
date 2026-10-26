namespace LlamaLogic.EpicTuningTime.Components.State;

/// <summary>
/// Represents how far a list was scrolled when it last showed particular contents
/// </summary>
public sealed class ScrollState
{
    /// <summary>
    /// Gets or sets what identifies the contents the scroll position belongs to
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets how far the list was scrolled, in pixels
    /// </summary>
    public double Top { get; set; }
}