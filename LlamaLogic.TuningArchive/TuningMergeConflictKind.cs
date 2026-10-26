namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies why a merge could not combine the mod's change with the game's
/// </summary>
public enum TuningMergeConflictKind
{
    /// <summary>
    /// The mod and the game both changed the element, differently
    /// </summary>
    BothChanged,

    /// <summary>
    /// The mod removed the element, and the game changed it
    /// </summary>
    ModRemovedGameChanged,

    /// <summary>
    /// The game removed the element, and the mod changed it
    /// </summary>
    GameRemovedModChanged,

    /// <summary>
    /// The list has too many items for its items to be aligned
    /// </summary>
    TooLarge
}