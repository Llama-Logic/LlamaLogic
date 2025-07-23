namespace LlamaLogic.Packages;

/// <summary>
/// A type of resource within a <see cref="DataBasePackedFile"/>
/// </summary>
[SuppressMessage("Design", "CA1028: Enum Storage should be Int32", Justification = "This is by nature of how the package files work. We have no control over that.")]
public enum ResourceType :
    uint
{
    /// <summary>
    /// Unspecified
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Account Reward Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    AccountRewardTuning = 0xbb0f19d8,

    /// <summary>
    /// Achievement Category Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    AchievementCategoryTuning = 0x2451c101,

    /// <summary>
    /// Achievement Collection Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    AchievementCollectionTuning = 0x04d2b465,

    /// <summary>
    /// Achievement Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    AchievementTuning = 0x78559e9e,

    /// <summary>
    /// Action Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ActionTuning = 0x0c772e27,

    /// <summary>
    /// Age Gender Map
    /// </summary>
    AgeGenderMap = 0x010faf71,

    /// <summary>
    /// Ambience
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    Ambience = 0xd2dc5bad,

    /// <summary>
    /// Animation Boundary Condition Cache
    /// </summary>
    AnimationBoundaryConditionCache = 0x1c99b344,

    /// <summary>
    /// Animation Constraint Cache
    /// </summary>
    AnimationConstraintCache = 0xe2249422,

    /// <summary>
    /// Animation State Machine
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    AnimationStateMachine = 0x02d5df13,

    /// <summary>
    /// Animation Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    AnimationTuning = 0xee17c6ad,

    /// <summary>
    /// Apartment Thumbnail 1
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    ApartmentThumbnail1 = 0xab19bcba,

    /// <summary>
    /// Apartment Thumbnail 2
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    ApartmentThumbnail2 = 0xbd491726,

    /// <summary>
    /// Aspiration Category Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    AspirationCategoryTuning = 0xe350dbd8,

    /// <summary>
    /// Aspiration Track Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    AspirationTrackTuning = 0xc020fcad,

    /// <summary>
    /// Aspiration Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    AspirationTuning = 0x28b64675,

    /// <summary>
    /// Audio Configuration
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    AudioConfiguration = 0xfd04e3be,

    /// <summary>
    /// Audio Configuration Reference
    /// </summary>
    AudioConfigurationReference = 0x39b2aa4a,

    /// <summary>
    /// Audio Effects
    /// </summary>
    AudioEffects = 0x01eef63a,

    /// <summary>
    /// Audio Vocals
    /// </summary>
    AudioVocals = 0x01a527db,

    /// <summary>
    /// Automation Tuning
    /// </summary>
    AutomationTuning = 0xdad162b8,

    /// <summary>
    /// AVI
    /// </summary>
    AVI = 0x376840d7,

    /// <summary>
    /// Away Action Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    AwayActionTuning = 0xafadac48,

    /// <summary>
    /// Balloon Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    BalloonTuning = 0xec6a8fc6,

    /// <summary>
    /// Batch Fix History
    /// </summary>
    [ResourceToolingMetadata]
    BatchFixHistory = 0x6bf15bbe,

    /// <summary>
    /// Blend Geometry
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    BlendGeometry = 0x067caa11,

    /// <summary>
    /// Block
    /// </summary>
    Block = 0x07936ce0,

    /// <summary>
    /// Blueprint
    /// </summary>
    Blueprint = 0x3924de26,

    /// <summary>
    /// Blueprint Image
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    BlueprintImage = 0xd33c281e,

    /// <summary>
    /// Body Part Thumbnail
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    BodyPartThumbnail = 0x5b282d45,

    /// <summary>
    /// Bone Pose
    /// </summary>
    BonePose = 0x0355e0a6,

    /// <summary>
    /// Breed Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    BreedTuning = 0x341d3f25,

    /// <summary>
    /// Broadcaster Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    BroadcasterTuning = 0xdebafb73,

    /// <summary>
    /// Bucks Perk Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    BucksPerkTuning = 0xec3da10e,

    /// <summary>
    /// Buff Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    BuffTuning = 0x6017e896,

    /// <summary>
    /// Build Buy Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    BuildBuyTuning = 0xae03c339,

    /// <summary>
    /// Business Rule Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    BusinessRuleTuning = 0xb8e58c6c,

    /// <summary>
    /// Business Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    BusinessTuning = 0x75d807f3,

    /// <summary>
    /// Buy Build Thumbnail
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    BuyBuildThumbnail = 0x3c2a8647,

    /// <summary>
    /// Call To Action Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CallToActionTuning = 0xf537b2e0,

    /// <summary>
    /// Camera Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CameraTuning = 0x12496650,

    /// <summary>
    /// Career Event Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CareerEventTuning = 0x94420322,

    /// <summary>
    /// Career Gig Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CareerGigTuning = 0xccdb0edd,

    /// <summary>
    /// Career Level Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CareerLevelTuning = 0x2c70adf8,

    /// <summary>
    /// Career Track Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CareerTrackTuning = 0x48c75ce3,

    /// <summary>
    /// Career Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CareerTuning = 0x73996beb,

    /// <summary>
    /// CAS Camera Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CASCameraTuning = 0x255adee2,

    /// <summary>
    /// CAS Lighting Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CASLightingTuning = 0x800a3690,

    /// <summary>
    /// CAS Menu Item Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CASMenuItemTuning = 0x0cba50f4,

    /// <summary>
    /// CAS Menu Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CASMenuTuning = 0x935a83c2,

    /// <summary>
    /// CAS Modifier Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CASModifierTuning = 0xf3abff3c,

    /// <summary>
    /// CAS Occult Skintone Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CASOccultSkintoneTuning = 0x7bc36c4e,

    /// <summary>
    /// CAS Occult Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CASOccultTuning = 0x219769e9,

    /// <summary>
    /// CAS Part
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    CASPart = 0x034aeecb,

    /// <summary>
    /// CAS Part Thumbnail
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    CASPartThumbnail = 0x3c1af1f2,

    /// <summary>
    /// CAS Preference Category Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CASPreferenceCategoryTuning = 0xce04fc4b,

    /// <summary>
    /// CAS Preference Item Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CASPreferenceItemTuning = 0xec68fd22,

    /// <summary>
    /// CAS Preset
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    CASPreset = 0xeaa32add,

    /// <summary>
    /// CAS Stories Answer Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CASStoriesAnswerTuning = 0x80f12d17,

    /// <summary>
    /// CAS Stories Question Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CASStoriesQuestionTuning = 0x03246b9d,

    /// <summary>
    /// CAS Stories Trait Chooser Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CASStoriesTraitChooserTuning = 0x8dad1549,

    /// <summary>
    /// CAS Thumbnail Part Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CASThumbnailPartTuning = 0xaed1d0ac,

    /// <summary>
    /// CAS Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    CASTuning = 0xe24b5287,

    /// <summary>
    /// Clan Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ClanTuning = 0xdebee6a5,

    /// <summary>
    /// Clan Value Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ClanValueTuning = 0x998ed0ab,

    /// <summary>
    /// Client Tutorial Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ClientTutorialTuning = 0x2673076d,

    /// <summary>
    /// Clip
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    Clip = 0x6b20c4f3,

    /// <summary>
    /// Clip Header
    /// </summary>
    ClipHeader = 0xbc4a5044,

    /// <summary>
    /// Club Interaction Group Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ClubInteractionGroupTuning = 0xfa0ffa34,

    /// <summary>
    /// Club Seed Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ClubSeedTuning = 0x2f59b437,

    /// <summary>
    /// CMRF
    /// </summary>
    CMRF = 0x656322b7,

    /// <summary>
    /// Color Blended Terrain
    /// </summary>
    ColorBlendedTerrain = 0xaee860e4,

    /// <summary>
    /// Color List
    /// </summary>
    ColorList = 0xa7815676,

    /// <summary>
    /// Color Timeline Data
    /// </summary>
    ColorTimelineData = 0xfd57a8d7,

    /// <summary>
    /// Column
    /// </summary>
    Column = 0x1d6df1cf,

    /// <summary>
    /// Combined Binary Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    CombinedTuning = 0x62e94d38,

    /// <summary>
    /// Conditional Layer Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ConditionalLayerTuning = 0x9183dc91,

    /// <summary>
    /// Credits
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    Credits = 0x669499dc,

    /// <summary>
    /// Cursor
    /// </summary>
    Cursor = 0x26978421,

    /// <summary>
    /// Cutout Info Table
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    CutoutInfoTable = 0x81ca1a10,

    /// <summary>
    /// Deco Trim
    /// </summary>
    DecoTrim = 0x13cf0ed2,

    /// <summary>
    /// Deformer Map
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    DeformerMap = 0xdb43e069,

    /// <summary>
    /// Detective Clue Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    DetectiveClueTuning = 0x537449f6,

    /// <summary>
    /// Developmental Milestone Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    DevelopmentalMilestoneTuning = 0xc5224f94,

    /// <summary>
    /// Drama Node Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    DramaNodeTuning = 0x2553f435,

    /// <summary>
    /// DST Image
    /// </summary>
    [ResourceFileType(ResourceFileType.DirectDrawSurface)]
    DSTImage = 0x00b2d882,

    /// <summary>
    /// Ensemble Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    EnsembleTuning = 0xb9881120,

    /// <summary>
    /// Fence
    /// </summary>
    Fence = 0x0418fe2a,

    /// <summary>
    /// Floor
    /// </summary>
    Floor = 0xb4f762c9,

    /// <summary>
    /// Floor Trim
    /// </summary>
    FloorTrim = 0x84c23219,

    /// <summary>
    /// Font Configuration
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    FontConfiguration = 0x0333406c,

    /// <summary>
    /// Footprint
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    Footprint = 0xd382bf57,

    /// <summary>
    /// Footprint 3
    /// </summary>
    Footprint3 = 0xb734e44f,

    /// <summary>
    /// Foundation
    /// </summary>
    Foundation = 0x2fae983e,

    /// <summary>
    /// Fountain Trim
    /// </summary>
    FountainTrim = 0xe7ada79d,

    /// <summary>
    /// Frieze
    /// </summary>
    Frieze = 0xa057811c,

    /// <summary>
    /// Game Ruleset Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    GameRulesetTuning = 0xe1477e18,

    /// <summary>
    /// Generic MTX
    /// </summary>
    GenericMTX = 0xac03a936,

    /// <summary>
    /// Geometry
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    Geometry = 0x015a1849,

    /// <summary>
    /// Guidance Tip Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    GuidanceTipTuning = 0xd4a09abd,

    /// <summary>
    /// HalfWall
    /// </summary>
    HalfWall = 0x9151e6bc,

    /// <summary>
    /// Headline Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    HeadlineTuning = 0xf401205d,

    /// <summary>
    /// Holiday Definition Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    HolidayDefinitionTuning = 0x0e316f6d,

    /// <summary>
    /// Holiday Tradition Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    HolidayTraditionTuning = 0x3fcd2486,

    /// <summary>
    /// Household Description
    /// </summary>
    HouseholdDescription = 0x729f6c4f,

    /// <summary>
    /// Household Milestone Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    HouseholdMilestoneTuning = 0x3972e6f3,

    /// <summary>
    /// Household Template
    /// </summary>
    HouseholdTemplate = 0xb3c438f0,

    /// <summary>
    /// Hsv Tweaker Settings Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    HsvTweakerSettingsTuning = 0x23fc90fe,

    /// <summary>
    /// Interaction Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    InteractionTuning = 0xe882d22f,

    /// <summary>
    /// Ladder
    /// </summary>
    Ladder = 0xcc09004d,

    /// <summary>
    /// Light
    /// </summary>
    Light = 0x03b4c61d,

    /// <summary>
    /// Locomotion Builder
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    LocomotionBuilder = 0x053a3e7b,

    /// <summary>
    /// Locomotion Config
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    LocomotionConfig = 0x9afe47f5,

    /// <summary>
    /// Lot Decoration Preset Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    LotDecorationPresetTuning = 0xde1ef8fb,

    /// <summary>
    /// Lot Decoration Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    LotDecorationTuning = 0xfe2db1ab,

    /// <summary>
    /// Lot Description
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    LotDescription = 0x01942e2c,

    /// <summary>
    /// Lot Footprint Reference
    /// </summary>
    LotFootprintReference = 0xc0084996,

    /// <summary>
    /// Lot Preview Thumbnail
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    LotPreviewThumbnail = 0x0d338a3a,

    /// <summary>
    /// Lot Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    LotTuning = 0xd8800d66,

    /// <summary>
    /// Lot Type Event Map
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    LotTypeEventMap = 0x122fc66a,

    /// <summary>
    /// LRLE Image
    /// </summary>
    LRLEImage = 0x2bc04edf,

    /// <summary>
    /// Lunar Cycle Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    LunarCycleTuning = 0x55493b18,

    /// <summary>
    /// Magalog
    /// </summary>
    Magalog = 0xb91e18db,

    /// <summary>
    /// Magazine Collection
    /// </summary>
    MagazineCollection = 0x74050b1f,

    /// <summary>
    /// Material Definition
    /// </summary>
    MaterialDefinition = 0x01d0e75d,

    /// <summary>
    /// Material Set
    /// </summary>
    MaterialSet = 0x02019972,

    /// <summary>
    /// Maxis World Pipeline 1
    /// </summary>
    MaxisWorldPipeline1 = 0xe0ed7129,

    /// <summary>
    /// Maxis World Pipeline 2
    /// </summary>
    MaxisWorldPipeline2 = 0xfa25b7de,

    /// <summary>
    /// Memorial Thumbnail
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    MemorialThumbnail = 0x00000015,

    /// <summary>
    /// Modal Music Mapping
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ModalMusicMapping = 0x1a8506c5,

    /// <summary>
    /// Model
    /// </summary>
    Model = 0x01661233,

    /// <summary>
    /// Model Cutout
    /// </summary>
    ModelCutout = 0x07576a17,

    /// <summary>
    /// Model LOD
    /// </summary>
    ModelLOD = 0x01d10f34,

    /// <summary>
    /// Modular Piece
    /// </summary>
    ModularPiece = 0x9917eacd,

    /// <summary>
    /// Modular Piece Catalog
    /// </summary>
    ModularPieceCatalog = 0xa0451cbd,

    /// <summary>
    /// Mood Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    MoodTuning = 0xba7b60b8,

    /// <summary>
    /// MTX Catalog
    /// </summary>
    MTXCatalog = 0x6dff1a66,

    /// <summary>
    /// Music Data
    /// </summary>
    MusicData = 0xc202c770,

    /// <summary>
    /// Name Map
    /// </summary>
    NameMap = 0x0166038c,

    /// <summary>
    /// Narrative Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    NarrativeTuning = 0x3e753c39,

    /// <summary>
    /// Native Build Buy Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    NativeBuildBuyTuning = 0x1a94e9b4,

    /// <summary>
    /// Native Seasons Weather Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    NativeSeasonsWeatherTuning = 0xb43a5ad1,

    /// <summary>
    /// Notebook Entry Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    NotebookEntryTuning = 0x9902fa76,

    /// <summary>
    /// Object Catalog
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    ObjectCatalog = 0x319e4f1d,

    /// <summary>
    /// Object Catalog Set
    /// </summary>
    ObjectCatalogSet = 0xff56010c,

    /// <summary>
    /// Object Definition
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    ObjectDefinition = 0xc0db5ae7,

    /// <summary>
    /// Objective Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ObjectiveTuning = 0x0069453e,

    /// <summary>
    /// Object Modifiers
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ObjectModifiers = 0xe231b3d8,

    /// <summary>
    /// Object Part Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ObjectPartTuning = 0x7147a350,

    /// <summary>
    /// Object State Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ObjectStateTuning = 0x5b02819e,

    /// <summary>
    /// Object Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ObjectTuning = 0xb61de6b4,

    /// <summary>
    /// Open Street Director Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    OpenStreetDirectorTuning = 0x4b6fdec4,

    /// <summary>
    /// OpenType Font
    /// </summary>
    OpenTypeFont = 0x25796dca,

    /// <summary>
    /// Path
    /// </summary>
    Path = 0x3a1e944e,

    /// <summary>
    /// Pet Breed Coat Pattern Thumbnail
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    PetBreedCoatPatternThumbnail = 0x8e71065d,

    /// <summary>
    /// Pet Coat Brush
    /// </summary>
    PetCoatBrush = 0x9d7e7558,

    /// <summary>
    /// Pet Coat Pattern
    /// </summary>
    PetCoatPattern = 0xc4dfae6d,

    /// <summary>
    /// Pet Face Preset Thumbnail
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    PetFacePresetThumbnail = 0xb67673a2,

    /// <summary>
    /// Pet Pelt Layer
    /// </summary>
    PetPeltLayer = 0x26af8338,

    /// <summary>
    /// Pie Menu Category Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    PieMenuCategoryTuning = 0x03e9d964,

    /// <summary>
    /// Platform
    /// </summary>
    Platform = 0x99c07284,

    /// <summary>
    /// PNG
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    PNG = 0x0119b36d,

    /// <summary>
    /// PNG Image converted from DDS Image
    /// </summary>
    [ResourceFileType(ResourceFileType.DirectDrawSurfaceAsPortableNetworkGraphic)]
    PNGImage = 0x2f7d0004,

    /// <summary>
    /// PNG Image converted from DDS Image 2
    /// </summary>
    [ResourceFileType(ResourceFileType.DirectDrawSurfaceAsPortableNetworkGraphic)]
    PNGImage2 = 0x2f7d0006,

    /// <summary>
    /// Pond
    /// </summary>
    Pond = 0x5003333c,

    /// <summary>
    /// Pool Trim
    /// </summary>
    PoolTrim = 0xa5dffcf3,

    /// <summary>
    /// Posture Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    PostureTuning = 0xad6fdf1f,

    /// <summary>
    /// Puberty Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    PubertyTuning = 0x4819084d,

    /// <summary>
    /// Queryable World Mask Manifest
    /// </summary>
    QueryableWorldMaskManifest = 0x4e71b4e6,

    /// <summary>
    /// Rabbit Hole Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RabbitHoleTuning = 0xb16ad2fa,

    /// <summary>
    /// Railing
    /// </summary>
    Railing = 0x1c1cf1f7,

    /// <summary>
    /// Recipe Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RecipeTuning = 0xeb97f823,

    /// <summary>
    /// Record Bundle
    /// </summary>
    RecordBundle = 0x3eaaa87c,

    /// <summary>
    /// Region Description
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    RegionDescription = 0xd65daff9,

    /// <summary>
    /// Region Map
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    RegionMap = 0xac16fbec,

    /// <summary>
    /// Region Sort Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RegionSortTuning = 0x3f57c885,

    /// <summary>
    /// Region Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RegionTuning = 0x51e7a18d,

    /// <summary>
    /// Relationship Bit Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RelationshipBitTuning = 0x0904df10,

    /// <summary>
    /// Relationship Lock Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RelationshipLockTuning = 0xae34e673,

    /// <summary>
    /// Renderer Censor Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RendererCensorTuning = 0xe1d6fad2,

    /// <summary>
    /// Renderer Fade Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RendererFadeTuning = 0x041f95da,

    /// <summary>
    /// Renderer Global DOF Settings Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RendererGlobalDOFSettingsTuning = 0x8d33a0de,

    /// <summary>
    /// Renderer Global Highlight Settings Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RendererGlobalHighlightSettingsTuning = 0xbc7e2e95,

    /// <summary>
    /// Renderer Global Light Settings Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RendererGlobalLightSettingsTuning = 0x6ab26065,

    /// <summary>
    /// Renderer Global Shadow Settings Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RendererGlobalShadowSettingsTuning = 0x45295831,

    /// <summary>
    /// Renderer Global SSAO Settings Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RendererGlobalSSAOSettingsTuning = 0x2f6ee30f,

    /// <summary>
    /// Renderer Global Vol Light Scattering Settings Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RendererGlobalVolLightScatteringSettingsTuning = 0xc5e245de,

    /// <summary>
    /// Reward Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RewardTuning = 0x6fa49828,

    /// <summary>
    /// Rig
    /// </summary>
    Rig = 0x8eaf13de,

    /// <summary>
    /// RLE 2 Image
    /// </summary>
    [ResourceFileType(ResourceFileType.DirectDrawSurface)]
    RLE2Image = 0x3453cf95,

    /// <summary>
    /// RLES Image
    /// </summary>
    [ResourceFileType(ResourceFileType.DirectDrawSurface)]
    RLESImage = 0xba856c78,

    /// <summary>
    /// Road Definition
    /// </summary>
    RoadDefinition = 0x9063660e,

    /// <summary>
    /// Role State Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RoleStateTuning = 0x0e4d15fb,

    /// <summary>
    /// Roof
    /// </summary>
    Roof = 0x91edbd3e,

    /// <summary>
    /// Roof Pattern
    /// </summary>
    RoofPattern = 0xf1edbd86,

    /// <summary>
    /// Roof Trim
    /// </summary>
    RoofTrim = 0xb0311d0f,

    /// <summary>
    /// Royalty Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    RoyaltyTuning = 0x37ef2ee7,

    /// <summary>
    /// Sims 4 Studio Merged Package Manifest
    /// </summary>
    [ResourceToolingMetadata]
    S4sMergedPackageManifest = 0x7fb6ad8a,

    /// <summary>
    /// Save Game Custom Texture
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    SaveGameCustomTexture = 0xe88db35f,

    /// <summary>
    /// Save Game Data
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    [StandardCompression(CompressionTypeMethodNumber.Internal_compression)]
    SaveGameData = 0x0000000d,

    /// <summary>
    /// Save Game Household Thumbnail
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    SaveGameHouseholdThumbnail = 0x00000014,

    /// <summary>
    /// Save Game Lot Level Thumbnail
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    SaveGameLotLevelThumbnail = 0x00000012,

    /// <summary>
    /// Save Game Lot Level Thumbnail Mask
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    SaveGameLotLevelThumbnailMask = 0x00000013,

    /// <summary>
    /// Save Game Lot Thumbnail 1
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    SaveGameLotThumbnail1 = 0x0000000f,

    /// <summary>
    /// Save Game Lot Thumbnail 2
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    SaveGameLotThumbnail2 = 0x00000010,

    /// <summary>
    /// Save Game Sim Custom Texture
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    SaveGameSimCustomTexture = 0xf8e1457a,

    /// <summary>
    /// Save-Specific Relational Data Storage
    /// </summary>
    /// <remarks>
    /// This is a ZIP archive's octet stream set to store, not any level of compression.
    /// Use DBPF ZLIB compression.
    /// Each entry should be a SQLite database file using the `.sqlite` extension, the name of which is a valid UUID.
    /// </remarks>
    [ResourceFileType(ResourceFileType.Binary)]
    SaveSpecificRelationalDataStorage = 0x5263aa84,

    /// <summary>
    /// ScaleForm GFX
    /// </summary>
    ScaleFormGFX = 0x62ecc59a,

    /// <summary>
    /// Sculpt
    /// </summary>
    Sculpt = 0x9d1ab874,

    /// <summary>
    /// Season Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SeasonTuning = 0xc98dd45e,

    /// <summary>
    /// Service NPC Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ServiceNPCTuning = 0x9cc21262,

    /// <summary>
    /// Game Shaders for Windows
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    ShaderPrecomp = 0x27fe5365,

    /// <summary>
    /// Sickness Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SicknessTuning = 0xc3fbd8de,

    /// <summary>
    /// Sim Data
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    SimData = 0x545ac67a,

    /// <summary>
    /// Sim Featured Outfit Thumbnail
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    SimFeaturedOutfitThumbnail = 0xcd9de247,

    /// <summary>
    /// Sim Filter Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SimFilterTuning = 0x6e0dda9f,

    /// <summary>
    /// Sim Hotspot Control
    /// </summary>
    SimHotspotControl = 0x8b18ff6e,

    /// <summary>
    /// Sim Household Thumbnail
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    SimHouseholdThumbnail = 0x3bd45407,

    /// <summary>
    /// Sim Info
    /// </summary>
    SimInfo = 0x025ed6f4,

    /// <summary>
    /// Sim Info Fixup Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SimInfoFixupTuning = 0xe2581892,

    /// <summary>
    /// Sim Lighting Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SimLightingTuning = 0x9ddb5fda,

    /// <summary>
    /// Sim Modifier
    /// </summary>
    SimModifier = 0xc5f6763e,

    /// <summary>
    /// Sim Preset
    /// </summary>
    SimPreset = 0x105205ba,

    /// <summary>
    /// Sim Preset Thumbnail
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    SimPresetThumbnail = 0x9c925813,

    /// <summary>
    /// Sim Template Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SimTemplateTuning = 0x0ca4c78b,

    /// <summary>
    /// Situation Goal Set Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SituationGoalSetTuning = 0x9df2f1f2,

    /// <summary>
    /// Situation Goal Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SituationGoalTuning = 0x598f28e7,

    /// <summary>
    /// Situation Job Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SituationJobTuning = 0x9c07855f,

    /// <summary>
    /// Situation Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SituationTuning = 0xfbc3aeeb,

    /// <summary>
    /// Skintone
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    Skintone = 0x0354796a,

    /// <summary>
    /// Sky Box Texture Data
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    SkyBoxTextureData = 0x71a449c9,

    /// <summary>
    /// Slot
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    Slot = 0xd3044521,

    /// <summary>
    /// Slot Type Set Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SlotTypeSetTuning = 0x3f163505,

    /// <summary>
    /// Slot Type Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SlotTypeTuning = 0x69a5daa4,

    /// <summary>
    /// Snippet Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SnippetTuning = 0x7df2169c,

    /// <summary>
    /// Social Group Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SocialGroupTuning = 0x2e47a104,

    /// <summary>
    /// Sound Mix Properties
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SoundMixProperties = 0x4115f9d5,

    /// <summary>
    /// Sound Modifier Mapping
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SoundModifierMapping = 0xa576c2e7,

    /// <summary>
    /// Sound Properties
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SoundProperties = 0x1b25a024,

    /// <summary>
    /// Spandrel
    /// </summary>
    Spandrel = 0x3f0c529a,

    /// <summary>
    /// Spawner
    /// </summary>
    Spawner = 0x48c28979,

    /// <summary>
    /// Spell Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SpellTuning = 0x1f3413d9,

    /// <summary>
    /// Stairs
    /// </summary>
    Stairs = 0x9a20cd1c,

    /// <summary>
    /// Static Commodity Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    StaticCommodityTuning = 0x51077643,

    /// <summary>
    /// Statistic Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    StatisticTuning = 0x339bc5bd,

    /// <summary>
    /// Story Arc Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    StoryArcTuning = 0x602b1dad,

    /// <summary>
    /// Story Chapter Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    StoryChapterTuning = 0x4a864a3a,

    /// <summary>
    /// Strategy Set Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    StrategySetTuning = 0x6224c9d6,

    /// <summary>
    /// Street Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    StreetTuning = 0xf6e4cb00,

    /// <summary>
    /// String Table
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    StringTable = 0x220557da,

    /// <summary>
    /// Style
    /// </summary>
    Style = 0x9f5cff10,

    /// <summary>
    /// Styled Look
    /// </summary>
    StyledLook = 0x71bdb8a2,

    /// <summary>
    /// Subroot Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    SubrootTuning = 0xb7ff8f95,

    /// <summary>
    /// Sync Point Schema
    /// </summary>
    SyncPointSchema = 0x2d277213,

    /// <summary>
    /// Tag Categories Metadata Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    TagCategoriesMetadataTuning = 0x1c12d458,

    /// <summary>
    /// Tag Set Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    TagSetTuning = 0x49395302,

    /// <summary>
    /// Tags Metadata Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    TagsMetadataTuning = 0x9db989fd,

    /// <summary>
    /// Tag Trait Group Metadata Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    TagTraitGroupMetadataTuning = 0x893e429c,

    /// <summary>
    /// Tag Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    TagTuning = 0xdd057dcc,

    /// <summary>
    /// Telemetry Memory Usage Telemetry Settings Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    TelemetryMemoryUsageTelemetrySettingsTuning = 0x89d65f75,

    /// <summary>
    /// Telemetry Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    TelemetryTuning = 0xace5d486,

    /// <summary>
    /// Template Chooser Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    TemplateChooserTuning = 0x48c2d5ed,

    /// <summary>
    /// Terrain Blend Map
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    TerrainBlendMap = 0x3d8632d0,

    /// <summary>
    /// Terrain Data
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    TerrainData = 0x9063660d,

    /// <summary>
    /// Terrain Height Map
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    TerrainHeightMap = 0x2ad195f2,

    /// <summary>
    /// Terrain KD Tree
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    TerrainKDTree = 0x033b2b66,

    /// <summary>
    /// Terrain Mesh
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    TerrainMesh = 0xae39399f,

    /// <summary>
    /// Terrain Paint
    /// </summary>
    TerrainPaint = 0xebcbb16c,

    /// <summary>
    /// Terrain Size Info
    /// </summary>
    TerrainSizeInfo = 0x90624c1b,

    /// <summary>
    /// Terrain Tool
    /// </summary>
    TerrainTool = 0x1427c109,

    /// <summary>
    /// Test Based Score Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    TestBasedScoreTuning = 0x4f739cee,

    /// <summary>
    /// A proprietary resource used by The Sims Resource Workshop
    /// </summary>
    [ResourceToolingMetadata]
    TheSimsResourceWorkshopProprietary = 0x01357924,

    /// <summary>
    /// Thrift Store Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ThriftStoreTuning = 0xe4fdcf04,

    /// <summary>
    /// Thumbnail Cache
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    ThumbnailCache = 0xb93a9915,

    /// <summary>
    /// Thumbnail Extra 1
    /// </summary>
    ThumbnailExtra1 = 0x16ca6bc4,

    /// <summary>
    /// Thumbnail Extra 2
    /// </summary>
    ThumbnailExtra2 = 0xb0118c15,

    /// <summary>
    /// Thumbnail Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ThumbnailTuning = 0xe1c27cd7,

    /// <summary>
    /// Timeline Events
    /// </summary>
    TimelineEvents = 0x06ac244f,

    /// <summary>
    /// Topic Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    TopicTuning = 0x738e6c56,

    /// <summary>
    /// Track Mask
    /// </summary>
    TrackMask = 0x033260e3,

    /// <summary>
    /// Trait Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    TraitTuning = 0xcb5fddc7,

    /// <summary>
    /// Tray Item
    /// </summary>
    TrayItem = 0x2a8a5e22,

    /// <summary>
    /// Trim
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    Trim = 0x76bcf80c,

    /// <summary>
    /// TrueType Font
    /// </summary>
    TrueTypeFont = 0x276ca4b9,

    /// <summary>
    /// Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    Tuning = 0x03b33ddf,

    /// <summary>
    /// Tutorial Tip Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    TutorialTipTuning = 0x8fb3e0b1,

    /// <summary>
    /// Tutorial Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    TutorialTuning = 0xe04a24a3,

    /// <summary>
    /// UI Control Event Map
    /// </summary>
    UIControlEventMap = 0xbdd82221,

    /// <summary>
    /// UI Event Mode Mapping
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    UIEventModeMapping = 0x99d98089,

    /// <summary>
    /// University Course Data Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    UniversityCourseDataTuning = 0x291cafbe,

    /// <summary>
    /// University Course Major Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    UniversityCourseMajorTuning = 0x2758b34b,

    /// <summary>
    /// University Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    UniversityTuning = 0xd958d5b1,

    /// <summary>
    /// Unknown World 2
    /// </summary>
    UnknownWorld2 = 0x17c0c281,

    /// <summary>
    /// Unknown World 3
    /// </summary>
    UnknownWorld3 = 0x96b0bd17,

    /// <summary>
    /// Unknown World 5
    /// </summary>
    UnknownWorld5 = 0xbc80ed59,

    /// <summary>
    /// User Interface Info Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    UserInterfaceInfoTuning = 0xb8bf1a63,

    /// <summary>
    /// Venue Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    VenueTuning = 0xe6bbd7de,

    /// <summary>
    /// Video Global Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    VideoGlobalTuning = 0x9188116f,

    /// <summary>
    /// Video Playlist Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    VideoPlaylistTuning = 0x40bf45cb,

    /// <summary>
    /// Visual Effects
    /// </summary>
    VisualEffects = 0x1b192049,

    /// <summary>
    /// Visual Effects Instance Map
    /// </summary>
    VisualEffectsInstanceMap = 0x1b19204a,

    /// <summary>
    /// Visual Effects Merged
    /// </summary>
    VisualEffectsMerged = 0xea5118b0,

    /// <summary>
    /// Voice Effect
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    VoiceEffect = 0x73cb32c2,

    /// <summary>
    /// Voice Plugin
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    VoicePlugin = 0xc582d2fb,

    /// <summary>
    /// Walk By Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    WalkByTuning = 0x3fd6243e,

    /// <summary>
    /// Walkstyle
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    Walkstyle = 0x27c01d95,

    /// <summary>
    /// Wall
    /// </summary>
    Wall = 0xd5f0f921,

    /// <summary>
    /// Wall Trim
    /// </summary>
    WallTrim = 0xfe33068e,

    /// <summary>
    /// Water Manifest
    /// </summary>
    WaterManifest = 0x1709627d,

    /// <summary>
    /// Water Mask
    /// </summary>
    WaterMask = 0x47fddfbc,

    /// <summary>
    /// Water Mask List
    /// </summary>
    WaterMaskList = 0xa4ba8645,

    /// <summary>
    /// Weather Event Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    WeatherEventTuning = 0x5806f5ba,

    /// <summary>
    /// Weather Forecast Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    WeatherForecastTuning = 0x497f3271,

    /// <summary>
    /// Whim Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    WhimTuning = 0x749a0636,

    /// <summary>
    /// Window Set
    /// </summary>
    WindowSet = 0xa8f7b517,

    /// <summary>
    /// WMRF Reference
    /// </summary>
    WMRFReference = 0x20d81496,

    /// <summary>
    /// World Camera Info
    /// </summary>
    WorldCameraInfo = 0x892c4b8a,

    /// <summary>
    /// World Camera Mesh
    /// </summary>
    WorldCameraMesh = 0xfb0dd002,

    /// <summary>
    /// World Conditional Data
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    WorldConditionalData = 0xd04fa861,

    /// <summary>
    /// World Data
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    WorldData = 0x810a102d,

    /// <summary>
    /// World Description
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    WorldDescription = 0xa680ea4b,

    /// <summary>
    /// World File Header
    /// </summary>
    WorldFileHeader = 0xf0633989,

    /// <summary>
    /// World Landing Strip
    /// </summary>
    WorldLandingStrip = 0x4f726bbe,

    /// <summary>
    /// World Lights Info
    /// </summary>
    WorldLightsInfo = 0x18f3c673,

    /// <summary>
    /// World Lot Architecture
    /// </summary>
    WorldLotArchitecture = 0x12952634,

    /// <summary>
    /// World Lot Objects
    /// </summary>
    WorldLotObjects = 0x91568fd8,

    /// <summary>
    /// World Lot ParameterInfo
    /// </summary>
    WorldLotParameterInfo = 0x3bf8fd86,

    /// <summary>
    /// World Manifest
    /// </summary>
    WorldManifest = 0x78c8bce4,

    /// <summary>
    /// World Map
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    WorldMap = 0x1cc04273,

    /// <summary>
    /// Worldmap Lot Thumbnail
    /// </summary>
    [ResourceFileType(ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)]
    WorldmapLotThumbnail = 0xa1ff2fc4,

    /// <summary>
    /// World Object Data
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    WorldObjectData = 0xfcb1a1e4,

    /// <summary>
    /// World Off Lot Mesh
    /// </summary>
    WorldOffLotMesh = 0x0a227bcf,

    /// <summary>
    /// World Road Polys
    /// </summary>
    WorldRoadPolys = 0x6f40796a,

    /// <summary>
    /// World Timeline Color
    /// </summary>
    [ResourceFileType(ResourceFileType.Binary)]
    WorldTimelineColor = 0x19301120,

    /// <summary>
    /// World Visual Effects Info
    /// </summary>
    WorldVisualEffectsInfo = 0x5be29703,

    /// <summary>
    /// World Water Manifest
    /// </summary>
    WorldWaterManifest = 0x153d2219,

    /// <summary>
    /// Zone Director Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ZoneDirectorTuning = 0xf958a092,

    /// <summary>
    /// Zone Modifier Tuning
    /// </summary>
    [ResourceFileType(ResourceFileType.TuningMarkup)]
    ZoneModifierTuning = 0x3c1d8799,

    /// <summary>
    /// Zone Object Data
    /// </summary>
    ZoneObjectData = 0x00000006
}
