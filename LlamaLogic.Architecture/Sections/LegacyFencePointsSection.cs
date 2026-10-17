namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.LegacyFencePoints"/> section: two lists of points with fence and ceiling rail keys, found only in older world files (🔓)
/// </summary>
/// <remarks>
/// <para>No code in the game's simulation library reads or writes this section (other parts of the game were not examined), so the layout comes from the lots examined and every meaning is a hypothesis.
/// The lots examined hold 21 of these sections, all in world files, 15 of them empty.
/// Hypothesis: the points are post or end-piece positions of the fences and ceiling rails of the <see cref="LegacyWallGraphSection"/>.</para>
/// <para>Version 2 is the only one supported, and the only one in the lots examined.</para>
/// <para>Layout: the version (<see cref="uint"/>), an <see cref="int"/> count and the <see cref="Points1"/>, then an <see cref="int"/> count and the <see cref="Points2"/>.</para>
/// </remarks>
public sealed class LegacyFencePointsSection :
    LotArchitectureSection
{
    const uint supportedVersion = 2;

    internal static LegacyFencePointsSection Read(ref SectionReader reader, SectionContext context)
    {
        reader.ReadVersion(supportedVersion, supportedVersion);
        var section = new LegacyFencePointsSection();
        ReadPoints(ref reader, section.Points1);
        ReadPoints(ref reader, section.Points2);
        return section;
    }

    static void ReadPoints(ref SectionReader reader, Collection<LegacyFencePoint> points)
    {
        var count = reader.ReadCount();
        for (var i = 0; i < count; ++i)
            points.Add(LegacyFencePoint.Read(ref reader));
    }

    static void WritePoints(SectionWriter writer, Collection<LegacyFencePoint> points)
    {
        writer.WriteCount(points.Count);
        foreach (var point in points)
            point.Write(writer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyFencePointsSection"/> class
    /// </summary>
    public LegacyFencePointsSection() :
        base(LotArchitectureSectionType.LegacyFencePoints)
    {
    }

    /// <summary>
    /// Gets the first list of points
    /// </summary>
    /// <remarks>
    /// Their keys are fence keys in every lot examined.
    /// </remarks>
    public Collection<LegacyFencePoint> Points1 { get; } = [];

    /// <summary>
    /// Gets the second list of points
    /// </summary>
    /// <remarks>
    /// Their keys are ceiling rail keys in every lot examined.
    /// </remarks>
    public Collection<LegacyFencePoint> Points2 { get; } = [];

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        writer.Write(supportedVersion);
        WritePoints(writer, Points1);
        WritePoints(writer, Points2);
    }
}
