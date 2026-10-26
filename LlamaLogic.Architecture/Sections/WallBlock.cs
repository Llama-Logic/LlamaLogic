namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a block: a room, platform, wall line, fence line, pool or fountain, as loops of segments on one level, with the catalog products along them
/// </summary>
/// <remarks>
/// <para>This is the game's WallInfo, which <see cref="WallBlocksSection"/> and <see cref="PlexesSection"/> both store; the layout follows the game's reader and writer.
/// Versions 17 through 36 are supported, and the game writes version 36. The game reads versions 18 through 36; in the lots examined, version 17 appears only where the <see cref="LotSizeSection"/> is version 2, and its layout is the game's layout for version 18 plus <see cref="Unknown1"/>.
/// The lots examined hold versions 17, 22, 29, 31, 32, 33, 35 and 36; the layouts of the other versions follow the game's reader.</para>
/// <para>Layout, with version gates: <see cref="Version"/> (<see cref="ushort"/>), <see cref="Id"/>, <see cref="Level"/>, <see cref="Unknown1"/> (17), <see cref="BlockType"/> (after 24), <see cref="PlexId"/> (after 35), <see cref="HasFloor"/>, <see cref="Unknown3"/> (after 27), <see cref="BaseElevation"/>, <see cref="FloorHeightOffset"/> (after 33), <see cref="Unknown4"/> (31 and 32),
/// then before 27: <see cref="Unknown5"/>, <see cref="Unknown6"/> (before 22), <see cref="LegacyKeys"/> (type, group, instance), <see cref="Unknown7"/>, <see cref="Unknown8"/>;
/// then a <see cref="ushort"/> loop count (after 23; one loop before), the <see cref="Loops"/>, <see cref="Unknown9"/> (before 19), <see cref="Products"/> (after 19), <see cref="LegacyStamp"/> (12 through 25), and before 27 <see cref="Unknown10"/>, <see cref="Unknown11"/> and <see cref="Unknown12"/>.</para>
/// <para>Each segment stores these product slots: none before version 20 (the game takes all from <see cref="LegacyKeys"/>), slots 0 through 3 in versions 20 through 26 (the game takes slots 4 through 8 from <see cref="LegacyKeys"/>), 0 through 6 in versions 27 and 28, 0 through 7 in versions 29 and 32 through 34, and all nine in versions 30, 31, 35 and 36.</para>
/// </remarks>
public sealed class WallBlock
{
    const ushort minimumVersion = 17;
    const ushort maximumVersion = 36;

    internal static WallBlock Read(ref SectionReader reader)
    {
        var version = reader.ReadUInt16();
        if (version is < minimumVersion or > maximumVersion)
            throw reader.UnsupportedVersion($"block version {version} is not supported, expected {minimumVersion} through {maximumVersion}");
        var block = new WallBlock
        {
            Version = version,
            Id = reader.ReadUInt16(),
            Level = reader.ReadSByte()
        };
        if (version == 17)
            block.Unknown1 = reader.ReadByte();
        if (version > 24)
            block.BlockType = (WallBlockType)reader.ReadByte();
        if (version > 35)
            block.PlexId = reader.ReadUInt16();
        block.HasFloor = reader.ReadBoolean();
        if (version > 27)
            block.Unknown3 = reader.ReadBoolean();
        block.BaseElevation = reader.ReadSingle();
        if (version > 33)
            block.FloorHeightOffset = reader.ReadSingle();
        if (version is 31 or 32)
            block.Unknown4 = reader.ReadByte();
        if (version < 27)
        {
            block.Unknown5 = reader.ReadByte();
            if (version < 22)
                block.Unknown6 = reader.ReadByte();
            var keyCount = GetLegacyKeyCount(version);
            for (var i = 0; i < keyCount; ++i)
                block.LegacyKeys.Add(reader.ReadKeyTypeGroupInstance());
            block.Unknown7 = reader.ReadBoolean();
            block.Unknown8 = reader.ReadBoolean();
        }
        var loopCount = version > 23 ? reader.ReadCount(reader.ReadUInt16()) : 1;
        for (var i = 0; i < loopCount; ++i)
            block.Loops.Add(WallBlockLoop.Read(ref reader, version));
        if (version < 19)
            block.Unknown9 = reader.ReadByte();
        if (version > 19)
            block.Products.Read(ref reader);
        if (version is >= 12 and <= 25)
            block.LegacyStamp = reader.ReadUInt32();
        if (version < 27)
        {
            block.Unknown10 = reader.ReadUInt16();
            block.Unknown11 = reader.ReadByte();
            block.Unknown12 = reader.ReadByte();
        }
        return block;
    }

