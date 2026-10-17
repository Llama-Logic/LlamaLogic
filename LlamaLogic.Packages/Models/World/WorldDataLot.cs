namespace LlamaLogic.Packages.Models.World;

/// <summary>
/// Represents the placement of a lot in a <see cref="ResourceType.WorldData"/> resource (🔓)
/// </summary>
public sealed class WorldDataLot
{
    /// <summary>
    /// Gets/sets the depth of the lot, in tiles (along the lot's local Z axis)
    /// </summary>
    public uint Depth { get; set; }

    /// <summary>
    /// Gets/sets the position of the lot's origin corner in the world
    /// </summary>
    /// <remarks>
    /// The lot's other corners are <see cref="Width"/> meters along its local X axis and <see cref="Depth"/> meters along its local Z axis from this one.
    /// </remarks>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Gets/sets the rotation of the lot about the vertical axis, in radians
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Gets/sets the width of the lot, in tiles (along the lot's local X axis)
    /// </summary>
    public uint Width { get; set; }
}
