namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a plex (a unit of a multi-unit lot) of the <see cref="PlexesSection"/>, with the blocks that make up its outline
/// </summary>
/// <remarks>
/// <para>The layout follows the game's reader and writer.</para>
/// <para>Layout, with gates on the section's version: <see cref="Id"/> (<see cref="ushort"/>), the offset within the section at which the plex ends (<see cref="uint"/>, from version 2 on), <see cref="IsVisible"/> (<see cref="uint"/>), a <see cref="uint"/> block count, the <see cref="Blocks"/>,
/// <see cref="Unknown1"/> and <see cref="Unknown2"/> (after 4), the name (version 6: a <see cref="uint"/> byte count and ASCII text), then after 5 <see cref="TileCount"/>, <see cref="Unknown3"/>, <see cref="Unknown4"/> and <see cref="Unknown5"/>.
/// The game writes the end offset as a placeholder and patches it, so that its reader can skip the plex; the offset is not a property, it is checked on reading and computed on writing.</para>
/// </remarks>
public sealed class Plex
{
    internal static Plex Read(ref SectionReader reader, uint version)
    {
        var plex = new Plex { Id = reader.ReadUInt16() };
        var endOffset = version >= 2 ? reader.ReadUInt32() : 0U;
        var visible = reader.ReadUInt32();
        if (visible > 1)
            throw reader.Invalid($"the visibility of plex {plex.Id} is {visible}, expected 0 or 1");
        plex.IsVisible = visible is 1;
        var count = reader.ReadCount(reader.ReadUInt32());
        for (var i = 0; i < count; ++i)
            plex.Blocks.Add(WallBlock.Read(ref reader));
        if (version > 4)
        {
            plex.Unknown1 = reader.ReadUInt64();
            plex.Unknown2 = reader.ReadUInt64();
        }
        if (version > 5)
        {
            if (version == 6)
            {
                var bytes = reader.ReadBytes(reader.ReadCount(reader.ReadUInt32()));
                if (bytes.Any(value => value > 0x7F))
                    throw reader.Invalid($"the name of plex {plex.Id} is not ASCII");
                plex.Name = Encoding.ASCII.GetString(bytes);
            }
            plex.TileCount = reader.ReadUInt32();
            plex.Unknown3 = reader.ReadByte();
            plex.Unknown4 = reader.ReadByte();
            plex.Unknown5 = reader.ReadByte();
        }
        if (version >= 2 && endOffset != reader.Position)
            throw reader.Invalid($"plex {plex.Id} gives its end as offset {endOffset}");
        return plex;
    }

    /// <summary>
    /// Gets the blocks of the plex
    /// </summary>
    /// <remarks>
    /// In the lots examined 1,182 of the 1,363 blocks of plexes have a <see cref="WallBlock.Id"/> that is also the id of a block of the bundle's <see cref="WallBlocksSection"/>; version 4 sections hold block versions 32, 33 and 35, later ones version 36.
    /// </remarks>
    public Collection<WallBlock> Blocks { get; } = [];

    /// <summary>
    /// Gets/sets the id of the plex
    /// </summary>
    /// <remarks>
    /// The game keys its plexes by this id; 300 is the shared plex.
    /// </remarks>
    public ushort Id { get; set; }

    /// <summary>
    /// Gets/sets whether the plex is visible
    /// </summary>
    /// <remarks>
    /// The game keeps the low byte at offset 0x44 of the plex, which its VisibleCallback compares with 1.
    /// </remarks>
    public bool IsVisible { get; set; }

    /// <summary>
    /// Gets/sets the name version 6 stores
    /// </summary>
    /// <remarks>
    /// The game reads it and discards it. Observed values are "Shared Unit" and "Unit 1".
    /// It is stored as a byte count and one byte per character, so it must be ASCII: reading a name with other bytes fails, and so does encoding a name with other characters.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets/sets the number of tiles of the plex
    /// </summary>
    /// <remarks>
    /// The game returns it from its get_plex_tile_count Python function.
    /// Stored from version 6 on.
    /// </remarks>
    public uint TileCount { get; set; }

