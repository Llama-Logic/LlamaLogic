namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents what writing an updated package did to a resource
/// </summary>
/// <param name="Kind">What was done</param>
/// <param name="Key">The key of the resource</param>
/// <param name="Name">The name of the override's tuning</param>
/// <param name="Message">What was done, for the creator</param>
public sealed record TuningPackageUpdateChange(TuningPackageUpdateChangeKind Kind, ResourceKey Key, string? Name, string Message);