    static int GetLegacyKeyCount(ushort version) =>
        version switch
        {
            < 21 => 15,
            < 27 => 9,
            _ => 0
        };

    internal static int GetSlotCount(ushort version) =>
        version switch
        {
            < 20 => 0,
            < 27 => 4,
            < 29 => 7,
            30 or 31 or 35 or 36 => 9,
            _ => 8
        };

    ushort version = maximumVersion;

    /// <summary>
    /// Gets/sets the elevation of the block's base, in meters
    /// </summary>
    /// <remarks>
    /// The game sets it to the elevation of <see cref="Level"/> plus the lot's offset; adding a foundation raised it by 0.25 (in-game test).
    /// It is −0.375 or −0.125 for fountains in the lots examined, and it was −3 for a new pool (in-game test).
    /// </remarks>
    public float BaseElevation { get; set; }

    /// <summary>
    /// Gets/sets the type of the block
    /// </summary>
    /// <remarks>
    /// Stored from version 25 on; the game treats older blocks as <see cref="WallBlockType.Standard"/>.
    /// </remarks>
    public WallBlockType BlockType { get; set; }

    /// <summary>
    /// Gets/sets the offset of the floor from <see cref="BaseElevation"/>, in meters
    /// </summary>
    /// <remarks>
    /// The game checks it with FloorHeightOffsetOutOfBounds before setting it and takes the block's bottom as the lower of <see cref="BaseElevation"/> and <see cref="BaseElevation"/> plus this offset.
    /// Raising a room's floor one step changed it from 0 to 0.25, and a platform had 0.25 when drawn and 0.5 after one raise (in-game tests); the game stores a platform as a block like a room, so a raised room floor and a platform differ in the data only by their segments, which are <see cref="WallBlockSegmentKind.Open"/> where a platform has no walls.
    /// Stored from version 34 on.
    /// </remarks>
    public float FloorHeightOffset { get; set; }

    /// <summary>
    /// Gets/sets whether the block has a floor
    /// </summary>
    /// <remarks>
    /// Deleting the entire floor of a room changed only this value of the room's block, from <see langword="true"/> to <see langword="false"/> (in-game test).
    /// The game keeps it at offset 0x80 of the block, sets it to whether the level is at most 0 when it copies blocks into a plex and needs it for <see cref="FloorHeightOffset"/>.
    /// In the lots examined it is 1 for every pool and fountain block, for 36,076 of the 51,425 <see cref="WallBlockType.Standard"/> blocks and for 5,271 of the 11,629 <see cref="WallBlockType.Fence"/> blocks.
    /// </remarks>
    public bool HasFloor { get; set; }

    /// <summary>
    /// Gets/sets the id of the block
    /// </summary>
    /// <remarks>
    /// The game keys its blocks by this id and drops a block whose id is 0.
    /// The game renumbers the blocks when it saves: the same rooms had different ids from one save to the next (in-game test).
    /// </remarks>
    public ushort Id { get; set; }

    /// <summary>
    /// Gets the catalog keys that versions before 27 store for the whole block
    /// </summary>
    /// <remarks>
    /// <para>Versions before 21 store 15 keys, versions 21 through 26 store 9.
    /// Of 15, the game uses positions 1, 2 and 5 for slots 0, 1 and 2, positions 3 and 4 for slot 7 (the later one wins), position 8 for slot 3 and positions 12 through 14 for slots 4 through 6; of 9, it uses positions 6 through 8 for slots 4 through 6.
    /// It reads the other positions as four <see cref="uint"/> values and discards them; in the lots examined they hold floor, window set and object catalog keys or the null key.</para>
    /// </remarks>
    public Collection<ResourceKey> LegacyKeys { get; } = [];

    /// <summary>
    /// Gets/sets the value versions 12 through 25 store once for the whole block in place of each segment's <see cref="WallBlockSegment.FenceStamp"/> and <see cref="WallBlockSegment.CeilingRailStamp"/>
    /// </summary>
    /// <remarks>
    /// The game copies it into both values of every segment.
    /// </remarks>
    public uint LegacyStamp { get; set; }

    /// <summary>
    /// Gets/sets the lot level of the block
    /// </summary>
    /// <remarks>
    /// The game restricts the level by <see cref="BlockType"/> and takes the block's level height from it.
    /// </remarks>
    public sbyte Level { get; set; }

    /// <summary>
    /// Gets the loops of the block
    /// </summary>
    /// <remarks>
    /// Versions before 24 hold exactly one loop. 1 through 12 in the lots examined.
    /// </remarks>
    public Collection<WallBlockLoop> Loops { get; } = [];

