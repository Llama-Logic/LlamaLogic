namespace LlamaLogic.Packages.Models.World;

/// <summary>
/// Represents an object in a <see cref="ResourceType.WorldObjectData"/> resource (🔓)
/// </summary>
public sealed class WorldObjectDataObject :
    WorldObjectDataLocator
{
    internal static WorldObjectDataObject Read(ReadOnlySpan<byte> span, ref int position, uint version)
    {
        var @object = new WorldObjectDataObject();
        Read(span, ref position, @object);
        @object.ObjectId = span.ReadAndAdvancePosition<ulong>(ref position);
        @object.LevelOfDetail = span.ReadAndAdvancePosition<byte>(ref position);
        @object.ParentObjectId = span.ReadAndAdvancePosition<ulong>(ref position);
        @object.ParentSlotHash = span.ReadAndAdvancePosition<uint>(ref position);
        if (version >= 8)
        {
            var snow = span.ReadAndAdvancePosition<byte>(ref position);
            if (snow > 1)
                throw new InvalidDataException($"The snow flag of object {@object.ObjectId} is {snow}, expected 0 or 1");
            @object.Snow = snow is 1;
        }
        if (version >= 10)
            @object.Flags = span.ReadAndAdvancePosition<uint>(ref position);
        return @object;
    }

    internal override void Write(ArrayBufferWriter<byte> writer, uint version)
    {
        base.Write(writer, version);
        var objectId = ObjectId;
        writer.Write(ref objectId);
        var levelOfDetail = LevelOfDetail;
        writer.Write(ref levelOfDetail);
        var parentObjectId = ParentObjectId;
        writer.Write(ref parentObjectId);
        var parentSlotHash = ParentSlotHash;
        writer.Write(ref parentSlotHash);
        if (version >= 8)
        {
            var snow = Snow ? BinaryUtilities.TrueByte : BinaryUtilities.FalseByte;
            writer.Write(ref snow);
        }
        if (version >= 10)
        {
            var flags = Flags;
            writer.Write(ref flags);
        }
    }

    /// <summary>
    /// Gets/sets the flags of the object
    /// </summary>
    /// <remarks>
    /// When bit 0 is set, the game does not create a routing footprint for the object. The meaning of the other bits is not known; in world files only bit 0 is ever set.
    /// </remarks>
    public uint Flags { get; set; }

    /// <summary>
    /// Gets/sets a level of detail value the game stores with the object (255, 254, 2, 1 or 0 in world files)
    /// </summary>
    /// <remarks>
    /// The simulation does not read this value; its exact meaning is not known.
    /// </remarks>
    public byte LevelOfDetail { get; set; } = byte.MaxValue;

    /// <summary>
    /// Gets/sets the id of the object
    /// </summary>
    public ulong ObjectId { get; set; }

    /// <summary>
    /// Gets/sets the id of the object to which this object is parented, or zero
    /// </summary>
    /// <remarks>
    /// The game spawns parents before their children.
    /// </remarks>
    public ulong ParentObjectId { get; set; }

    /// <summary>
    /// Gets/sets the hash of the parent's slot in which this object is placed
    /// </summary>
    public uint ParentSlotHash { get; set; }

    /// <summary>
    /// Gets/sets a flag the game stores with the object (set for most objects in world files)
    /// </summary>
    /// <remarks>
    /// The simulation does not read this flag; Sims 4 Studio calls it whether the object accumulates snow, which is not established.
    /// Stored only by version 8 and later of the resource.
    /// </remarks>
    public bool Snow { get; set; } = true;
}
