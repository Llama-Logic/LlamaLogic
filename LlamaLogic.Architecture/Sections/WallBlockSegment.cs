namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a segment of a <see cref="WallBlockLoop"/>: the run from one point of the loop to the next, with the catalog products along it
/// </summary>
/// <remarks>
/// <para>The products are ids into the block's <see cref="WallBlock.Products"/> table (0 for none); every nonzero id in the lots examined is an id of that table.
/// The game reads them as nine slots; which slots a block version stores is described with <see cref="WallBlock.Version"/>, and the game fills the slots an older version does not store from the block's <see cref="WallBlock.LegacyKeys"/>.
/// The slot names follow the catalog types the slots refer to in every lot examined (names from the game's CTProduct table).</para>
/// <para>Layout: the loop stores <see cref="X"/> and <see cref="Z"/> of all its segments first, then, per segment, <see cref="Kind"/>, <see cref="Unknown1"/> (versions before 23), the stored slots in slot order, then <see cref="FenceStamp"/> and <see cref="CeilingRailStamp"/> (versions after 25).</para>
/// </remarks>
public sealed class WallBlockSegment
{
    internal static WallBlockSegment ReadPoint(ref SectionReader reader) =>
        new()
        {
            X = reader.ReadByte(),
            Z = reader.ReadByte()
        };

    /// <summary>
    /// Gets/sets the id of the ceiling rail (CTProductCeilingRail 0x3F0C529A), slot 3
    /// </summary>
    /// <remarks>
    /// This is the product other tools call a spandrel.
    /// </remarks>
    public uint CeilingRailProductId { get; set; }

    /// <summary>
    /// Gets/sets the value the game keeps with the ceiling rail slot
    /// </summary>
    /// <remarks>
    /// The game returns it with slot 3.
    /// Adding a foundation set it, together with <see cref="FenceStamp"/>, to the value <see cref="WallBlocksSection.StampCounter"/> was raised to (in-game test); what the game uses it for is unknown.
    /// It equals <see cref="FenceStamp"/> in 66% of the segments of the <see cref="WallBlocksSection"/> blocks examined and in 81% of those of the <see cref="PlexesSection"/> blocks.
    /// Stored from version 26 on; older versions store one value for the whole block, <see cref="WallBlock.LegacyStamp"/>.
    /// </remarks>
    public uint CeilingRailStamp { get; set; }

    /// <summary>
    /// Gets/sets the id of the fence (Fence 0x0418FE2A) or half wall (CTProductWall 0x9151E6BC), slot 2
    /// </summary>
    /// <remarks>
    /// The game emits CantPlacePoolUnderFence when a pool would go under a segment with this slot set.
    /// </remarks>
    public uint FenceProductId { get; set; }

    /// <summary>
    /// Gets/sets the value the game keeps with the fence slot
    /// </summary>
    /// <remarks>
    /// The game returns it with slot 2.
    /// Adding a foundation set it, together with <see cref="CeilingRailStamp"/>, to the value <see cref="WallBlocksSection.StampCounter"/> was raised to (in-game test); what the game uses it for is unknown.
    /// Stored from version 26 on; older versions store one value for the whole block, <see cref="WallBlock.LegacyStamp"/>.
    /// </remarks>
    public uint FenceStamp { get; set; }

    /// <summary>
    /// Gets/sets the id of the floor trim (0x84C23219), slot 6
    /// </summary>
    /// <remarks>
    /// Never 0 in the lots examined.
    /// </remarks>
    public uint FloorTrimProductId { get; set; }

    /// <summary>
    /// Gets/sets the id of the foundation (0x2FAE983E), slot 4
    /// </summary>
    /// <remarks>
    /// The game requires the same foundation on all segments of a block, otherwise InconsistentFoundationTypes.
    /// Never 0 in the lots examined.
    /// </remarks>
    public uint FoundationProductId { get; set; }

    /// <summary>
    /// Gets/sets the id of the frieze (0xA057811C), slot 5
    /// </summary>
    /// <remarks>
    /// Putting a frieze on one wall in game set this slot of that wall's segment to the chosen frieze and cleared the default frieze (0x1F1E) from the room's other segments; painting one tile of that wall afterwards left the chosen frieze in place (in-game tests).
    /// </remarks>
    public uint FriezeProductId { get; set; }

    /// <summary>
    /// Gets/sets the kind of the segment
    /// </summary>
    /// <remarks>
    /// Stored as read; see <see cref="WallBlockSegmentKind"/> for how the game maps the values of versions before 27.
    /// </remarks>
    public WallBlockSegmentKind Kind { get; set; }

    /// <summary>
    /// Gets/sets the id of the platform trim (CTProductPlatformTrim 0x99C07284), slot 8
    /// </summary>
    /// <remarks>
    /// Changing a platform's edge trim in game changed only this slot (in-game test).
    /// When it reads a pool block of version 30 or 31, the game copies this slot into <see cref="TrimProductId"/>.
    /// 0 in every version 31 block examined.
    /// </remarks>
    public uint PlatformTrimProductId { get; set; }

