namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.LegacyWallGraph"/> section: a graph of wall edges with their wall patterns and products, found only in older world files (🔓)
/// </summary>
/// <remarks>
/// <para>No code in the game's simulation library reads or writes this section (other parts of the game were not examined), so the layout comes from the lots examined and every meaning is a hypothesis.
/// The lots examined hold 21 of these sections, all in world files; 17 are empty.
/// The two tables have the shape of the game's <see cref="ProductTable"/>, and in each of the four non-empty sections examined every id is referenced and every <see cref="ProductTableEntry.ReferenceCount"/> equals the number of references in the section.</para>
/// <para>Version 1 is the only one supported, and the only one in the lots examined.</para>
/// <para>Layout: the version (<see cref="uint"/>), the <see cref="Products"/> table, the <see cref="WallPatterns"/> table, then three lists, each an <see cref="int"/> count and its entries: <see cref="Edges"/>, <see cref="ProductEdges1"/> and <see cref="ProductEdges2"/>.</para>
/// </remarks>
public sealed class LegacyWallGraphSection :
    LotArchitectureSection
{
    const uint supportedVersion = 1;

    internal static LegacyWallGraphSection Read(ref SectionReader reader, SectionContext context)
    {
        reader.ReadVersion(supportedVersion, supportedVersion);
        var section = new LegacyWallGraphSection();
        section.Products.Read(ref reader);
        section.WallPatterns.Read(ref reader);
        var count = reader.ReadCount();
        for (var i = 0; i < count; ++i)
            section.Edges.Add(LegacyWallGraphEdge.Read(ref reader));
        ReadProductEdges(ref reader, section.ProductEdges1);
        ReadProductEdges(ref reader, section.ProductEdges2);
        return section;
    }

    static void ReadProductEdges(ref SectionReader reader, Collection<LegacyWallGraphProductEdge> edges)
    {
        var count = reader.ReadCount();
        for (var i = 0; i < count; ++i)
            edges.Add(LegacyWallGraphProductEdge.Read(ref reader));
    }

    static void WriteProductEdges(SectionWriter writer, Collection<LegacyWallGraphProductEdge> edges)
    {
        writer.WriteCount(edges.Count);
        foreach (var edge in edges)
            edge.Write(writer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWallGraphSection"/> class
    /// </summary>
    public LegacyWallGraphSection() :
        base(LotArchitectureSectionType.LegacyWallGraph)
    {
    }

    /// <summary>
    /// Gets the edges with wall parts
    /// </summary>
    public Collection<LegacyWallGraphEdge> Edges { get; } = [];

    /// <summary>
    /// Gets the first list of edges with a product
    /// </summary>
    /// <remarks>
    /// Their products are ceiling rails (CTProductCeilingRail 0x3F0C529A) in every lot examined.
    /// </remarks>
    public Collection<LegacyWallGraphProductEdge> ProductEdges1 { get; } = [];

    /// <summary>
    /// Gets the second list of edges with a product
    /// </summary>
    /// <remarks>
    /// Their products are fences (0x0418FE2A) in every lot examined.
    /// </remarks>
    public Collection<LegacyWallGraphProductEdge> ProductEdges2 { get; } = [];

    /// <summary>
    /// Gets the table of the products the edges refer to
    /// </summary>
    /// <remarks>
    /// In the lots examined it holds fence (0x0418FE2A), CTProductWall (0x9151E6BC) and ceiling rail (CTProductCeilingRail 0x3F0C529A) keys, and its <see cref="ProductTable.DefaultKey"/> is the null key.
    /// </remarks>
    public ProductTable Products { get; } = new();

    /// <summary>
    /// Gets the table of the wall patterns the edge parts refer to
    /// </summary>
    /// <remarks>
    /// In the lots examined it holds only CTProductWallPattern (0xD5F0F921) keys, and its <see cref="ProductTable.DefaultKey"/> is the null key.
    /// </remarks>
    public ProductTable WallPatterns { get; } = new();

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        writer.Write(supportedVersion);
        Products.Write(writer);
        WallPatterns.Write(writer);
        writer.WriteCount(Edges.Count);
        foreach (var edge in Edges)
            edge.Write(writer);
        WriteProductEdges(writer, ProductEdges1);
        WriteProductEdges(writer, ProductEdges2);
    }
}
