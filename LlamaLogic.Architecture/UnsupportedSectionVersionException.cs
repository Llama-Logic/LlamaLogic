namespace LlamaLogic.Architecture;

[SuppressMessage("Design", "CA1064: Exceptions should be public", Justification = "It never leaves the library: decoding turns it into an undecoded record.")]
sealed class UnsupportedSectionVersionException :
    Exception
{
    public UnsupportedSectionVersionException(string message) :
        base(message)
    {
    }
}
