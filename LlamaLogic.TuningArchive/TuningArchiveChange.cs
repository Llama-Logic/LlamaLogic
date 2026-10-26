namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents how a resource differs, if at all, between two snapshots
/// </summary>
/// <param name="Kind">How the resource differs</param>
/// <param name="Old">The version of the resource in the older snapshot, if it was there</param>
/// <param name="New">The version of the resource in the newer snapshot, if it is there</param>
public sealed record TuningArchiveChange(TuningArchiveChangeKind Kind, TuningArchiveEntry? Old, TuningArchiveEntry? New)
{
    /// <summary>
    /// Gets the newer version of the resource if there is one, otherwise the older one
    /// </summary>
    public TuningArchiveEntry Entry =>
        New ?? Old!;
}