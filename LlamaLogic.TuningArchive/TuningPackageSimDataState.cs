namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies whether a mod's SimData for an override is what the override's tuning needs in the game's new version
/// </summary>
public enum TuningPackageSimDataState
{
    /// <summary>
    /// SimData was not checked
    /// </summary>
    NotChecked,

    /// <summary>
    /// The mod's SimData (or the game's, if the mod has none) is what the tuning needs
    /// </summary>
    Current,

    /// <summary>
    /// The mod's SimData is not what the tuning needs, and SimData that is was generated
    /// </summary>
    Stale,

    /// <summary>
    /// The mod has no SimData, but the tuning's changes reach SimData, and SimData that has them was generated
    /// </summary>
    Missing,

    /// <summary>
    /// The mod has SimData, but the tuning's changes don't reach SimData, so the game's SimData is what the tuning needs
    /// </summary>
    Unneeded,

    /// <summary>
    /// The tuning's changes may reach SimData, but SimData that could be trusted could not be generated
    /// </summary>
    Unverifiable
}