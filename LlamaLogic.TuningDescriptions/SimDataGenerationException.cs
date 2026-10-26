namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents an exception thrown by <see cref="SimDataGenerator"/> when it cannot generate SimData for tuning
/// </summary>
public sealed class SimDataGenerationException :
    Exception
{
    const string defaultMessage = "SimData cannot be generated for the tuning";

    /// <summary>
    /// Initializes a new <see cref="SimDataGenerationException"/>
    /// </summary>
    public SimDataGenerationException() :
        this(defaultMessage)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="SimDataGenerationException"/> with the specified <paramref name="message"/>
    /// </summary>
    public SimDataGenerationException(string message) :
        this(message, null)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="SimDataGenerationException"/> with the specified <paramref name="innerException"/>
    /// </summary>
    public SimDataGenerationException(Exception? innerException) :
        this(defaultMessage, innerException)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="SimDataGenerationException"/> with the specified <paramref name="message"/> and <paramref name="innerException"/>
    /// </summary>
    public SimDataGenerationException(string message, Exception? innerException) :
        base(message, innerException)
    {
    }
}