    /// <summary>
    /// Gets/sets the id of the wall pattern (CTProductWallPattern 0xD5F0F921) of one side, slot 0
    /// </summary>
    /// <remarks>
    /// Painting one wall tile split its segment out and gave this slot a new wall pattern in one in-game test, and <see cref="SideBWallPatternId"/> in another: which of the two slots changes depends on the face painted.
    /// </remarks>
    public uint SideAWallPatternId { get; set; }

    /// <summary>
    /// Gets/sets the id of the wall pattern (CTProductWallPattern 0xD5F0F921) of the other side, slot 1
    /// </summary>
    /// <remarks>
    /// Painting one wall tile split its segment out and gave this slot a new wall pattern in one in-game test, and <see cref="SideAWallPatternId"/> in another: which of the two slots changes depends on the face painted.
    /// That this is the side opposite <see cref="SideAWallPatternId"/> is a hypothesis from the slot's catalog type.
    /// </remarks>
    public uint SideBWallPatternId { get; set; }

    /// <summary>
    /// Gets/sets the id of the trim, slot 7: a half wall trim (CTProductHalfWallTrim 0xFE33068E), pool trim (0xA5DFFCF3) or fountain trim (0xE7ADA79D)
    /// </summary>
    /// <remarks>
    /// In the lots examined fountain trims appear only in <see cref="WallBlockType.Fountain"/> blocks, half wall trims only in <see cref="WallBlockType.Standard"/> and <see cref="WallBlockType.Pool"/> blocks, and pool trims mostly in <see cref="WallBlockType.Pool"/> blocks (2,267 of 2,785 segments) and otherwise in <see cref="WallBlockType.Standard"/> and <see cref="WallBlockType.Fountain"/> blocks.
    /// </remarks>
    public uint TrimProductId { get; set; }

    /// <summary>
    /// Gets/sets a byte versions before 23 store after <see cref="Kind"/>
    /// </summary>
    /// <remarks>
    /// The game reads it and discards it.
    /// </remarks>
    public byte Unknown1 { get; set; }

    /// <summary>
    /// Gets/sets the coordinate of the segment's start point along the lot's X axis
    /// </summary>
    public byte X { get; set; }

    /// <summary>
    /// Gets/sets the coordinate of the segment's start point along the lot's Z axis
    /// </summary>
    public byte Z { get; set; }

    uint GetSlot(int slot) =>
        slot switch
        {
            0 => SideAWallPatternId,
            1 => SideBWallPatternId,
            2 => FenceProductId,
            3 => CeilingRailProductId,
            4 => FoundationProductId,
            5 => FriezeProductId,
            6 => FloorTrimProductId,
            7 => TrimProductId,
            _ => PlatformTrimProductId
        };

    internal void ReadProducts(ref SectionReader reader, ushort version)
    {
        Kind = (WallBlockSegmentKind)reader.ReadByte();
        if (version < 23)
            Unknown1 = reader.ReadByte();
        var slotCount = WallBlock.GetSlotCount(version);
        for (var slot = 0; slot < slotCount; ++slot)
            SetSlot(slot, reader.ReadUInt32());
        if (version > 25)
        {
            FenceStamp = reader.ReadUInt32();
            CeilingRailStamp = reader.ReadUInt32();
        }
    }

    internal void RequireDefaults(SectionWriter writer, ushort version, ushort blockId)
    {
        if (version > 22)
            writer.RequireDefault(Unknown1, (byte)0, $"the {nameof(Unknown1)} of a segment of block {blockId}", version);
        for (var slot = WallBlock.GetSlotCount(version); slot < 9; ++slot)
            writer.RequireDefault(GetSlot(slot), 0U, $"product slot {slot} of a segment of block {blockId}", version);
        if (version < 26)
        {
            writer.RequireDefault(FenceStamp, 0U, $"the {nameof(FenceStamp)} of a segment of block {blockId}", version);
            writer.RequireDefault(CeilingRailStamp, 0U, $"the {nameof(CeilingRailStamp)} of a segment of block {blockId}", version);
        }
    }

    void SetSlot(int slot, uint value)
    {
        switch (slot)
        {
            case 0:
                SideAWallPatternId = value;
                break;
            case 1:
                SideBWallPatternId = value;
                break;
            case 2:
                FenceProductId = value;
                break;
            case 3:
                CeilingRailProductId = value;
                break;
            case 4:
                FoundationProductId = value;
                break;
            case 5:
                FriezeProductId = value;
                break;
            case 6:
                FloorTrimProductId = value;
                break;
            case 7:
                TrimProductId = value;
                break;
            default:
                PlatformTrimProductId = value;
                break;
        }
    }

    internal void WritePoint(SectionWriter writer)
    {
        writer.Write(X);
        writer.Write(Z);
    }

    internal void WriteProducts(SectionWriter writer, ushort version)
    {
        writer.Write((byte)Kind);
        if (version < 23)
            writer.Write(Unknown1);
        var slotCount = WallBlock.GetSlotCount(version);
        for (var slot = 0; slot < slotCount; ++slot)
            writer.Write(GetSlot(slot));
        if (version > 25)
        {
            writer.Write(FenceStamp);
            writer.Write(CeilingRailStamp);
        }
    }
}
