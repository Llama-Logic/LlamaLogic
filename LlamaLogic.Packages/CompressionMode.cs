namespace LlamaLogic.Packages;

/// <summary>
/// Represents the compression mode to use when setting the content of a resource in a <see cref="DataBasePackedFile"/>
/// </summary>
/// <remarks>
/// *Important*: While this library does support the older compression methods, it does not have a complete catalog of how to do so properly for every resource type automatically (<see cref="Auto"/>).
/// If you want to be sure that you're compressing a resource correctly, utilize <see cref="DataBasePackedFile.GetExplicitCompressionMode(ResourceKey)"/> with a package the game is currently accepting.
///
/// **Warning**: The Sims 4 does not always expect or tolerate ZLib compression for certain types of resources, such as for world package files which ship with the game
/// (see [thepancake1's comment about this on the Creator Musings Discord](https://discord.com/channels/605863047654801428/621760667581677568/1274623417391517717) for more information).
/// </remarks>
public enum CompressionMode
{
    /// <summary>
    /// The library will choose whether and how to compress the data or not
    /// </summary>
    Auto,

    /// <summary>
    /// The data will not be compressed
    /// </summary>
    ForceOff,

    /// <summary>
    /// Deprecated: do not use
    /// </summary>
    [Obsolete("This setting can no longer be used and will be removed in a future version. If you need to force a compression, use a different force member of this enum.", true)]
    ForceOn,

    /// <summary>
    /// The data will be compressed using ZLib
    /// </summary>
    ForceZLib,

    /// <summary>
    /// The data will be compressed using Maxis' internal method
    /// </summary>
    ForceInternal,

    /// <summary>
    /// ⚠️ The data will not be processed for compression by the library—thus callers will need to invoke <see cref="DataBasePackedFile.ZLibCompress(ReadOnlyMemory{byte})"/> or <see cref="DataBasePackedFile.ZLibCompressAsync(ReadOnlyMemory{byte}, CancellationToken)"/> themselves and pass the result to <see cref="DataBasePackedFile.Set(ResourceKey, ReadOnlyMemory{byte}, CompressionMode)"/> or <see cref="DataBasePackedFile.SetAsync(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, CancellationToken)"/>, respectively, if they intend for the content to be compressed with ZLib despite being flagged—and the deleted flag will be set (use with caution)
    /// </summary>
    SetDeletedFlag,

    /// <summary>
    /// Deprecated: do not use
    /// </summary>
    [Obsolete($"This setting can no longer be used and will be removed in a future version. Supply the uncompressed data and {nameof(Auto)} or {nameof(ForceInternal)}.", true)]
    CallerSuppliedInternal,

    /// <summary>
    /// ⚠️ The data will not be processed for compression by the library and the resource will be marked as having been compressed using Maxis' streamable method (use with caution)
    /// </summary>
    CallerSuppliedStreamable
}
