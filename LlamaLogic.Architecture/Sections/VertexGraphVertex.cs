namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a vertex of a <see cref="VertexGraph"/>
/// </summary>
/// <remarks>
/// Layout: <see cref="Id"/>, <see cref="Unknown1"/>, <see cref="Unknown2"/>, <see cref="Unknown3"/>, as the game's writer writes them.
/// </remarks>
public sealed class VertexGraphVertex
{
    internal static VertexGraphVertex Read(ref SectionReader reader) =>
        new()
        {
            Id = reader.ReadInt32(),
            Unknown1 = reader.ReadByte(),
            Unknown2 = reader.ReadByte(),
            Unknown3 = reader.ReadSByte()
        };

    /// <summary>
    /// Gets/sets the id of the vertex, by which <see cref="VertexGraphEdge.Vertex1Id"/> and <see cref="VertexGraphEdge.Vertex2Id"/> refer to it
    /// </summary>
    /// <remarks>
    /// In every lot examined it is <see cref="Unknown1"/> &lt;&lt; 16 | <see cref="Unknown2"/> &lt;&lt; 8 | the byte of <see cref="Unknown3"/>, so it is derived from them; it is stored as read.
    /// </remarks>
    public int Id { get; set; }

    /// <summary>
    /// Gets/sets the first byte of the vertex
    /// </summary>
    /// <remarks>
    /// Hypothesis: a coordinate along the lot's X axis. 1 through 63 in the lots examined.
    /// </remarks>
    public byte Unknown1 { get; set; }

    /// <summary>
    /// Gets/sets the second byte of the vertex
    /// </summary>
    /// <remarks>
    /// Hypothesis: a coordinate along the lot's Z axis. 1 through 63 in the lots examined.
    /// </remarks>
    public byte Unknown2 { get; set; }

    /// <summary>
    /// Gets/sets the third byte of the vertex
    /// </summary>
    /// <remarks>
    /// Hypothesis: a lot level. -4 through 4 in the lots examined.
    /// </remarks>
    public sbyte Unknown3 { get; set; }

    internal void Write(SectionWriter writer)
    {
        writer.Write(Id);
        writer.Write(Unknown1);
        writer.Write(Unknown2);
        writer.Write(Unknown3);
    }
}
