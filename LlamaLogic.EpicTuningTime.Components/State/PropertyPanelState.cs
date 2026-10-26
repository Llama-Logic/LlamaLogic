namespace LlamaLogic.EpicTuningTime.Components.State;

/// <summary>
/// Represents what a properties panel has expanded, what it is searching for, and how far it is scrolled
/// </summary>
public sealed class PropertyPanelState
{
    /// <summary>
    /// Gets the paths of the properties the panel has expanded
    /// </summary>
    public HashSet<string> ExpandedPaths { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets how far the panel's tree is scrolled
    /// </summary>
    public ScrollState Scroll { get; } = new();

    /// <summary>
    /// Gets or sets what the panel is searching for
    /// </summary>
    public string Search { get; set; } = string.Empty;

    /// <summary>
    /// Forgets everything
    /// </summary>
    public void Reset()
    {
        ExpandedPaths.Clear();
        Scroll.Key = null;
        Scroll.Top = 0;
        Search = string.Empty;
    }
}