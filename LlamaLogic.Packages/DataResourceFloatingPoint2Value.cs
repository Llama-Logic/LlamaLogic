namespace LlamaLogic.Packages;

/// <summary>
/// Represents a data resource value composed of two floating point numbers
/// </summary>
public class DataResourceFloatingPoint2Value(float x, float y)
{
    /// <summary>
    /// Gets/sets the first floating point number
    /// </summary>
    public float X { get; set; } = x;

    /// <summary>
    /// Gets/sets the second floating point number
    /// </summary>
    public float Y { get; set; } = y;
}
