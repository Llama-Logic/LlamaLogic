namespace LlamaLogic.Architecture;

/// <summary>
/// Represents an entry of the <see cref="LegacyFencePointsSection"/>
/// </summary>
/// <remarks>
/// Layout: <see cref="Unknown1"/>, <see cref="Unknown2"/>, <see cref="Unknown3"/>, <see cref="Key"/> (type, group, instance).
/// </remarks>
public sealed class LegacyFencePoint
{
    internal static LegacyFencePoint Read(ref SectionReader reader) =>
        new()
        {
            Unknown1 = reader.ReadUInt32(),
            Unknown2 = reader.ReadSingle(),
            Unknown3 = reader.ReadSingle(),
            Key = reader.ReadKeyTypeGroupInstance()
        };

    /// <summary>
    /// Gets/sets the catalog key of the entry
    /// </summary>
    /// <remarks>
    /// A fence (0x0418FE2A) key in every <see cref="LegacyFencePointsSection.Points1"/> entry and a ceiling rail (CTProductCeilingRail 0x3F0C529A) key in every <see cref="LegacyFencePointsSection.Points2"/> entry examined, group 0.
    /// </remarks>
    public ResourceKey Key { get; set; }

    /// <summary>
    /// Gets/sets the first value of the entry
    /// </summary>
    /// <remarks>
    /// Hypothesis: a lot level. 0, 1, 2 or 4 in the lots examined; bits 28 through 31 of the <see cref="LegacyWallGraphSection"/> codes, the levels under the same hypothesis, are 0 through 4.
    /// </remarks>
    public uint Unknown1 { get; set; }

    /// <summary>
    /// Gets/sets the second value of the entry
    /// </summary>
    /// <remarks>
    /// A whole number in the lots examined. Hypothesis: a coordinate.
    /// In each of the 252 entries examined in lots that also hold a <see cref="LegacyWallGraphSection"/>, it, <see cref="Unknown3"/> and <see cref="Unknown1"/> equal bits 7 through 13, bits 0 through 6 and bits 28 through 31 of a <see cref="LegacyWallGraphEdge.Code"/> or <see cref="LegacyWallGraphProductEdge.Code"/> of that section (or bits 21 through 27, bits 14 through 20 and bits 28 through 31); the other 80 entries are in lots without that section.
    /// </remarks>
    public float Unknown2 { get; set; }

    /// <summary>
    /// Gets/sets the third value of the entry
    /// </summary>
    /// <remarks>
    /// A whole number in the lots examined. Hypothesis: a coordinate.
    /// </remarks>
    public float Unknown3 { get; set; }

    internal void Write(SectionWriter writer)
    {
        writer.Write(Unknown1);
        writer.Write(Unknown2);
        writer.Write(Unknown3);
        writer.WriteKeyTypeGroupInstance(Key);
    }
}
