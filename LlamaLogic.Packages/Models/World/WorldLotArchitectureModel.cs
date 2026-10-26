namespace LlamaLogic.Packages.Models.World;

/// <summary>
/// Represents a <see cref="ResourceType.WorldLotArchitecture"/> resource: the architecture of a lot template in a world file, as a <see cref="RecordBundle"/> (🔓)
/// </summary>
/// <remarks>
/// A world file is a package (see <see cref="DataBasePackedFile"/>); this models one of the resources in it, not the file.
/// The game names the resource after the lot template it belongs to; see <see cref="GetKey(uint)"/>.
/// </remarks>
public sealed class WorldLotArchitectureModel :
    Model,
    IModel<WorldLotArchitectureModel>
{
    static readonly ImmutableHashSet<ResourceType> supportedTypes =
    [
        ResourceType.WorldLotArchitecture
    ];

    /// <inheritdoc/>
    public static new ISet<ResourceType> SupportedTypes =>
        supportedTypes;

    /// <inheritdoc/>
    /// <exception cref="InvalidDataException">The data is not a length-prefixed record bundle, or the bundle is not well-formed</exception>
    public static WorldLotArchitectureModel Decode(ReadOnlyMemory<byte> data)
    {
        if (data.Length < 4)
            throw new InvalidDataException($"{data.Length} bytes are too few for a world lot architecture resource");
        var length = BinaryPrimitives.ReadUInt32LittleEndian(data.Span);
        if (length != data.Length - 4)
            throw new InvalidDataException($"The resource says its bundle is {length} bytes, but {data.Length - 4} follow");
        return new WorldLotArchitectureModel(RecordBundle.Decode(data[4..]));
    }

    /// <summary>
    /// Gets the key of the <see cref="ResourceType.WorldLotArchitecture"/> resource of the lot template with the specified <paramref name="lotTemplateId"/>
    /// </summary>
    /// <remarks>
    /// The game derives the key from a file name: the instance is the FNV-1 64-bit hash of <c>1-</c> followed by the lot template id in eight lowercase hexadecimal digits, and the type is the FNV-1 32-bit hash of the extension <c>dat</c>.
    /// The <see cref="ResourceType.WorldLotObjects"/> resource of the same lot template has the same instance.
    /// </remarks>
    public static ResourceKey GetKey(uint lotTemplateId) =>
        new(ResourceType.WorldLotArchitecture, 0, GetLotTemplateInstance(lotTemplateId));

    /// <summary>
    /// Gets the instance the game gives the <see cref="ResourceType.WorldLotArchitecture"/> and <see cref="ResourceType.WorldLotObjects"/> resources of the lot template with the specified <paramref name="lotTemplateId"/>
    /// </summary>
    public static ulong GetLotTemplateInstance(uint lotTemplateId) =>
        Fnv64.GetHash($"1-{lotTemplateId:x8}");

    /// <inheritdoc/>
    public static new string? GetName(Stream stream) =>
        null;

    /// <inheritdoc/>
    public static new Task<string?> GetNameAsync(Stream stream, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldLotArchitectureModel"/> class with an empty <see cref="Bundle"/>
    /// </summary>
    public WorldLotArchitectureModel() :
        this(new RecordBundle())
    {
    }

    WorldLotArchitectureModel(RecordBundle bundle) =>
        Bundle = bundle;

    /// <summary>
    /// Gets the record bundle holding the lot's architecture
    /// </summary>
    public RecordBundle Bundle { get; }

    /// <inheritdoc/>
    public override string? ResourceName =>
        null;

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Encode()
    {
        var bundle = Bundle.Encode();
        var result = new byte[4 + bundle.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(result, (uint)bundle.Length);
        bundle.Span.CopyTo(result.AsSpan(4));
        return result;
    }
}
