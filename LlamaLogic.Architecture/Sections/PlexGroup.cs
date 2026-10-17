namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a group of the <see cref="PlexGroupsSection"/>
/// </summary>
/// <remarks>
/// <para>The layout follows the game's reader and writer.</para>
/// <para>Layout: a <see cref="ushort"/> group version (the game writes 1 and fails to load the lot when it is 0; 1 in every lot examined, the only one supported), <see cref="Id"/>, <see cref="Unknown1"/>, a <see cref="ushort"/> count, then the <see cref="Values"/>.</para>
/// </remarks>
public sealed class PlexGroup
{
    const ushort supportedVersion = 1;

    internal static PlexGroup Read(ref SectionReader reader)
    {
        var version = reader.ReadUInt16();
        if (version != supportedVersion)
            throw reader.UnsupportedVersion($"group version {version} is not supported, expected {supportedVersion}");
        var group = new PlexGroup
        {
            Id = reader.ReadUInt32(),
            Unknown1 = reader.ReadByte()
        };
        var count = reader.ReadCount(reader.ReadUInt16());
        for (var i = 0; i < count; ++i)
            group.Values.Add(reader.ReadUInt16());
        return group;
    }

    /// <summary>
    /// Gets/sets the id of the group, by which <see cref="PlexGroupMapEntry.GroupId"/> refers to it
    /// </summary>
    /// <remarks>
    /// In every lot examined the groups of a section have the ids 0, 1 and 2, in order.
    /// </remarks>
    public uint Id { get; set; }

    /// <summary>
    /// Gets/sets a byte of the group
    /// </summary>
    /// <remarks>
    /// In every lot examined the three groups of a section have a permutation of 0, 1 and 2.
    /// </remarks>
    public byte Unknown1 { get; set; }

    /// <summary>
    /// Gets the values of the group
    /// </summary>
    /// <remarks>
    /// Hypothesis: block ids. They are strictly ascending, 0 through 307, in the lots examined.
    /// 469 of the 736 groups examined are empty; of the 3,522 values of the other 267, 3,271 are ids of blocks of the bundle's <see cref="WallBlocksSection"/>, and in 21 of those groups every value is.
    /// </remarks>
    public Collection<ushort> Values { get; } = [];

    internal void Write(SectionWriter writer)
    {
        writer.Write(supportedVersion);
        writer.Write(Id);
        writer.Write(Unknown1);
        if (Values.Count > ushort.MaxValue)
            throw writer.Invalid($"group {Id} has {Values.Count} values, more than the {ushort.MaxValue} its count can hold");
        writer.Write((ushort)Values.Count);
        foreach (var value in Values)
            writer.Write(value);
    }
}
