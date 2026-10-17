namespace LlamaLogic.Architecture;

/// <summary>
/// Represents an entry of <see cref="WallEdgesSection.LegacyEntries"/>
/// </summary>
/// <remarks>
/// Layout: <see cref="Unknown1"/>, <see cref="Unknown2"/>. The game reads both and discards them.
/// </remarks>
public sealed class WallEdgesLegacyEntry
{
    internal static WallEdgesLegacyEntry Read(ref SectionReader reader) =>
        new()
        {
            Unknown1 = reader.ReadUInt32(),
            Unknown2 = reader.ReadByte()
        };

    /// <summary>
    /// Gets/sets the <see cref="uint"/> of the entry
    /// </summary>
    public uint Unknown1 { get; set; }

    /// <summary>
    /// Gets/sets the byte of the entry
    /// </summary>
    public byte Unknown2 { get; set; }

    internal void Write(SectionWriter writer)
    {
        writer.Write(Unknown1);
        writer.Write(Unknown2);
    }
}
