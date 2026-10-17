namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a tray blueprint (a <c>.blueprint</c> file): a lot's objects and architecture (🔓)
/// </summary>
/// <remarks>
/// <para>The file is a 32-bit version and the length of its content, followed by the content compressed with Snappy (the raw block format).
/// The content is a header, the <see cref="LotObjects"/> as a protobuf message, <see cref="ObjectDefinitionMappings"/>, <see cref="Images"/> and finally the <see cref="Architecture"/> as a record bundle.
/// Blueprints without the header (<see cref="HasHeader"/> is <see langword="false"/>) hold only the objects and the architecture; Sims 4 Studio reads them, but none has been observed.</para>
/// <para>Snappy compressors differ in their output, so <see cref="Encode"/> reuses the compressed bytes it decoded when the content it would write is unchanged, and otherwise compresses the content anew.
/// Decoding and encoding a blueprint the game wrote therefore reproduces it byte for byte.</para>
/// </remarks>
public sealed class Blueprint
{
    const uint headerIdentifier = 0xfea1baad;

    /// <summary>
    /// Decodes a blueprint from the content of its file
    /// </summary>
    /// <exception cref="InvalidDataException">The data is not a well-formed blueprint, or its architecture cannot be decoded</exception>
    public static Blueprint Decode(ReadOnlyMemory<byte> data)
    {
        var span = data.Span;
        if (span.Length < 8)
            throw new InvalidDataException($"{span.Length} bytes are too few for a blueprint");
        var version = BinaryPrimitives.ReadInt32LittleEndian(span);
        var compressedLength = BinaryPrimitives.ReadUInt32LittleEndian(span[4..]);
        if (compressedLength != span.Length - 8)
            throw new InvalidDataException($"The blueprint says its compressed content is {compressedLength} bytes, but {span.Length - 8} follow");
        var compressed = data[8..];
        var content = Snappy.Decompress(compressed.Span);
        var position = 0;
        var blueprint = new Blueprint { Version = version };
        var first = ReadUInt32(content, ref position);
        uint lotObjectsLength;
        if (first == headerIdentifier)
        {
            blueprint.HasHeader = true;
            blueprint.ContentVersion = ReadInt32(content, ref position);
            lotObjectsLength = ReadUInt32(content, ref position);
        }
        else
        {
            blueprint.HasHeader = false;
            lotObjectsLength = first;
        }
        if (lotObjectsLength > (uint)(content.Length - position))
            throw new InvalidDataException($"The blueprint's objects are {lotObjectsLength} bytes, but only {content.Length - position} remain");
        blueprint.LotObjects = DeserializeLotObjects(content.AsMemory(position, (int)lotObjectsLength));
        position += (int)lotObjectsLength;
        if (blueprint.HasHeader)
        {
            blueprint.Unknown1 = ReadUInt32(content, ref position);
            var mappingCount = ReadCount(content, ref position, 16);
            for (var i = 0; i < mappingCount; ++i)
                blueprint.ObjectDefinitionMappings.Add(new(ReadUInt64(content, ref position), ReadUInt64(content, ref position)));
            if (blueprint.ContentVersion == 2)
            {
                var imageCount = ReadCount(content, ref position, 12);
                for (var i = 0; i < imageCount; ++i)
                {
                    var id = ReadUInt64(content, ref position);
                    var length = ReadCount(content, ref position, 1);
                    blueprint.Images.Add(new(id, content.AsMemory(position, length)));
                    position += length;
                }
            }
        }
        blueprint.Architecture = LotArchitecture.Decode(content.AsMemory(position));
        blueprint.decodedContent = content;
        blueprint.decodedCompressedContent = compressed;
        return blueprint;
    }

    static LotObjectList DeserializeLotObjects(ReadOnlyMemory<byte> data)
    {
        try
        {
            return ProtoBuf.Serializer.Deserialize<LotObjectList>(data);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new InvalidDataException("The blueprint's objects are not a well-formed LotObjectList message", ex);
        }
    }

    static int ReadCount(byte[] content, ref int position, int minimumItemSize)
    {
        var count = ReadInt32(content, ref position);
        if (count < 0 || (long)count * minimumItemSize > content.Length - position)
            throw new InvalidDataException($"A count of {count} at offset {position - 4} of the blueprint's content cannot fit in the {content.Length - position} remaining bytes");
        return count;
    }

    static int ReadInt32(byte[] content, ref int position) =>
        unchecked((int)ReadUInt32(content, ref position));

