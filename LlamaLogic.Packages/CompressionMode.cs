namespace LlamaLogic.Packages;

/// <summary>
/// Represents the compression mode to use when setting the content of a resource in a <see cref="DataBasePackedFile"/>
/// </summary>
/// <remarks>
/// *Important*: While this library does support the older compression methods, it does so for decompression only.
/// When it does compress resources, it always uses the newer ZLib compression method.
///
/// **Warning**: The Sims 4 does not always expect or tolerate ZLib compression for certain types of resources, such as for world package files which ship with the game
/// (see [thepancake1's comment about this on the Creator Musings Discord](https://discord.com/channels/605863047654801428/621760667581677568/1274623417391517717) for more information).
/// </remarks>
public enum CompressionMode
{
    /// <summary>
    /// The library will choose whether to compress the data or not
    /// </summary>
    Auto,

    /// <summary>
    /// The data will not be compressed
    /// </summary>
    ForceOff,

    /// <summary>
    /// The data will be compressed
    /// </summary>
    ForceOn,

    /// <summary>
    /// ⚠️ The data will not be processed for compression by the library—thus callers will need to invoke <see cref="DataBasePackedFile.ZLibCompress(ReadOnlyMemory{byte})"/> or <see cref="DataBasePackedFile.ZLibCompressAsync(ReadOnlyMemory{byte}, CancellationToken)"/> themselves and pass the result to <see cref="DataBasePackedFile.Set(ResourceKey, ReadOnlyMemory{byte}, CompressionMode)"/> or <see cref="DataBasePackedFile.SetAsync(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, CancellationToken)"/>, respectively, if they intend for the content to be compressed with ZLib despite being flagged—and the deleted flag will be set (use with caution)
    /// </summary>
    SetDeletedFlag,

    /// <summary>
    /// ⚠️ The data will not be processed for compression by the library and the resource will be marked as having been compressed using Maxis' internal method (use with caution)
    /// </summary>
    CallerSuppliedInternal,

    /// <summary>
    /// ⚠️ The data will not be processed for compression by the library and the resource will be marked as having been compressed using Maxis' streamable method (use with caution)
    /// </summary>
    CallerSuppliedStreamable
}
