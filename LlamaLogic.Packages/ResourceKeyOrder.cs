namespace LlamaLogic.Packages;

/// <summary>
/// Specifies the order in which a <see cref="ResourceKey"/> sequence should be processed
/// </summary>
public enum ResourceKeyOrder
{
    /// <summary>
    /// The operation will process the <see cref="ResourceKey"/> sequence in the order in which it was originally encountered
    /// </summary>
    Preserve,

    /// <summary>
    /// The operation will process the <see cref="ResourceKey"/> sequence in the order <see cref="ResourceKey.FullInstance"/>, then <see cref="ResourceKey.Type"/>, then <see cref="ResourceKey.Group"/>
    /// </summary>
    InstanceTypeGroup,

    /// <summary>
    /// The operation will process the <see cref="ResourceKey"/> sequence in the order <see cref="ResourceKey.Type"/>, then <see cref="ResourceKey.Group"/>, then <see cref="ResourceKey.FullInstance"/>
    /// </summary>
    TypeGroupInstance
}
