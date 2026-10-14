namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents a document of a <see cref="TuningDescriptionSet"/>: an instance, a module, or a fragment (🔒)
/// </summary>
public abstract class TuningDescriptionDocument
{
    private protected TuningDescriptionDocument(TuningDescriptionSet set, string path, XElement source, XElement? versionHistory)
    {
        Set = set;
        Path = path;
        Source = source;
        Attributes = TuningDescriptionUtilities.GetAttributes(source);
        VersionHistory = TuningDescriptionUtilities.ParseVersionHistory(versionHistory);
    }

    /// <summary>
    /// Gets all of the attributes of the document's root element, by name
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>
    /// Gets the unique identifier Maxis assigns the document (its <c>muid</c> attribute)
    /// </summary>
    public ulong? Muid =>
        TuningDescriptionUtilities.ParseMuid(Attributes.GetValueOrDefault("muid"));

    /// <summary>
    /// Gets the path of the document in its set, with <c>/</c> separating directories
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the set to which the document belongs
    /// </summary>
    public TuningDescriptionSet Set { get; }

    internal XElement Source { get; }

    /// <summary>
    /// Gets the changes Maxis recorded for the document, one list per <c>ActionList</c> element, in document order
    /// </summary>
    public IReadOnlyList<IReadOnlyList<TuningDescriptionAction>> VersionHistory { get; }
}
