namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.LotObjects"/> section: the objects of the lot, as an <c>EA.Sims4.Network.LotObjectList</c> message (🔓)
/// </summary>
/// <remarks>
/// <para>Version 1 is the only one; the game fails to load the lot with any other.
/// The version is followed by the 32-bit length of the serialized message.
/// The game writes the section only when the list is not empty.</para>
/// <para>Re-serializing the message reproduces every payload examined byte for byte.</para>
/// </remarks>
public sealed class LotObjectsSection :
    LotArchitectureSection
{
    const uint supportedVersion = 1;

    internal static LotObjectsSection Read(ref SectionReader reader, SectionContext context)
    {
        reader.ReadVersion(supportedVersion, supportedVersion);
        var length = reader.ReadCount();
        var payload = reader.ReadBytes(length);
        try
        {
            return new LotObjectsSection { Objects = ProtoBuf.Serializer.Deserialize<LotObjectList>(new ReadOnlyMemory<byte>(payload)) };
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            throw new InvalidDataException($"The {reader.Type} section holds a message that cannot be parsed: {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LotObjectsSection"/> class
    /// </summary>
    public LotObjectsSection() :
        base(LotArchitectureSectionType.LotObjects)
    {
    }

    LotObjectList objects = new();

    /// <summary>
    /// Gets/sets the message
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/></exception>
    public LotObjectList Objects
    {
        get => objects;
        set => objects = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, objects);
        writer.Write(supportedVersion);
        writer.WriteCount((int)stream.Length);
        writer.Write(stream.ToArray());
    }
}
