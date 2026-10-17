namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.WallBlocks"/> section: the lot's blocks, which are its rooms, wall lines, fence lines, pools and fountains (🔓)
/// </summary>
/// <remarks>
/// <para>The layout follows the game's reader and writer.
/// Versions 0 through 2 are supported, and the game writes version 2; the lots examined hold only version 2.</para>
/// <para>Layout: <see cref="Version"/>, <see cref="StampCounter"/> (after 1), a <see cref="ushort"/> block count, then per block its <see cref="WallBlock.Id"/> (<see cref="ushort"/>, the key of the game's block map, which the game writes from the block) and the <see cref="WallBlock"/>.</para>
/// </remarks>
public sealed class WallBlocksSection :
    LotArchitectureSection
{
    const uint minimumVersion = 0;
    const uint maximumVersion = 2;

    internal static WallBlocksSection Read(ref SectionReader reader, SectionContext context)
    {
        var section = new WallBlocksSection { Version = reader.ReadVersion(minimumVersion, maximumVersion) };
        if (section.Version > 1)
            section.StampCounter = reader.ReadUInt32();
        var count = reader.ReadCount(reader.ReadUInt16());
        for (var i = 0; i < count; ++i)
        {
            var mapKey = reader.ReadUInt16();
            var block = WallBlock.Read(ref reader);
            if (mapKey != block.Id)
                throw reader.Invalid($"block {block.Id} is stored under the id {mapKey}");
            section.Blocks.Add(block);
        }
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WallBlocksSection"/> class
    /// </summary>
    public WallBlocksSection() :
        base(LotArchitectureSectionType.WallBlocks)
    {
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets the blocks
    /// </summary>
    public Collection<WallBlock> Blocks { get; } = [];

    /// <summary>
    /// Gets/sets the counter from which the game stamps <see cref="WallBlockSegment.FenceStamp"/> and <see cref="WallBlockSegment.CeilingRailStamp"/>
    /// </summary>
    /// <remarks>
    /// The game keeps it at offset 0x58 of its block manager.
    /// Adding a foundation raised it by 1 and set the stamps of the block's segments to the new value (in-game test).
    /// It is at least the highest <see cref="WallBlock.Id"/> in every lot examined; of the 653 lots examined without blocks, 450 hold 0 and the rest 1 through 143,271. Whether the game also allocates block ids from it is a hypothesis.
    /// Stored from version 2 on.
    /// </remarks>
    public uint StampCounter { get; set; }

    /// <summary>
    /// Gets/sets the version of the section, which decides its layout
    /// </summary>
    /// <remarks>
    /// Encoding throws an <see cref="InvalidOperationException"/> when a field the version lacks holds a value other than its default, or a collection the version lacks is not empty.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 0 through 2</exception>
    public uint Version
    {
        get => version;
        set
        {
            if (value > maximumVersion)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"The version must be {minimumVersion} through {maximumVersion}");
            version = value;
        }
    }

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        if (version < 2)
            writer.RequireDefault(StampCounter, 0U, nameof(StampCounter), version);
        writer.Write(version);
        if (version > 1)
            writer.Write(StampCounter);
        if (Blocks.Count > ushort.MaxValue)
            throw writer.Invalid($"there are {Blocks.Count} blocks, more than the {ushort.MaxValue} the count can hold");
        writer.Write((ushort)Blocks.Count);
        foreach (var block in Blocks)
        {
            writer.Write(block.Id);
            block.Write(writer);
        }
    }
}
