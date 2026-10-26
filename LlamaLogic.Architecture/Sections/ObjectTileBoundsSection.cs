namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.ObjectTileBounds"/> section: the tile rectangles of the lot's objects (🔓)
/// </summary>
/// <remarks>
/// <para>The layout is a 32-bit entry count and, per entry, the object id, the tile rectangle (from version 2 on) and the level.
/// The game sorts the entries when it writes the section; they are kept here in the order read.</para>
/// <para>Versions 1 and 2 are supported, and the game writes version 2.</para>
/// </remarks>
public sealed class ObjectTileBoundsSection :
    LotArchitectureSection
{
    const uint minimumVersion = 1;
    const uint maximumVersion = 2;

    internal static ObjectTileBoundsSection Read(ref SectionReader reader, SectionContext context)
    {
        var section = new ObjectTileBoundsSection { Version = reader.ReadVersion(minimumVersion, maximumVersion) };
        var entrySize = section.Version > 1 ? 13 : 9;
        var count = reader.ReadCount(reader.ReadUInt32() * (long)entrySize) / entrySize;
        for (var i = 0; i < count; ++i)
        {
            var entry = new ObjectTileBounds { ObjectId = reader.ReadUInt64() };
            if (section.Version > 1)
            {
                entry.MinimumZ = reader.ReadByte();
                entry.MaximumZ = reader.ReadByte();
                entry.MinimumX = reader.ReadByte();
                entry.MaximumX = reader.ReadByte();
            }
            entry.Level = reader.ReadSByte();
            section.Entries.Add(entry);
        }
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectTileBoundsSection"/> class
    /// </summary>
    public ObjectTileBoundsSection() :
        base(LotArchitectureSectionType.ObjectTileBounds)
    {
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets the entries
    /// </summary>
    public Collection<ObjectTileBounds> Entries { get; } = [];

    /// <summary>
    /// Gets/sets the version of the section, which decides its layout
    /// </summary>
    /// <remarks>
    /// Encoding throws an <see cref="InvalidOperationException"/> when a field the version lacks holds a value other than its default, or a collection the version lacks is not empty.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 1 or 2</exception>
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
        writer.Write(version);
        writer.WriteCount(Entries.Count);
        foreach (var entry in Entries)
        {
            if (version < 2 && (entry.MinimumZ != 0 || entry.MaximumZ != 0 || entry.MinimumX != 0 || entry.MaximumX != 0))
                throw writer.Invalid($"the entry of object {entry.ObjectId} has a tile rectangle, which version {version} cannot store");
            writer.Write(entry.ObjectId);
            if (version > 1)
            {
                writer.Write(entry.MinimumZ);
                writer.Write(entry.MaximumZ);
                writer.Write(entry.MinimumX);
                writer.Write(entry.MaximumX);
            }
            writer.Write(entry.Level);
        }
    }
}
