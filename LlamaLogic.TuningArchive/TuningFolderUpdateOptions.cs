namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents which changes a folder update makes beyond the merges that need no judgment from the creator
/// </summary>
/// <param name="RemoveOverridesThatDoNothing">Whether overrides the same as the game's new version are removed</param>
/// <param name="SimDataChanges">Which changes are made to the SimData of overrides</param>
/// <param name="AddedSimDataFixes">Which fixes are made to the SimData of tuning the packages add</param>
public sealed record TuningFolderUpdateOptions(bool RemoveOverridesThatDoNothing = false, TuningPackageSimDataChanges SimDataChanges = TuningPackageSimDataChanges.Replace | TuningPackageSimDataChanges.Remove, TuningPackageAddedSimDataFixes AddedSimDataFixes = TuningPackageAddedSimDataFixes.AddMissingColumns);