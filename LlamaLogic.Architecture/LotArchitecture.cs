namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the architecture of a lot: its walls, floors, roofs, stairs, terrain and the rest of its build, as typed sections (🔓)
/// </summary>
/// <remarks>
/// <para>The game stores a lot's architecture as a <see cref="RecordBundle"/> in three places: the <see cref="ResourceType.WorldLotArchitecture"/> resources of world files, the architecture data of each lot in a save game (see <see cref="ZoneArchitecture"/>) and tray blueprints (see <see cref="Blueprint"/>).
/// Each record of the bundle is one section, keyed by its <see cref="LotArchitectureSectionType"/>, group 0 and instance 1.</para>
/// <para>Many sections can only be read with the lot's dimensions, which the <see cref="LotSize"/> section gives, so the bundle, not the section, is the unit of decoding.
/// Records <see cref="Decode(RecordBundle)"/> cannot turn into sections are kept, unchanged, in <see cref="UndecodedRecords"/>: those of types this library does not know, those with another group or instance, those with a version (of the section or of an element of it) or a floor feature layer type this library does not support, and those that need dimensions an older <see cref="LotSize"/> section does not give (the game itself discards those).</para>
/// <para><see cref="Encode"/> writes the sections in the layout of their versions, so decoding and encoding the architecture of any lot the game wrote reproduces it byte for byte.</para>
/// <para>Where the documentation of the sections speaks of the lots, sections or entries examined, it means the 2,919 lot architectures examined while developing this library, at the game's patch level as of October 2026: those of the lots of the game's installed world files (of which twelve base-game worlds had been replaced by a community lot-replacement mod), the 179 lots of the Maxis originals of those twelve worlds, the lots of four save games and 73 tray blueprints.</para>
/// </remarks>
public sealed class LotArchitecture
{
    /// <summary>
    /// Gets the instance of the record of every section
    /// </summary>
    public const ulong SectionInstance = 1;

    static readonly LotArchitectureSectionType[] manifestCollectionOrder =
    [
        LotArchitectureSectionType.Revision,
        LotArchitectureSectionType.LotInfo,
        LotArchitectureSectionType.WallEdges,
        LotArchitectureSectionType.VertexGraphs,
        LotArchitectureSectionType.LotSize,
        LotArchitectureSectionType.Floors,
        LotArchitectureSectionType.TerrainFloors,
        LotArchitectureSectionType.Ceilings,
        LotArchitectureSectionType.PoolSurfaces,
        LotArchitectureSectionType.PondBottoms,
        LotArchitectureSectionType.CeilingPaints,
        LotArchitectureSectionType.SecondaryFloors,
        LotArchitectureSectionType.FloorRotations,
        LotArchitectureSectionType.TerrainFloorRotations,
        LotArchitectureSectionType.CeilingRotations,
        LotArchitectureSectionType.CeilingPaintRotations,
        LotArchitectureSectionType.SecondaryFloorRotations,
        LotArchitectureSectionType.TerrainHeights,
        LotArchitectureSectionType.FloorKeys,
        LotArchitectureSectionType.Roofs,
        LotArchitectureSectionType.ModularPieces,
        LotArchitectureSectionType.ObjectTileBounds,
        LotArchitectureSectionType.LotObjects,
        LotArchitectureSectionType.ObjectFallbacks,
        LotArchitectureSectionType.WallBlocks,
        LotArchitectureSectionType.Plexes,
        LotArchitectureSectionType.Stairs
    ];

    /// <summary>
    /// Decodes the architecture of a lot from the content of a record bundle
    /// </summary>
    /// <exception cref="InvalidDataException">The data is not a well-formed record bundle, or one of its sections is not well-formed</exception>
    public static LotArchitecture Decode(ReadOnlyMemory<byte> bundleData) =>
        Decode(RecordBundle.Decode(bundleData));

