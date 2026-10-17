namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a closed or open polygon of a <see cref="WallBlock"/>, as its segments
/// </summary>
/// <remarks>
/// That the first loop of a block is its outer polygon and the rest are holes is a hypothesis from the docstring of the game's get_plex_outline Python function.
/// Layout: a <see cref="ushort"/> segment count, the start points of all <see cref="Segments"/>, then the rest of each segment.
/// </remarks>
public sealed class WallBlockLoop
{
    internal static WallBlockLoop Read(ref SectionReader reader, ushort version)
    {
        var loop = new WallBlockLoop();
        var count = reader.ReadCount(reader.ReadUInt16());
        for (var i = 0; i < count; ++i)
            loop.Segments.Add(WallBlockSegment.ReadPoint(ref reader));
        foreach (var segment in loop.Segments)
            segment.ReadProducts(ref reader, version);
        return loop;
    }

    /// <summary>
    /// Gets the segments, in order around the loop
    /// </summary>
    public Collection<WallBlockSegment> Segments { get; } = [];

    internal void Write(SectionWriter writer, ushort version)
    {
        if (Segments.Count > ushort.MaxValue)
            throw writer.Invalid($"a block loop has {Segments.Count} segments, more than the {ushort.MaxValue} its count can hold");
        writer.Write((ushort)Segments.Count);
        foreach (var segment in Segments)
            segment.WritePoint(writer);
        foreach (var segment in Segments)
            segment.WriteProducts(writer, version);
    }
}
