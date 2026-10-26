namespace LlamaLogic.Architecture;

/// <summary>
/// Represents an entry of <see cref="WallEdgesSection.WallPatternEdges"/>: the two wall patterns of a wall edge
/// </summary>
/// <remarks>
/// Layout: <see cref="MapKey"/>, <see cref="WallPattern1Id"/>, <see cref="WallPattern2Id"/>, as the game's reader reads them.
/// </remarks>
public sealed class WallEdgePatterns
{
    internal static WallEdgePatterns Read(ref SectionReader reader) =>
        new()
        {
            MapKey = reader.ReadUInt32(),
            WallPattern1Id = reader.ReadUInt16(),
            WallPattern2Id = reader.ReadUInt16()
        };

    /// <summary>
    /// Gets/sets the key under which the game maps the entry
    /// </summary>
    /// <remarks>
    /// Hypothesis: it identifies a wall edge, its bytes packing the level (high nibble of byte 0, signed) and a kind (low nibble), a direction (byte 1: 0, 1, 6 or 7) and two coordinates (bytes 2 and 3).
    /// About 42% of the keys in the lots examined are also <see cref="VertexGraphEdge.Id"/> values of the bundle's <see cref="VertexGraphsSection"/>.
    /// </remarks>
    public uint MapKey { get; set; }

    /// <summary>
    /// Gets/sets the <see cref="ProductTableEntry.Id"/> of one wall pattern in <see cref="WallEdgesSection.WallPatterns"/>, or 0 for none
    /// </summary>
    /// <remarks>
    /// That the two ids are the patterns of the two sides of the wall is a hypothesis from their catalog type (CTProductWallPattern). In 99.79% of the entries examined both are ids of the table or 0.
    /// </remarks>
    public ushort WallPattern1Id { get; set; }

    /// <summary>
    /// Gets/sets the <see cref="ProductTableEntry.Id"/> of the other wall pattern in <see cref="WallEdgesSection.WallPatterns"/>, or 0 for none
    /// </summary>
    public ushort WallPattern2Id { get; set; }

    internal void Write(SectionWriter writer)
    {
        writer.Write(MapKey);
        writer.Write(WallPattern1Id);
        writer.Write(WallPattern2Id);
    }
}
