namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.ObjectFallbacks"/> section: the game's object fallback data, four sets of object ids (🔓)
/// </summary>
/// <remarks>
/// <para>The game hands this data to its "Object Fallback Manager", the target of a Python function of that name.
/// Version 2 is the only one supported: a byte followed by four categories, each a 32-bit count and that many object ids.
/// Version 1 stores the data as an <c>ObjectFallbackDataList</c> message instead; no lot examined has it, so it is rejected.</para>
/// <para>The game reads exactly four categories whatever <see cref="Unknown1"/> says, and loads each id other than 0 into a set, so the order of the ids on disk is the order of the game's hash set and means nothing.
/// The meaning of the categories has not been established.
/// In the lots examined <see cref="Unknown1"/> is 4, and each section has two or three ids, all in the second category.</para>
/// </remarks>
public sealed class ObjectFallbacksSection :
    LotArchitectureSection
{
    /// <summary>
    /// Gets the number of categories, which is 4
    /// </summary>
    public const int CategoryCount = 4;

    const uint supportedVersion = 2;

    internal static ObjectFallbacksSection Read(ref SectionReader reader, SectionContext context)
    {
        reader.ReadVersion(supportedVersion, supportedVersion);
        var section = new ObjectFallbacksSection { Unknown1 = reader.ReadByte() };
        foreach (var category in section.categories)
        {
            var count = reader.ReadCount(reader.ReadUInt32() * 8L) / 8;
            for (var i = 0; i < count; ++i)
                category.Add(reader.ReadUInt64());
        }
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectFallbacksSection"/> class
    /// </summary>
    public ObjectFallbacksSection() :
        base(LotArchitectureSectionType.ObjectFallbacks)
    {
        categories = new Collection<ulong>[CategoryCount];
        for (var i = 0; i < categories.Length; ++i)
            categories[i] = [];
    }

    readonly Collection<ulong>[] categories;

    /// <summary>
    /// Gets the object ids of each of the four categories
    /// </summary>
    public IReadOnlyList<Collection<ulong>> Categories =>
        categories;

    /// <summary>
    /// Gets/sets the byte that precedes the categories
    /// </summary>
    /// <remarks>
    /// The game clamps it to at most 4 and passes it along while inserting ids, but no code in the game's simulation library makes use of it (other parts of the game were not examined).
    /// It is 4 in every lot examined.
    /// </remarks>
    public byte Unknown1 { get; set; } = CategoryCount;

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        writer.Write(supportedVersion);
        writer.Write(Unknown1);
        for (var i = 0; i < categories.Length; ++i)
        {
            var category = categories[i];
            writer.WriteCount(category.Count);
            foreach (var id in category)
                writer.Write(id);
        }
    }
}
