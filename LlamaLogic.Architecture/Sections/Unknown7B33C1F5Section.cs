namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.Unknown7B33C1F5"/> section: two lists of 64-bit values whose meaning has not been established (🔓)
/// </summary>
/// <remarks>
/// <para>Version 2 is the only one.
/// Each list is preceded by a 16-bit count.</para>
/// <para>Only three of the lots examined hold this section: one lot of one world, in both its installed and its Maxis original form, and one lot of another world. The three are identical: <see cref="FirstValues"/> holds 20 values and <see cref="SecondValues"/> holds 1.
/// Hypothesis: the values are object ids, because they resemble them, but none of them appears elsewhere in the same bundle.</para>
/// </remarks>
public sealed class Unknown7B33C1F5Section :
    LotArchitectureSection
{
    const uint supportedVersion = 2;

    internal static Unknown7B33C1F5Section Read(ref SectionReader reader, SectionContext context)
    {
        var version = reader.ReadUInt16();
        if (version != supportedVersion)
            throw reader.UnsupportedVersion($"version {version} is not supported, expected {supportedVersion}");
        var section = new Unknown7B33C1F5Section();
        ReadValues(ref reader, section.FirstValues);
        ReadValues(ref reader, section.SecondValues);
        return section;
    }

    static void ReadValues(ref SectionReader reader, Collection<ulong> values)
    {
        var count = reader.ReadCount(reader.ReadUInt16() * 8L) / 8;
        for (var i = 0; i < count; ++i)
            values.Add(reader.ReadUInt64());
    }

    static void WriteValues(SectionWriter writer, Collection<ulong> values, string name)
    {
        if (values.Count > ushort.MaxValue)
            throw writer.Invalid($"{name} holds {values.Count} values, more than the 65,535 a 16-bit count can hold");
        writer.Write((ushort)values.Count);
        foreach (var value in values)
            writer.Write(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Unknown7B33C1F5Section"/> class
    /// </summary>
    public Unknown7B33C1F5Section() :
        base(LotArchitectureSectionType.Unknown7B33C1F5)
    {
    }

    /// <summary>
    /// Gets the values of the first list
    /// </summary>
    public Collection<ulong> FirstValues { get; } = [];

    /// <summary>
    /// Gets the values of the second list
    /// </summary>
    public Collection<ulong> SecondValues { get; } = [];

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        writer.Write((ushort)supportedVersion);
        WriteValues(writer, FirstValues, nameof(FirstValues));
        WriteValues(writer, SecondValues, nameof(SecondValues));
    }
}
