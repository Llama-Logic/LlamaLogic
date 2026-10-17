namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.ResourceManifest"/> section: the keys of the catalog resources the other sections reference (🔓)
/// </summary>
/// <remarks>
/// The game builds the manifest while saving: it records every key it writes in the sections that precede the manifest in its save order, skipping repeats, so the manifest starts with the lot's style key and the null key.
/// <see cref="LotArchitecture.GetManifestKeys"/> does the same with the current sections.
/// The game writes the manifest, but no code in the game's simulation library reads it back (other parts of the game were not examined).
/// The section has no version.
/// </remarks>
public sealed class ResourceManifestSection :
    LotArchitectureSection
{
    internal static ResourceManifestSection Read(ref SectionReader reader, SectionContext context)
    {
        var section = new ResourceManifestSection();
        var count = reader.ReadCount(reader.ReadInt32() * 16L);
        for (var i = 0; i < count / 16; ++i)
            section.Keys.Add(reader.ReadKeyTypeGroupInstance());
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceManifestSection"/> class
    /// </summary>
    public ResourceManifestSection() :
        base(LotArchitectureSectionType.ResourceManifest)
    {
    }

    /// <summary>
    /// Gets the keys
    /// </summary>
    public Collection<ResourceKey> Keys { get; } = [];

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        writer.WriteCount(Keys.Count);
        foreach (var key in Keys)
        {
            writer.Write((uint)key.Type);
            writer.Write(key.Group);
            writer.Write(key.FullInstance);
        }
    }
}