    /// <summary>
    /// Decodes the architecture of a lot from a record bundle
    /// </summary>
    /// <exception cref="InvalidDataException">One of the bundle's sections is not well-formed</exception>
    public static LotArchitecture Decode(RecordBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        var architecture = new LotArchitecture
        {
            Reserved1 = bundle.Reserved1,
            Reserved2 = bundle.Reserved2
        };
        var pending = new List<(LotArchitectureSectionType type, SectionCodec codec, ReadOnlyMemory<byte> data)>();
        foreach (var record in bundle.Records)
        {
            var type = (LotArchitectureSectionType)(uint)record.Key.Type;
            if (record.Key.Group != 0 || record.Key.FullInstance != SectionInstance || !SectionCodecs.ByType.TryGetValue(type, out var codec))
                architecture.UndecodedRecords.Add(record);
            else
                pending.Add((type, codec, record.Data));
        }
        var empty = new SectionContext();
        foreach (var leadingType in new[] { LotArchitectureSectionType.LotSize, LotArchitectureSectionType.VertexGraphs })
        {
            var index = pending.FindIndex(entry => entry.type == leadingType);
            if (index < 0)
                continue;
            architecture.DecodeSection(pending[index].type, pending[index].codec, pending[index].data, empty);
            pending.RemoveAt(index);
        }
        var context = architecture.CreateContext();
        foreach (var (type, codec, data) in pending)
            architecture.DecodeSection(type, codec, data, context);
        return architecture;
    }

    readonly Dictionary<LotArchitectureSectionType, LotArchitectureSection> sections = [];

    /// <summary>
    /// Gets/sets the rotation of the ceiling paint of each level (🔓)
    /// </summary>
    public FloorRotationGridSection? CeilingPaintRotations
    {
        get => Get<FloorRotationGridSection>(LotArchitectureSectionType.CeilingPaintRotations);
        set => Set(LotArchitectureSectionType.CeilingPaintRotations, value);
    }

    /// <summary>
    /// Gets/sets the ceiling paint of each level (🔓)
    /// </summary>
    public FloorIdGridSection? CeilingPaints
    {
        get => Get<FloorIdGridSection>(LotArchitectureSectionType.CeilingPaints);
        set => Set(LotArchitectureSectionType.CeilingPaints, value);
    }

    /// <summary>
    /// Gets/sets the rotation of the ceiling of each level (🔓)
    /// </summary>
    /// <remarks>
    /// It is paired with <see cref="Ceilings"/>, whose role in-game tests and the game's code show.
    /// </remarks>
    public FloorRotationGridSection? CeilingRotations
    {
        get => Get<FloorRotationGridSection>(LotArchitectureSectionType.CeilingRotations);
        set => Set(LotArchitectureSectionType.CeilingRotations, value);
    }

    /// <summary>
    /// Gets/sets the ceiling of each level (🔓)
    /// </summary>
    /// <remarks>
    /// In-game tests show that building a room fills this grid at the level above the room over exactly the room's tiles, and that painting the ceiling changes only <see cref="CeilingPaints"/>; the game's code assigns the grid to the room on the level below.
    /// </remarks>
    public FloorIdGridSection? Ceilings
    {
        get => Get<FloorIdGridSection>(LotArchitectureSectionType.Ceilings);
        set => Set(LotArchitectureSectionType.Ceilings, value);
    }

    /// <summary>
    /// Gets/sets the floor feature layers (🔓)
    /// </summary>
    public FloorFeaturesSection? FloorFeatures
    {
        get => Get<FloorFeaturesSection>(LotArchitectureSectionType.FloorFeatures);
        set => Set(LotArchitectureSectionType.FloorFeatures, value);
    }

    /// <summary>
    /// Gets/sets the floor keys the floor id grids refer to (🔓)
    /// </summary>
    public FloorKeysSection? FloorKeys
    {
        get => Get<FloorKeysSection>(LotArchitectureSectionType.FloorKeys);
        set => Set(LotArchitectureSectionType.FloorKeys, value);
    }

    /// <summary>
    /// Gets/sets the rotation of the floor pattern of each level (🔓)
    /// </summary>
    public FloorRotationGridSection? FloorRotations
    {
        get => Get<FloorRotationGridSection>(LotArchitectureSectionType.FloorRotations);
        set => Set(LotArchitectureSectionType.FloorRotations, value);
    }

    /// <summary>
    /// Gets/sets the floor pattern of each level (🔓)
    /// </summary>
    public FloorIdGridSection? Floors
    {
        get => Get<FloorIdGridSection>(LotArchitectureSectionType.Floors);
        set => Set(LotArchitectureSectionType.Floors, value);
    }

