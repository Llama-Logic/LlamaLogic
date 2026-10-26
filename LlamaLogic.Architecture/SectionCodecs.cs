namespace LlamaLogic.Architecture;

delegate LotArchitectureSection SectionDecoder(ref SectionReader reader, SectionContext context);

[Flags]
enum SectionRequirements
{
    None = 0,
    LotSizeVersion = 1,
    Dimensions = 2 | LotSizeVersion,
    VertexGraphsVersion = 4
}

sealed record SectionCodec(SectionDecoder Decode, SectionRequirements Requirements);

static class SectionCodecs
{
    public static readonly IReadOnlyDictionary<LotArchitectureSectionType, SectionCodec> ByType = new Dictionary<LotArchitectureSectionType, SectionCodec>
    {
        [LotArchitectureSectionType.Ceilings] = new(FloorIdGridSection.Read, SectionRequirements.Dimensions),
        [LotArchitectureSectionType.CeilingPaintRotations] = new(FloorRotationGridSection.Read, SectionRequirements.Dimensions),
        [LotArchitectureSectionType.CeilingPaints] = new(FloorIdGridSection.Read, SectionRequirements.Dimensions),
        [LotArchitectureSectionType.CeilingRotations] = new(FloorRotationGridSection.Read, SectionRequirements.Dimensions),
        [LotArchitectureSectionType.FloorFeatures] = new(FloorFeaturesSection.Read, SectionRequirements.Dimensions),
        [LotArchitectureSectionType.FloorKeys] = new(FloorKeysSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.FloorRotations] = new(FloorRotationGridSection.Read, SectionRequirements.Dimensions),
        [LotArchitectureSectionType.Floors] = new(FloorIdGridSection.Read, SectionRequirements.Dimensions),
        [LotArchitectureSectionType.LegacyFencePoints] = new(LegacyFencePointsSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.LegacyWallGraph] = new(LegacyWallGraphSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.LotInfo] = new(LotInfoSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.LotObjects] = new(LotObjectsSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.LotSize] = new(LotSizeSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.ModularPieces] = new(ModularPiecesSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.ObjectFallbacks] = new(ObjectFallbacksSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.ObjectTileBounds] = new(ObjectTileBoundsSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.PendingPlacement] = new(PendingPlacementSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.PlexGroups] = new(PlexGroupsSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.Plexes] = new(PlexesSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.PondBottoms] = new(FloorIdGridSection.Read, SectionRequirements.Dimensions),
        [LotArchitectureSectionType.Ponds] = new(PondsSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.PoolSurfaces] = new(FloorIdGridSection.Read, SectionRequirements.Dimensions),
        [LotArchitectureSectionType.ResourceManifest] = new(ResourceManifestSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.Revision] = new(RevisionSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.Roofs] = new(RoofsSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.SecondaryFloorRotations] = new(FloorRotationGridSection.Read, SectionRequirements.Dimensions),
        [LotArchitectureSectionType.SecondaryFloors] = new(FloorIdGridSection.Read, SectionRequirements.Dimensions),
        [LotArchitectureSectionType.Stairs] = new(StairsSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.TerrainFloorRotations] = new(FloorRotationGridSection.Read, SectionRequirements.Dimensions),
        [LotArchitectureSectionType.TerrainFloors] = new(FloorIdGridSection.Read, SectionRequirements.Dimensions),
        [LotArchitectureSectionType.TerrainHeights] = new(TerrainHeightsSection.Read, SectionRequirements.Dimensions),
        [LotArchitectureSectionType.TerrainPaint] = new(TerrainPaintSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.Unknown7B33C1F5] = new(Unknown7B33C1F5Section.Read, SectionRequirements.None),
        [LotArchitectureSectionType.UnknownC9106475] = new(UnknownC9106475Section.Read, SectionRequirements.None),
        [LotArchitectureSectionType.Venue] = new(VenueSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.WallBlocks] = new(WallBlocksSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.VertexGraphs] = new(VertexGraphsSection.Read, SectionRequirements.None),
        [LotArchitectureSectionType.WallEdges] = new(WallEdgesSection.Read, SectionRequirements.VertexGraphsVersion),
        [LotArchitectureSectionType.Weather] = new(WeatherSection.Read, SectionRequirements.None)
    };

    public static bool CanDecode(SectionRequirements requirements, SectionContext context) =>
        (!requirements.HasFlag(SectionRequirements.LotSizeVersion) || context.LotSizeVersion is not null)
        && (!requirements.HasFlag(SectionRequirements.Dimensions) || context.HasDimensions)
        && (!requirements.HasFlag(SectionRequirements.VertexGraphsVersion) || context.VertexGraphsVersion is not null);
}
