namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies which fixes writing an updated package makes to the SimData of tuning it adds, and to that tuning
/// </summary>
/// <remarks>
/// A value is only ever taken from SimData generated from the mod's tuning when the generator reproduces that column in every SimData of the game's that has it.
/// The SimData is changed only when, after the fixes, it has every column the tuning descriptions give, with their types.
/// </remarks>
[Flags]
public enum TuningPackageAddedSimDataFixes
{
    /// <summary>
    /// The SimData is left as it is
    /// </summary>
    None = 0,

    /// <summary>
    /// The columns the tuning descriptions give but the SimData doesn't have are added, with values generated from the tuning, keeping every value the SimData has
    /// </summary>
    AddMissingColumns = 1,

    /// <summary>
    /// Columns whose types aren't what the tuning descriptions give get those types, with values generated from the tuning
    /// </summary>
    CorrectColumnTypes = 2,

    /// <summary>
    /// Columns the tuning descriptions don't give are removed, unless the package has a script mod beside it, which could add those tunables to the game
    /// </summary>
    RemoveExtraColumns = 4,

    /// <summary>
    /// Schema hashes that don't match their columns are recomputed
    /// </summary>
    CorrectSchemaHashes = 8,

    /// <summary>
    /// The SimData is replaced with SimData generated from the tuning, when every column of it can be generated reliably
    /// </summary>
    MatchTuning = 16,

    /// <summary>
    /// Tunables the tuning leaves out, but the SimData has other values for than their defaults, are added to the tuning with those values, when SimData generated from the amended tuning has them and nothing else changes
    /// </summary>
    AmendTuning = 32,

    /// <summary>
    /// All of the fixes
    /// </summary>
    All = AddMissingColumns | CorrectColumnTypes | RemoveExtraColumns | CorrectSchemaHashes | MatchTuning | AmendTuning
}