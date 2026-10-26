namespace LlamaLogic.Packages.Formats;

/// <summary>
/// Represents a record in a <see cref="RecordBundle"/>
/// </summary>
public sealed class RecordBundleRecord(ResourceKey key, ReadOnlyMemory<byte> data)
{
    /// <summary>
    /// Gets the content of the record
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; } = data;

    /// <summary>
    /// Gets the key of the record
    /// </summary>
    public ResourceKey Key { get; } = key;
}