    static uint ReadUInt32(byte[] content, ref int position)
    {
        if (content.Length - position < 4)
            throw new InvalidDataException($"The blueprint's content ends at offset {content.Length}, inside a value at offset {position}");
        var value = BinaryPrimitives.ReadUInt32LittleEndian(content.AsSpan(position));
        position += 4;
        return value;
    }

    static ulong ReadUInt64(byte[] content, ref int position)
    {
        if (content.Length - position < 8)
            throw new InvalidDataException($"The blueprint's content ends at offset {content.Length}, inside a value at offset {position}");
        var value = BinaryPrimitives.ReadUInt64LittleEndian(content.AsSpan(position));
        position += 8;
        return value;
    }

    ReadOnlyMemory<byte> decodedCompressedContent;
    byte[]? decodedContent;

    /// <summary>
    /// Gets/sets the lot's architecture
    /// </summary>
    public LotArchitecture Architecture { get; set; } = new();

    /// <summary>
    /// Gets/sets the version of the content's header, which is 2 in every blueprint observed and decides whether the content holds <see cref="Images"/>
    /// </summary>
    public int ContentVersion { get; set; } = 2;

    /// <summary>
    /// Gets/sets whether the content begins with a header, which every blueprint observed does
    /// </summary>
    public bool HasHeader { get; set; } = true;

    /// <summary>
    /// Gets the images, which only content version 2 holds
    /// </summary>
    public Collection<BlueprintImage> Images { get; } = [];

    /// <summary>
    /// Gets/sets the lot's objects
    /// </summary>
    public LotObjectList LotObjects { get; set; } = new();

    /// <summary>
    /// Gets the pairs of object definition ids, which only blueprints with a header hold
    /// </summary>
    public Collection<BlueprintObjectDefinitionMapping> ObjectDefinitionMappings { get; } = [];

    /// <summary>
    /// Gets/sets a value of the header that follows the objects, which is 1 in every blueprint observed
    /// </summary>
    public uint Unknown1 { get; set; } = 1;

    /// <summary>
    /// Gets/sets the version of the file, which is 1 in every blueprint observed
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Encodes the blueprint as the content of its file
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Architecture"/> or <see cref="LotObjects"/> is <see langword="null"/>, the architecture cannot be encoded as it is, or the content is too large for the format</exception>
    public ReadOnlyMemory<byte> Encode()
    {
        var content = EncodeContent();
        var compressed = decodedContent is not null && content.AsSpan().SequenceEqual(decodedContent)
            ? decodedCompressedContent
            : Snappy.Compress(content);
        var result = new byte[8 + compressed.Length];
        BinaryPrimitives.WriteInt32LittleEndian(result, Version);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), (uint)compressed.Length);
        compressed.Span.CopyTo(result.AsSpan(8));
        return result;
    }

    byte[] EncodeContent()
    {
        if (Architecture is null)
            throw new InvalidOperationException($"The blueprint has no {nameof(Architecture)}");
        if (LotObjects is null)
            throw new InvalidOperationException($"The blueprint has no {nameof(LotObjects)}");
        using var lotObjects = new MemoryStream();
        ProtoBuf.Serializer.Serialize(lotObjects, LotObjects);
        var architecture = Architecture.Encode();
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        if (HasHeader)
        {
            writer.Write(headerIdentifier);
            writer.Write(ContentVersion);
        }
        writer.Write((uint)lotObjects.Length);
        lotObjects.Position = 0;
        lotObjects.CopyTo(stream);
        if (HasHeader)
        {
            writer.Write(Unknown1);
            writer.Write(ObjectDefinitionMappings.Count);
            foreach (var mapping in ObjectDefinitionMappings)
            {
                writer.Write(mapping.From);
                writer.Write(mapping.To);
            }
            if (ContentVersion != 2 && Images.Count > 0)
                throw new InvalidOperationException($"Content version {ContentVersion} cannot hold images");
            if (ContentVersion == 2)
            {
                writer.Write(Images.Count);
                foreach (var image in Images)
                {
                    writer.Write(image.Id);
                    writer.Write(image.Data.Length);
                    writer.Write(image.Data.Span);
                }
            }
        }
        else if (ObjectDefinitionMappings.Count > 0 || Images.Count > 0 || ContentVersion != 2 || Unknown1 != 1)
            throw new InvalidOperationException("A blueprint without a header cannot hold a content version, the value that follows the objects, object definition mappings or images, so they must keep their defaults");
        writer.Write(architecture.Span);
        writer.Flush();
        return stream.ToArray();
    }
}
