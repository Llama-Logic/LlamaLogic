namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents a reference from a tuning to another tuning or resource
/// </summary>
/// <param name="Source">The tuning containing the reference</param>
/// <param name="TargetType">The type of the resource referred to</param>
/// <param name="TargetInstance">The instance of the resource referred to</param>
public sealed record TuningArchiveReference(TuningArchiveEntry Source, ResourceType TargetType, ulong TargetInstance);