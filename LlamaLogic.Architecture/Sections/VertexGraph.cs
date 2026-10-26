namespace LlamaLogic.Architecture;

/// <summary>
/// Represents one of the graphs of vertices and edges of the <see cref="VertexGraphsSection"/>
/// </summary>
/// <remarks>
/// The layout follows the game's writer; no code in the game's simulation library reads a graph back (other parts of the game were not examined). What each graph of the section describes is not established.
/// Layout: <see cref="Unknown1"/>, the tag "TREV", an <see cref="int"/> count, the <see cref="Vertices"/>, the tag "TCES", an <see cref="int"/> count, then the <see cref="Edges"/>.
/// </remarks>
public sealed class VertexGraph
{
    const uint vertexTag = 0x56455254;
    const uint edgeTag = 0x53454354;

    internal static VertexGraph Read(ref SectionReader reader)
    {
        var graph = new VertexGraph { Unknown1 = reader.ReadUInt32() };
        if (reader.ReadUInt32() != vertexTag)
            throw reader.Invalid("the vertex tag TREV is missing");
        var vertexCount = reader.ReadCount();
        for (var i = 0; i < vertexCount; ++i)
            graph.Vertices.Add(VertexGraphVertex.Read(ref reader));
        if (reader.ReadUInt32() != edgeTag)
            throw reader.Invalid("the edge tag TCES is missing");
        var edgeCount = reader.ReadCount();
        for (var i = 0; i < edgeCount; ++i)
            graph.Edges.Add(VertexGraphEdge.Read(ref reader));
        return graph;
    }

    /// <summary>
    /// Gets the edges
    /// </summary>
    public Collection<VertexGraphEdge> Edges { get; } = [];

    /// <summary>
    /// Gets/sets the value the game writes before the vertices
    /// </summary>
    /// <remarks>
    /// The game always writes 5; hypothesis: a version of the graph. 5 in every lot examined.
    /// </remarks>
    public uint Unknown1 { get; set; } = 5;

    /// <summary>
    /// Gets the vertices
    /// </summary>
    public Collection<VertexGraphVertex> Vertices { get; } = [];

    internal void Write(SectionWriter writer)
    {
        writer.Write(Unknown1);
        writer.Write(vertexTag);
        writer.WriteCount(Vertices.Count);
        foreach (var vertex in Vertices)
            vertex.Write(writer);
        writer.Write(edgeTag);
        writer.WriteCount(Edges.Count);
        foreach (var edge in Edges)
            edge.Write(writer);
    }
}
