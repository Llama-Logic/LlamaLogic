namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.PendingPlacement"/> section: a footprint the game places on the lot after loading (🔓)
/// </summary>
/// <remarks>
/// <para>When <see cref="State"/> is 1, the game centers a footprint of <see cref="Width"/> by <see cref="Depth"/> tiles, turned by <see cref="Rotation"/>, on the lot and builds a placement operation with <see cref="LotInfoSection.BlueprintInstance"/>; that this places a pending lot template or blueprint is a hypothesis.
/// Every lot examined holds the defaults (state 0).</para>
/// <para>The layout is <see cref="Width"/>, <see cref="Depth"/>, <see cref="Rotation"/> (from version 3 on), <see cref="State"/> (a Boolean in version 1), then from version 2 on <see cref="Unknown1"/>, <see cref="Unknown2"/>, <see cref="Unknown3"/> (from version 4 on) and <see cref="Unknown4"/> (from version 5 on).
/// The game reads this section only when <see cref="LotInfoSection.Version"/> is 9 or later.</para>
/// <para>Versions 1 through 5 are supported, and the game writes version 5.</para>
/// </remarks>
public sealed class PendingPlacementSection :
    LotArchitectureSection
{
    const uint minimumVersion = 1;
    const uint maximumVersion = 5;

    internal static PendingPlacementSection Read(ref SectionReader reader, SectionContext context)
    {
        var section = new PendingPlacementSection { Version = reader.ReadVersion(minimumVersion, maximumVersion) };
        var version = section.Version;
        section.Width = reader.ReadByte();
        section.Depth = reader.ReadByte();
        if (version > 2)
            section.Rotation = reader.ReadByte();
        if (version < 2)
        {
            section.State = reader.ReadBoolean() ? (byte)1 : (byte)0;
            return section;
        }
        section.State = reader.ReadByte();
        section.Unknown1 = reader.ReadBoolean();
        section.Unknown2 = reader.ReadUInt64();
        if (version > 3)
            section.Unknown3 = reader.ReadBoolean();
        if (version > 4)
            section.Unknown4 = reader.ReadBoolean();
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PendingPlacementSection"/> class
    /// </summary>
    public PendingPlacementSection() :
        base(LotArchitectureSectionType.PendingPlacement)
    {
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets/sets the second dimension of the footprint, in tiles
    /// </summary>
    public byte Depth { get; set; }

    /// <summary>
    /// Gets/sets the rotation of the footprint relative to the lot, in quarter turns
    /// </summary>
    /// <remarks>
    /// Present from version 3 on. The game uses it modulo 4.
    /// </remarks>
    public byte Rotation { get; set; }

    /// <summary>
    /// Gets/sets the state of the placement; the game places the footprint only when it is 1
    /// </summary>
    /// <remarks>
    /// In version 1 it is stored as a Boolean, so it must be 0 or 1 to be written.
    /// </remarks>
    public byte State { get; set; }

    /// <summary>
    /// Gets/sets the first unknown value
    /// </summary>
    /// <remarks>
    /// Present from version 2 on. It is <see langword="true"/> in every lot examined.
    /// </remarks>
    public bool Unknown1 { get; set; } = true;

    /// <summary>
    /// Gets/sets the second unknown value
    /// </summary>
    /// <remarks>
    /// Present from version 2 on. It is 0 in every lot examined.
    /// </remarks>
    public ulong Unknown2 { get; set; }

    /// <summary>
    /// Gets/sets the third unknown value
    /// </summary>
    /// <remarks>
    /// Present from version 4 on. It is <see langword="false"/> in every lot examined.
    /// </remarks>
    public bool Unknown3 { get; set; }

    /// <summary>
    /// Gets/sets the fourth unknown value
    /// </summary>
    /// <remarks>
    /// Present from version 5 on. It is <see langword="false"/> in every lot examined.
    /// </remarks>
    public bool Unknown4 { get; set; }

    /// <summary>
    /// Gets/sets the version of the section, which decides its layout
    /// </summary>
    /// <remarks>
    /// Encoding throws an <see cref="InvalidOperationException"/> when a field the version lacks holds a value other than its default, or a collection the version lacks is not empty.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 1 through 5</exception>
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

    /// <summary>
    /// Gets/sets the first dimension of the footprint, in tiles
    /// </summary>
    /// <remarks>
    /// Which lot axis each dimension runs along depends on the rotations of the footprint and the lot.
    /// </remarks>
    public byte Width { get; set; }

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        if (version < 3)
            writer.RequireDefault(Rotation, (byte)0, nameof(Rotation), version);
        if (version < 2)
        {
            writer.RequireDefault(Unknown1, true, nameof(Unknown1), version);
            writer.RequireDefault(Unknown2, 0UL, nameof(Unknown2), version);
        }
        if (version < 4)
            writer.RequireDefault(Unknown3, false, nameof(Unknown3), version);
        if (version < 5)
            writer.RequireDefault(Unknown4, false, nameof(Unknown4), version);
        writer.Write(version);
        writer.Write(Width);
        writer.Write(Depth);
        if (version > 2)
            writer.Write(Rotation);
        if (version < 2)
        {
            if (State > 1)
                throw writer.Invalid($"the state is {State}, but version 1 stores it as a Boolean");
            writer.Write(State is 1);
            return;
        }
        writer.Write(State);
        writer.Write(Unknown1);
        writer.Write(Unknown2);
        if (version > 3)
            writer.Write(Unknown3);
        if (version > 4)
            writer.Write(Unknown4);
    }
}
