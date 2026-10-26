namespace LlamaLogic.Architecture;

/// <summary>
/// Represents an edge of a <see cref="VertexGraph"/>
/// </summary>
/// <remarks>
/// Layout: <see cref="Id"/>, <see cref="Vertex1Id"/>, <see cref="Vertex2Id"/>, as the game's writer writes them.
/// </remarks>
public sealed class VertexGraphEdge
{
    internal static VertexGraphEdge Read(ref SectionReader reader) =>
        new()
        {
            Id = reader.ReadInt32(),
            Vertex1Id = reader.ReadInt32(),
            Vertex2Id = reader.ReadInt32()
        };

    /// <summary>
    /// Gets/sets the id of the edge
    /// </summary>
    /// <remarks>
    /// Its bytes appear to pack a level and kind, a direction and two coordinates (hypothesis); the same packing appears in the <see cref="WallEdgesSection"/> map keys.
    /// </remarks>
    public int Id { get; set; }

    /// <summary>
    /// Gets/sets the <see cref="VertexGraphVertex.Id"/> of the edge's first vertex, or -1 for none
    /// </summary>
    public int Vertex1Id { get; set; }

    /// <summary>
    /// Gets/sets the <see cref="VertexGraphVertex.Id"/> of the edge's second vertex, or -1 for none
    /// </summary>
    public int Vertex2Id { get; set; }

    internal void Write(SectionWriter writer)
    {
        writer.Write(Id);
        writer.Write(Vertex1Id);
        writer.Write(Vertex2Id);
    }
}
