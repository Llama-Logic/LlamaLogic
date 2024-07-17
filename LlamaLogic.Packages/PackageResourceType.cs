namespace LlamaLogic.Packages;

/// <summary>
/// A type of resource within a package
/// </summary>
[CLSCompliant(false)]
[SuppressMessage("Design", "CA1028: Enum Storage should be Int32", Justification = "This is by nature of how the package files work. We have no control over that.")]
public enum PackageResourceType :
    uint
{
    /// <summary>
    /// Unspecified
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Account Reward Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    AccountRewardTuning = 0xbb0f19d8,

    /// <summary>
    /// Achievement Category Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    AchievementCategoryTuning = 0x2451c101,

    /// <summary>
    /// Achievement Collection Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    AchievementCollectionTuning = 0x04d2b465,

    /// <summary>
    /// Achievement Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    AchievementTuning = 0x78559e9e,

    /// <summary>
    /// Action Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ActionTuning = 0x0c772e27,

    /// <summary>
    /// Age Gender Map
    /// </summary>
    AgeGenderMap = 0x010faf71,

    /// <summary>
    /// Ambience
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    AnimationStateMachine = 0x02d5df13,

    /// <summary>
    /// Animation Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    AnimationTuning = 0xee17c6ad,

    /// <summary>
    /// Apartment Thumbnail 1
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
    ApartmentThumbnail1 = 0xab19bcba,

    /// <summary>
    /// Apartment Thumbnail 2
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
    ApartmentThumbnail2 = 0xbd491726,

    /// <summary>
    /// Aspiration Category Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    AspirationCategoryTuning = 0xe350dbd8,

    /// <summary>
    /// Aspiration Track Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    AspirationTrackTuning = 0xc020fcad,

    /// <summary>
    /// Aspiration Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    AspirationTuning = 0x28b64675,

    /// <summary>
    /// Audio Configuration
    /// </summary>
    AudioConfiguration = 0xfd04e3be,

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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    AwayActionTuning = 0xafadac48,

    /// <summary>
    /// Balloon Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    BalloonTuning = 0xec6a8fc6,

    /// <summary>
    /// Blend Geometry
    /// </summary>
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
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
    BlueprintImage = 0xd33c281e,

    /// <summary>
    /// Body Part Thumbnail
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
    BodyPartThumbnail = 0x5b282d45,

    /// <summary>
    /// Bone Pose
    /// </summary>
    BonePose = 0x0355e0a6,

    /// <summary>
    /// Breed Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    BreedTuning = 0x341d3f25,

    /// <summary>
    /// Broadcaster Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    BroadcasterTuning = 0xdebafb73,

    /// <summary>
    /// Bucks Perk Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    BucksPerkTuning = 0xec3da10e,

    /// <summary>
    /// Buff Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    BuffTuning = 0x6017e896,

    /// <summary>
    /// Build Buy Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    BuildBuyTuning = 0xae03c339,

    /// <summary>
    /// Business Rule Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    BusinessRuleTuning = 0xb8e58c6c,

    /// <summary>
    /// Business Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    BusinessTuning = 0x75d807f3,

    /// <summary>
    /// Buy Build Thumbnail
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
    BuyBuildThumbnail = 0x3c2a8647,

    /// <summary>
    /// C71CA490
    /// </summary>
    C71CA490 = 0xc71ca490,

    /// <summary>
    /// Call To Action Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CallToActionTuning = 0xf537b2e0,

    /// <summary>
    /// Camera Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CameraTuning = 0x12496650,

    /// <summary>
    /// Career Event Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CareerEventTuning = 0x94420322,

    /// <summary>
    /// Career Gig Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CareerGigTuning = 0xccdb0edd,

    /// <summary>
    /// Career Level Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CareerLevelTuning = 0x2c70adf8,

    /// <summary>
    /// Career Track Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CareerTrackTuning = 0x48c75ce3,

    /// <summary>
    /// Career Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CareerTuning = 0x73996beb,

    /// <summary>
    /// CAS Camera Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CASCameraTuning = 0x255adee2,

    /// <summary>
    /// CAS Lighting Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CASLightingTuning = 0x800a3690,

    /// <summary>
    /// CAS Menu Item Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CASMenuItemTuning = 0x0cba50f4,

    /// <summary>
    /// CAS Menu Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CASMenuTuning = 0x935a83c2,

    /// <summary>
    /// CAS Modifier Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CASModifierTuning = 0xf3abff3c,

    /// <summary>
    /// CAS Occult Skintone Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CASOccultSkintoneTuning = 0x7bc36c4e,

    /// <summary>
    /// CAS Occult Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CASOccultTuning = 0x219769e9,

    /// <summary>
    /// CAS Part
    /// </summary>
    CASPart = 0x034aeecb,

    /// <summary>
    /// CAS Part Thumbnail
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
    CASPartThumbnail = 0x3c1af1f2,

    /// <summary>
    /// CAS Preference Category Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CASPreferenceCategoryTuning = 0xce04fc4b,

    /// <summary>
    /// CAS Preference Item Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CASPreferenceItemTuning = 0xec68fd22,

    /// <summary>
    /// CAS Preset
    /// </summary>
    CASPreset = 0xeaa32add,

    /// <summary>
    /// CAS Stories Answer Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CASStoriesAnswerTuning = 0x80f12d17,

    /// <summary>
    /// CAS Stories Question Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CASStoriesQuestionTuning = 0x03246b9d,

    /// <summary>
    /// CAS Stories Trait Chooser Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CASStoriesTraitChooserTuning = 0x8dad1549,

    /// <summary>
    /// CAS Thumbnail Part Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CASThumbnailPartTuning = 0xaed1d0ac,

    /// <summary>
    /// CAS Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CASTuning = 0xe24b5287,

    /// <summary>
    /// Clan Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ClanTuning = 0xdebee6a5,

    /// <summary>
    /// Clan Value Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ClanValueTuning = 0x998ed0ab,

    /// <summary>
    /// Client Tutorial Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ClientTutorialTuning = 0x2673076d,

    /// <summary>
    /// Clip
    /// </summary>
    Clip = 0x6b20c4f3,

    /// <summary>
    /// Clip Header
    /// </summary>
    ClipHeader = 0xbc4a5044,

    /// <summary>
    /// Club Interaction Group Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ClubInteractionGroupTuning = 0xfa0ffa34,

    /// <summary>
    /// Club Seed Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    CombinedBinaryTuning = 0x62e94d38,

    /// <summary>
    /// Conditional Layer Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ConditionalLayerTuning = 0x9183dc91,

    /// <summary>
    /// Credits
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    Credits = 0x669499dc,

    /// <summary>
    /// Cursor
    /// </summary>
    Cursor = 0x26978421,

    /// <summary>
    /// Cutout Info Table
    /// </summary>
    CutoutInfoTable = 0x81ca1a10,

    /// <summary>
    /// Deco Trim
    /// </summary>
    DecoTrim = 0x13cf0ed2,

    /// <summary>
    /// Deformer Map
    /// </summary>
    DeformerMap = 0xdb43e069,

    /// <summary>
    /// Detective Clue Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    DetectiveClueTuning = 0x537449f6,

    /// <summary>
    /// Developmental Milestone Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    DevelopmentalMilestoneTuning = 0xc5224f94,

    /// <summary>
    /// Drama Node Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    DramaNodeTuning = 0x2553f435,

    /// <summary>
    /// DST Image
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.DirectDrawSurface)]
    DSTImage = 0x00b2d882,

    /// <summary>
    /// Ensemble Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    FontConfiguration = 0x0333406c,

    /// <summary>
    /// Footprint
    /// </summary>
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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    GameRulesetTuning = 0xe1477e18,

    /// <summary>
    /// Generic MTX
    /// </summary>
    GenericMTX = 0xac03a936,

    /// <summary>
    /// Geometry
    /// </summary>
    Geometry = 0x015a1849,

    /// <summary>
    /// Guidance Tip Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    GuidanceTipTuning = 0xd4a09abd,

    /// <summary>
    /// HalfWall
    /// </summary>
    HalfWall = 0x9151e6bc,

    /// <summary>
    /// Headline Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    HeadlineTuning = 0xf401205d,

    /// <summary>
    /// Holiday Definition Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    HolidayDefinitionTuning = 0x0e316f6d,

    /// <summary>
    /// Holiday Tradition Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    HolidayTraditionTuning = 0x3fcd2486,

    /// <summary>
    /// Household Description
    /// </summary>
    HouseholdDescription = 0x729f6c4f,

    /// <summary>
    /// Household Milestone Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    HouseholdMilestoneTuning = 0x3972e6f3,

    /// <summary>
    /// Household Template
    /// </summary>
    HouseholdTemplate = 0xb3c438f0,

    /// <summary>
    /// Hsv Tweaker Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    HsvTweakerSettingsTuning = 0x23fc90fe,

    /// <summary>
    /// Interaction Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    LocomotionBuilder = 0x053a3e7b,

    /// <summary>
    /// Locomotion Config
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    LocomotionConfig = 0x9afe47f5,

    /// <summary>
    /// Lot Decoration Preset Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    LotDecorationPresetTuning = 0xde1ef8fb,

    /// <summary>
    /// Lot Decoration Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    LotDecorationTuning = 0xfe2db1ab,

    /// <summary>
    /// Lot Description
    /// </summary>
    LotDescription = 0x01942e2c,

    /// <summary>
    /// Lot Footprint Reference
    /// </summary>
    LotFootprintReference = 0xc0084996,

    /// <summary>
    /// Lot Preview Thumbnail
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
    LotPreviewThumbnail = 0x0d338a3a,

    /// <summary>
    /// Lot Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    LotTuning = 0xd8800d66,

    /// <summary>
    /// Lot Type Event Map
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    LotTypeEventMap = 0x122fc66a,

    /// <summary>
    /// LRLE Image
    /// </summary>
    LRLEImage = 0x2bc04edf,

    /// <summary>
    /// Lunar Cycle Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
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
    /// Modal Music Mapping
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    MoodTuning = 0xba7b60b8,

    /// <summary>
    /// MTX Catalog
    /// </summary>
    MTXCatalog = 0x6dff1a66,

    /// <summary>
    /// Music Data
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    MusicData = 0xc202c770,

    /// <summary>
    /// Name Map
    /// </summary>
    NameMap = 0x0166038c,

    /// <summary>
    /// Narrative Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    NarrativeTuning = 0x3e753c39,

    /// <summary>
    /// Native Build Buy Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    NativeBuildBuyTuning = 0x1a94e9b4,

    /// <summary>
    /// Native Seasons Weather Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    NativeSeasonsWeatherTuning = 0xb43a5ad1,

    /// <summary>
    /// Notebook Entry Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    NotebookEntryTuning = 0x9902fa76,

    /// <summary>
    /// Object Catalog
    /// </summary>
    ObjectCatalog = 0x319e4f1d,

    /// <summary>
    /// Object Catalog Set
    /// </summary>
    ObjectCatalogSet = 0xff56010c,

    /// <summary>
    /// Object Definition
    /// </summary>
    ObjectDefinition = 0xc0db5ae7,

    /// <summary>
    /// Objective Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ObjectiveTuning = 0x0069453e,

    /// <summary>
    /// Object Modifiers
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ObjectModifiers = 0xe231b3d8,

    /// <summary>
    /// Object Part Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ObjectPartTuning = 0x7147a350,

    /// <summary>
    /// Object State Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ObjectStateTuning = 0x5b02819e,

    /// <summary>
    /// Object Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ObjectTuning = 0xb61de6b4,

    /// <summary>
    /// Open Street Director Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
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
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
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
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
    PetFacePresetThumbnail = 0xb67673a2,

    /// <summary>
    /// Pet Pelt Layer
    /// </summary>
    PetPeltLayer = 0x26af8338,

    /// <summary>
    /// Pie Menu Category Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    PieMenuCategoryTuning = 0x03e9d964,

    /// <summary>
    /// Platform
    /// </summary>
    Platform = 0x99c07284,

    /// <summary>
    /// PNG
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
    PNG = 0x0119b36d,

    /// <summary>
    /// PNG Image
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
    PNGImage = 0x2f7d0004,

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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    PostureTuning = 0xad6fdf1f,

    /// <summary>
    /// Puberty Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    PubertyTuning = 0x4819084d,

    /// <summary>
    /// Queryable World Mask Manifest
    /// </summary>
    QueryableWorldMaskManifest = 0x4e71b4e6,

    /// <summary>
    /// Rabbit Hole Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RabbitHoleTuning = 0xb16ad2fa,

    /// <summary>
    /// Railing
    /// </summary>
    Railing = 0x1c1cf1f7,

    /// <summary>
    /// Recipe Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RecipeTuning = 0xeb97f823,

    /// <summary>
    /// Record Bundle
    /// </summary>
    RecordBundle = 0x3eaaa87c,

    /// <summary>
    /// Region Description
    /// </summary>
    RegionDescription = 0xd65daff9,

    /// <summary>
    /// Region Map
    /// </summary>
    RegionMap = 0xac16fbec,

    /// <summary>
    /// Region Sort Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RegionSortTuning = 0x3f57c885,

    /// <summary>
    /// Region Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RegionTuning = 0x51e7a18d,

    /// <summary>
    /// Relationship Bit Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RelationshipBitTuning = 0x0904df10,

    /// <summary>
    /// Relationship Lock Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RelationshipLockTuning = 0xae34e673,

    /// <summary>
    /// Renderer Censor Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RendererCensorTuning = 0xe1d6fad2,

    /// <summary>
    /// Renderer Fade Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RendererFadeTuning = 0x041f95da,

    /// <summary>
    /// Renderer Global DOF Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RendererGlobalDOFSettingsTuning = 0x8d33a0de,

    /// <summary>
    /// Renderer Global Highlight Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RendererGlobalHighlightSettingsTuning = 0xbc7e2e95,

    /// <summary>
    /// Renderer Global Light Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RendererGlobalLightSettingsTuning = 0x6ab26065,

    /// <summary>
    /// Renderer Global Shadow Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RendererGlobalShadowSettingsTuning = 0x45295831,

    /// <summary>
    /// Renderer Global SSAO Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RendererGlobalSSAOSettingsTuning = 0x2f6ee30f,

    /// <summary>
    /// Renderer Global Vol Light Scattering Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RendererGlobalVolLightScatteringSettingsTuning = 0xc5e245de,

    /// <summary>
    /// Reward Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RewardTuning = 0x6fa49828,

    /// <summary>
    /// Rig
    /// </summary>
    Rig = 0x8eaf13de,

    /// <summary>
    /// RLE 2 Image
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.DirectDrawSurface)]
    RLE2Image = 0x3453cf95,

    /// <summary>
    /// RLES Image
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.DirectDrawSurface)]
    RLESImage = 0xba856c78,

    /// <summary>
    /// Road Definition
    /// </summary>
    RoadDefinition = 0x9063660e,

    /// <summary>
    /// Role State Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    RoyaltyTuning = 0x37ef2ee7,

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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SeasonTuning = 0xc98dd45e,

    /// <summary>
    /// Service NPC Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ServiceNPCTuning = 0x9cc21262,

    /// <summary>
    /// Sickness Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SicknessTuning = 0xc3fbd8de,

    /// <summary>
    /// Sim Data
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SimData = 0x545ac67a,

    /// <summary>
    /// Sim Featured Outfit Thumbnail
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
    SimFeaturedOutfitThumbnail = 0xcd9de247,

    /// <summary>
    /// Sim Filter Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SimFilterTuning = 0x6e0dda9f,

    /// <summary>
    /// Sim Hotspot Control
    /// </summary>
    SimHotspotControl = 0x8b18ff6e,

    /// <summary>
    /// Sim Household Thumbnail
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
    SimHouseholdThumbnail = 0x3bd45407,

    /// <summary>
    /// Sim Info
    /// </summary>
    SimInfo = 0x025ed6f4,

    /// <summary>
    /// Sim Info Fixup Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SimInfoFixupTuning = 0xe2581892,

    /// <summary>
    /// Sim Lighting Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
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
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
    SimPresetThumbnail = 0x9c925813,

    /// <summary>
    /// Sim Template Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SimTemplateTuning = 0x0ca4c78b,

    /// <summary>
    /// Situation Goal Set Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SituationGoalSetTuning = 0x9df2f1f2,

    /// <summary>
    /// Situation Goal Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SituationGoalTuning = 0x598f28e7,

    /// <summary>
    /// Situation Job Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SituationJobTuning = 0x9c07855f,

    /// <summary>
    /// Situation Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SituationTuning = 0xfbc3aeeb,

    /// <summary>
    /// Skintone
    /// </summary>
    Skintone = 0x0354796a,

    /// <summary>
    /// Sky Box Texture Data
    /// </summary>
    SkyBoxTextureData = 0x71a449c9,

    /// <summary>
    /// Slot
    /// </summary>
    Slot = 0xd3044521,

    /// <summary>
    /// Slot Type Set Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SlotTypeSetTuning = 0x3f163505,

    /// <summary>
    /// Slot Type Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SlotTypeTuning = 0x69a5daa4,

    /// <summary>
    /// Snippet Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SnippetTuning = 0x7df2169c,

    /// <summary>
    /// Social Group Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SocialGroupTuning = 0x2e47a104,

    /// <summary>
    /// Sound Mix Properties
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SoundMixProperties = 0x4115f9d5,

    /// <summary>
    /// Sound Modifier Mapping
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SoundModifierMapping = 0xa576c2e7,

    /// <summary>
    /// Sound Properties
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SpellTuning = 0x1f3413d9,

    /// <summary>
    /// Stairs
    /// </summary>
    Stairs = 0x9a20cd1c,

    /// <summary>
    /// Static Commodity Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    StaticCommodityTuning = 0x51077643,

    /// <summary>
    /// Statistic Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    StatisticTuning = 0x339bc5bd,

    /// <summary>
    /// Story Arc Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    StoryArcTuning = 0x602b1dad,

    /// <summary>
    /// Story Chapter Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    StoryChapterTuning = 0x4a864a3a,

    /// <summary>
    /// Strategy Set Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    StrategySetTuning = 0x6224c9d6,

    /// <summary>
    /// Street Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    StreetTuning = 0xf6e4cb00,

    /// <summary>
    /// String Table
    /// </summary>
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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    SubrootTuning = 0xb7ff8f95,

    /// <summary>
    /// Sync Point Schema
    /// </summary>
    SyncPointSchema = 0x2d277213,

    /// <summary>
    /// Tag Categories Metadata Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    TagCategoriesMetadataTuning = 0x1c12d458,

    /// <summary>
    /// Tag Set Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    TagSetTuning = 0x49395302,

    /// <summary>
    /// Tags Metadata Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    TagsMetadataTuning = 0x9db989fd,

    /// <summary>
    /// Tag Trait Group Metadata Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    TagTraitGroupMetadataTuning = 0x893e429c,

    /// <summary>
    /// Tag Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    TagTuning = 0xdd057dcc,

    /// <summary>
    /// Telemetry Memory Usage Telemetry Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    TelemetryMemoryUsageTelemetrySettingsTuning = 0x89d65f75,

    /// <summary>
    /// Telemetry Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    TelemetryTuning = 0xace5d486,

    /// <summary>
    /// Template Chooser Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    TemplateChooserTuning = 0x48c2d5ed,

    /// <summary>
    /// Terrain Blend Map
    /// </summary>
    TerrainBlendMap = 0x3d8632d0,

    /// <summary>
    /// Terrain Data
    /// </summary>
    TerrainData = 0x9063660d,

    /// <summary>
    /// Terrain Height Map
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
    TerrainHeightMap = 0x2ad195f2,

    /// <summary>
    /// Terrain KD Tree
    /// </summary>
    TerrainKDTree = 0x033b2b66,

    /// <summary>
    /// Terrain Mesh
    /// </summary>
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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    TestBasedScoreTuning = 0x4f739cee,

    /// <summary>
    /// Thrift Store Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ThriftStoreTuning = 0xe4fdcf04,

    /// <summary>
    /// Thumbnail Cache
    /// </summary>
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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ThumbnailTuning = 0xe1c27cd7,

    /// <summary>
    /// Timeline Events
    /// </summary>
    TimelineEvents = 0x06ac244f,

    /// <summary>
    /// Topic Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    TopicTuning = 0x738e6c56,

    /// <summary>
    /// Track Mask
    /// </summary>
    TrackMask = 0x033260e3,

    /// <summary>
    /// Trait Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    TraitTuning = 0xcb5fddc7,

    /// <summary>
    /// Tray Item
    /// </summary>
    TrayItem = 0x2a8a5e22,

    /// <summary>
    /// Trim
    /// </summary>
    Trim = 0x76bcf80c,

    /// <summary>
    /// TrueType Font
    /// </summary>
    TrueTypeFont = 0x276ca4b9,

    /// <summary>
    /// Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    Tuning = 0x03b33ddf,

    /// <summary>
    /// Tutorial Tip Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    TutorialTipTuning = 0x8fb3e0b1,

    /// <summary>
    /// Tutorial Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    TutorialTuning = 0xe04a24a3,

    /// <summary>
    /// UI Control Event Map
    /// </summary>
    UIControlEventMap = 0xbdd82221,

    /// <summary>
    /// UI Event Mode Mapping
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    UIEventModeMapping = 0x99d98089,

    /// <summary>
    /// University Course Data Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    UniversityCourseDataTuning = 0x291cafbe,

    /// <summary>
    /// University Course Major Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    UniversityCourseMajorTuning = 0x2758b34b,

    /// <summary>
    /// University Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    UserInterfaceInfoTuning = 0xb8bf1a63,

    /// <summary>
    /// Venue Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    VenueTuning = 0xe6bbd7de,

    /// <summary>
    /// Video Global Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    VideoGlobalTuning = 0x9188116f,

    /// <summary>
    /// Video Playlist Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    VoiceEffect = 0x73cb32c2,

    /// <summary>
    /// Voice Plugin
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    VoicePlugin = 0xc582d2fb,

    /// <summary>
    /// Walk By Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    WalkByTuning = 0x3fd6243e,

    /// <summary>
    /// Walkstyle
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    WeatherEventTuning = 0x5806f5ba,

    /// <summary>
    /// Weather Forecast Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    WeatherForecastTuning = 0x497f3271,

    /// <summary>
    /// Whim Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
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
    WorldConditionalData = 0xd04fa861,

    /// <summary>
    /// World Data
    /// </summary>
    WorldData = 0x810a102d,

    /// <summary>
    /// World Description
    /// </summary>
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
    WorldMap = 0x1cc04273,

    /// <summary>
    /// Worldmap Lot Thumbnail
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.PortableNetworkGraphic)]
    WorldmapLotThumbnail = 0xa1ff2fc4,

    /// <summary>
    /// WorldObjectData
    /// </summary>
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
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ZoneDirectorTuning = 0xf958a092,

    /// <summary>
    /// Zone Modifier Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.TuningMarkup)]
    ZoneModifierTuning = 0x3c1d8799
}
