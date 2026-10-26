namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents a row of a folder update's report: an override, the SimData of a tuning a package adds, or a package that couldn't be read
/// </summary>
/// <param name="Package">The path of the package within the folder</param>
/// <param name="Override">The override, if the row is for one</param>
/// <param name="AddedSimData">The SimData of a tuning the package adds, if the row is for one</param>
/// <param name="Tuning">The name of the tuning</param>
/// <param name="Type">The name of the tuning's type</param>
/// <param name="Instance">The tuning's instance, in decimal</param>
/// <param name="Outcome">What the game version means for the override, for the creator</param>
/// <param name="NeedsCreator">Whether the override or SimData needs the creator's judgment</param>
/// <param name="Done">What writing the updated copies did to the override or SimData, if they were written</param>
/// <param name="Base">The game version the override was made from</param>
/// <param name="BaseSource"><c>declared</c> or <c>inferred</c></param>
/// <param name="BaseNote">Why the base may not be what the override was made from, if it may not be</param>
/// <param name="Conflicts">How many of the mod's changes conflict with the game's</param>
/// <param name="SimData">Whether the override's SimData is what its tuning needs, if it was checked, or what is wrong with the SimData of a tuning the package adds</param>
/// <param name="Message">What the outcome means, or what is wrong with the SimData of a tuning the package adds, for the creator</param>
/// <param name="SimDataMessage">What the SimData state means, if it isn't current, or what the update does to the SimData of a tuning the package adds</param>
public sealed record TuningFolderUpdateReportRow(string Package, TuningPackageOverride? Override, TuningPackageAddedSimData? AddedSimData, string Tuning, string Type, string Instance, string Outcome, bool NeedsCreator, string Done, string Base, string BaseSource, string BaseNote, int Conflicts, string SimData, string Message, string SimDataMessage);