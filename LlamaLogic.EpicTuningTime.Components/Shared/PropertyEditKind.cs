namespace LlamaLogic.EpicTuningTime.Components.Shared;

/// <summary>
/// Specifies an edit a creator can ask for in the property tree
/// </summary>
public enum PropertyEditKind
{
    /// <summary>
    /// Set the node's value
    /// </summary>
    SetValue,

    /// <summary>
    /// Set a tunable the tuning does not set, starting from its default
    /// </summary>
    Set,

    /// <summary>
    /// Remove the tunable, so it takes its default
    /// </summary>
    Reset,

    /// <summary>
    /// Add a copy of the list item after it
    /// </summary>
    Duplicate,

    /// <summary>
    /// Remove the list item
    /// </summary>
    Remove,

    /// <summary>
    /// Restore what the original tuning has
    /// </summary>
    Revert,

    /// <summary>
    /// Add a new item, with its tunables' defaults, to the end of the list
    /// </summary>
    AddItem,

    /// <summary>
    /// Choose another option of the variant
    /// </summary>
    SetOption
}