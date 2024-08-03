namespace LlamaLogic.Packages;

/// <summary>
/// Represents a data resource value composed of two unsigned integers
/// </summary>
public class DataResourceVectorValue(uint offset, uint count)
{
    /// <summary>
    /// Gets/sets the second unsigned integer
    /// </summary>
    public uint Count { get; set; } = count;

    /// <summary>
    /// Gets/sets the first unsigned integer
    /// </summary>
    public uint Offset { get; set; } = offset;
}
