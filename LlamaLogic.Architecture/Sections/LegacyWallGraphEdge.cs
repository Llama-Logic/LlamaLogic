namespace LlamaLogic.Architecture;

/// <summary>
/// Represents an edge of <see cref="LegacyWallGraphSection.Edges"/>
/// </summary>
/// <remarks>
/// Layout: <see cref="Code"/>, an <see cref="int"/> count, then the <see cref="Parts"/>.
/// </remarks>
public sealed class LegacyWallGraphEdge
{
    internal static LegacyWallGraphEdge Read(ref SectionReader reader)
    {
        var edge = new LegacyWallGraphEdge { Code = reader.ReadUInt32() };
        var count = reader.ReadCount();
        for (var i = 0; i < count; ++i)
            edge.Parts.Add(LegacyWallGraphEdgePart.Read(ref reader));
        return edge;
    }

    /// <summary>
    /// Gets/sets the code of the edge
    /// </summary>
    /// <remarks>
    /// Hypothesis: the edge's two endpoints and level, as 7-bit coordinates in bits 0 through 6, 7 through 13, 14 through 20 and 21 through 27 and the level in bits 28 through 31.
    /// Bits 6, 13, 20, 27 and 31 are never set in the lots examined, and read that way every edge lies within the lot and joins neighboring points: 2,784 of the 2,824 codes of the lots examined (counting those of the product edges) differ by 1 in one coordinate, the other 40 by 1 in both.
    /// </remarks>
    public uint Code { get; set; }

    /// <summary>
    /// Gets the parts of the edge
    /// </summary>
    /// <remarks>
    /// 1 or 2 in the lots examined.
    /// </remarks>
    public Collection<LegacyWallGraphEdgePart> Parts { get; } = [];

    internal void Write(SectionWriter writer)
    {
        writer.Write(Code);
        writer.WriteCount(Parts.Count);
        foreach (var part in Parts)
            part.Write(writer);
    }
}
