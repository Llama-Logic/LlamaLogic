namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies what writing an updated package did to a resource
/// </summary>
public enum TuningPackageUpdateChangeKind
{
    /// <summary>
    /// The override's tuning was replaced
    /// </summary>
    TuningReplaced,

    /// <summary>
    /// The override was removed
    /// </summary>
    TuningRemoved,

    /// <summary>
    /// The override's SimData was replaced with generated SimData
    /// </summary>
    SimDataReplaced,

    /// <summary>
    /// Generated SimData was added for the override
    /// </summary>
    SimDataAdded,

    /// <summary>
    /// The override's SimData was removed, since the game's is what the tuning needs
    /// </summary>
    SimDataRemoved,

    /// <summary>
    /// The override's SimData was left as it was, since SimData that could be trusted could not be generated
    /// </summary>
    SimDataUnverifiable,

    /// <summary>
    /// The SimData of tuning the package adds was fixed
    /// </summary>
    SimDataRepaired,

    /// <summary>
    /// Tuning the package adds was given values its SimData has that it left out
    /// </summary>
    TuningAmended
}