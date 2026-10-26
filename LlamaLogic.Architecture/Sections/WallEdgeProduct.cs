namespace LlamaLogic.Architecture;

/// <summary>
/// Represents an entry of <see cref="WallEdgesSection.ProductEdges"/> or <see cref="WallEdgesSection.AdditionalProductEdges"/>: the catalog product along a wall edge
/// </summary>
/// <remarks>
/// Layout: <see cref="MapKey"/>, <see cref="ProductId"/>, as the game's reader reads them.
/// </remarks>
public sealed class WallEdgeProduct
{
    internal static WallEdgeProduct Read(ref SectionReader reader) =>
        new()
        {
            MapKey = reader.ReadUInt32(),
            ProductId = reader.ReadUInt32()
        };

    /// <summary>
    /// Gets/sets the key under which the game maps the entry
    /// </summary>
    /// <remarks>
    /// It is packed like <see cref="WallEdgePatterns.MapKey"/> (hypothesis). In <see cref="WallEdgesSection.ProductEdges"/> the low nibble of byte 0 follows the product's catalog type in the lots examined: 0 for CTProductWall, 1 for ceiling rails, 2 for fences and railings, and 3 for some fences.
    /// </remarks>
    public uint MapKey { get; set; }

    /// <summary>
    /// Gets/sets the <see cref="ProductTableEntry.Id"/> of the product in the table that goes with the list
    /// </summary>
    /// <remarks>
    /// In <see cref="WallEdgesSection.AdditionalProductEdges"/> it is 0 in 99% of the entries examined.
    /// </remarks>
    public uint ProductId { get; set; }

    internal void Write(SectionWriter writer)
    {
        writer.Write(MapKey);
        writer.Write(ProductId);
    }
}