    /// <summary>
    /// Gets/sets the id of the plex (unit) the block belongs to
    /// </summary>
    /// <remarks>
    /// The game's plex manager sets it; 300 is the shared plex.
    /// The values match the ids of the <see cref="PlexesSection"/> plexes.
    /// Stored from version 36 on.
    /// </remarks>
    public ushort PlexId { get; set; }

    /// <summary>
    /// Gets the table of the catalog products the segments refer to
    /// </summary>
    /// <remarks>
    /// Stored from version 20 on.
    /// </remarks>
    public ProductTable Products { get; } = new();

    /// <summary>
    /// Gets/sets a byte version 17 stores after <see cref="Level"/>
    /// </summary>
    /// <remarks>
    /// It equals <see cref="Level"/> in every lot examined. The game's reader has no such byte.
    /// </remarks>
    public byte Unknown1 { get; set; }

    /// <summary>
    /// Gets/sets a Boolean the game keeps at offset 0x81 of the block
    /// </summary>
    /// <remarks>
    /// Hypothesis: whether the block has an automatic ceiling. The game sets it to whether every outer segment has a wall or the level is below 0, and clears it when it copies blocks into a plex.
    /// In the lots examined it is 0 for every <see cref="WallBlockType.Fence"/> block, 1 for every fountain block, 1 for 466 of the 482 pool blocks and for 22,161 of the 51,182 <see cref="WallBlockType.Standard"/> blocks that store it.
    /// In in-game tests a new fence block and a platform had 0 and enclosed rooms had 1, which is consistent with the hypothesis.
    /// Stored from version 28 on.
    /// </remarks>
    public bool Unknown3 { get; set; }

    /// <summary>
    /// Gets/sets a byte versions 31 and 32 store after <see cref="BaseElevation"/>
    /// </summary>
    /// <remarks>
    /// The game reads it and discards it. 0 in every version 31 block examined.
    /// </remarks>
    public byte Unknown4 { get; set; }

    /// <summary>
    /// Gets/sets a byte versions before 27 store before <see cref="LegacyKeys"/>
    /// </summary>
    /// <remarks>
    /// The game reads it and discards it. 2 in every lot examined.
    /// </remarks>
    public byte Unknown5 { get; set; }

    /// <summary>
    /// Gets/sets a byte versions before 22 store before <see cref="LegacyKeys"/>
    /// </summary>
    /// <remarks>
    /// The game reads it and discards it. 0 or 2 in the lots examined.
    /// </remarks>
    public byte Unknown6 { get; set; }

    /// <summary>
    /// Gets/sets the first of two Booleans versions before 27 store after <see cref="LegacyKeys"/>
    /// </summary>
    /// <remarks>
    /// The game reads it as a Boolean and discards it.
    /// </remarks>
    public bool Unknown7 { get; set; }

    /// <summary>
    /// Gets/sets the second of two Booleans versions before 27 store after <see cref="LegacyKeys"/>
    /// </summary>
    /// <remarks>
    /// The game reads it as a Boolean and discards it.
    /// </remarks>
    public bool Unknown8 { get; set; }

    /// <summary>
    /// Gets/sets a byte versions before 19 store after the <see cref="Loops"/>
    /// </summary>
    /// <remarks>
    /// The game's reader would discard it. 0 through 3 in the lots examined.
    /// </remarks>
    public byte Unknown9 { get; set; }

    /// <summary>
    /// Gets/sets a <see cref="ushort"/> versions before 27 store at the end of the block
    /// </summary>
    /// <remarks>
    /// The game reads it and discards it. 4 in every lot examined.
    /// </remarks>
    public ushort Unknown10 { get; set; }

    /// <summary>
    /// Gets/sets a byte versions before 27 store after <see cref="Unknown10"/>
    /// </summary>
    /// <remarks>
    /// The game reads it and discards it. 1 in every lot examined.
    /// </remarks>
    public byte Unknown11 { get; set; }

    /// <summary>
    /// Gets/sets a byte versions before 27 store after <see cref="Unknown11"/>
    /// </summary>
    /// <remarks>
    /// The game reads it and discards it. 1 in every lot examined.
    /// </remarks>
    public byte Unknown12 { get; set; }

