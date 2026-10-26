namespace LlamaLogic.Packages.Models.AudioConfiguration;

/// <summary>
/// Represents a point of a response curve in an <see cref="AudioConfigurationProperty"/> (🔓)
/// </summary>
public sealed class AudioConfigurationCurvePoint
{
    /// <summary>
    /// Gets/sets the ratio of change to the previous point
    /// </summary>
    public float DeltaRatio { get; set; }

    /// <summary>
    /// Gets/sets the input value
    /// </summary>
    public float InputValue { get; set; }

    /// <summary>
    /// Gets/sets the output value
    /// </summary>
    public float OutputValue { get; set; }

    /// <summary>
    /// Gets/sets the input value of the previous point
    /// </summary>
    public float PreviousInputValue { get; set; }

    /// <summary>
    /// Gets/sets the output value of the previous point
    /// </summary>
    public float PreviousOutputValue { get; set; }
}