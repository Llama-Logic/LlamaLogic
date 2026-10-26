namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies what a game version means for a mod's override of a tuning
/// </summary>
public enum TuningPackageOverrideOutcome
{
    /// <summary>
    /// The game has not changed the tuning since the version the mod was made from, so the override needs nothing
    /// </summary>
    UpToDate,

    /// <summary>
    /// The game changed the tuning, and the mod's changes merge onto the game's new version without conflicts
    /// </summary>
    Merged,

    /// <summary>
    /// The game changed the tuning, and some of the mod's changes conflict with the game's
    /// </summary>
    Conflicted,

    /// <summary>
    /// The override is the same as the game's new version, so it does nothing
    /// </summary>
    MatchesGame,

    /// <summary>
    /// The override is the same as an older version of the game's, so all it does is undo the game's changes since then
    /// </summary>
    MatchesOlderGame,

    /// <summary>
    /// The game's new version no longer has the tuning
    /// </summary>
    RemovedFromGame,

    /// <summary>
    /// The override could not be read as tuning
    /// </summary>
    Unreadable
}