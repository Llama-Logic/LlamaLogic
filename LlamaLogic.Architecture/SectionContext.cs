namespace LlamaLogic.Architecture;

sealed class SectionContext
{
    public static SectionContext Create(LotSizeSection? lotSize, VertexGraphsSection? vertexGraphs) =>
        new()
        {
            LotSizeVersion = lotSize?.Version,
            SizeX = lotSize?.HasDimensions is true ? lotSize.SizeX : null,
            SizeZ = lotSize?.HasDimensions is true ? lotSize.SizeZ : null,
            LevelCount = lotSize?.HasDimensions is true ? lotSize.LevelCount : null,
            VertexGraphsVersion = vertexGraphs?.Version
        };

    public bool HasDimensions =>
        SizeX is not null && SizeZ is not null && LevelCount is not null;

    public int? LevelCount { get; init; }

    public uint? LotSizeVersion { get; init; }

    public int? SizeX { get; init; }

    public int? SizeZ { get; init; }

    public uint? VertexGraphsVersion { get; init; }

    public int RequireLevelCount(LotArchitectureSectionType type) =>
        LevelCount ?? throw new InvalidOperationException($"The {type} section requires the lot's dimensions, which the {LotArchitectureSectionType.LotSize} section does not give");

    public uint RequireLotSizeVersion(LotArchitectureSectionType type) =>
        LotSizeVersion ?? throw new InvalidOperationException($"The {type} section requires the version of the {LotArchitectureSectionType.LotSize} section, which is missing");

    public int RequireSizeX(LotArchitectureSectionType type) =>
        SizeX ?? throw new InvalidOperationException($"The {type} section requires the lot's dimensions, which the {LotArchitectureSectionType.LotSize} section does not give");

    public int RequireSizeZ(LotArchitectureSectionType type) =>
        SizeZ ?? throw new InvalidOperationException($"The {type} section requires the lot's dimensions, which the {LotArchitectureSectionType.LotSize} section does not give");

    public uint RequireVertexGraphsVersion(LotArchitectureSectionType type) =>
        VertexGraphsVersion ?? throw new InvalidOperationException($"The {type} section requires the version of the {LotArchitectureSectionType.VertexGraphs} section, which is missing");
}
