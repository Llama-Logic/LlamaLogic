namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.UnknownC9106475"/> section: a list of entries, each holding levels with keys, whose meaning has not been established (🔓)
/// </summary>
/// <remarks>
/// <para>Versions 1 through 3 are supported, and the game writes version 3 (the game's reader accepts any version up to 3; the lower bound is a choice).
/// Each level of an entry holds six keys from version 3, three before.
/// The game discards the data after loading unless a flag is set.</para>
/// <para>Every lot examined holds version 3 with no entries, so the layout of the entries follows the game's reader and has not been observed.</para>
/// </remarks>
public sealed class UnknownC9106475Section :
    LotArchitectureSection
{
    const uint minimumVersion = 1;
    const uint maximumVersion = 3;

    internal static UnknownC9106475Section Read(ref SectionReader reader, SectionContext context)
    {
        var section = new UnknownC9106475Section { Version = reader.ReadVersion(minimumVersion, maximumVersion) };
        var keyCount = section.Version >= 3 ? UnknownC9106475Level.KeyCount : UnknownC9106475Level.LegacyKeyCount;
        var entryCount = reader.ReadCount();
        for (var i = 0; i < entryCount; ++i)
        {
            var entry = new UnknownC9106475Entry { Id = reader.ReadUInt64() };
            var levelCount = reader.ReadCount();
            for (var j = 0; j < levelCount; ++j)
            {
                var level = reader.ReadInt32();
                if (level is < sbyte.MinValue or > sbyte.MaxValue)
                    throw reader.Invalid($"a level of {level} does not fit in a signed byte");
                var item = new UnknownC9106475Level { Level = (sbyte)level };
                for (var k = 0; k < keyCount; ++k)
                    item.Keys.Add(reader.ReadKeyInstanceTypeGroup());
                entry.Levels.Add(item);
            }
            section.Entries.Add(entry);
        }
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownC9106475Section"/> class
    /// </summary>
    public UnknownC9106475Section() :
        base(LotArchitectureSectionType.UnknownC9106475)
    {
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets the entries
    /// </summary>
    public Collection<UnknownC9106475Entry> Entries { get; } = [];

    /// <summary>
    /// Gets/sets the version of the section, which decides its layout
    /// </summary>
    /// <remarks>
    /// Encoding throws an <see cref="InvalidOperationException"/> when a field the version lacks holds a value other than its default, or a collection the version lacks is not empty.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 1 through 3</exception>
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
        var keyCount = version >= 3 ? UnknownC9106475Level.KeyCount : UnknownC9106475Level.LegacyKeyCount;
        writer.Write(version);
        writer.WriteCount(Entries.Count);
        foreach (var entry in Entries)
        {
            writer.Write(entry.Id);
            writer.WriteCount(entry.Levels.Count);
            foreach (var level in entry.Levels)
            {
                if (level.Keys.Count != keyCount)
                    throw writer.Invalid($"a level holds {level.Keys.Count} keys, but version {version} stores {keyCount}");
                writer.Write((int)level.Level);
                for (var k = 0; k < keyCount; ++k)
                    writer.WriteKeyInstanceTypeGroup(level.Keys[k]);
            }
        }
    }
}
