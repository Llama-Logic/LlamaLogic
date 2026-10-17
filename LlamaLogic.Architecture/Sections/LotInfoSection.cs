namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.LotInfo"/> section: the lot's levels, style, blueprint and users (🔓)
/// </summary>
/// <remarks>
/// <para>The layout is <see cref="Unknown1"/> (from version 13 on), the lowest and highest levels (signed bytes), one height per level from the lowest to the highest, <see cref="LevelElevationOffset"/> (from version 6 on), <see cref="NegativeLevelElevationOffset"/> (from version 16 on), <see cref="Unknown2"/> (from version 17 on), <see cref="Unknown3"/> (from version 18 on) and <see cref="StyleKey"/> (from version 7 on).
/// From version 11 on, <see cref="BlueprintInstance"/>, the ids of the two users, <see cref="Uuid"/>, the names of the two users and, from version 19 on, the rest of the two user records follow.
/// Versions 12 through 14 end with one plex map entry (value and has-value flag) and versions 15 and later with a 32-bit entry count and the entries.</para>
/// <para>The highest level is not kept: it is <see cref="LowestLevel"/> plus the number of <see cref="LevelHeights"/> minus 1, which the game's reader relies on to read the heights.
/// The number of heights equals <see cref="LotSizeSection.LevelCount"/>.</para>
/// <para>Versions 5 through 20 are supported, and the game writes version 20.
/// The game reads some other sections only for some versions of this one (for example <see cref="PendingPlacementSection"/> from version 9 on).</para>
/// </remarks>
public sealed class LotInfoSection :
    LotArchitectureSection
{
    const uint minimumVersion = 5;
    const uint maximumVersion = 20;

    internal static LotInfoSection Read(ref SectionReader reader, SectionContext context)
    {
        var section = new LotInfoSection { Version = reader.ReadVersion(minimumVersion, maximumVersion) };
        var version = section.Version;
        if (version > 12)
            section.Unknown1 = reader.ReadUInt32();
        section.LowestLevel = reader.ReadSByte();
        var highestLevel = reader.ReadSByte();
        var levelCount = highestLevel - section.LowestLevel + 1;
        if (levelCount is < 0 or > sbyte.MaxValue)
            throw reader.Invalid($"the levels {section.LowestLevel} through {highestLevel} are not a range the game can read");
        for (var i = 0; i < levelCount; ++i)
            section.LevelHeights.Add(reader.ReadSingle());
        if (version > 5)
            section.LevelElevationOffset = reader.ReadSingle();
        if (version > 15)
            section.NegativeLevelElevationOffset = reader.ReadSingle();
        if (version > 16)
            section.Unknown2 = reader.ReadBoolean();
        if (version > 17)
            section.Unknown3 = reader.ReadBoolean();
        if (version > 6)
            section.StyleKey = reader.ReadKeyTypeGroupInstance();
        if (version > 10)
        {
            section.BlueprintInstance = reader.ReadUInt64();
            section.FirstUser.Id = reader.ReadUInt64();
            section.SecondUser.Id = reader.ReadUInt64();
            section.Uuid = reader.ReadBytes(reader.ReadCount());
            section.FirstUser.Name = ReadString(ref reader);
            section.SecondUser.Name = ReadString(ref reader);
            if (version > 18)
            {
                foreach (var user in new[] { section.FirstUser, section.SecondUser })
                {
                    user.Unknown1 = reader.ReadUInt32();
                    user.Unknown2 = ReadString(ref reader);
                    user.RepeatedId = reader.ReadUInt64();
                }
            }
        }
        if (version is > 11 and < 15)
            section.PlexValues.Add(new LotInfoPlexValue { Value = reader.ReadUInt32(), HasValue = reader.ReadBoolean() });
        else if (version > 14)
        {
            var count = reader.ReadCount(reader.ReadUInt32() * 9L) / 9;
            for (var i = 0; i < count; ++i)
                section.PlexValues.Add(new LotInfoPlexValue { UnitId = reader.ReadUInt32(), Value = reader.ReadUInt32(), HasValue = reader.ReadBoolean() });
        }
        return section;
    }

    static string ReadString(ref SectionReader reader) =>
        reader.ReadUtf16String(reader.ReadInt32());

    static void WriteString(SectionWriter writer, string? value)
    {
        if (value is null)
            throw writer.Invalid("a user's string is null");
        writer.Write(value.Length);
        writer.WriteUtf16String(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LotInfoSection"/> class
    /// </summary>
    public LotInfoSection() :
        base(LotArchitectureSectionType.LotInfo)
    {
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets/sets the tray instance of the blueprint the lot was saved as
    /// </summary>
    /// <remarks>
    /// Present from version 11 on.
    /// In-game tests show that it equals the tray instance of each blueprint saved, and it is nonzero in every blueprint examined; the game passes it to the placement operation it builds from the <see cref="PendingPlacementSection"/>.
    /// </remarks>
    public ulong BlueprintInstance { get; set; }

    /// <summary>
    /// Gets the first user record
    /// </summary>
    /// <remarks>
    /// Present from version 11 on.
    /// </remarks>
    public LotInfoUser FirstUser { get; } = new();

    /// <summary>
    /// Gets the highest level of the lot: <see cref="LowestLevel"/> plus the number of <see cref="LevelHeights"/> minus 1
    /// </summary>
    public int HighestLevel =>
        LowestLevel + LevelHeights.Count - 1;

    /// <summary>
    /// Gets/sets the vertical offset the game adds to the elevation of levels 0 and up
    /// </summary>
    /// <remarks>
    /// Present from version 6 on.
    /// The game sets it together with <see cref="NegativeLevelElevationOffset"/>, quantized, moving the lot's blocks with it; that it is the foundation height is a hypothesis.
    /// </remarks>
    public float LevelElevationOffset { get; set; }

    /// <summary>
    /// Gets the height of each level, from the lowest to the highest
    /// </summary>
    /// <remarks>
    /// The game sums these to get the elevation of a level. They are 3, 4 or 5 in the lots examined.
    /// Raising a level's walls from 3 m to 5 m in game changed that level's height from 3 to 5, and the base elevations of the blocks, roofs and stair tops above it followed (in-game test).
    /// </remarks>
    public Collection<float> LevelHeights { get; } = [];

    /// <summary>
    /// Gets/sets the lowest level of the lot
    /// </summary>
    public sbyte LowestLevel { get; set; }

    /// <summary>
    /// Gets/sets the vertical offset the game adds to the elevation of levels below 0
    /// </summary>
    /// <remarks>
    /// Present from version 16 on.
    /// </remarks>
    public float NegativeLevelElevationOffset { get; set; }

    /// <summary>
    /// Gets the entries of the plex map
    /// </summary>
    /// <remarks>
    /// Versions before 12 hold none, and versions 12 through 14 exactly one.
    /// </remarks>
    public Collection<LotInfoPlexValue> PlexValues { get; } = [];

    /// <summary>
    /// Gets the second user record
    /// </summary>
    /// <remarks>
    /// Present from version 11 on.
    /// </remarks>
    public LotInfoUser SecondUser { get; } = new();

    /// <summary>
    /// Gets/sets the key of the lot's style, a <c>CTProductStyle</c> (0x9F5CFF10) or the null key
    /// </summary>
    /// <remarks>
    /// Present from version 7 on. The game replaces a missing or invalid style with the default style while loading.
    /// </remarks>
    public ResourceKey StyleKey { get; set; }

    /// <summary>
    /// Gets/sets the first unknown value
    /// </summary>
    /// <remarks>
    /// Present from version 13 on. It defaults to 1 in the game and is 1 in all but 24 lots examined (which hold 0).
    /// </remarks>
    public uint Unknown1 { get; set; } = 1;

    /// <summary>
    /// Gets/sets the second unknown value
    /// </summary>
    /// <remarks>
    /// Present from version 17 on.
    /// The game overwrites it while loading with whether the lot has plexes in a certain state or another flag of the lot is set.
    /// </remarks>
    public bool Unknown2 { get; set; }

    /// <summary>
    /// Gets/sets the third unknown value
    /// </summary>
    /// <remarks>
    /// Present from version 18 on; the game sets it for older versions.
    /// When it is set, the game moves the lot's objects to their level's height minus a constant while loading, then clears it.
    /// </remarks>
    public bool Unknown3 { get; set; }

    /// <summary>
    /// Gets/sets the bytes stored between the user ids and the user names
    /// </summary>
    /// <remarks>
    /// Present from version 11 on. It is empty or a 16-byte RFC 4122 UUID (version 1 or 4) in the lots examined; its purpose is unknown.
    /// </remarks>
    public ReadOnlyMemory<byte> Uuid { get; set; }

    /// <summary>
    /// Gets/sets the version of the section, which decides its layout
    /// </summary>
    /// <remarks>
    /// Encoding throws an <see cref="InvalidOperationException"/> when a field the version lacks holds a value other than its default, or a collection the version lacks is not empty.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 5 through 20</exception>
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

    void RequireDefaults(SectionWriter writer)
    {
        if (version < 13)
            writer.RequireDefault(Unknown1, 1U, nameof(Unknown1), version);
        if (version < 6)
            writer.RequireDefault(LevelElevationOffset, 0F, nameof(LevelElevationOffset), version);
        if (version < 16)
            writer.RequireDefault(NegativeLevelElevationOffset, 0F, nameof(NegativeLevelElevationOffset), version);
        if (version < 17)
            writer.RequireDefault(Unknown2, false, nameof(Unknown2), version);
        if (version < 18)
            writer.RequireDefault(Unknown3, false, nameof(Unknown3), version);
        if (version < 7)
            writer.RequireDefault(StyleKey, default, nameof(StyleKey), version);
        if (version < 11)
        {
            writer.RequireDefault(BlueprintInstance, 0UL, nameof(BlueprintInstance), version);
            writer.RequireDefault(Uuid.Length, 0, $"the length of {nameof(Uuid)}", version);
            foreach (var (user, name) in new[] { (FirstUser, nameof(FirstUser)), (SecondUser, nameof(SecondUser)) })
            {
                writer.RequireDefault(user.Id, 0UL, $"the {nameof(LotInfoUser.Id)} of {name}", version);
                writer.RequireDefault(user.Name, string.Empty, $"the {nameof(LotInfoUser.Name)} of {name}", version);
            }
        }
        if (version < 19)
            foreach (var (user, name) in new[] { (FirstUser, nameof(FirstUser)), (SecondUser, nameof(SecondUser)) })
            {
                writer.RequireDefault(user.Unknown1, 0U, $"the {nameof(LotInfoUser.Unknown1)} of {name}", version);
                writer.RequireDefault(user.Unknown2, string.Empty, $"the {nameof(LotInfoUser.Unknown2)} of {name}", version);
                writer.RequireDefault(user.RepeatedId, 0UL, $"the {nameof(LotInfoUser.RepeatedId)} of {name}", version);
            }
        if (version < 12)
            writer.RequireEmpty(PlexValues.Count, nameof(PlexValues), version);
        else if (version < 15)
        {
            if (PlexValues.Count != 1)
                throw writer.Invalid($"there are {PlexValues.Count} plex map entries, but version {version} holds exactly one");
            writer.RequireDefault(PlexValues[0].UnitId, 0U, $"the {nameof(LotInfoPlexValue.UnitId)} of the plex map entry", version);
        }
    }

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        if (LevelHeights.Count > sbyte.MaxValue || HighestLevel is < sbyte.MinValue or > sbyte.MaxValue)
            throw writer.Invalid($"the {LevelHeights.Count} level heights from level {LowestLevel} up do not fit the signed byte the highest level is stored in");
        if (context.LevelCount is { } levelCount && LevelHeights.Count != levelCount)
            throw writer.Invalid($"there are {LevelHeights.Count} level heights, but the lot has {levelCount} levels");
        RequireDefaults(writer);
        writer.Write(version);
        if (version > 12)
            writer.Write(Unknown1);
        writer.Write(LowestLevel);
        writer.Write((sbyte)HighestLevel);
        foreach (var height in LevelHeights)
            writer.Write(height);
        if (version > 5)
            writer.Write(LevelElevationOffset);
        if (version > 15)
            writer.Write(NegativeLevelElevationOffset);
        if (version > 16)
            writer.Write(Unknown2);
        if (version > 17)
            writer.Write(Unknown3);
        if (version > 6)
            writer.WriteKeyTypeGroupInstance(StyleKey);
        if (version > 10)
        {
            writer.Write(BlueprintInstance);
            writer.Write(FirstUser.Id);
            writer.Write(SecondUser.Id);
            writer.WriteCount(Uuid.Length);
            writer.Write(Uuid.Span);
            WriteString(writer, FirstUser.Name);
            WriteString(writer, SecondUser.Name);
            if (version > 18)
            {
                foreach (var user in new[] { FirstUser, SecondUser })
                {
                    writer.Write(user.Unknown1);
                    WriteString(writer, user.Unknown2);
                    writer.Write(user.RepeatedId);
                }
            }
        }
        if (version is > 11 and < 15)
        {
            writer.Write(PlexValues[0].Value);
            writer.Write(PlexValues[0].HasValue);
        }
        else if (version > 14)
        {
            writer.WriteCount(PlexValues.Count);
            foreach (var plexValue in PlexValues)
            {
                writer.Write(plexValue.UnitId);
                writer.Write(plexValue.Value);
                writer.Write(plexValue.HasValue);
            }
        }
    }
}
