namespace LlamaLogic.Packages.Models.AnimationStateMachine;

/// <summary>
/// Represents a <see cref="ResourceType.AnimationStateMachine"/> resource (🔓)
/// </summary>
/// <remarks>
/// Maxis ships animation state machines (which creators also know as ASMs or JAZZ) encoded either as a binary packed XML document (a <c>DATA</c> file) or as XML text.
/// <see cref="Decode(ReadOnlyMemory{byte})"/> accepts both and presents the state machine as <see cref="Document"/>.
/// <see cref="Encode"/> always produces XML text, written the way Maxis writes it.
/// </remarks>
public sealed class AnimationStateMachineModel :
    Model,
    IModel<AnimationStateMachineModel>
{
    static readonly ImmutableHashSet<ResourceType> supportedTypes =
    [
        ResourceType.AnimationStateMachine
    ];

    /// <inheritdoc/>
    public static new ISet<ResourceType> SupportedTypes =>
        supportedTypes;

    /// <inheritdoc/>
    /// <exception cref="InvalidDataException">The data is neither a well-formed binary nor a well-formed XML document</exception>
    public static AnimationStateMachineModel Decode(ReadOnlyMemory<byte> data)
    {
        if (PackedXml.IsBinary(data.Span))
            return new AnimationStateMachineModel(PackedXml.DecodeBinary(data.Span));
        try
        {
            using var stream = new ReadOnlyMemoryOfByteStream(data);
            var document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
            if (document.Root is null)
                throw new InvalidDataException("The XML animation state machine has no root element");
            return new AnimationStateMachineModel(document);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException("The data is neither a binary nor an XML animation state machine", ex);
        }
    }

    /// <inheritdoc/>
    public static new string? GetName(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return GetName(buffer.ToArray());
    }

    static string? GetName(ReadOnlyMemory<byte> data)
    {
        try
        {
            return Decode(data).ResourceName;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public static new async Task<string?> GetNameAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return GetName(buffer.ToArray());
    }

    /// <summary>
    /// Initializes a new <see cref="AnimationStateMachineModel"/> for the specified <paramref name="document"/>
    /// </summary>
    /// <param name="document">The state machine document</param>
    public AnimationStateMachineModel(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Document = document;
    }

    /// <summary>
    /// Gets the state machine document
    /// </summary>
    public XDocument Document { get; }

    /// <summary>
    /// Gets the name of the state machine (the <c>name</c> attribute of its root element), if it has one
    /// </summary>
    public override string? ResourceName =>
        Document.Root?.Attribute("name")?.Value;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException"><see cref="Document"/> has no root element or contains a node that Maxis XML cannot express</exception>
    public override ReadOnlyMemory<byte> Encode()
    {
        if (Document.Root is not { } root)
            throw new InvalidOperationException("The document has no root element");
        return PackedXml.Encode(root);
    }
}
