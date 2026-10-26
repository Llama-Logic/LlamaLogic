namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies how a resource differs, if at all, between two snapshots
/// </summary>
public enum TuningArchiveChangeKind
{
    /// <summary>
    /// The resource is only in the newer snapshot
    /// </summary>
    Added = 0,

    /// <summary>
    /// The resource is only in the older snapshot
    /// </summary>
    Removed = 1,

    /// <summary>
    /// The resource's content differs between the snapshots
    /// </summary>
    Modified = 2,

    /// <summary>
    /// The resource's content is the same, but it moved to another pack or was renamed
    /// </summary>
    Moved = 3,

    /// <summary>
    /// The resource is the same in both snapshots
    /// </summary>
    Unchanged = 4
}