namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents a version of a resource and the snapshots that contain it
/// </summary>
/// <param name="Entry">The version of the resource</param>
/// <param name="Snapshots">The snapshots containing this version, in order</param>
public sealed record TuningArchiveHistoryItem(TuningArchiveEntry Entry, IReadOnlyList<TuningArchiveSnapshot> Snapshots);