    /// <summary>
    /// Gets/sets the first of two <see cref="ulong"/> values the game keeps at offsets 0x30 and 0x38 of the plex
    /// </summary>
    /// <remarks>
    /// Hypothesis: the id of the owning household; where it is 0 the game falls back to the lot owner.
    /// The game zeroes both values when loading with one of its load flags. 0 in every lot examined.
    /// Stored from version 5 on.
    /// </remarks>
    public ulong Unknown1 { get; set; }

    /// <summary>
    /// Gets/sets the second of two <see cref="ulong"/> values the game keeps at offsets 0x30 and 0x38 of the plex
    /// </summary>
    /// <remarks>
    /// 0 in every lot examined. Stored from version 5 on.
    /// </remarks>
    public ulong Unknown2 { get; set; }

    /// <summary>
    /// Gets/sets a byte the game keeps at offset 0x45 of the plex
    /// </summary>
    /// <remarks>
    /// The game treats it as bit flags. 0, 1 or 10 in the lots examined.
    /// Stored from version 6 on.
    /// </remarks>
    public byte Unknown3 { get; set; }

    /// <summary>
    /// Gets/sets a byte the game keeps at offset 0x46 of the plex
    /// </summary>
    /// <remarks>
    /// The game copies it from the plex's zone object. 0 in every lot examined.
    /// Stored from version 6 on.
    /// </remarks>
    public byte Unknown4 { get; set; }

    /// <summary>
    /// Gets/sets a byte the game keeps at offset 0x47 of the plex
    /// </summary>
    /// <remarks>
    /// The game copies it from the plex's zone object. 0 in every lot examined.
    /// Stored from version 6 on.
    /// </remarks>
    public byte Unknown5 { get; set; }

    internal void Write(SectionWriter writer, uint version)
    {
        if (version < 5)
        {
            writer.RequireDefault(Unknown1, 0UL, $"the {nameof(Unknown1)} of plex {Id}", version);
            writer.RequireDefault(Unknown2, 0UL, $"the {nameof(Unknown2)} of plex {Id}", version);
        }
        if (version != 6)
            writer.RequireDefault(Name, string.Empty, $"the {nameof(Name)} of plex {Id}", version);
        if (version < 6)
        {
            writer.RequireDefault(TileCount, 0U, $"the {nameof(TileCount)} of plex {Id}", version);
            writer.RequireDefault(Unknown3, (byte)0, $"the {nameof(Unknown3)} of plex {Id}", version);
            writer.RequireDefault(Unknown4, (byte)0, $"the {nameof(Unknown4)} of plex {Id}", version);
            writer.RequireDefault(Unknown5, (byte)0, $"the {nameof(Unknown5)} of plex {Id}", version);
        }
        writer.Write(Id);
        var endOffsetPosition = writer.Position;
        if (version >= 2)
            writer.Write(0);
        writer.Write(IsVisible ? 1U : 0U);
        writer.Write((uint)Blocks.Count);
        foreach (var block in Blocks)
            block.Write(writer);
        if (version > 4)
        {
            writer.Write(Unknown1);
            writer.Write(Unknown2);
        }
        if (version > 5)
        {
            if (version == 6)
            {
                var name = Name ?? string.Empty;
                if (name.Any(character => character > 0x7F))
                    throw writer.Invalid($"the name of plex {Id} is not ASCII");
                var bytes = Encoding.ASCII.GetBytes(name);
                writer.Write((uint)bytes.Length);
                writer.Write(bytes);
            }
            writer.Write(TileCount);
            writer.Write(Unknown3);
            writer.Write(Unknown4);
            writer.Write(Unknown5);
        }
        if (version >= 2)
            writer.PatchInt32(endOffsetPosition, writer.Position);
    }
}
