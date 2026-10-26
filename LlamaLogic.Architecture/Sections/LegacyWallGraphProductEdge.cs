namespace LlamaLogic.Architecture;

/// <summary>
/// Represents an edge of <see cref="LegacyWallGraphSection.ProductEdges1"/> or <see cref="LegacyWallGraphSection.ProductEdges2"/>
/// </summary>
/// <remarks>
/// Layout: <see cref="Code"/>, <see cref="Unknown1"/> through <see cref="Unknown5"/>, <see cref="ProductId"/>.
/// </remarks>
public sealed class LegacyWallGraphProductEdge
{
    internal static LegacyWallGraphProductEdge Read(ref SectionReader reader) =>
        new()
        {
            Code = reader.ReadUInt32(),
            Unknown1 = reader.ReadUInt32(),
            Unknown2 = reader.ReadUInt32(),
            Unknown3 = reader.ReadUInt32(),
            Unknown4 = reader.ReadUInt32(),
            Unknown5 = reader.ReadUInt32(),
            ProductId = reader.ReadUInt32()
        };

    /// <summary>
    /// Gets/sets the code of the edge
    /// </summary>
    /// <remarks>
    /// Packed like <see cref="LegacyWallGraphEdge.Code"/> (hypothesis).
    /// </remarks>
    public uint Code { get; set; }

    /// <summary>
    /// Gets/sets the <see cref="ProductTableEntry.Id"/> of the edge's product in <see cref="LegacyWallGraphSection.Products"/>
    /// </summary>
    public uint ProductId { get; set; }

    /// <summary>
    /// Gets/sets the first <see cref="uint"/> after <see cref="Code"/>
    /// </summary>
    /// <remarks>
    /// 1 in every lot examined.
    /// </remarks>
    public uint Unknown1 { get; set; }

    /// <summary>
    /// Gets/sets the second <see cref="uint"/> after <see cref="Code"/>
    /// </summary>
    /// <remarks>
    /// 0 in every lot examined.
    /// </remarks>
    public uint Unknown2 { get; set; }

    /// <summary>
    /// Gets/sets the third <see cref="uint"/> after <see cref="Code"/>
    /// </summary>
    /// <remarks>
    /// 0xCCCCCCCC in every lot examined, the fill pattern of uninitialized memory in debug builds of the compiler the game uses; hypothesis: not meaningful.
    /// </remarks>
    public uint Unknown3 { get; set; }

    /// <summary>
    /// Gets/sets the fourth <see cref="uint"/> after <see cref="Code"/>
    /// </summary>
    /// <remarks>
    /// 0xCCCCCCCC in every lot examined, like <see cref="Unknown3"/>.
    /// </remarks>
    public uint Unknown4 { get; set; }

    /// <summary>
    /// Gets/sets the fifth <see cref="uint"/> after <see cref="Code"/>
    /// </summary>
    /// <remarks>
    /// 0xCCCCCCCC in every lot examined, like <see cref="Unknown3"/>.
    /// </remarks>
    public uint Unknown5 { get; set; }

    internal void Write(SectionWriter writer)
    {
        writer.Write(Code);
        writer.Write(Unknown1);
        writer.Write(Unknown2);
        writer.Write(Unknown3);
        writer.Write(Unknown4);
        writer.Write(Unknown5);
        writer.Write(ProductId);
    }
}
