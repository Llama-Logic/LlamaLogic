namespace LlamaLogic.EpicTuningTime.Components.State;

/// <summary>
/// Represents a request from elsewhere for Browse to show something
/// </summary>
/// <param name="SnapshotId">The snapshot to show</param>
/// <param name="Target">The type and instance of the tuning to show, if any</param>
/// <param name="RevealReferenceTo">The type and instance of a resource whose reference in the tuning should be revealed, if any</param>
public sealed record BrowseRequest(long SnapshotId, (ResourceType type, ulong instance)? Target = null, (ResourceType type, ulong instance)? RevealReferenceTo = null);