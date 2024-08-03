namespace LlamaLogic.Packages;

/// <summary>
/// Represents a data resource value composed of four floating point numbers
/// </summary>
public class DataResourceFloatingPoint4Value(float x, float y, float z, float w)
{
    /// <summary>
    /// Gets/sets the first floating point number
    /// </summary>
    public float W { get; set; } = w;

    /// <summary>
    /// Gets/sets the second floating point number
    /// </summary>
    public float X { get; set; } = x;

    /// <summary>
    /// Gets/sets the third floating point number
    /// </summary>
    public float Y { get; set; } = y;

    /// <summary>
    /// Gets/sets the fourth floating point number
    /// </summary>
    public float Z { get; set; } = z;
}
