namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies what is wrong with the SimData of tuning a package adds, by the tuning descriptions of the game version it is being updated for
/// </summary>
[Flags]
public enum TuningPackageAddedSimDataProblems
{
    /// <summary>
    /// Nothing is wrong
    /// </summary>
    None = 0,

    /// <summary>
    /// The tuning descriptions give the SimData columns it doesn't have
    /// </summary>
    MissingColumns = 1,

    /// <summary>
    /// Some of its columns have other types than the tuning descriptions give
    /// </summary>
    MistypedColumns = 2,

    /// <summary>
    /// It has columns the tuning descriptions don't give
    /// </summary>
    ExtraColumns = 4,

    /// <summary>
    /// Some of its schema hashes don't match their columns
    /// </summary>
    MismatchedHashes = 8,

    /// <summary>
    /// Some of its values aren't what its tuning makes
    /// </summary>
    Values = 16,

    /// <summary>
    /// It couldn't be checked, because it couldn't be read or SimData couldn't be generated from its tuning
    /// </summary>
    Unchecked = 32
}