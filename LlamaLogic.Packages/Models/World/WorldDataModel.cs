namespace LlamaLogic.Packages.Models.World;

/// <summary>
/// Represents a <see cref="ResourceType.WorldData"/> resource: the placement of a world's lots and the objects of the world outside them (🔓)
/// </summary>
/// <remarks>
/// A world file is a package (see <see cref="DataBasePackedFile"/>); this models one of the resources in it, not the file.
/// The resource is a tree of tagged chunks (<c>OMGS</c>, holding <c>OMGR</c> object managers, holding <c>ID</c>, <c>LOT</c> and <c>OBJ</c> chunks).
/// <see cref="Decode(ReadOnlyMemory{byte})"/> supports the chunks the game's world files contain and rejects the others the game can also read (<c>MODL</c>, <c>DEFG</c>, <c>LEVL</c>, <c>SCRP</c>, <c>TRES</c>, <c>REFS</c>, <c>SIZY</c>), whose layouts are not established.
/// <see cref="Encode"/> writes the chunks in the order the game's world files do, so decoding and encoding one of them reproduces it byte for byte.
/// Older world files lack the <c>SNOW</c> and <c>OPRP</c> chunks of objects; the corresponding properties are then <see langword="null"/> and are not written.
/// The game names the resource after its world file; see <see cref="GetKey(string)"/>.
/// </remarks>
public sealed class WorldDataModel :
    Model,
    IModel<WorldDataModel>
{
    const string objectManagersTag = "OMGS";
    const string objectManagerTag = "OMGR";
    const string idTag = "ID  ";
    const string lotTag = "LOT ";
    const string objectTag = "OBJ ";
    const string positionTag = "POS ";
    const string rotationTag = "ROT ";
    const string widthTag = "SIZX";
    const string depthTag = "SIZZ";
    const string objectDefinitionTag = "DGUD";
    const string scaleTag = "SCAL";
    const string levelOfDetailTag = "MLOD";
    const string parentTag = "PTID";
    const string slotTag = "SLOT";
    const string snowTag = "SNOW";
    const string propertiesTag = "OPRP";
    static readonly ImmutableHashSet<ResourceType> supportedTypes =
    [
        ResourceType.WorldData
    ];

    /// <inheritdoc/>
    public static new ISet<ResourceType> SupportedTypes =>
        supportedTypes;

    /// <inheritdoc/>
    /// <exception cref="InvalidDataException">The data is not a well-formed world data resource, or it contains a chunk this model does not support</exception>
    public static WorldDataModel Decode(ReadOnlyMemory<byte> data)
    {
        var top = ReadChunks(data);
        if (top.Count != 1 || top[0].tag != objectManagersTag)
            throw new InvalidDataException($"A world data resource must be exactly one {objectManagersTag} chunk");
        var model = new WorldDataModel();
        foreach (var (managerTag, managerData) in ReadChunks(top[0].data))
        {
            if (managerTag != objectManagerTag)
                throw new InvalidDataException($"Chunk {managerTag} is not supported in {objectManagersTag}");
            var manager = new WorldDataObjectManager();
            var hasId = false;
            foreach (var (tag, chunk) in ReadChunks(managerData))
                switch (tag)
                {
                    case idTag when !hasId:
                        manager.Id = ReadValue<ulong>(tag, chunk);
                        hasId = true;
                        break;
                    case lotTag:
                        manager.Lots.Add(ReadLot(chunk));
                        break;
                    case objectTag:
                        manager.Objects.Add(ReadObject(chunk));
                        break;
                    default:
                        throw new InvalidDataException($"Chunk {tag} is not supported in {objectManagerTag}");
                }
            if (!hasId)
                throw new InvalidDataException($"An {objectManagerTag} chunk has no {idTag} chunk");
            model.ObjectManagers.Add(manager);
        }
        return model;
    }

    /// <summary>
    /// Gets the key the game gives the <see cref="ResourceType.WorldData"/> resource of the world file named <paramref name="worldFileName"/> (such as <c>EP01_AlienWorld_01</c> or <c>EP01_AlienWorld_01.world</c>)
    /// </summary>
    /// <remarks>
    /// The game derives the key from a file name: the instance is the FNV-1 64-bit hash of the world file's name without its extension, lowercased, and the type is the FNV-1 32-bit hash of the extension <c>dworld</c>.
    /// </remarks>
    public static ResourceKey GetKey(string worldFileName)
    {
        ArgumentNullException.ThrowIfNull(worldFileName);
        return new(ResourceType.WorldData, 0, Fnv64.GetHash(Path.GetFileNameWithoutExtension(worldFileName)));
    }

    /// <inheritdoc/>
    public static new string? GetName(Stream stream) =>
        null;

    /// <inheritdoc/>
    public static new Task<string?> GetNameAsync(Stream stream, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    static List<(string tag, ReadOnlyMemory<byte> data)> ReadChunks(ReadOnlyMemory<byte> data)
    {
        var chunks = new List<(string tag, ReadOnlyMemory<byte> data)>();
        var position = 0;
        while (position < data.Length)
        {
            if (data.Length - position < 8)
                throw new InvalidDataException($"A chunk header at offset {position} extends beyond its container");
            var span = data.Span[position..];
            var tag = Encoding.ASCII.GetString(span[..4]);
            var length = BinaryPrimitives.ReadUInt32LittleEndian(span[4..]);
            if (length > (uint)(data.Length - position - 8))
                throw new InvalidDataException($"Chunk {tag} at offset {position} extends beyond its container");
            chunks.Add((tag, data.Slice(position + 8, (int)length)));
            position += 8 + (int)length;
        }
        return chunks;
    }

    static WorldDataLot ReadLot(ReadOnlyMemory<byte> data)
    {
        var lot = new WorldDataLot();
        var seen = new HashSet<string>();
        foreach (var (tag, chunk) in ReadChunks(data))
        {
            if (!seen.Add(tag))
                throw new InvalidDataException($"A {lotTag} chunk has more than one {tag} chunk");
            switch (tag)
            {
                case widthTag:
                    lot.Width = ReadValue<uint>(tag, chunk);
                    break;
                case depthTag:
                    lot.Depth = ReadValue<uint>(tag, chunk);
                    break;
                case positionTag:
                    lot.Position = ReadValue<Vector3>(tag, chunk);
                    break;
                case rotationTag:
                    lot.Rotation = ReadValue<float>(tag, chunk);
                    break;
                default:
                    throw new InvalidDataException($"Chunk {tag} is not supported in {lotTag}");
            }
        }
        if (seen.Count != 4)
            throw new InvalidDataException($"A {lotTag} chunk must have {widthTag}, {depthTag}, {positionTag} and {rotationTag} chunks");
        return lot;
    }

    static WorldDataObject ReadObject(ReadOnlyMemory<byte> data)
    {
        var @object = new WorldDataObject();
        var seen = new HashSet<string>();
        foreach (var (tag, chunk) in ReadChunks(data))
        {
            if (!seen.Add(tag))
                throw new InvalidDataException($"An {objectTag} chunk has more than one {tag} chunk");
            switch (tag)
            {
                case idTag:
                    @object.ObjectId = ReadValue<ulong>(tag, chunk);
                    break;
                case positionTag:
                    @object.Position = ReadValue<Vector3>(tag, chunk);
                    break;
                case rotationTag:
                    @object.Orientation = ReadValue<Quaternion>(tag, chunk);
                    break;
                case objectDefinitionTag:
                    @object.ObjectDefinitionId = ReadValue<ulong>(tag, chunk);
                    break;
                case scaleTag:
                    @object.Scale = ReadValue<float>(tag, chunk);
                    break;
                case levelOfDetailTag:
                    @object.LevelOfDetail = ReadValue<byte>(tag, chunk);
                    break;
                case parentTag:
                    @object.ParentObjectId = ReadValue<ulong>(tag, chunk);
                    break;
                case slotTag:
                    @object.ParentSlotHash = ReadValue<uint>(tag, chunk);
                    break;
                case snowTag:
                    var snow = ReadValue<byte>(tag, chunk);
                    if (snow > 1)
                        throw new InvalidDataException($"The {snowTag} chunk of an object is {snow}, expected 0 or 1");
                    @object.Snow = snow is 1;
                    break;
                case propertiesTag:
                    @object.Properties = ReadValue<uint>(tag, chunk);
                    break;
                default:
                    throw new InvalidDataException($"Chunk {tag} is not supported in {objectTag}");
            }
        }
        if (seen.Count - (seen.Contains(snowTag) ? 1 : 0) - (seen.Contains(propertiesTag) ? 1 : 0) != 8)
            throw new InvalidDataException($"An {objectTag} chunk must have the chunks {idTag.TrimEnd()}, {positionTag.TrimEnd()}, {rotationTag.TrimEnd()}, {objectDefinitionTag}, {scaleTag}, {levelOfDetailTag}, {parentTag} and {slotTag}");
        return @object;
    }

    static T ReadValue<T>(string tag, ReadOnlyMemory<byte> chunk)
        where T : struct
    {
        if (chunk.Length != BinaryUtilities.TypeSizeValueFactory(typeof(T)))
            throw new InvalidDataException($"Chunk {tag} is {chunk.Length} bytes, expected {BinaryUtilities.TypeSizeValueFactory(typeof(T))}");
        return MemoryMarshal.Read<T>(chunk.Span);
    }

    static void WriteChunk(ArrayBufferWriter<byte> writer, string tag, Action<ArrayBufferWriter<byte>> writeContent)
    {
        Encoding.ASCII.GetBytes(tag, writer.GetSpanAndAdvance(4));
        var lengthPosition = writer.WrittenCount;
        writer.GetSpanAndAdvance(4);
        var start = writer.WrittenCount;
        writeContent(writer);
        var length = (uint)(writer.WrittenCount - start);
        BinaryPrimitives.WriteUInt32LittleEndian(MemoryMarshal.AsMemory(writer.WrittenMemory).Span[lengthPosition..], length);
    }

    static void WriteValueChunk<T>(ArrayBufferWriter<byte> writer, string tag, T value)
        where T : struct =>
        WriteChunk(writer, tag, w => w.Write(ref value));

    /// <summary>
    /// Gets the object managers
    /// </summary>
    public Collection<WorldDataObjectManager> ObjectManagers { get; } = [];

    /// <inheritdoc/>
    public override string? ResourceName =>
        null;

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Encode()
    {
        var writer = new ArrayBufferWriter<byte>();
        WriteChunk(writer, objectManagersTag, w =>
        {
            foreach (var manager in ObjectManagers)
                WriteChunk(w, objectManagerTag, mw =>
                {
                    WriteValueChunk(mw, idTag, manager.Id);
                    foreach (var lot in manager.Lots)
                        WriteChunk(mw, lotTag, lw =>
                        {
                            WriteValueChunk(lw, widthTag, lot.Width);
                            WriteValueChunk(lw, depthTag, lot.Depth);
                            WriteValueChunk(lw, positionTag, lot.Position);
                            WriteValueChunk(lw, rotationTag, lot.Rotation);
                        });
                    foreach (var @object in manager.Objects)
                        WriteChunk(mw, objectTag, ow =>
                        {
                            WriteValueChunk(ow, idTag, @object.ObjectId);
                            WriteValueChunk(ow, positionTag, @object.Position);
                            WriteValueChunk(ow, rotationTag, @object.Orientation);
                            WriteValueChunk(ow, objectDefinitionTag, @object.ObjectDefinitionId);
                            WriteValueChunk(ow, scaleTag, @object.Scale);
                            WriteValueChunk(ow, levelOfDetailTag, @object.LevelOfDetail);
                            WriteValueChunk(ow, parentTag, @object.ParentObjectId);
                            WriteValueChunk(ow, slotTag, @object.ParentSlotHash);
                            if (@object.Snow is { } snow)
                                WriteValueChunk(ow, snowTag, snow ? BinaryUtilities.TrueByte : BinaryUtilities.FalseByte);
                            if (@object.Properties is { } properties)
                                WriteValueChunk(ow, propertiesTag, properties);
                        });
                });
        });
        return writer.WrittenMemory;
    }
}
