namespace LlamaLogic.Packages.Models;

/// <summary>
/// Represents an exception thrown by an <see cref="IModel{TSelf}"/> when it attempts to decode a resource that is not in the expected format
/// </summary>
public sealed class UnexpectedFormatDecodingException :
    Exception
{
    const string defaultMessage = "The resource is not in the expected format";

    /// <summary>
    /// Initializes a new <see cref="UnexpectedFormatDecodingException"/>
    /// </summary>
    public UnexpectedFormatDecodingException() :
        this(defaultMessage)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="UnexpectedFormatDecodingException"/> with the specified <paramref name="message"/>
    /// </summary>
    public UnexpectedFormatDecodingException(string message) :
        this(message, null)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="UnexpectedFormatDecodingException"/> with the specified <paramref name="innerException"/>
    /// </summary>
    public UnexpectedFormatDecodingException(Exception? innerException) :
        this(defaultMessage, innerException)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="UnexpectedFormatDecodingException"/> with the specified <paramref name="message"/> and <paramref name="innerException"/>
    /// </summary>
    public UnexpectedFormatDecodingException(string message, Exception? innerException) :
        base(message, innerException)
    {
    }
}
