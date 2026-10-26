namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents a package in a folder being updated
/// </summary>
/// <param name="RelativePath">The path of the package within the folder</param>
/// <param name="Update">What the game version means for the package's overrides, if it could be read</param>
/// <param name="Problem">Why the package couldn't be read, if it couldn't</param>
/// <param name="AddedSimData">The SimData of the tuning the package adds that isn't what the tuning descriptions give, if it was checked</param>
public sealed record TuningFolderUpdatePackage(string RelativePath, TuningPackageUpdate? Update, string? Problem, IReadOnlyList<TuningPackageAddedSimData> AddedSimData);