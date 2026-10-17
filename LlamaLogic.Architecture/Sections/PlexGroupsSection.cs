namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.PlexGroups"/> section: groups of values the <see cref="PlexesSection"/> refers to by id (🔓)
/// </summary>
/// <remarks>
/// <para>The layout follows the game's reader and writer; the game reads the section only when the <see cref="LotInfoSection"/> is version 20 or later, and after loading forwards the groups to a global service. What the groups mean is unknown; that their values are block ids is a hypothesis (see <see cref="PlexGroup.Values"/>).</para>
/// <para>Version 1 is the only one supported; the game writes it, and the lots examined hold only it. The game reads nothing more from a section of version 0, and the layout of every other version as that of version 1.</para>
/// <para>Layout: the version (<see cref="uint"/>), a <see cref="ushort"/> count, then the <see cref="Groups"/>.</para>
/// </remarks>
public sealed class PlexGroupsSection :
    LotArchitectureSection
{
    const uint supportedVersion = 1;

    internal static PlexGroupsSection Read(ref SectionReader reader, SectionContext context)
    {
        reader.ReadVersion(supportedVersion, supportedVersion);
        var section = new PlexGroupsSection();
        var count = reader.ReadCount(reader.ReadUInt16());
        for (var i = 0; i < count; ++i)
            section.Groups.Add(PlexGroup.Read(ref reader));
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlexGroupsSection"/> class
    /// </summary>
    public PlexGroupsSection() :
        base(LotArchitectureSectionType.PlexGroups)
    {
    }

    /// <summary>
    /// Gets the groups
    /// </summary>
    /// <remarks>
    /// 0, 1 or 3 in the lots examined.
    /// </remarks>
    public Collection<PlexGroup> Groups { get; } = [];

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        writer.Write(supportedVersion);
        if (Groups.Count > ushort.MaxValue)
            throw writer.Invalid($"there are {Groups.Count} groups, more than the {ushort.MaxValue} the count can hold");
        writer.Write((ushort)Groups.Count);
        foreach (var group in Groups)
            group.Write(writer);
    }
}