    /// <summary>
    /// Gets/sets the points with fence and ceiling rail keys of older world files (🔓)
    /// </summary>
    /// <remarks>
    /// The name reflects the catalog key types the data holds; the section appears only in older world files.
    /// </remarks>
    public LegacyFencePointsSection? LegacyFencePoints
    {
        get => Get<LegacyFencePointsSection>(LotArchitectureSectionType.LegacyFencePoints);
        set => Set(LotArchitectureSectionType.LegacyFencePoints, value);
    }

    /// <summary>
    /// Gets/sets the graph of edges with wall pattern and product keys of older world files (🔓)
    /// </summary>
    /// <remarks>
    /// The name reflects the catalog key types the data holds; the section appears only in older world files.
    /// </remarks>
    public LegacyWallGraphSection? LegacyWallGraph
    {
        get => Get<LegacyWallGraphSection>(LotArchitectureSectionType.LegacyWallGraph);
        set => Set(LotArchitectureSectionType.LegacyWallGraph, value);
    }

    /// <summary>
    /// Gets/sets the lot's levels, style and creators (🔓)
    /// </summary>
    public LotInfoSection? LotInfo
    {
        get => Get<LotInfoSection>(LotArchitectureSectionType.LotInfo);
        set => Set(LotArchitectureSectionType.LotInfo, value);
    }

    /// <summary>
    /// Gets/sets the objects stored with the architecture (🔓)
    /// </summary>
    public LotObjectsSection? LotObjects
    {
        get => Get<LotObjectsSection>(LotArchitectureSectionType.LotObjects);
        set => Set(LotArchitectureSectionType.LotObjects, value);
    }

    /// <summary>
    /// Gets/sets the lot's dimensions (🔓)
    /// </summary>
    public LotSizeSection? LotSize
    {
        get => Get<LotSizeSection>(LotArchitectureSectionType.LotSize);
        set => Set(LotArchitectureSectionType.LotSize, value);
    }

    /// <summary>
    /// Gets/sets the modular pieces (🔓)
    /// </summary>
    public ModularPiecesSection? ModularPieces
    {
        get => Get<ModularPiecesSection>(LotArchitectureSectionType.ModularPieces);
        set => Set(LotArchitectureSectionType.ModularPieces, value);
    }

    /// <summary>
    /// Gets/sets the objects the game falls back to placing when others are unavailable (🔓)
    /// </summary>
    public ObjectFallbacksSection? ObjectFallbacks
    {
        get => Get<ObjectFallbacksSection>(LotArchitectureSectionType.ObjectFallbacks);
        set => Set(LotArchitectureSectionType.ObjectFallbacks, value);
    }

    /// <summary>
    /// Gets/sets the tiles each object covers (🔓)
    /// </summary>
    public ObjectTileBoundsSection? ObjectTileBounds
    {
        get => Get<ObjectTileBoundsSection>(LotArchitectureSectionType.ObjectTileBounds);
        set => Set(LotArchitectureSectionType.ObjectTileBounds, value);
    }

    /// <summary>
    /// Gets/sets the pending placement (🔓)
    /// </summary>
    /// <remarks>
    /// When its state is 1, the game centers a footprint of the stored width, depth and rotation on the lot and builds a placement operation with the lot's <see cref="LotInfoSection.BlueprintInstance"/>.
    /// </remarks>
    public PendingPlacementSection? PendingPlacement
    {
        get => Get<PendingPlacementSection>(LotArchitectureSectionType.PendingPlacement);
        set => Set(LotArchitectureSectionType.PendingPlacement, value);
    }

    /// <summary>
    /// Gets/sets the plexes (units) of the lot (🔓)
    /// </summary>
    public PlexesSection? Plexes
    {
        get => Get<PlexesSection>(LotArchitectureSectionType.Plexes);
        set => Set(LotArchitectureSectionType.Plexes, value);
    }

    /// <summary>
    /// Gets/sets the groups of values the <see cref="Plexes"/> section refers to by id (🔓)
    /// </summary>
    /// <remarks>
    /// That the values are block ids is a hypothesis.
    /// </remarks>
    public PlexGroupsSection? PlexGroups
    {
        get => Get<PlexGroupsSection>(LotArchitectureSectionType.PlexGroups);
        set => Set(LotArchitectureSectionType.PlexGroups, value);
    }

