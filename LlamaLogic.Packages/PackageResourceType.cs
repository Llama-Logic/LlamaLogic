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
    /// None
    /// </summary>
    None = 0,

    /// <summary>
    /// Account Reward Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    AccountRewardTuning = 0xbb0f19d8,

    /// <summary>
    /// Achievement Category Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    AchievementCategoryTuning = 0x2451c101,

    /// <summary>
    /// Achievement Collection Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    AchievementCollectionTuning = 0x04d2b465,

    /// <summary>
    /// Achievement Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    AchievementTuning = 0x78559e9e,

    /// <summary>
    /// Action Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ActionTuning = 0x0c772e27,

    /// <summary>
    /// Animation Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    AnimationTuning = 0xee17c6ad,

    /// <summary>
    /// Aspiration Category Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    AspirationCategoryTuning = 0xe350dbd8,

    /// <summary>
    /// Aspiration Track Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    AspirationTrackTuning = 0xc020fcad,

    /// <summary>
    /// Aspiration Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    AspirationTuning = 0x28b64675,

    /// <summary>
    /// Automation Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    AutomationTuning = 0xdad162b8,

    /// <summary>
    /// Away Action Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    AwayActionTuning = 0xafadac48,

    /// <summary>
    /// Balloon Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    BalloonTuning = 0xec6a8fc6,

    /// <summary>
    /// Breed Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    BreedTuning = 0x341d3f25,

    /// <summary>
    /// Broadcaster Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    BroadcasterTuning = 0xdebafb73,

    /// <summary>
    /// Bucks Perk Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    BucksPerkTuning = 0xec3da10e,

    /// <summary>
    /// Buff Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    BuffTuning = 0x6017e896,

    /// <summary>
    /// Build Buy Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    BuildBuyTuning = 0xae03c339,

    /// <summary>
    /// Business Rule Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    BusinessRuleTuning = 0xb8e58c6c,

    /// <summary>
    /// Business Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    BusinessTuning = 0x75d807f3,

    /// <summary>
    /// Call To Action Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CallToActionTuning = 0xf537b2e0,

    /// <summary>
    /// Camera Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CameraTuning = 0x12496650,

    /// <summary>
    /// Career Event Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CareerEventTuning = 0x94420322,

    /// <summary>
    /// Career Gig Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CareerGigTuning = 0xccdb0edd,

    /// <summary>
    /// Career Level Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CareerLevelTuning = 0x2c70adf8,

    /// <summary>
    /// Career Track Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CareerTrackTuning = 0x48c75ce3,

    /// <summary>
    /// Career Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CareerTuning = 0x73996beb,

    /// <summary>
    /// CAS Camera Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CASCameraTuning = 0x255adee2,

    /// <summary>
    /// CAS Lighting Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CASLightingTuning = 0x800a3690,

    /// <summary>
    /// CAS Menu Item Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CASMenuItemTuning = 0x0cba50f4,

    /// <summary>
    /// CAS Menu Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CASMenuTuning = 0x935a83c2,

    /// <summary>
    /// CAS Modifier Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CASModifierTuning = 0xf3abff3c,

    /// <summary>
    /// CAS Occult Skintone Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CASOccultSkintoneTuning = 0x7bc36c4e,

    /// <summary>
    /// CAS Occult Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CASOccultTuning = 0x219769e9,

    /// <summary>
    /// CAS Preference Category Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CASPreferenceCategoryTuning = 0xce04fc4b,

    /// <summary>
    /// CAS Preference Item Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CASPreferenceItemTuning = 0xec68fd22,

    /// <summary>
    /// CAS Stories Answer Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CASStoriesAnswerTuning = 0x80f12d17,

    /// <summary>
    /// CAS Stories Question Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CASStoriesQuestionTuning = 0x03246b9d,

    /// <summary>
    /// CAS Stories Trait Chooser Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CASStoriesTraitChooserTuning = 0x8dad1549,

    /// <summary>
    /// CAS Thumbnail Part Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CASThumbnailPartTuning = 0xaed1d0ac,

    /// <summary>
    /// CAS Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    CASTuning = 0xe24b5287,

    /// <summary>
    /// Clan Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ClanTuning = 0xdebee6a5,

    /// <summary>
    /// Clan Value Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ClanValueTuning = 0x998ed0ab,

    /// <summary>
    /// Client Tutorial Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ClientTutorialTuning = 0x2673076d,

    /// <summary>
    /// Club Interaction Group Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ClubInteractionGroupTuning = 0xfa0ffa34,

    /// <summary>
    /// Club Seed Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ClubSeedTuning = 0x2f59b437,

    /// <summary>
    /// Conditional Layer Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ConditionalLayerTuning = 0x9183dc91,

    /// <summary>
    /// Detective Clue Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    DetectiveClueTuning = 0x537449f6,

    /// <summary>
    /// Developmental Milestone Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    DevelopmentalMilestoneTuning = 0xc5224f94,

    /// <summary>
    /// Drama Node Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    DramaNodeTuning = 0x2553f435,

    /// <summary>
    /// Ensemble Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    EnsembleTuning = 0xb9881120,

    /// <summary>
    /// Game Ruleset Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    GameRulesetTuning = 0xe1477e18,

    /// <summary>
    /// Guidance Tip Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    GuidanceTipTuning = 0xd4a09abd,

    /// <summary>
    /// Headline Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    HeadlineTuning = 0xf401205d,

    /// <summary>
    /// Holiday Definition Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    HolidayDefinitionTuning = 0x0e316f6d,

    /// <summary>
    /// Holiday Tradition Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    HolidayTraditionTuning = 0x3fcd2486,

    /// <summary>
    /// Household Milestone Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    HouseholdMilestoneTuning = 0x3972e6f3,

    /// <summary>
    /// Hsv Tweaker Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    HsvTweakerSettingsTuning = 0x23fc90fe,

    /// <summary>
    /// Interaction Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    InteractionTuning = 0xe882d22f,

    /// <summary>
    /// Lot Decoration Preset Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    LotDecorationPresetTuning = 0xde1ef8fb,

    /// <summary>
    /// Lot Decoration Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    LotDecorationTuning = 0xfe2db1ab,

    /// <summary>
    /// Lot Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    LotTuning = 0xd8800d66,

    /// <summary>
    /// Lunar Cycle Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    LunarCycleTuning = 0x55493b18,

    /// <summary>
    /// Mood Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    MoodTuning = 0xba7b60b8,

    /// <summary>
    /// Narrative Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    NarrativeTuning = 0x3e753c39,

    /// <summary>
    /// Native Build Buy Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    NativeBuildBuyTuning = 0x1a94e9b4,

    /// <summary>
    /// Native Seasons Weather Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    NativeSeasonsWeatherTuning = 0xb43a5ad1,

    /// <summary>
    /// Notebook Entry Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    NotebookEntryTuning = 0x9902fa76,

    /// <summary>
    /// Object Part Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ObjectPartTuning = 0x7147a350,

    /// <summary>
    /// Object State Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ObjectStateTuning = 0x5b02819e,

    /// <summary>
    /// Object Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ObjectTuning = 0xb61de6b4,

    /// <summary>
    /// Objective Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ObjectiveTuning = 0x0069453e,

    /// <summary>
    /// Open Street Director Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    OpenStreetDirectorTuning = 0x4b6fdec4,

    /// <summary>
    /// Pie Menu Category Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    PieMenuCategoryTuning = 0x03e9d964,

    /// <summary>
    /// Posture Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    PostureTuning = 0xad6fdf1f,

    /// <summary>
    /// Puberty Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    PubertyTuning = 0x4819084d,

    /// <summary>
    /// Rabbit Hole Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RabbitHoleTuning = 0xb16ad2fa,

    /// <summary>
    /// Recipe Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RecipeTuning = 0xeb97f823,

    /// <summary>
    /// Region Sort Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RegionSortTuning = 0x3f57c885,

    /// <summary>
    /// Region Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RegionTuning = 0x51e7a18d,

    /// <summary>
    /// Relationship Bit Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RelationshipBitTuning = 0x0904df10,

    /// <summary>
    /// Relationship Lock Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RelationshipLockTuning = 0xae34e673,

    /// <summary>
    /// Renderer Censor Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RendererCensorTuning = 0xe1d6fad2,

    /// <summary>
    /// Renderer Fade Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RendererFadeTuning = 0x041f95da,

    /// <summary>
    /// Renderer Global DOF Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RendererGlobalDOFSettingsTuning = 0x8d33a0de,

    /// <summary>
    /// Renderer Global Highlight Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RendererGlobalHighlightSettingsTuning = 0xbc7e2e95,

    /// <summary>
    /// Renderer Global Light Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RendererGlobalLightSettingsTuning = 0x6ab26065,

    /// <summary>
    /// Renderer Global Shadow Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RendererGlobalShadowSettingsTuning = 0x45295831,

    /// <summary>
    /// Renderer Global SSAO Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RendererGlobalSSAOSettingsTuning = 0x2f6ee30f,

    /// <summary>
    /// Renderer Global Vol Light Scattering Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RendererGlobalVolLightScatteringSettingsTuning = 0xc5e245de,

    /// <summary>
    /// Reward Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RewardTuning = 0x6fa49828,

    /// <summary>
    /// Role State Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RoleStateTuning = 0x0e4d15fb,

    /// <summary>
    /// Royalty Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    RoyaltyTuning = 0x37ef2ee7,

    /// <summary>
    /// Season Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SeasonTuning = 0xc98dd45e,

    /// <summary>
    /// Service NPC Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ServiceNPCTuning = 0x9cc21262,

    /// <summary>
    /// Sickness Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SicknessTuning = 0xc3fbd8de,

    /// <summary>
    /// Sim Data
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SimData = 0x545ac67a,

    /// <summary>
    /// Sim Filter Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SimFilterTuning = 0x6e0dda9f,

    /// <summary>
    /// Sim Info Fixup Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SimInfoFixupTuning = 0xe2581892,

    /// <summary>
    /// Sim Lighting Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SimLightingTuning = 0x9ddb5fda,

    /// <summary>
    /// Sim Template Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SimTemplateTuning = 0x0ca4c78b,

    /// <summary>
    /// Situation Goal Set Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SituationGoalSetTuning = 0x9df2f1f2,

    /// <summary>
    /// Situation Goal Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SituationGoalTuning = 0x598f28e7,

    /// <summary>
    /// Situation Job Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SituationJobTuning = 0x9c07855f,

    /// <summary>
    /// Situation Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SituationTuning = 0xfbc3aeeb,

    /// <summary>
    /// Slot Type Set Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SlotTypeSetTuning = 0x3f163505,

    /// <summary>
    /// Slot Type Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SlotTypeTuning = 0x69a5daa4,

    /// <summary>
    /// Snippet Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SnippetTuning = 0x7df2169c,

    /// <summary>
    /// Social Group Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SocialGroupTuning = 0x2e47a104,

    /// <summary>
    /// Spell Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SpellTuning = 0x1f3413d9,

    /// <summary>
    /// Static Commodity Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    StaticCommodityTuning = 0x51077643,

    /// <summary>
    /// Statistic Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    StatisticTuning = 0x339bc5bd,

    /// <summary>
    /// Story Arc Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    StoryArcTuning = 0x602b1dad,

    /// <summary>
    /// Story Chapter Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    StoryChapterTuning = 0x4a864a3a,

    /// <summary>
    /// Strategy Set Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    StrategySetTuning = 0x6224c9d6,

    /// <summary>
    /// Street Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    StreetTuning = 0xf6e4cb00,

    /// <summary>
    /// Subroot Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    SubrootTuning = 0xb7ff8f95,

    /// <summary>
    /// Tag Categories Metadata Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    TagCategoriesMetadataTuning = 0x1c12d458,

    /// <summary>
    /// Tag Set Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    TagSetTuning = 0x49395302,

    /// <summary>
    /// Tag Trait Group Metadata Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    TagTraitGroupMetadataTuning = 0x893e429c,

    /// <summary>
    /// Tag Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    TagTuning = 0xdd057dcc,

    /// <summary>
    /// Tags Metadata Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    TagsMetadataTuning = 0x9db989fd,

    /// <summary>
    /// Telemetry Memory Usage Telemetry Settings Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    TelemetryMemoryUsageTelemetrySettingsTuning = 0x89d65f75,

    /// <summary>
    /// Telemetry Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    TelemetryTuning = 0xace5d486,

    /// <summary>
    /// Template Chooser Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    TemplateChooserTuning = 0x48c2d5ed,

    /// <summary>
    /// Test Based Score Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    TestBasedScoreTuning = 0x4f739cee,

    /// <summary>
    /// Thrift Store Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ThriftStoreTuning = 0xe4fdcf04,

    /// <summary>
    /// Thumbnail Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ThumbnailTuning = 0xe1c27cd7,

    /// <summary>
    /// Topic Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    TopicTuning = 0x738e6c56,

    /// <summary>
    /// Trait Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    TraitTuning = 0xcb5fddc7,

    /// <summary>
    /// Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    Tuning = 0x03b33ddf,

    /// <summary>
    /// Tutorial Tip Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    TutorialTipTuning = 0x8fb3e0b1,

    /// <summary>
    /// Tutorial Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    TutorialTuning = 0xe04a24a3,

    /// <summary>
    /// University Course Data Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    UniversityCourseDataTuning = 0x291cafbe,

    /// <summary>
    /// University Course Major Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    UniversityCourseMajorTuning = 0x2758b34b,

    /// <summary>
    /// University Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    UniversityTuning = 0xd958d5b1,

    /// <summary>
    /// User Interface Info Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    UserInterfaceInfoTuning = 0xb8bf1a63,

    /// <summary>
    /// Venue Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    VenueTuning = 0xe6bbd7de,

    /// <summary>
    /// Video Global Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    VideoGlobalTuning = 0x9188116f,

    /// <summary>
    /// Video Playlist Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    VideoPlaylistTuning = 0x40bf45cb,

    /// <summary>
    /// Walk By Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    WalkByTuning = 0x3fd6243e,

    /// <summary>
    /// Weather Event Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    WeatherEventTuning = 0x5806f5ba,

    /// <summary>
    /// Weather Forecast Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    WeatherForecastTuning = 0x497f3271,

    /// <summary>
    /// Whim Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    WhimTuning = 0x749a0636,

    /// <summary>
    /// Zone Director Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ZoneDirectorTuning = 0xf958a092,

    /// <summary>
    /// Zone Modifier Tuning
    /// </summary>
    [PackageResourceFileType(PackageResourceFileType.Xml)]
    ZoneModifierTuning = 0x3c1d8799
}
