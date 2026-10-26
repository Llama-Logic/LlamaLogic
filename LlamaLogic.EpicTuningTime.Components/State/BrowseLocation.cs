namespace LlamaLogic.EpicTuningTime.Components.State;

/// <summary>
/// Represents a place Browse has been, for going back and forward
/// </summary>
/// <param name="SnapshotId">The snapshot</param>
/// <param name="Kind">The kind of resource</param>
/// <param name="Type">The type of the resource</param>
/// <param name="Instance">The instance of the resource</param>
public sealed record BrowseLocation(long SnapshotId, TuningArchiveResourceKind Kind, ResourceType Type, ulong Instance);