namespace LlamaLogic.Packages;

/// <summary>
/// Represents a data resource value composed of three floating point numbers
/// </summary>

public class DataResourceFloatingPoint3Value(float x, float y, float z)
{
    /// <summary>
    /// Gets/sets the first floating point number
    /// </summary>
    public float X { get; set; } = x;

    /// <summary>
    /// Gets/sets the second floating point number
    /// </summary>
    public float Y { get; set; } = y;

    /// <summary>
    /// Gets/sets the third floating point number
    /// </summary>
    public float Z { get; set; } = z;
}