    /// <summary>
    /// Gets/sets the pond bottom pattern (🔓)
    /// </summary>
    public FloorIdGridSection? PondBottoms
    {
        get => Get<FloorIdGridSection>(LotArchitectureSectionType.PondBottoms);
        set => Set(LotArchitectureSectionType.PondBottoms, value);
    }

    /// <summary>
    /// Gets/sets the ponds (🔓)
    /// </summary>
    public PondsSection? Ponds
    {
        get => Get<PondsSection>(LotArchitectureSectionType.Ponds);
        set => Set(LotArchitectureSectionType.Ponds, value);
    }

    /// <summary>
    /// Gets/sets the pool surface pattern of each level (🔓)
    /// </summary>
    /// <remarks>
    /// In-game tests show that building a pool fills this grid over exactly the pool's tiles at the level above the pool's block; the game's code assigns the grid to the room on the level below.
    /// </remarks>
    public FloorIdGridSection? PoolSurfaces
    {
        get => Get<FloorIdGridSection>(LotArchitectureSectionType.PoolSurfaces);
        set => Set(LotArchitectureSectionType.PoolSurfaces, value);
    }

    /// <summary>
    /// Gets/sets the first reserved header value of the record bundle, which no code in the game's simulation library reads (other parts of the game were not examined)
    /// </summary>
    public uint Reserved1 { get; set; }

    /// <summary>
    /// Gets/sets the second reserved header value of the record bundle, which no code in the game's simulation library reads (other parts of the game were not examined)
    /// </summary>
    public uint Reserved2 { get; set; }

    /// <summary>
    /// Gets/sets the keys of the catalog resources the other sections reference (🔓)
    /// </summary>
    public ResourceManifestSection? ResourceManifest
    {
        get => Get<ResourceManifestSection>(LotArchitectureSectionType.ResourceManifest);
        set => Set(LotArchitectureSectionType.ResourceManifest, value);
    }

    /// <summary>
    /// Gets/sets the lot's revision counter (🔓)
    /// </summary>
    public RevisionSection? Revision
    {
        get => Get<RevisionSection>(LotArchitectureSectionType.Revision);
        set => Set(LotArchitectureSectionType.Revision, value);
    }

    /// <summary>
    /// Gets/sets the roofs (🔓)
    /// </summary>
    public RoofsSection? Roofs
    {
        get => Get<RoofsSection>(LotArchitectureSectionType.Roofs);
        set => Set(LotArchitectureSectionType.Roofs, value);
    }

    /// <summary>
    /// Gets/sets the rotation of the second floor layer of each level (🔓)
    /// </summary>
    public FloorRotationGridSection? SecondaryFloorRotations
    {
        get => Get<FloorRotationGridSection>(LotArchitectureSectionType.SecondaryFloorRotations);
        set => Set(LotArchitectureSectionType.SecondaryFloorRotations, value);
    }

    /// <summary>
    /// Gets/sets the second floor layer of each level (🔓)
    /// </summary>
    /// <remarks>
    /// In-game tests show that building a room or a platform fills this grid over its tiles with the lot's default floor key, and that painting a floor or a platform's top changes only <see cref="Floors"/>; the grid's role is unknown.
    /// </remarks>
    public FloorIdGridSection? SecondaryFloors
    {
        get => Get<FloorIdGridSection>(LotArchitectureSectionType.SecondaryFloors);
        set => Set(LotArchitectureSectionType.SecondaryFloors, value);
    }

    /// <summary>
    /// Gets the decoded sections, in ascending order of type
    /// </summary>
    public IReadOnlyList<LotArchitectureSection> Sections =>
        [.. sections.Values.OrderBy(section => (uint)section.Type)];

    /// <summary>
    /// Gets/sets the stairs and ladders (🔓)
    /// </summary>
    public StairsSection? Stairs
    {
        get => Get<StairsSection>(LotArchitectureSectionType.Stairs);
        set => Set(LotArchitectureSectionType.Stairs, value);
    }

