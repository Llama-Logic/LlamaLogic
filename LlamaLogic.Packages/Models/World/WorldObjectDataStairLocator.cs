namespace LlamaLogic.Packages.Models.World;

/// <summary>
/// Represents a stair locator in a <see cref="ResourceType.WorldObjectData"/> resource, for which the game builds a stair routing footprint (🔓)
/// </summary>
/// <remarks>
/// The footprint is a rectangle <see cref="Width"/> wide, centered on the locator, whose length is <see cref="WorldObjectDataLocator.Scale"/> times 3 meters rounded to the nearest quarter meter.
/// </remarks>
public sealed class WorldObjectDataStairLocator :
    WorldObjectDataLocator
{
    internal static WorldObjectDataStairLocator Read(ReadOnlySpan<byte> span, ref int position)
    {
        var locator = new WorldObjectDataStairLocator();
        Read(span, ref position, locator);
        locator.Width = span.ReadAndAdvancePosition<uint>(ref position);
        return locator;
    }

    internal override void Write(ArrayBufferWriter<byte> writer, uint version)
    {
        base.Write(writer, version);
        var width = Width;
        writer.Write(ref width);
    }

    /// <summary>
    /// Gets/sets the width of the stairs, in tiles
    /// </summary>
    public uint Width { get; set; } = 1;
}
