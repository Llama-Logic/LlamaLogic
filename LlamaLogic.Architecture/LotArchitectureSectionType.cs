namespace LlamaLogic.Architecture;

/// <summary>
/// The types of the sections of a lot's architecture, each of which is one record of the lot's <see cref="RecordBundle"/>
/// </summary>
/// <remarks>
/// The game keys every section by its type, group 0 and instance 1.
/// Names reflect what the game's code or in-game tests show each section holds; where neither does, the name is the type's hexadecimal value.
/// </remarks>
[SuppressMessage("Design", "CA1008: Enums should have zero value", Justification = "No section has type zero.")]
[SuppressMessage("Design", "CA1028: Enum Storage should be Int32", Justification = "Section types are resource types, which are 32-bit unsigned.")]
public enum LotArchitectureSectionType :
    uint
{
    /// <summary>
    /// The objects the game falls back to placing when others are unavailable (<see cref="ObjectFallbacksSection"/>)
    /// </summary>
    ObjectFallbacks = 0x000fe6f1,

    /// <summary>
    /// The keys of the catalog resources the other sections reference (<see cref="ResourceManifestSection"/>)
    /// </summary>
    ResourceManifest = 0x0918e671,

    /// <summary>
    /// The stairs and ladders (<see cref="StairsSection"/>)
    /// </summary>
    Stairs = 0x0b188259,

    /// <summary>
    /// The ceiling of each level, as floor key ids (<see cref="FloorIdGridSection"/>)
    /// </summary>
    /// <remarks>
    /// In-game tests show that building a room fills this grid at the level above the room over exactly the room's tiles, and that painting the ceiling changes only <see cref="CeilingPaints"/>; the game's code assigns the grid to the room on the level below.
    /// </remarks>
    Ceilings = 0x1de7b2ac,

    /// <summary>
    /// The lot's levels, style and creators (<see cref="LotInfoSection"/>)
    /// </summary>
    LotInfo = 0x2081b4e6,

    /// <summary>
    /// The floor pattern of each level, as floor key ids (<see cref="FloorIdGridSection"/>)
    /// </summary>
    Floors = 0x251d6fdc,

    /// <summary>
    /// The terrain paint layers (<see cref="TerrainPaintSection"/>)
    /// </summary>
    TerrainPaint = 0x2d173f89,

    /// <summary>
    /// The pool surface pattern of each level, as floor key ids (<see cref="FloorIdGridSection"/>)
    /// </summary>
    /// <remarks>
    /// In-game tests show that building a pool fills this grid over exactly the pool's tiles at the level above the pool's block; the game's code assigns the grid to the room on the level below.
    /// </remarks>
    PoolSurfaces = 0x3e0a3111,

    /// <summary>
    /// The rotation of the floor pattern of each level (<see cref="FloorRotationGridSection"/>)
    /// </summary>
    FloorRotations = 0x42f8f4ba,

    /// <summary>
    /// The lot's dimensions (<see cref="LotSizeSection"/>)
    /// </summary>
    LotSize = 0x44e749f6,

    /// <summary>
    /// The lot's seasonal weather interpolation value (<see cref="WeatherSection"/>)
    /// </summary>
    Weather = 0x49b12b23,

    /// <summary>
    /// The rotation of the ceiling of each level (<see cref="FloorRotationGridSection"/>)
    /// </summary>
    CeilingRotations = 0x65859eb2,

    /// <summary>
    /// A second floor layer of each level, as floor key ids (<see cref="FloorIdGridSection"/>)
    /// </summary>
    /// <remarks>
    /// In-game tests show that building a room or a platform fills this grid over its tiles with the lot's default floor key, and that painting a floor or a platform's top changes only <see cref="Floors"/>; the grid's role is unknown.
    /// </remarks>
    SecondaryFloors = 0x659efbf4,

    /// <summary>
    /// The wall patterns and products along wall edges (<see cref="WallEdgesSection"/>)
    /// </summary>
    WallEdges = 0x69c99784,

    /// <summary>
    /// Groups of values the <see cref="Plexes"/> section refers to by id (<see cref="PlexGroupsSection"/>)
    /// </summary>
    /// <remarks>
    /// That the values are block ids is a hypothesis.
    /// </remarks>
    PlexGroups = 0x72187564,

    /// <summary>
    /// Points with fence and ceiling rail keys (<see cref="LegacyFencePointsSection"/>)
    /// </summary>
    /// <remarks>
    /// The name reflects the catalog key types the data holds; the section appears only in older world files.
    /// </remarks>
    LegacyFencePoints = 0x76093325,

    /// <summary>
    /// A section of unknown purpose found in three lots of world files (<see cref="Unknown7B33C1F5Section"/>)
    /// </summary>
    Unknown7B33C1F5 = 0x7b33c1f5,

    /// <summary>
    /// Graphs of vertices and edges whose version decides the layout of the <see cref="WallEdges"/> section (<see cref="VertexGraphsSection"/>)
    /// </summary>
    /// <remarks>
    /// The game reads only the section's version; what the graphs describe is not established. Sims 4 Studio calls this section WallCutout.
    /// </remarks>
    VertexGraphs = 0x7b36e0d3,

    /// <summary>
    /// The ponds (<see cref="PondsSection"/>)
    /// </summary>
    Ponds = 0x826c8b59,

    /// <summary>
    /// The tiles each object covers (<see cref="ObjectTileBoundsSection"/>)
    /// </summary>
    ObjectTileBounds = 0x881171f6,

    /// <summary>
    /// The blocks: rooms, wall lines, fences, pools and fountains (<see cref="WallBlocksSection"/>)
    /// </summary>
    WallBlocks = 0x908c01c8,

    /// <summary>
    /// The ceiling paint of each level, as floor key ids (<see cref="FloorIdGridSection"/>)
    /// </summary>
    CeilingPaints = 0x90a9c827,

    /// <summary>
    /// The plexes (units) of the lot (<see cref="PlexesSection"/>)
    /// </summary>
    Plexes = 0x9118fdff,

    /// <summary>
    /// A graph of edges with wall pattern and product keys (<see cref="LegacyWallGraphSection"/>)
    /// </summary>
    /// <remarks>
    /// The name reflects the catalog key types the data holds; the section appears only in older world files.
    /// </remarks>
    LegacyWallGraph = 0x92ede8bd,

    /// <summary>
    /// A pending placement (<see cref="PendingPlacementSection"/>)
    /// </summary>
    /// <remarks>
    /// When its state is 1, the game centers a footprint of the stored width, depth and rotation on the lot and builds a placement operation with the lot's <see cref="LotInfoSection.BlueprintInstance"/>.
    /// </remarks>
    PendingPlacement = 0x93f3d6ef,

    /// <summary>
    /// The pond bottom pattern, as floor key ids (<see cref="FloorIdGridSection"/>)
    /// </summary>
    PondBottoms = 0xa276970b,

    /// <summary>
    /// The lot's revision counter (<see cref="RevisionSection"/>)
    /// </summary>
    Revision = 0xa691f240,

    /// <summary>
    /// The floor pattern painted on the terrain, as floor key ids (<see cref="FloorIdGridSection"/>)
    /// </summary>
    TerrainFloors = 0xab53b0c5,

    /// <summary>
    /// The roofs (<see cref="RoofsSection"/>)
    /// </summary>
    Roofs = 0xc3d98b48,

    /// <summary>
    /// The rotation of the floor pattern painted on the terrain (<see cref="FloorRotationGridSection"/>)
    /// </summary>
    TerrainFloorRotations = 0xc59b5293,

    /// <summary>
    /// Objects stored with the architecture (<see cref="LotObjectsSection"/>)
    /// </summary>
    LotObjects = 0xc63be3b4,

    /// <summary>
    /// A section of unknown purpose (<see cref="UnknownC9106475Section"/>)
    /// </summary>
    UnknownC9106475 = 0xc9106475,

    /// <summary>
    /// The rotation of the ceiling paint of each level (<see cref="FloorRotationGridSection"/>)
    /// </summary>
    CeilingPaintRotations = 0xd2af154f,

    /// <summary>
    /// The terrain heights (<see cref="TerrainHeightsSection"/>)
    /// </summary>
    TerrainHeights = 0xe17a612d,

    /// <summary>
    /// The rotation of the second floor layer of each level (<see cref="FloorRotationGridSection"/>)
    /// </summary>
    SecondaryFloorRotations = 0xe6405efa,

    /// <summary>
    /// The lot's venue (<see cref="VenueSection"/>)
    /// </summary>
    Venue = 0xe6bbd7de,

    /// <summary>
    /// The floor feature layers (<see cref="FloorFeaturesSection"/>)
    /// </summary>
    FloorFeatures = 0xf22074aa,

    /// <summary>
    /// The floor keys the floor id grids refer to (<see cref="FloorKeysSection"/>)
    /// </summary>
    FloorKeys = 0xf8b7fd69,

    /// <summary>
    /// The modular pieces (<see cref="ModularPiecesSection"/>)
    /// </summary>
    ModularPieces = 0xfad4c271
}
