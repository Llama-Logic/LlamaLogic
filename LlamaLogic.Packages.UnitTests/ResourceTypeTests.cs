namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class ResourceTypeTests :
    TestPackageTests
{
    static uint GetFnv1Hash(string text)
    {
        var hash = 0x811c9dc5U;
        foreach (var b in Encoding.UTF8.GetBytes(text))
            hash = (hash * 0x01000193U) ^ b;
        return hash;
    }

    [TestMethod]
    [DataRow(ResourceType.AutomationTuning, "Automation")]
    [DataRow(ResourceType.BuildBuyTuning, "BuildBuy")]
    [DataRow(ResourceType.HsvTweakerSettingsTuning, "HsvTweakerSettings")]
    [DataRow(ResourceType.NativeBuildBuyTuning, "NativeBuildBuy")]
    [DataRow(ResourceType.NativeSeasonsWeatherTuning, "Native_SeasonsWeather")]
    [DataRow(ResourceType.RegionSortTuning, "RegionSort")]
    [DataRow(ResourceType.RendererCensorTuning, "Renderer_CensorTuning")]
    [DataRow(ResourceType.RendererFadeTuning, "Renderer_FadeTuning")]
    [DataRow(ResourceType.RendererGlobalDOFSettingsTuning, "Renderer_GlobalDofSettings")]
    [DataRow(ResourceType.RendererGlobalHighlightSettingsTuning, "Renderer_GlobalHighlightSettings")]
    [DataRow(ResourceType.RendererGlobalLightSettingsTuning, "Renderer_GlobalLightSettings")]
    [DataRow(ResourceType.RendererGlobalShadowSettingsTuning, "Renderer_GlobalShadowSettings")]
    [DataRow(ResourceType.RendererGlobalSSAOSettingsTuning, "Renderer_GlobalSsaoSettings")]
    [DataRow(ResourceType.RendererGlobalVolLightScatteringSettingsTuning, "Renderer_GlobalVolLightScatteringSettings")]
    [DataRow(ResourceType.TelemetryMemoryUsageTelemetrySettingsTuning, "Telemetry_MemoryUsageTelemetrySettings")]
    [DataRow(ResourceType.TelemetryTuning, "Telemetry_Tuning")]
    [DataRow(ResourceType.ThumbnailTuning, "Thumbnail")]
    [DataRow(ResourceType.VideoGlobalTuning, "Video_GlobalTuning")]
    [DataRow(ResourceType.VideoPlaylistTuning, "Video_Playlist")]
    public void EngineSettingsTypesHashTheirGameNames(ResourceType resourceType, string gameName) =>
        Assert.AreEqual(GetFnv1Hash(gameName), (uint)resourceType);

    [TestMethod]
    [DataRow(0x6690f724U)]
    [DataRow(0x16b37b7aU)]
    [DataRow(0x83f9aa6dU)]
    [DataRow(0xde64c78bU)]
    [DataRow(0x057424ddU)]
    [DataRow(0xb22ea24dU)]
    [DataRow(0x988c2af5U)]
    public void GameInstanceTuningTypesAreNamed(uint resourceType)
    {
        var key = new ResourceKey((ResourceType)resourceType, 0x80000000, 1);
        using var dbpf = new DataBasePackedFile();
        dbpf.SetXml(key, "<I c=\"Tuning\" i=\"tuning\" m=\"tuning\" n=\"LlamaLogicPackageTests:Named\" s=\"1\" />");
        Assert.AreEqual("LlamaLogicPackageTests:Named", dbpf.GetNameByKey(key));
        Assert.IsInstanceOfType<string>(dbpf[key]);
    }

    [TestMethod]
    [DataRow(0x6690f724U, "CASPreferenceGroupTuning")]
    [DataRow(0x16b37b7aU, "CheatSheetTuning")]
    [DataRow(0x83f9aa6dU, "DynastySeedTuning")]
    [DataRow(0xde64c78bU, "GetawayChallengeTuning")]
    [DataRow(0x8614bed9U, "GPINI")]
    [DataRow(0xa9fadc5dU, "HalfWallTrim")]
    [DataRow(0x8db1dd26U, "MTXBundle")]
    [DataRow(0xd81bddc9U, "ObjDef")]
    [DataRow(0x057424ddU, "SmallBusinessBinaryTuning")]
    [DataRow(0xb22ea24dU, "SmallBusinessSeedTuning")]
    [DataRow(0x346cb93eU, "TuningDependenciesCache")]
    [DataRow(0x962c5034U, "TuningDescription")]
    [DataRow(0x988c2af5U, "UniversityCourseScheduleTuning")]
    public void GameNamedTypesAreDefined(uint resourceType, string expectedName) =>
        Assert.AreEqual(expectedName, Enum.GetName((ResourceType)resourceType));
}
