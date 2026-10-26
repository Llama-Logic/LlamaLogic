namespace LlamaLogic.Packages.Models.World;

/// <summary>
/// Represents a placed item in a <see cref="ResourceType.WorldObjectData"/> resource: a footprint, a transform and an object definition (🔓)
/// </summary>
public class WorldObjectDataLocator
{
    internal static void Read(ReadOnlySpan<byte> span, ref int position, WorldObjectDataLocator locator)
    {
        locator.FootprintKey = WorldObjectDataModel.ReadTypeGroupInstance(span, ref position);
        locator.Position = span.ReadAndAdvancePosition<Vector3>(ref position);
        locator.Orientation = span.ReadAndAdvancePosition<Quaternion>(ref position);
        locator.Scale = span.ReadAndAdvancePosition<float>(ref position);
        locator.ObjectDefinitionId = span.ReadAndAdvancePosition<ulong>(ref position);
    }

    internal virtual void Write(ArrayBufferWriter<byte> writer, uint version)
    {
        WorldObjectDataModel.WriteTypeGroupInstance(writer, FootprintKey);
        var position = Position;
        writer.Write(ref position);
        var orientation = Orientation;
        writer.Write(ref orientation);
        var scale = Scale;
        writer.Write(ref scale);
        var objectDefinitionId = ObjectDefinitionId;
        writer.Write(ref objectDefinitionId);
    }

    /// <summary>
    /// Gets/sets the key of the routing footprint resource (<see cref="ResourceType.Footprint"/>) the game places for the item, or <see cref="ResourceKey.Empty"/>
    /// </summary>
    public ResourceKey FootprintKey { get; set; }

    /// <summary>
    /// Gets/sets the instance of the item's object definition
    /// </summary>
    public ulong ObjectDefinitionId { get; set; }

    /// <summary>
    /// Gets/sets the orientation of the item
    /// </summary>
    public Quaternion Orientation { get; set; }

    /// <summary>
    /// Gets/sets the position of the item in the world
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Gets/sets the scale of the item
    /// </summary>
    public float Scale { get; set; } = 1F;
}
