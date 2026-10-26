namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.Roofs"/> section: the lot's roofs (🔓)
/// </summary>
/// <remarks>
/// <para>The header holds <see cref="LegacyKey"/> (before version 8), <see cref="ReservedKey"/>, <see cref="Unknown1"/>, <see cref="Unknown2"/>, <see cref="Unknown3"/> and the roof count (a 32-bit value); the roofs follow.
/// The layout of each roof depends on the version, as <see cref="Roof"/> describes.</para>
/// <para>Versions 5 through 13 are supported, and the game writes version 13.</para>
/// <para>In-game tests show that a roof's shape is decided by its <see cref="Roof.RoofKey"/> alone, with the per-side bytes following the shape; <see cref="Roof"/> lists the observations.</para>
/// </remarks>
public sealed class RoofsSection :
    LotArchitectureSection
{
    const uint minimumVersion = 5;
    const uint maximumVersion = 13;

    internal static RoofsSection Read(ref SectionReader reader, SectionContext context)
    {
        var section = new RoofsSection { Version = reader.ReadVersion(minimumVersion, maximumVersion) };
        if (section.Version < 8)
            section.LegacyKey = reader.ReadKeyTypeGroupInstance();
        section.ReservedKey = reader.ReadKeyTypeGroupInstance();
        section.Unknown1 = reader.ReadSingle();
        section.Unknown2 = reader.ReadUInt32();
        section.Unknown3 = reader.ReadBoolean();
        var count = reader.ReadCount(reader.ReadUInt32());
        for (var i = 0; i < count; ++i)
            section.Roofs.Add(Roof.Read(ref reader, section.Version));
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoofsSection"/> class
    /// </summary>
    public RoofsSection() :
        base(LotArchitectureSectionType.Roofs)
    {
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets/sets the legacy key of the header
    /// </summary>
    /// <remarks>
    /// Present before version 8. The game's reader discards it.
    /// </remarks>
    public ResourceKey LegacyKey { get; set; }

    /// <summary>
    /// Gets/sets the reserved key of the header
    /// </summary>
    /// <remarks>
    /// The game's writer writes the null key (recording it for the <see cref="ResourceManifestSection"/>) and its reader discards it; it is the null key in every lot examined.
    /// </remarks>
    public ResourceKey ReservedKey { get; set; }

    /// <summary>
    /// Gets the roofs
    /// </summary>
    public Collection<Roof> Roofs { get; } = [];

    /// <summary>
    /// Gets/sets the first unknown value of the header
    /// </summary>
    /// <remarks>
    /// The game keeps it in its roof manager; it is 0.5 in every lot examined.
    /// </remarks>
    public float Unknown1 { get; set; } = 0.5F;

    /// <summary>
    /// Gets/sets the second unknown value of the header
    /// </summary>
    /// <remarks>
    /// The game's writer writes 0 and its reader discards it.
    /// It is 0 in the lots examined except in three version 6 and 8 records (45, 45 and 5), where it may count roof ids (hypothesis: 45 is the highest roof id there).
    /// </remarks>
    public uint Unknown2 { get; set; }

    /// <summary>
    /// Gets/sets the third unknown value of the header
    /// </summary>
    /// <remarks>
    /// The game's writer writes <see langword="true"/> and its reader discards it; it is <see langword="true"/> in every lot examined.
    /// </remarks>
    public bool Unknown3 { get; set; } = true;

    /// <summary>
    /// Gets/sets the version of the section, which decides its layout
    /// </summary>
    /// <remarks>
    /// Encoding throws an <see cref="InvalidOperationException"/> when a field the version lacks holds a value other than its default, or a collection the version lacks is not empty.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 5 through 13</exception>
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
        if (version > 7)
            writer.RequireDefault(LegacyKey, default, nameof(LegacyKey), version);
        writer.Write(version);
        if (version < 8)
            writer.WriteKeyTypeGroupInstance(LegacyKey);
        writer.WriteKeyTypeGroupInstance(ReservedKey);
        writer.Write(Unknown1);
        writer.Write(Unknown2);
        writer.Write(Unknown3);
        writer.WriteCount(Roofs.Count);
        foreach (var roof in Roofs)
            roof.Write(writer, version);
    }
}
