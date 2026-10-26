namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.LotSize"/> section: the lot's dimensions (🔓)
/// </summary>
/// <remarks>
/// <para>The grids of the other sections are <see cref="SizeX"/> tiles along the lot's local X axis by <see cref="SizeZ"/> tiles along its local Z axis, with one layer per level.
/// <see cref="LevelCount"/> is the number of levels from the lowest to the highest of <see cref="LotInfoSection"/>, inclusive.</para>
/// <para>Versions 2 through 13 are supported, and the game writes version 13.
/// Version 2 holds no dimensions (<see cref="HasDimensions"/> is <see langword="false"/>); the game discards the floor data of such a lot.</para>
/// </remarks>
public sealed class LotSizeSection :
    LotArchitectureSection
{
    const uint minimumVersion = 2;
    const uint maximumVersion = 13;

    internal static LotSizeSection Read(ref SectionReader reader, SectionContext context)
    {
        var section = new LotSizeSection { Version = reader.ReadVersion(minimumVersion, maximumVersion) };
        if (section.Version > 2)
        {
            section.SizeX = reader.ReadByte();
            section.SizeZ = reader.ReadByte();
            section.LevelCount = reader.ReadByte();
        }
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LotSizeSection"/> class
    /// </summary>
    public LotSizeSection() :
        base(LotArchitectureSectionType.LotSize)
    {
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets whether the section holds the lot's dimensions, which it does from version 3 on
    /// </summary>
    public bool HasDimensions =>
        version > 2;

    /// <summary>
    /// Gets/sets the number of levels of the lot, from the lowest to the highest, inclusive
    /// </summary>
    public byte LevelCount { get; set; }

    /// <summary>
    /// Gets/sets the number of tiles of the lot along its local X axis
    /// </summary>
    public byte SizeX { get; set; }

    /// <summary>
    /// Gets/sets the number of tiles of the lot along its local Z axis
    /// </summary>
    public byte SizeZ { get; set; }

    /// <summary>
    /// Gets/sets the version of the section, which decides its layout
    /// </summary>
    /// <remarks>
    /// Encoding throws an <see cref="InvalidOperationException"/> when a field the version lacks holds a value other than its default, or a collection the version lacks is not empty.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 2 through 13</exception>
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
        {
            writer.RequireDefault(SizeX, (byte)0, nameof(SizeX), version);
            writer.RequireDefault(SizeZ, (byte)0, nameof(SizeZ), version);
            writer.RequireDefault(LevelCount, (byte)0, nameof(LevelCount), version);
        }
        writer.Write(version);
        if (version > 2)
        {
            writer.Write(SizeX);
            writer.Write(SizeZ);
            writer.Write(LevelCount);
        }
    }
}
