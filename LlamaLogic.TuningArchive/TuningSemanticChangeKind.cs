namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies how an element differs between two versions of a tuning document
/// </summary>
public enum TuningSemanticChangeKind
{
    /// <summary>
    /// The element is only in the newer version
    /// </summary>
    Added = 0,

    /// <summary>
    /// The element is only in the older version
    /// </summary>
    Removed,

    /// <summary>
    /// The element's value (or, for a variant, its option) differs
    /// </summary>
    Changed
}