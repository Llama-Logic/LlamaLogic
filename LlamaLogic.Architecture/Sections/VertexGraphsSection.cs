namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.VertexGraphs"/> section: graphs of vertices and edges, whose version decides the layout of the <see cref="WallEdgesSection"/> (🔓)
/// </summary>
/// <remarks>
/// <para>Each graph holds vertices, under the tag "TREV" ("VERT" reversed), and edges, under the tag "TCES". What the graphs describe is not established; Sims 4 Studio calls this section WallCutout.</para>
/// <para>In in-game tests the first graph changed whenever walls, roofs, stairs or platforms were added or reshaped (including when a roof was replaced with another shape) and never for paint, trim, terrain or lot type changes; this is an observation only.</para>
/// <para>The layout follows the game's writer; on loading, the game reads only the section's version, which decides the layout of the <see cref="WallEdgesSection"/>, and does not read the graphs.
/// Versions 5 through 9 are supported, and the game writes version 9; the lots examined hold versions 5 through 9.</para>
/// <para>Layout: <see cref="Version"/>, then the <see cref="Graphs"/>: three before version 8 and four from version 8 on, the numbers of graph objects the game's reader resets for those versions.</para>
/// </remarks>
public sealed class VertexGraphsSection :
    LotArchitectureSection
{
    const uint minimumVersion = 5;
    const uint maximumVersion = 9;

    internal static VertexGraphsSection Read(ref SectionReader reader, SectionContext context)
    {
        var section = new VertexGraphsSection { Version = reader.ReadVersion(minimumVersion, maximumVersion) };
        var count = GetGraphCount(section.Version);
        for (var i = 0; i < count; ++i)
            section.Graphs.Add(VertexGraph.Read(ref reader));
        return section;
    }

    static int GetGraphCount(uint version) =>
        version < 8 ? 3 : 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexGraphsSection"/> class
    /// </summary>
    public VertexGraphsSection() :
        base(LotArchitectureSectionType.VertexGraphs)
    {
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets the graphs
    /// </summary>
    /// <remarks>
    /// Their number must match <see cref="Version"/>: three before version 8, four from version 8 on.
    /// </remarks>
    public Collection<VertexGraph> Graphs { get; } = [];

    /// <summary>
    /// Gets/sets the version of the section, which decides its layout and that of the <see cref="WallEdgesSection"/>
    /// </summary>
    /// <remarks>
    /// Encoding throws an <see cref="InvalidOperationException"/> when the number of <see cref="Graphs"/> does not match the version, or when a table or list of the <see cref="WallEdgesSection"/> that the layout of this version lacks is not empty.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 5 through 9</exception>
    public uint Version
    {
        get => version;
        set
        {
            if (value is < minimumVersion or > maximumVersion)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"The version must be {minimumVersion} through {maximumVersion}");
            version = value;
        }
    }

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        var count = GetGraphCount(version);
        if (Graphs.Count != count)
            throw writer.Invalid($"there are {Graphs.Count} graphs, but version {version} stores {count}");
        writer.Write(version);
        foreach (var graph in Graphs)
            graph.Write(writer);
    }
}
