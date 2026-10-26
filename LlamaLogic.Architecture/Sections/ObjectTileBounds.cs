namespace LlamaLogic.Architecture;

/// <summary>
/// Represents one entry of an <see cref="ObjectTileBoundsSection"/>: the tiles an object covers
/// </summary>
/// <remarks>
/// The game computes the rectangle from the object's world bounds, rounded out to whole tiles and clipped to the lot.
/// It is stored as <see cref="MinimumZ"/>, <see cref="MaximumZ"/>, <see cref="MinimumX"/>, <see cref="MaximumX"/>, in that order; each minimum is at or below its maximum in every lot examined.
/// </remarks>
public sealed class ObjectTileBounds
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectTileBounds"/> class
    /// </summary>
    public ObjectTileBounds()
    {
    }

    /// <summary>
    /// Gets/sets the signed level of the object
    /// </summary>
    public sbyte Level { get; set; }

    /// <summary>
    /// Gets/sets the highest tile the object covers along the lot's local X axis
    /// </summary>
    /// <remarks>
    /// Present from version 2 on.
    /// </remarks>
    public byte MaximumX { get; set; }

    /// <summary>
    /// Gets/sets the highest tile the object covers along the lot's local Z axis
    /// </summary>
    /// <remarks>
    /// Present from version 2 on.
    /// </remarks>
    public byte MaximumZ { get; set; }

    /// <summary>
    /// Gets/sets the lowest tile the object covers along the lot's local X axis
    /// </summary>
    /// <remarks>
    /// Present from version 2 on.
    /// </remarks>
    public byte MinimumX { get; set; }

    /// <summary>
    /// Gets/sets the lowest tile the object covers along the lot's local Z axis
    /// </summary>
    /// <remarks>
    /// Present from version 2 on.
    /// </remarks>
    public byte MinimumZ { get; set; }

    /// <summary>
    /// Gets/sets the id of the object
    /// </summary>
    public ulong ObjectId { get; set; }
}
