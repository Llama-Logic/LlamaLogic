namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.Stairs"/> section: the lot's stairs and ladders (🔓)
/// </summary>
/// <remarks>
/// <para>The header holds <see cref="Unknown1"/> (from version 9 on) and the stair count (8 bits before version 9, 16 bits from then on); the stairs follow, and from version 11 on a 16-bit ladder count and the ladders.
/// The layout of each stair or ladder depends on the version, as <see cref="Stair"/> describes.</para>
/// <para>Versions 4 through 15 are supported, and the game writes version 15.</para>
/// </remarks>
public sealed class StairsSection :
    LotArchitectureSection
{
    const uint minimumVersion = 4;
    const uint maximumVersion = 15;

    internal static StairsSection Read(ref SectionReader reader, SectionContext context)
    {
        var section = new StairsSection { Version = reader.ReadVersion(minimumVersion, maximumVersion) };
        if (section.Version > 8)
            section.Unknown1 = reader.ReadBoolean();
        var count = reader.ReadCount(section.Version > 8 ? reader.ReadUInt16() : reader.ReadByte());
        for (var i = 0; i < count; ++i)
            section.Stairs.Add(Stair.Read(ref reader, section.Version));
        if (section.Version > 10)
        {
            count = reader.ReadCount(reader.ReadUInt16());
            for (var i = 0; i < count; ++i)
                section.Ladders.Add(Stair.Read(ref reader, section.Version));
        }
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StairsSection"/> class
    /// </summary>
    public StairsSection() :
        base(LotArchitectureSectionType.Stairs)
    {
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets the ladders
    /// </summary>
    /// <remarks>
    /// Present from version 11 on.
    /// </remarks>
    public Collection<Stair> Ladders { get; } = [];

    /// <summary>
    /// Gets the stairs
    /// </summary>
    public Collection<Stair> Stairs { get; } = [];

    /// <summary>
    /// Gets/sets the unknown value of the header
    /// </summary>
    /// <remarks>
    /// Present from version 9 on; the game sets it for older versions.
    /// When it is set, the game recomputes bits 4 and 5 of each stair's <see cref="Stair.Flags"/> while loading.
    /// </remarks>
    public bool Unknown1 { get; set; }

    /// <summary>
    /// Gets/sets the version of the section, which decides its layout
    /// </summary>
    /// <remarks>
    /// Encoding throws an <see cref="InvalidOperationException"/> when a field the version lacks holds a value other than its default, or a collection the version lacks is not empty.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 4 through 15</exception>
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
        if (version < 9)
            writer.RequireDefault(Unknown1, false, nameof(Unknown1), version);
        if (version < 11)
            writer.RequireEmpty(Ladders.Count, nameof(Ladders), version);
        writer.Write(version);
        if (version > 8)
        {
            writer.Write(Unknown1);
            if (Stairs.Count > ushort.MaxValue)
                throw writer.Invalid($"there are {Stairs.Count} stairs, more than the 65,535 the count can hold");
            writer.Write((ushort)Stairs.Count);
        }
        else
        {
            if (Stairs.Count > byte.MaxValue)
                throw writer.Invalid($"there are {Stairs.Count} stairs, more than the 255 version {version} can hold");
            writer.Write((byte)Stairs.Count);
        }
        foreach (var stair in Stairs)
            stair.Write(writer, version);
        if (version > 10)
        {
            if (Ladders.Count > ushort.MaxValue)
                throw writer.Invalid($"there are {Ladders.Count} ladders, more than the 65,535 the count can hold");
            writer.Write((ushort)Ladders.Count);
            foreach (var ladder in Ladders)
                ladder.Write(writer, version);
        }
    }
}
