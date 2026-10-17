namespace LlamaLogic.Architecture;

/// <summary>
/// Represents an entry of the map from which the game looks up groups of the <see cref="PlexGroupsSection"/>
/// </summary>
/// <remarks>
/// The game looks a group id up by <see cref="Unknown1"/> and a <see cref="PlexGroupMapEntry.MapKey"/>.
/// Layout: <see cref="Unknown1"/>, a <see cref="ushort"/> count, then the <see cref="Entries"/>.
/// </remarks>
public sealed class PlexGroupMap
{
    internal static PlexGroupMap Read(ref SectionReader reader)
    {
        var map = new PlexGroupMap { Unknown1 = reader.ReadUInt32() };
        var count = reader.ReadCount(reader.ReadUInt16());
        for (var i = 0; i < count; ++i)
            map.Entries.Add(new PlexGroupMapEntry
            {
                MapKey = reader.ReadSByte(),
                GroupId = reader.ReadUInt32()
            });
        return map;
    }

    /// <summary>
    /// Gets the entries
    /// </summary>
    /// <remarks>
    /// 3 entries with the keys 0, 1 and 2, or 1 entry, in the lots examined.
    /// </remarks>
    public Collection<PlexGroupMapEntry> Entries { get; } = [];

    /// <summary>
    /// Gets/sets the outer key of the map entry
    /// </summary>
    /// <remarks>
    /// Hypothesis: a plex id. 0 in every lot examined.
    /// </remarks>
    public uint Unknown1 { get; set; }

    internal void Write(SectionWriter writer)
    {
        writer.Write(Unknown1);
        if (Entries.Count > ushort.MaxValue)
            throw writer.Invalid($"a group map has {Entries.Count} entries, more than the {ushort.MaxValue} its count can hold");
        writer.Write((ushort)Entries.Count);
        foreach (var entry in Entries)
        {
            writer.Write(entry.MapKey);
            writer.Write(entry.GroupId);
        }
    }
}
