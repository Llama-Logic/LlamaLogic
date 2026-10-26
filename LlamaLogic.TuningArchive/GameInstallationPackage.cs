namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents a package file of a <see cref="GameInstallationPack"/> and the priority the game loads it with
/// </summary>
/// <param name="Path">The path of the package file</param>
/// <param name="Priority">The priority the game's resource configuration assigns the package; when packages contain the same resource, the one with the greater priority prevails</param>
/// <param name="IsPatch">Whether the package is in the pack's folder under <c>Delta</c>, which patches the files installed with the pack</param>
public sealed record GameInstallationPackage(string Path, int Priority, bool IsPatch = false);