    /// <summary>
    /// Gets/sets the rotation of the floor pattern painted on the terrain (🔓)
    /// </summary>
    public FloorRotationGridSection? TerrainFloorRotations
    {
        get => Get<FloorRotationGridSection>(LotArchitectureSectionType.TerrainFloorRotations);
        set => Set(LotArchitectureSectionType.TerrainFloorRotations, value);
    }

    /// <summary>
    /// Gets/sets the floor pattern painted on the terrain (🔓)
    /// </summary>
    public FloorIdGridSection? TerrainFloors
    {
        get => Get<FloorIdGridSection>(LotArchitectureSectionType.TerrainFloors);
        set => Set(LotArchitectureSectionType.TerrainFloors, value);
    }

    /// <summary>
    /// Gets/sets the terrain heights (🔓)
    /// </summary>
    public TerrainHeightsSection? TerrainHeights
    {
        get => Get<TerrainHeightsSection>(LotArchitectureSectionType.TerrainHeights);
        set => Set(LotArchitectureSectionType.TerrainHeights, value);
    }

    /// <summary>
    /// Gets/sets the terrain paint layers (🔓)
    /// </summary>
    public TerrainPaintSection? TerrainPaint
    {
        get => Get<TerrainPaintSection>(LotArchitectureSectionType.TerrainPaint);
        set => Set(LotArchitectureSectionType.TerrainPaint, value);
    }

    /// <summary>
    /// Gets the records that were not decoded into sections, which <see cref="Encode"/> writes back unchanged
    /// </summary>
    public Collection<RecordBundleRecord> UndecodedRecords { get; } = [];

    /// <summary>
    /// Gets/sets the section of unknown purpose of type <see cref="LotArchitectureSectionType.Unknown7B33C1F5"/> (🔓)
    /// </summary>
    public Unknown7B33C1F5Section? Unknown7B33C1F5
    {
        get => Get<Unknown7B33C1F5Section>(LotArchitectureSectionType.Unknown7B33C1F5);
        set => Set(LotArchitectureSectionType.Unknown7B33C1F5, value);
    }

    /// <summary>
    /// Gets/sets the section of unknown purpose of type <see cref="LotArchitectureSectionType.UnknownC9106475"/> (🔓)
    /// </summary>
    public UnknownC9106475Section? UnknownC9106475
    {
        get => Get<UnknownC9106475Section>(LotArchitectureSectionType.UnknownC9106475);
        set => Set(LotArchitectureSectionType.UnknownC9106475, value);
    }

    /// <summary>
    /// Gets/sets the lot's venue (🔓)
    /// </summary>
    public VenueSection? Venue
    {
        get => Get<VenueSection>(LotArchitectureSectionType.Venue);
        set => Set(LotArchitectureSectionType.Venue, value);
    }

    /// <summary>
    /// Gets/sets the graphs of vertices and edges whose version decides the layout of <see cref="WallEdges"/> (🔓)
    /// </summary>
    /// <remarks>
    /// The game reads only the section's version; what the graphs describe is not established. Sims 4 Studio calls this section WallCutout.
    /// </remarks>
    public VertexGraphsSection? VertexGraphs
    {
        get => Get<VertexGraphsSection>(LotArchitectureSectionType.VertexGraphs);
        set => Set(LotArchitectureSectionType.VertexGraphs, value);
    }

    /// <summary>
    /// Gets/sets the blocks: rooms, wall lines, fences, pools and fountains (🔓)
    /// </summary>
    public WallBlocksSection? WallBlocks
    {
        get => Get<WallBlocksSection>(LotArchitectureSectionType.WallBlocks);
        set => Set(LotArchitectureSectionType.WallBlocks, value);
    }

    /// <summary>
    /// Gets/sets the wall patterns and products along wall edges (🔓)
    /// </summary>
    public WallEdgesSection? WallEdges
    {
        get => Get<WallEdgesSection>(LotArchitectureSectionType.WallEdges);
        set => Set(LotArchitectureSectionType.WallEdges, value);
    }

    /// <summary>
    /// Gets/sets the lot's seasonal weather interpolation value (🔓)
    /// </summary>
    public WeatherSection? Weather
    {
        get => Get<WeatherSection>(LotArchitectureSectionType.Weather);
        set => Set(LotArchitectureSectionType.Weather, value);
    }

