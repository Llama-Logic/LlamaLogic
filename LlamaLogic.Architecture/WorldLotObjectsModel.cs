namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a <see cref="ResourceType.WorldLotObjects"/> resource: the objects of a lot template in a world file, as a <c>LotObjectList</c> protobuf message in protobuf text format (🔓)
/// </summary>
/// <remarks>
/// <para>The game names the resource after the lot template it belongs to; see <see cref="GetKey(uint)"/>.
/// Its <see cref="ResourceType.WorldLotArchitecture"/> counterpart holds the lot template's architecture.</para>
/// <para>Maxis writes the text with fields in field-number order, two-space indentation, CRLF line endings and floats in .NET Framework's round-trip format; <see cref="Encode"/> writes the same, so it reproduces every resource Maxis wrote byte for byte.
/// <see cref="Decode(ReadOnlyMemory{byte})"/> also reads text written otherwise (community tools add commas, reorder fields or write hexadecimal integers), and <see cref="Encode"/> returns the decoded bytes unchanged as long as <see cref="LotObjects"/> is unchanged.</para>
/// </remarks>
public sealed class WorldLotObjectsModel :
    Model,
    IModel<WorldLotObjectsModel>
{
    static readonly ImmutableHashSet<ResourceType> supportedTypes =
    [
        ResourceType.WorldLotObjects
    ];

    /// <inheritdoc/>
    public static new ISet<ResourceType> SupportedTypes =>
        supportedTypes;

    /// <inheritdoc/>
    /// <exception cref="InvalidDataException">The data is not protobuf text format of a <c>LotObjectList</c> message</exception>
    public static WorldLotObjectsModel Decode(ReadOnlyMemory<byte> data)
    {
        string text;
        try
        {
            text = new UTF8Encoding(false, true).GetString(data.Span);
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidDataException("The resource is not UTF-8 text", ex);
        }
        var lotObjects = ProtobufText.Parse<LotObjectList>(text);
        return new WorldLotObjectsModel(lotObjects)
        {
            decodedData = data,
            decodedMessage = Serialize(lotObjects)
        };
    }

    /// <summary>
    /// Gets the key of the <see cref="ResourceType.WorldLotObjects"/> resource of the lot template with the specified <paramref name="lotTemplateId"/>
    /// </summary>
    /// <remarks>
    /// The instance is that of the lot template's <see cref="ResourceType.WorldLotArchitecture"/> resource (see <see cref="WorldLotArchitectureModel.GetLotTemplateInstance(uint)"/>), and the type is the FNV-1 32-bit hash of the extension <c>lottemp</c>.
    /// </remarks>
    public static ResourceKey GetKey(uint lotTemplateId) =>
        new(ResourceType.WorldLotObjects, 0, WorldLotArchitectureModel.GetLotTemplateInstance(lotTemplateId));

    /// <inheritdoc/>
    public static new string? GetName(Stream stream) =>
        null;

    /// <inheritdoc/>
    public static new Task<string?> GetNameAsync(Stream stream, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    static byte[] Serialize(LotObjectList lotObjects)
    {
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, lotObjects);
        return stream.ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldLotObjectsModel"/> class with no objects
    /// </summary>
    public WorldLotObjectsModel() :
        this(new LotObjectList())
    {
    }

    WorldLotObjectsModel(LotObjectList lotObjects) =>
        LotObjects = lotObjects;

    ReadOnlyMemory<byte> decodedData;
    byte[]? decodedMessage;

    /// <summary>
    /// Gets/sets the lot template's objects
    /// </summary>
    public LotObjectList LotObjects { get; set; }

    /// <inheritdoc/>
    public override string? ResourceName =>
        null;

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Encode()
    {
        ArgumentNullException.ThrowIfNull(LotObjects);
        if (decodedMessage is not null && Serialize(LotObjects).AsSpan().SequenceEqual(decodedMessage))
            return decodedData;
        return Encoding.ASCII.GetBytes(ProtobufText.Format(LotObjects));
    }
}
