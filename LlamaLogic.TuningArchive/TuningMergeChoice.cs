namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies which side of a merge conflict a merge takes
/// </summary>
public enum TuningMergeChoice
{
    /// <summary>
    /// The merge takes the game's new version
    /// </summary>
    Game,

    /// <summary>
    /// The merge takes the mod's version
    /// </summary>
    Mod
}