    /// <summary>
    /// Gets/sets the version of the block, which decides its layout
    /// </summary>
    /// <remarks>
    /// Encoding throws an <see cref="InvalidOperationException"/> when a field the version lacks holds a value other than its default, or a collection the version lacks is not empty.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 17 through 36</exception>
    public ushort Version
    {
        get => version;
        set
        {
            if (value is < minimumVersion or > maximumVersion)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"The version must be {minimumVersion} through {maximumVersion}");
            version = value;
        }
    }

    void RequireDefaults(SectionWriter writer)
    {
        if (version != 17)
            writer.RequireDefault(Unknown1, (byte)0, $"the {nameof(Unknown1)} of block {Id}", version);
        if (version < 25)
            writer.RequireDefault(BlockType, WallBlockType.Standard, $"the {nameof(BlockType)} of block {Id}", version);
        if (version < 36)
            writer.RequireDefault(PlexId, (ushort)0, $"the {nameof(PlexId)} of block {Id}", version);
        if (version < 28)
            writer.RequireDefault(Unknown3, false, $"the {nameof(Unknown3)} of block {Id}", version);
        if (version < 34)
            writer.RequireDefault(FloorHeightOffset, 0F, $"the {nameof(FloorHeightOffset)} of block {Id}", version);
        if (version is not (31 or 32))
            writer.RequireDefault(Unknown4, (byte)0, $"the {nameof(Unknown4)} of block {Id}", version);
        if (version > 26)
        {
            writer.RequireDefault(Unknown5, (byte)0, $"the {nameof(Unknown5)} of block {Id}", version);
            writer.RequireEmpty(LegacyKeys.Count, $"the {nameof(LegacyKeys)} of block {Id}", version);
            writer.RequireDefault(Unknown7, false, $"the {nameof(Unknown7)} of block {Id}", version);
            writer.RequireDefault(Unknown8, false, $"the {nameof(Unknown8)} of block {Id}", version);
            writer.RequireDefault(Unknown10, (ushort)0, $"the {nameof(Unknown10)} of block {Id}", version);
            writer.RequireDefault(Unknown11, (byte)0, $"the {nameof(Unknown11)} of block {Id}", version);
            writer.RequireDefault(Unknown12, (byte)0, $"the {nameof(Unknown12)} of block {Id}", version);
        }
        if (version > 21)
            writer.RequireDefault(Unknown6, (byte)0, $"the {nameof(Unknown6)} of block {Id}", version);
        if (version > 18)
            writer.RequireDefault(Unknown9, (byte)0, $"the {nameof(Unknown9)} of block {Id}", version);
        if (version < 20)
        {
            writer.RequireDefault(Products.DefaultKey, default, $"the default key of the {nameof(Products)} of block {Id}", version);
            writer.RequireEmpty(Products.Entries.Count, $"the {nameof(Products)} of block {Id}", version);
        }
        if (version > 25)
            writer.RequireDefault(LegacyStamp, 0U, $"the {nameof(LegacyStamp)} of block {Id}", version);
        foreach (var loop in Loops)
            foreach (var segment in loop.Segments)
                segment.RequireDefaults(writer, version, Id);
    }

    internal void Write(SectionWriter writer)
    {
        RequireDefaults(writer);
        writer.Write(version);
        writer.Write(Id);
        writer.Write(Level);
        if (version == 17)
            writer.Write(Unknown1);
        if (version > 24)
            writer.Write((byte)BlockType);
        if (version > 35)
            writer.Write(PlexId);
        writer.Write(HasFloor);
        if (version > 27)
            writer.Write(Unknown3);
        writer.Write(BaseElevation);
        if (version > 33)
            writer.Write(FloorHeightOffset);
        if (version is 31 or 32)
            writer.Write(Unknown4);
        if (version < 27)
        {
            writer.Write(Unknown5);
            if (version < 22)
                writer.Write(Unknown6);
            var keyCount = GetLegacyKeyCount(version);
            if (LegacyKeys.Count != keyCount)
                throw writer.Invalid($"block {Id} has {LegacyKeys.Count} legacy keys, but version {version} stores {keyCount}");
            foreach (var key in LegacyKeys)
                writer.WriteKeyTypeGroupInstance(key);
            writer.Write(Unknown7);
            writer.Write(Unknown8);
        }
        if (version > 23)
        {
            if (Loops.Count > ushort.MaxValue)
                throw writer.Invalid($"block {Id} has {Loops.Count} loops, more than the {ushort.MaxValue} its count can hold");
            writer.Write((ushort)Loops.Count);
        }
        else if (Loops.Count != 1)
            throw writer.Invalid($"block {Id} has {Loops.Count} loops, but version {version} stores exactly one");
        foreach (var loop in Loops)
            loop.Write(writer, version);
        if (version < 19)
            writer.Write(Unknown9);
        if (version > 19)
            Products.Write(writer);
        if (version is >= 12 and <= 25)
            writer.Write(LegacyStamp);
        if (version < 27)
        {
            writer.Write(Unknown10);
            writer.Write(Unknown11);
            writer.Write(Unknown12);
        }
    }
}