    SectionContext CreateContext() =>
        SectionContext.Create(LotSize, VertexGraphs);

    void DecodeSection(LotArchitectureSectionType type, SectionCodec codec, ReadOnlyMemory<byte> data, SectionContext context)
    {
        if (SectionCodecs.CanDecode(codec.Requirements, context))
        {
            try
            {
                var reader = new SectionReader(data.Span, type);
                var section = codec.Decode(ref reader, context);
                reader.ExpectEnd();
                sections[type] = section;
                return;
            }
            catch (UnsupportedSectionVersionException)
            {
            }
        }
        UndecodedRecords.Add(new RecordBundleRecord(new ResourceKey((ResourceType)(uint)type, 0, SectionInstance), data));
    }

    /// <summary>
    /// Encodes the architecture as the content of a record bundle
    /// </summary>
    /// <exception cref="InvalidOperationException">A section cannot be encoded as it is, or a section and an undecoded record have the same key</exception>
    public ReadOnlyMemory<byte> Encode() =>
        ToRecordBundle().Encode();

    T? Get<T>(LotArchitectureSectionType type)
        where T : LotArchitectureSection =>
        sections.TryGetValue(type, out var section) ? (T)section : null;

    /// <summary>
    /// Gets the keys the sections reference, in the order the game's saver would collect them into the <see cref="ResourceManifest"/>
    /// </summary>
    /// <remarks>
    /// The game builds the manifest by recording every key it writes while saving the sections that precede the manifest, in its fixed save order, skipping repeats.
    /// This method does the same with the current sections, so assigning the result to a new <see cref="ResourceManifestSection"/> keeps the manifest consistent after edits.
    /// No code in the game's simulation library reads the manifest back (other parts of the game were not examined).
    /// For 2,907 of 2,912 lots in the game's world files, save games and tray blueprints examined, the result equals the stored manifest; the exceptions are lots of older world files whose blocks hold inline keys, which the current game no longer writes, and one saved lot whose stored manifest lacks two keys its sections reference.
    /// </remarks>
    /// <exception cref="InvalidOperationException">A section cannot be encoded as it is</exception>
    public IReadOnlyList<ResourceKey> GetManifestKeys()
    {
        var context = CreateContext();
        var keys = new List<ResourceKey>();
        var seen = new HashSet<ResourceKey>();
        foreach (var type in manifestCollectionOrder)
        {
            if (!sections.TryGetValue(type, out var section))
                continue;
            var writer = new SectionWriter(type) { KeyCollector = [] };
            section.Write(writer, context);
            foreach (var key in writer.KeyCollector)
                if (seen.Add(key))
                    keys.Add(key);
        }
        return keys;
    }

    void Set(LotArchitectureSectionType type, LotArchitectureSection? section)
    {
        if (section is null)
        {
            sections.Remove(type);
            return;
        }
        if (section.Type != type)
            throw new ArgumentException($"A {section.Type} section cannot be the {type} section", nameof(section));
        sections[type] = section;
    }

    /// <summary>
    /// Creates a record bundle holding the architecture
    /// </summary>
    /// <exception cref="InvalidOperationException">A section cannot be encoded as it is, or a section and an undecoded record have the same key</exception>
    public RecordBundle ToRecordBundle()
    {
        var context = CreateContext();
        var bundle = new RecordBundle
        {
            Reserved1 = Reserved1,
            Reserved2 = Reserved2
        };
        foreach (var record in UndecodedRecords)
        {
            if (bundle.TryGet(record.Key, out _))
                throw new InvalidOperationException($"Two undecoded records have the key {record.Key}");
            bundle.Set(record.Key, record.Data);
        }
        foreach (var section in sections.Values)
        {
            var key = new ResourceKey((ResourceType)(uint)section.Type, 0, SectionInstance);
            if (bundle.TryGet(key, out _))
                throw new InvalidOperationException($"The {section.Type} section and an undecoded record have the same key, {key}");
            var writer = new SectionWriter(section.Type);
            section.Write(writer, context);
            bundle.Set(key, writer.WrittenMemory);
        }
        return bundle;
    }
}
