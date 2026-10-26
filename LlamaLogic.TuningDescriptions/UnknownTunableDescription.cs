namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents a description in an element this library does not recognize, kept so that nothing in a document is lost (🔒)
/// </summary>
public sealed class UnknownTunableDescription :
    TunableDescription
{
    internal UnknownTunableDescription(XElement source) :
        base(source)
    {
    }

    /// <summary>
    /// Gets the name of the description's XML element
    /// </summary>
    public string ElementName =>
        Source.Name.LocalName;
}
