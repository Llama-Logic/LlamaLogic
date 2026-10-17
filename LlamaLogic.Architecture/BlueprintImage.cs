namespace LlamaLogic.Architecture;

/// <summary>
/// Represents an image stored in a <see cref="Blueprint"/> (🔓)
/// </summary>
/// <remarks>
/// Every image observed is a JPEG.
/// In every blueprint observed, the low 56 bits of <see cref="Id"/> are a counter unique within the blueprint and its high byte is 0x10, 0x30 or 0x50; what the id identifies has not been established.
/// </remarks>
public sealed class BlueprintImage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlueprintImage"/> class
    /// </summary>
    public BlueprintImage(ulong id, ReadOnlyMemory<byte> data)
    {
        Id = id;
        Data = data;
    }

    /// <summary>
    /// Gets/sets the content of the image
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; set; }

    /// <summary>
    /// Gets/sets the id of the image
    /// </summary>
    public ulong Id { get; set; }
}
