namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.Plexes"/> section: the lot's plexes (units), with the blocks of their outlines (🔓)
/// </summary>
/// <remarks>
/// <para>The layout follows the game's reader and writer; the game reads the section only when the <see cref="LotInfoSection"/> is version 14 or later.
/// Versions 1 through 9 are supported, and the game writes version 9.</para>
/// <para>Layout: <see cref="Version"/>, <see cref="Unknown1"/> (after 2), a <see cref="uint"/> plex count, the <see cref="Plexes"/>,
/// then after 7 a <see cref="uint"/> count of <see cref="Unknown2"/>, and only if it is not 0: <see cref="Unknown2"/>, <see cref="Unknown3"/>, a <see cref="uint"/> count and <see cref="Unknown4"/>;
/// then after 8 a <see cref="ushort"/> count and the <see cref="GroupMaps"/>.</para>
/// </remarks>
public sealed class PlexesSection :
    LotArchitectureSection
{
    const uint minimumVersion = 1;
    const uint maximumVersion = 9;

    internal static PlexesSection Read(ref SectionReader reader, SectionContext context)
    {
        var section = new PlexesSection { Version = reader.ReadVersion(minimumVersion, maximumVersion) };
        var version = section.Version;
        if (version > 2)
            section.Unknown1 = reader.ReadBoolean();
        var count = reader.ReadCount(reader.ReadUInt32());
        for (var i = 0; i < count; ++i)
            section.Plexes.Add(Plex.Read(ref reader, version));
        if (version > 7)
        {
            var count2 = reader.ReadCount(reader.ReadUInt32());
            if (count2 != 0)
            {
                for (var i = 0; i < count2; ++i)
                    section.Unknown2.Add(WallBlock.Read(ref reader));
                section.Unknown3 = WallBlock.Read(ref reader);
                var count4 = reader.ReadCount(reader.ReadUInt32());
                for (var i = 0; i < count4; ++i)
                    section.Unknown4.Add(WallBlock.Read(ref reader));
            }
        }
        if (version > 8)
        {
            var mapCount = reader.ReadCount(reader.ReadUInt16());
            for (var i = 0; i < mapCount; ++i)
                section.GroupMaps.Add(PlexGroupMap.Read(ref reader));
        }
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlexesSection"/> class
    /// </summary>
    public PlexesSection() :
        base(LotArchitectureSectionType.Plexes)
    {
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets the maps from which the game looks up groups of the <see cref="PlexGroupsSection"/>
    /// </summary>
    /// <remarks>
    /// 0 or 1 entries in the lots examined. Stored from version 9 on.
    /// </remarks>
    public Collection<PlexGroupMap> GroupMaps { get; } = [];

    /// <summary>
    /// Gets the plexes
    /// </summary>
    /// <remarks>
    /// 0 through 5 in the lots examined.
    /// </remarks>
    public Collection<Plex> Plexes { get; } = [];

    /// <summary>
    /// Gets/sets a Boolean the game keeps at offset 0x115 of its plex manager
    /// </summary>
    /// <remarks>
    /// While it is set, the game runs a routine of its plex manager and then clears it; the game sets it for sections before version 4.
    /// Hypothesis: the plex outlines need regenerating. 0 in every lot examined.
    /// Stored from version 3 on.
    /// </remarks>
    public bool Unknown1 { get; set; }

    /// <summary>
    /// Gets the first of the extra lists of blocks of versions 8 and later
    /// </summary>
    /// <remarks>
    /// The game keeps them at offset 200 of its plex manager. Empty in every lot examined.
    /// </remarks>
    public Collection<WallBlock> Unknown2 { get; } = [];

    /// <summary>
    /// Gets/sets the block stored after <see cref="Unknown2"/>
    /// </summary>
    /// <remarks>
    /// The game keeps it at offset 0x30 of its plex manager. It is stored only when <see cref="Unknown2"/> is not empty, and must then be set.
    /// </remarks>
    public WallBlock? Unknown3 { get; set; }

    /// <summary>
    /// Gets the second of the extra lists of blocks of versions 8 and later
    /// </summary>
    /// <remarks>
    /// The game keeps them at offset 0xE0 of its plex manager. It is stored only when <see cref="Unknown2"/> is not empty.
    /// </remarks>
    public Collection<WallBlock> Unknown4 { get; } = [];

    /// <summary>
    /// Gets/sets the version of the section, which decides its layout
    /// </summary>
    /// <remarks>
    /// Encoding throws an <see cref="InvalidOperationException"/> when a field the version lacks holds a value other than its default, or a collection the version lacks is not empty.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 1 through 9</exception>
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
        if (version < 3)
            writer.RequireDefault(Unknown1, false, nameof(Unknown1), version);
        if (version < 8)
        {
            writer.RequireEmpty(Unknown2.Count, nameof(Unknown2), version);
            if (Unknown3 is not null)
                throw writer.Invalid($"{nameof(Unknown3)} is set, which version {version} cannot store");
            writer.RequireEmpty(Unknown4.Count, nameof(Unknown4), version);
        }
        if (version < 9)
            writer.RequireEmpty(GroupMaps.Count, nameof(GroupMaps), version);
        writer.Write(version);
        if (version > 2)
            writer.Write(Unknown1);
        writer.Write((uint)Plexes.Count);
        foreach (var plex in Plexes)
            plex.Write(writer, version);
        if (version > 7)
        {
            writer.Write((uint)Unknown2.Count);
            if (Unknown2.Count != 0)
            {
                if (Unknown3 is null)
                    throw writer.Invalid($"{nameof(Unknown3)} must be set when {nameof(Unknown2)} is not empty");
                foreach (var block in Unknown2)
                    block.Write(writer);
                Unknown3.Write(writer);
                writer.Write((uint)Unknown4.Count);
                foreach (var block in Unknown4)
                    block.Write(writer);
            }
            else if (Unknown3 is not null || Unknown4.Count != 0)
                throw writer.Invalid($"{nameof(Unknown3)} and {nameof(Unknown4)} are stored only when {nameof(Unknown2)} is not empty");
        }
        if (version > 8)
        {
            if (GroupMaps.Count > ushort.MaxValue)
                throw writer.Invalid($"there are {GroupMaps.Count} group map entries, more than the {ushort.MaxValue} the count can hold");
            writer.Write((ushort)GroupMaps.Count);
            foreach (var map in GroupMaps)
                map.Write(writer);
        }
    }
}
