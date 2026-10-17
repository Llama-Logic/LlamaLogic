namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.Revision"/> section: the lot's revision counter (🔓)
/// </summary>
/// <remarks>
/// <para>The game keeps the counter on the lot, starts it at 1 and increments it whenever the lot is modified, for example when the height of a level changes.
/// It writes the section only when a save flag is set.</para>
/// <para>The section has no version.
/// In the lots examined the value fits in 32 bits, from 1 to 587,252.</para>
/// </remarks>
public sealed class RevisionSection :
    LotArchitectureSection
{
    internal static RevisionSection Read(ref SectionReader reader, SectionContext context) =>
        new() { Value = reader.ReadUInt64() };

    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionSection"/> class
    /// </summary>
    public RevisionSection() :
        base(LotArchitectureSectionType.Revision)
    {
    }

    /// <summary>
    /// Gets/sets the revision counter
    /// </summary>
    public ulong Value { get; set; }

    internal override void Write(SectionWriter writer, SectionContext context) =>
        writer.Write(Value);
}
