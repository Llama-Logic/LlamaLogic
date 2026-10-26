namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a part of a <see cref="LegacyWallGraphEdge"/>
/// </summary>
/// <remarks>
/// Layout: <see cref="Unknown1"/> through <see cref="Unknown4"/>, <see cref="WallPattern1Id"/>, <see cref="WallPattern2Id"/>, <see cref="ProductId"/>.
/// </remarks>
public sealed class LegacyWallGraphEdgePart
{
    internal static LegacyWallGraphEdgePart Read(ref SectionReader reader) =>
        new()
        {
            Unknown1 = reader.ReadUInt16(),
            Unknown2 = reader.ReadUInt16(),
            Unknown3 = reader.ReadUInt16(),
            Unknown4 = reader.ReadUInt16(),
            WallPattern1Id = reader.ReadUInt32(),
            WallPattern2Id = reader.ReadUInt32(),
            ProductId = reader.ReadUInt32()
        };

    /// <summary>
    /// Gets/sets the <see cref="ProductTableEntry.Id"/> of the part's product in <see cref="LegacyWallGraphSection.Products"/>
    /// </summary>
    /// <remarks>
    /// It refers to a CTProductWall (0x9151E6BC) key in every lot examined.
    /// </remarks>
    public uint ProductId { get; set; }

    /// <summary>
    /// Gets/sets the first <see cref="ushort"/> of the part
    /// </summary>
    /// <remarks>
    /// 0, 1 or 3 in the lots examined.
    /// </remarks>
    public ushort Unknown1 { get; set; }

    /// <summary>
    /// Gets/sets the second <see cref="ushort"/> of the part
    /// </summary>
    /// <remarks>
    /// 0 through 43 in the lots examined.
    /// </remarks>
    public ushort Unknown2 { get; set; }

    /// <summary>
    /// Gets/sets the third <see cref="ushort"/> of the part
    /// </summary>
    /// <remarks>
    /// Equal to <see cref="Unknown4"/> in 1,374 of the 1,590 parts examined, where both are 76, 204, 384, 512, 545, 546 or 640; 0 through 546 otherwise.
    /// </remarks>
    public ushort Unknown3 { get; set; }

    /// <summary>
    /// Gets/sets the fourth <see cref="ushort"/> of the part
    /// </summary>
    public ushort Unknown4 { get; set; }

    /// <summary>
    /// Gets/sets the <see cref="ProductTableEntry.Id"/> of one wall pattern in <see cref="LegacyWallGraphSection.WallPatterns"/>
    /// </summary>
    /// <remarks>
    /// Hypothesis: the patterns of the two sides of the wall.
    /// </remarks>
    public uint WallPattern1Id { get; set; }

    /// <summary>
    /// Gets/sets the <see cref="ProductTableEntry.Id"/> of the other wall pattern in <see cref="LegacyWallGraphSection.WallPatterns"/>
    /// </summary>
    public uint WallPattern2Id { get; set; }

    internal void Write(SectionWriter writer)
    {
        writer.Write(Unknown1);
        writer.Write(Unknown2);
        writer.Write(Unknown3);
        writer.Write(Unknown4);
        writer.Write(WallPattern1Id);
        writer.Write(WallPattern2Id);
        writer.Write(ProductId);
    }
}
