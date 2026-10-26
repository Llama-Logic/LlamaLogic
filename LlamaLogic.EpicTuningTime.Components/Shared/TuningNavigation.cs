namespace LlamaLogic.EpicTuningTime.Components.Shared;

/// <summary>
/// Represents a request to show a tuning, optionally revealing where it refers to another
/// </summary>
/// <param name="Type">The type of the tuning to show</param>
/// <param name="Instance">The instance of the tuning to show</param>
/// <param name="RevealReferenceTo">The type and instance of a tuning whose reference should be revealed, if any</param>
/// <param name="SnapshotId">The identifier of the snapshot to show the tuning in, if not the one shown</param>
public sealed record TuningNavigation(ResourceType Type, ulong Instance, (ResourceType type, ulong instance)? RevealReferenceTo = null, long? SnapshotId = null);