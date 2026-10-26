namespace LlamaLogic.Packages.Models.World;

/// <summary>
/// Represents an object placed in the world outside lots in a <see cref="ResourceType.WorldData"/> resource (🔓)
/// </summary>
/// <remarks>
/// The simulation reads these values but does not use them; their roles below come from their chunk tags and values, and those marked as not known are not established.
/// </remarks>
public sealed class WorldDataObject
{
    /// <summary>
    /// Gets/sets the instance of the object's definition (chunk <c>DGUD</c>)
    /// </summary>
    public ulong ObjectDefinitionId { get; set; }

    /// <summary>
    /// Gets/sets the id of the object (chunk <c>ID</c>)
    /// </summary>
    public ulong ObjectId { get; set; }

    /// <summary>
    /// Gets/sets a level of detail value (chunk <c>MLOD</c>; 255, 254, 2, 1 or 0 in world files), whose exact meaning is not known
    /// </summary>
    public byte LevelOfDetail { get; set; } = byte.MaxValue;

    /// <summary>
    /// Gets/sets the orientation of the object (chunk <c>ROT</c>)
    /// </summary>
    public Quaternion Orientation { get; set; }

    /// <summary>
    /// Gets/sets the id of the object to which this object is parented, or zero (chunk <c>PTID</c>)
    /// </summary>
    public ulong ParentObjectId { get; set; }

    /// <summary>
    /// Gets/sets the hash of the parent's slot in which this object is placed (chunk <c>SLOT</c>)
    /// </summary>
    public uint ParentSlotHash { get; set; }

    /// <summary>
    /// Gets/sets the position of the object in the world (chunk <c>POS</c>)
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Gets/sets a value whose meaning is not known (chunk <c>OPRP</c>; 0 or 1 in current world files), or <see langword="null"/> if the object has no such chunk, as in older world files
    /// </summary>
    public uint? Properties { get; set; }

    /// <summary>
    /// Gets/sets the scale of the object (chunk <c>SCAL</c>)
    /// </summary>
    public float Scale { get; set; } = 1F;

    /// <summary>
    /// Gets/sets a flag whose meaning is not known (chunk <c>SNOW</c>; set for most objects in current world files), or <see langword="null"/> if the object has no such chunk, as in older world files
    /// </summary>
    public bool? Snow { get; set; }
}
