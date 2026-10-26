namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents what writing a folder update did
/// </summary>
/// <param name="Packages">What was done for each package that had changes to make</param>
/// <param name="ReportPath">Where the report was written, if it was</param>
public sealed record TuningFolderUpdateResult(IReadOnlyList<TuningFolderUpdateWrite> Packages, string? ReportPath);