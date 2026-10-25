namespace LlamaLogic.Packages.Models.CombinedTuning;

/// <summary>
/// Represents a <see cref="ResourceType.CombinedTuning"/> resource (🔓)
/// </summary>
/// <remarks>
/// Maxis ships the game's tuning merged into resources of this type, encoded either as a binary packed XML document (a <c>DATA</c> file) or as XML text.
/// <see cref="Decode(ReadOnlyMemory{byte})"/> accepts both and presents the merged document as <see cref="Document"/>.
/// <see cref="Encode"/> always produces XML text, written the way Maxis writes it.
/// <see cref="GetTunings"/> produces the individual tuning documents the merged document contains.
/// </remarks>
public sealed class CombinedTuningModel :
    Model,
    IModel<CombinedTuningModel>
{
    static readonly ImmutableHashSet<ResourceType> supportedTypes =
    [
        ResourceType.CombinedTuning
    ];

    /// <inheritdoc/>
    public static new ISet<ResourceType> SupportedTypes =>
        supportedTypes;

    /// <inheritdoc/>
    /// <exception cref="InvalidDataException">The data is neither a well-formed binary nor a well-formed XML combined tuning document</exception>
    public static CombinedTuningModel Decode(ReadOnlyMemory<byte> data)
    {
        if (PackedXml.IsBinary(data.Span))
            return new CombinedTuningModel(PackedXml.DecodeBinary(data.Span));
        XDocument document;
        try
        {
            using var stream = new ReadOnlyMemoryOfByteStream(data);
            document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException("The data is neither a binary nor an XML combined tuning document", ex);
        }
        if (document.Root is not { Name.LocalName: "combined" })
            throw new InvalidDataException($"The root element of an XML combined tuning document must be <combined>, not <{document.Root?.Name}>");
        return new CombinedTuningModel(document);
    }

    /// <inheritdoc/>
    public static new string? GetName(Stream stream) =>
        null;

    /// <inheritdoc/>
    public static new Task<string?> GetNameAsync(Stream stream, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    /// <summary>
    /// Initializes a new <see cref="CombinedTuningModel"/> for the specified <paramref name="document"/>
    /// </summary>
    /// <param name="document">The merged document, the root element of which is <c>&lt;combined&gt;</c></param>
    public CombinedTuningModel(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Document = document;
    }

    /// <summary>
    /// Gets the merged document, the root element of which is <c>&lt;combined&gt;</c>
    /// </summary>
    public XDocument Document { get; }

    /// <inheritdoc/>
    public override string? ResourceName =>
        null;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException"><see cref="Document"/> has no root element or contains a node that XML combined tuning cannot express</exception>
    public override ReadOnlyMemory<byte> Encode()
    {
        if (Document.Root is not { } root)
            throw new InvalidOperationException("The document has no root element");
        return PackedXml.Encode(root);
    }

    /// <summary>
    /// Gets the individual tuning documents in <see cref="Document"/>, in document order, with each reference to a shared node (<c>&lt;r x="…"&gt;</c>) replaced by a copy of that node (💤)
    /// </summary>
    /// <remarks>
    /// The documents are produced as the sequence is enumerated, and are independent of <see cref="Document"/> and of each other.
    /// Modifying <see cref="Document"/> while enumerating has undefined results.
    /// </remarks>
    /// <exception cref="InvalidDataException"><see cref="Document"/> is not a well-formed merged document: it lacks the shared node group, a reference names a node that does not exist or contains itself, or a tuning document lacks a valid instance</exception>
    public IEnumerable<CombinedTuningModelTuning> GetTunings()
    {
        var root = Document.Root;
        if (root is not { Name.LocalName: "combined" })
            throw new InvalidDataException("The root element of a combined tuning document must be <combined>");
        var sharedNodeGroup = root.Element("g")
            ?? throw new InvalidDataException("The combined tuning document has no shared node group (<g>)");
        var sharedNodes = new Dictionary<string, XElement>();
        foreach (var sharedNode in sharedNodeGroup.Elements())
            if (sharedNode.Attribute("x")?.Value is { } x)
                sharedNodes[x] = sharedNode;
        var resolvedSharedNodes = new Dictionary<string, XElement>();
        var sharedNodesBeingResolved = new HashSet<string>();
        XElement resolve(XElement element)
        {
            var resolved = new XElement(element.Name, element.Attributes().Where(attribute => attribute.Name.LocalName is not "x"));
            foreach (var node in element.Nodes())
            {
                if (node is not XElement childElement)
                {
                    resolved.Add(node);
                    continue;
                }
                if (childElement.Name.LocalName is not "r")
                {
                    resolved.Add(resolve(childElement));
                    continue;
                }
                var x = childElement.Attribute("x")?.Value
                    ?? throw new InvalidDataException("A reference (<r>) has no x attribute");
                if (!resolvedSharedNodes.TryGetValue(x, out var resolvedSharedNode))
                {
                    if (!sharedNodes.TryGetValue(x, out var sharedNode))
                        throw new InvalidDataException($"A reference (<r>) names shared node {x}, which does not exist");
                    if (!sharedNodesBeingResolved.Add(x))
                        throw new InvalidDataException($"Shared node {x} contains itself");
                    resolvedSharedNode = resolve(sharedNode);
                    sharedNodesBeingResolved.Remove(x);
                    resolvedSharedNodes.Add(x, resolvedSharedNode);
                }
                var copy = new XElement(resolvedSharedNode);
                if (childElement.Attribute("n") is { } name)
                    copy.ReplaceAttributes(new[] { name }.Concat(copy.Attributes().Where(attribute => attribute.Name.LocalName is not "n")).Select(attribute => new XAttribute(attribute)).ToList());
                resolved.Add(copy);
            }
            return resolved;
        }
        foreach (var group in root.Elements("R"))
        {
            var typeName = group.Attribute("n")?.Value
                ?? throw new InvalidDataException("A resource type group (<R>) has no n attribute");
            foreach (var tuning in group.Elements())
            {
                if (!ulong.TryParse(tuning.Attribute("s")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var instance))
                    throw new InvalidDataException($"A tuning document in {typeName} has no valid instance (s attribute)");
                yield return new CombinedTuningModelTuning(typeName, instance, tuning.Attribute("n")?.Value, new XDocument(resolve(tuning)));
            }
        }
    }
}
