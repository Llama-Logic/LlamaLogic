namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.WallEdges"/> section: the wall patterns and catalog products along the lot's wall edges (🔓)
/// </summary>
/// <remarks>
/// <para>The game reads and writes the section right after the <see cref="VertexGraphsSection"/>, whose <see cref="VertexGraphsSection.Version"/> decides the layout; so the section can only be read and written together with that section.
/// The section has no version of its own; the game writes it for a <see cref="VertexGraphsSection"/> of version 9.</para>
/// <para>Layout: a <see cref="uint"/> count, the <see cref="WallPatternEdges"/> and the <see cref="WallPatterns"/> table;
/// if the vertex graphs version is above 1, the <see cref="Products"/> table, a <see cref="uint"/> count and the <see cref="ProductEdges"/>;
/// if it is 6, a <see cref="uint"/> count and the <see cref="LegacyEntries"/>;
/// if it is above 8, the <see cref="AdditionalProducts"/> table, a <see cref="uint"/> count and the <see cref="AdditionalProductEdges"/>.</para>
/// <para>Encoding throws an <see cref="InvalidOperationException"/> when a table or list that the layout of the vertex graphs version lacks is not empty.</para>
/// <para>Other tools detect the optional parts by the length that remains; in every lot examined that gives the same split as the version gates.</para>
/// </remarks>
public sealed class WallEdgesSection :
    LotArchitectureSection
{
    internal static WallEdgesSection Read(ref SectionReader reader, SectionContext context)
    {
        var graphsVersion = context.RequireVertexGraphsVersion(LotArchitectureSectionType.WallEdges);
        var section = new WallEdgesSection();
        var count = reader.ReadCount(reader.ReadUInt32());
        for (var i = 0; i < count; ++i)
            section.WallPatternEdges.Add(WallEdgePatterns.Read(ref reader));
        section.WallPatterns.Read(ref reader);
        if (graphsVersion > 1)
            ReadProductEdges(ref reader, section.Products, section.ProductEdges);
        if (graphsVersion == 6)
        {
            var legacyCount = reader.ReadCount(reader.ReadUInt32());
            for (var i = 0; i < legacyCount; ++i)
                section.LegacyEntries.Add(WallEdgesLegacyEntry.Read(ref reader));
        }
        if (graphsVersion > 8)
            ReadProductEdges(ref reader, section.AdditionalProducts, section.AdditionalProductEdges);
        return section;
    }

    static void ReadProductEdges(ref SectionReader reader, ProductTable products, Collection<WallEdgeProduct> edges)
    {
        products.Read(ref reader);
        var count = reader.ReadCount(reader.ReadUInt32());
        for (var i = 0; i < count; ++i)
            edges.Add(WallEdgeProduct.Read(ref reader));
    }

    static void RequireEmpty(SectionWriter writer, ProductTable products, Collection<WallEdgeProduct> edges, string productsName, string edgesName, uint graphsVersion)
    {
        if (products.Entries.Count != 0 || !products.DefaultKey.Equals(default(ResourceKey)))
            throw writer.Invalid($"{productsName} is not empty, which the layout of vertex graphs version {graphsVersion} cannot store");
        if (edges.Count != 0)
            throw writer.Invalid($"{edgesName} holds {edges.Count} entries, which the layout of vertex graphs version {graphsVersion} cannot store");
    }

    static void WriteProductEdges(SectionWriter writer, ProductTable products, Collection<WallEdgeProduct> edges)
    {
        products.Write(writer);
        writer.Write((uint)edges.Count);
        foreach (var edge in edges)
            edge.Write(writer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WallEdgesSection"/> class
    /// </summary>
    public WallEdgesSection() :
        base(LotArchitectureSectionType.WallEdges)
    {
    }

    /// <summary>
    /// Gets the entries of the third product map
    /// </summary>
    /// <remarks>
    /// What the map is for is unknown. Stored when the vertex graphs version is above 8.
    /// </remarks>
    public Collection<WallEdgeProduct> AdditionalProductEdges { get; } = [];

    /// <summary>
    /// Gets the table of the catalog products <see cref="AdditionalProductEdges"/> refers to
    /// </summary>
    /// <remarks>
    /// It holds at most 3 keys, all CTProductWall (0x9151E6BC), in the lots examined. Stored when the vertex graphs version is above 8.
    /// </remarks>
    public ProductTable AdditionalProducts { get; } = new();

    /// <summary>
    /// Gets the entries of a list the game reads and discards
    /// </summary>
    /// <remarks>
    /// Stored when the vertex graphs version is 6; two lots examined hold entries (1,284 in all).
    /// </remarks>
    public Collection<WallEdgesLegacyEntry> LegacyEntries { get; } = [];

    /// <summary>
    /// Gets the catalog products along the wall edges
    /// </summary>
    /// <remarks>
    /// Stored when the vertex graphs version is above 1.
    /// </remarks>
    public Collection<WallEdgeProduct> ProductEdges { get; } = [];

    /// <summary>
    /// Gets the table of the catalog products <see cref="ProductEdges"/> refers to
    /// </summary>
    /// <remarks>
    /// In the lots examined it holds CTProductWall (0x9151E6BC), fence (0x0418FE2A), ceiling rail (CTProductCeilingRail 0x3F0C529A) and railing (0x1C1CF1F7) keys.
    /// Stored when the vertex graphs version is above 1.
    /// </remarks>
    public ProductTable Products { get; } = new();

    /// <summary>
    /// Gets the wall patterns of the wall edges
    /// </summary>
    public Collection<WallEdgePatterns> WallPatternEdges { get; } = [];

    /// <summary>
    /// Gets the table of the wall patterns (CTProductWallPattern 0xD5F0F921) <see cref="WallPatternEdges"/> refers to
    /// </summary>
    public ProductTable WallPatterns { get; } = new();

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        var graphsVersion = context.RequireVertexGraphsVersion(LotArchitectureSectionType.WallEdges);
        if (graphsVersion < 2)
            RequireEmpty(writer, Products, ProductEdges, nameof(Products), nameof(ProductEdges), graphsVersion);
        if (graphsVersion != 6 && LegacyEntries.Count != 0)
            throw writer.Invalid($"{nameof(LegacyEntries)} holds {LegacyEntries.Count} entries, which the layout of vertex graphs version {graphsVersion} cannot store");
        if (graphsVersion < 9)
            RequireEmpty(writer, AdditionalProducts, AdditionalProductEdges, nameof(AdditionalProducts), nameof(AdditionalProductEdges), graphsVersion);
        writer.Write((uint)WallPatternEdges.Count);
        foreach (var entry in WallPatternEdges)
            entry.Write(writer);
        WallPatterns.Write(writer);
        if (graphsVersion > 1)
            WriteProductEdges(writer, Products, ProductEdges);
        if (graphsVersion == 6)
        {
            writer.Write((uint)LegacyEntries.Count);
            foreach (var entry in LegacyEntries)
                entry.Write(writer);
        }
        if (graphsVersion > 8)
            WriteProductEdges(writer, AdditionalProducts, AdditionalProductEdges);
    }
}
