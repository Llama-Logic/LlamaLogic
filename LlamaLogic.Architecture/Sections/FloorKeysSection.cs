namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.FloorKeys"/> section: the floor pattern keys the floor id grids refer to by id (🔓)
/// </summary>
/// <remarks>
/// <para>Every nonzero id in a <see cref="FloorIdGridSection"/> is the <see cref="FloorKeyEntry.Id"/> of one of <see cref="Entries"/>.
/// The game returns <see cref="DefaultKey"/> for id 0 or an id with no entry, allocates a new entry the smallest unused id of 1 or more, and frees an entry when its <see cref="FloorKeyEntry.ReferenceCount"/> drops to 0.</para>
/// <para>The layout follows the game's reader and writer. The section has no version.</para>
/// </remarks>
public sealed class FloorKeysSection :
    LotArchitectureSection
{
    const int entrySize = 24;

    internal static FloorKeysSection Read(ref SectionReader reader, SectionContext context)
    {
        var section = new FloorKeysSection { DefaultKey = reader.ReadKeyInstanceTypeGroup() };
        var count = reader.ReadCount(reader.ReadUInt32() * (long)entrySize) / entrySize;
        for (var i = 0; i < count; ++i)
            section.Entries.Add(new FloorKeyEntry
            {
                Key = reader.ReadKeyInstanceTypeGroup(),
                Id = reader.ReadUInt32(),
                ReferenceCount = reader.ReadUInt32()
            });
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FloorKeysSection"/> class
    /// </summary>
    public FloorKeysSection() :
        base(LotArchitectureSectionType.FloorKeys)
    {
    }

    /// <summary>
    /// Gets/sets the key the game returns for id 0 and for ids with no entry
    /// </summary>
    /// <remarks>
    /// It is the null key in every lot examined.
    /// </remarks>
    public ResourceKey DefaultKey { get; set; }

    /// <summary>
    /// Gets the entries
    /// </summary>
    public Collection<FloorKeyEntry> Entries { get; } = [];

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        writer.WriteKeyInstanceTypeGroup(DefaultKey);
        writer.WriteCount(Entries.Count);
        foreach (var entry in Entries)
        {
            writer.WriteKeyInstanceTypeGroup(entry.Key);
            writer.Write(entry.Id);
            writer.Write(entry.ReferenceCount);
        }
    }
}
