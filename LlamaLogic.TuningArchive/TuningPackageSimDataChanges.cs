namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies which changes writing an updated package makes to its SimData
/// </summary>
[Flags]
public enum TuningPackageSimDataChanges
{
    /// <summary>
    /// The SimData is left as it is
    /// </summary>
    None = 0,

    /// <summary>
    /// SimData that isn't what its tuning needs is replaced with generated SimData that is (<see cref="TuningPackageSimDataState.Stale"/>)
    /// </summary>
    Replace = 1,

    /// <summary>
    /// SimData the game's own covers is removed (<see cref="TuningPackageSimDataState.Unneeded"/>), as is the SimData of a removed override
    /// </summary>
    Remove = 2,

    /// <summary>
    /// Generated SimData is added for tuning whose changes reach SimData but has none (<see cref="TuningPackageSimDataState.Missing"/>), which makes those changes take effect in the game for the first time
    /// </summary>
    Add = 4,

    /// <summary>
    /// All of the changes
    /// </summary>
    All = Replace | Remove | Add
}