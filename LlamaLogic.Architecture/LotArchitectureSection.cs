namespace LlamaLogic.Architecture;

/// <summary>
/// Represents one section of a lot's architecture: one record of the lot's <see cref="RecordBundle"/>
/// </summary>
public abstract class LotArchitectureSection
{
    private protected LotArchitectureSection(LotArchitectureSectionType type) =>
        Type = type;

    /// <summary>
    /// Gets the type of the section
    /// </summary>
    public LotArchitectureSectionType Type { get; }

    internal abstract void Write(SectionWriter writer, SectionContext context);
}
