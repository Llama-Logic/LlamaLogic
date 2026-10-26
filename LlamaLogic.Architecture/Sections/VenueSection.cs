namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.Venue"/> section: the lot's venue (🔓)
/// </summary>
/// <remarks>
/// <para>Versions 3 through 6 are supported, and the game writes version 6.
/// <see cref="BusinessInfos"/> exists from version 4, <see cref="Unknown3"/> from version 5 and <see cref="HouseholdId"/> from version 6; <see cref="Unknown1"/> and <see cref="Unknown2"/> exist from versions 2 and 3, so in every supported version.</para>
/// <para>In tray and blueprint mode the game keeps only the entry of the current venue, so a record holds none or one entry.</para>
/// </remarks>
public sealed class VenueSection :
    LotArchitectureSection
{
    const uint minimumVersion = 3;
    const uint maximumVersion = 6;

    internal static VenueSection Read(ref SectionReader reader, SectionContext context)
    {
        var section = new VenueSection { Version = reader.ReadVersion(minimumVersion, maximumVersion) };
        section.VenueTuning = reader.ReadUInt64();
        section.Unknown1 = reader.ReadBoolean();
        section.Unknown2 = reader.ReadBoolean();
        if (section.Version >= 4)
        {
            var count = reader.ReadCount();
            for (var i = 0; i < count; ++i)
            {
                var info = new VenueBusinessInfo
                {
                    Key = reader.ReadUInt64(),
                    VenueTuning = reader.ReadUInt64()
                };
                var length = reader.ReadCount();
                info.Payload = reader.ReadBytes(length);
                section.BusinessInfos.Add(info);
            }
        }
        if (section.Version >= 5)
            section.Unknown3 = reader.ReadUInt32();
        if (section.Version >= 6)
            section.HouseholdId = reader.ReadUInt64();
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VenueSection"/> class
    /// </summary>
    public VenueSection() :
        base(LotArchitectureSectionType.Venue)
    {
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets the business entries, which exist from version 4
    /// </summary>
    public Collection<VenueBusinessInfo> BusinessInfos { get; } = [];

    /// <summary>
    /// Gets/sets the id of the household, which exists from version 6
    /// </summary>
    /// <remarks>
    /// The game turns a stored value of 1 into 0 on load.
    /// It is 0 in all but 5 of the 1,524 version 6 sections examined.
    /// </remarks>
    public ulong HouseholdId { get; set; }

    /// <summary>
    /// Gets/sets a flag
    /// </summary>
    /// <remarks>
    /// Hypothesis: it marks a venue override, going by how the game's code uses it.
    /// It is 0 or 1 in the lots examined, and the game stores it in the lot's venue object.
    /// </remarks>
    public bool Unknown1 { get; set; }

    /// <summary>
    /// Gets/sets a second flag, which the game stores next to <see cref="Unknown1"/>
    /// </summary>
    /// <remarks>
    /// It is 0 in every lot examined.
    /// </remarks>
    public bool Unknown2 { get; set; }

    /// <summary>
    /// Gets/sets a 32-bit value, which exists from version 5
    /// </summary>
    /// <remarks>
    /// The game recomputes it when saving, from the <see cref="LotArchitectureSectionType.WallBlocks"/> section, and its meaning has not been established.
    /// In the 1,856 version 5 and 6 sections examined it has 331 distinct values and is 0 in 239.
    /// </remarks>
    public uint Unknown3 { get; set; }

    /// <summary>
    /// Gets/sets the tuning id of the lot's venue
    /// </summary>
    /// <remarks>
    /// The game treats 0 as its default venue, 0x6FC6.
    /// Changing the lot type in game from Residential to Generic changed it from 0x6FC6 to 0x18201, and changing it back restored 0x6FC6 (in-game test).
    /// It is never 0 in the lots examined.
    /// </remarks>
    public ulong VenueTuning { get; set; }

    /// <summary>
    /// Gets/sets the version of the section, which decides its layout
    /// </summary>
    /// <remarks>
    /// Encoding throws an <see cref="InvalidOperationException"/> when a field the version lacks holds a value other than its default, or a collection the version lacks is not empty.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 3 through 6</exception>
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
        if (version < 4)
            writer.RequireEmpty(BusinessInfos.Count, nameof(BusinessInfos), version);
        if (version < 5)
            writer.RequireDefault(Unknown3, 0U, nameof(Unknown3), version);
        if (version < 6)
            writer.RequireDefault(HouseholdId, 0UL, nameof(HouseholdId), version);
        writer.Write(version);
        writer.Write(VenueTuning);
        writer.Write(Unknown1);
        writer.Write(Unknown2);
        if (version >= 4)
        {
            writer.WriteCount(BusinessInfos.Count);
            foreach (var info in BusinessInfos)
            {
                writer.Write(info.Key);
                writer.Write(info.VenueTuning);
                writer.WriteCount(info.Payload.Length);
                writer.Write(info.Payload.Span);
            }
        }
        if (version >= 5)
            writer.Write(Unknown3);
        if (version >= 6)
            writer.Write(HouseholdId);
    }
}
