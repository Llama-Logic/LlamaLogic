namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents what writing a folder update did for one of its packages
/// </summary>
/// <param name="RelativePath">The path of the package within the folder</param>
/// <param name="WrittenPath">Where its updated copy was written, if one was</param>
/// <param name="Changes">What was changed</param>
/// <param name="Skipped">Why no copy was written, if one was planned but not written</param>
public sealed record TuningFolderUpdateWrite(string RelativePath, string? WrittenPath, IReadOnlyList<TuningPackageUpdateChange> Changes, string? Skipped);