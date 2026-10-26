namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningSimDataDrafterTests
{
    const string BuffSad = "<I c=\"Buff\" i=\"buff\" m=\"buffs.buff\" n=\"Buff_Sad\" s=\"201\"><T n=\"buff_name\">0x0000ABCE</T><T n=\"mood_type\">100</T><T n=\"mood_weight\">2</T></I>";

    const string SimDataFileName = "545AC67A!0017E8F6!00000000000000C9.Buff_Sad.SimData.xml";

    static TuningDescriptionSet OpenDescriptions()
    {
        var descriptions = TuningArchiveFixtures.Descriptions.ToDictionary(pair => pair.Key, pair => pair.Value);
        descriptions["Buffs/Descriptions/Buff.tdesc"] = descriptions["Buffs/Descriptions/Buff.tdesc"].Replace("name=\"mood_weight\"", "name=\"mood_weight\" export_modes=\"client_binary,server_xml\"", StringComparison.Ordinal);
        return TuningArchiveFixtures.OpenDescriptions(descriptions);
    }

    static async Task<TuningSimDataDraftResult> DraftAsync(string draft, string? gameSimDataTuning)
    {
        using var set = OpenDescriptions();
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var files = new Dictionary<string, string> { ["BG/Buff_Sad.xml"] = BuffSad };
        if (gameSimDataTuning is not null)
            files[$"BG/{SimDataFileName}"] = new SimDataGenerator(set).Generate(TuningXml.Parse(gameSimDataTuning)).ToXml().ToString();
        var result = await archive.ImportFolderAsync(directory.WriteFiles("tuning", files), "1.0.1.1000");
        var entry = archive.GetEntries(result.Snapshot, TuningArchiveResourceKind.Tuning).Single();
        var drafter = TuningSimDataDrafter.Create(archive, result.Snapshot, set);
        return drafter.Draft(entry, TuningXml.GetCanonicalText(TuningXml.Parse(draft)));
    }

    [TestMethod]
    public async Task AChangeToAnExportedTunableGeneratesSimData()
    {
        var result = await DraftAsync(BuffSad.Replace(">2<", ">3<", StringComparison.Ordinal), BuffSad);
        Assert.AreEqual(TuningSimDataDraftKind.Generated, result.Kind);
        Assert.AreEqual(new ResourceKey(ResourceType.SimData, 0x0017E8F6, 201), result.Key);
        Assert.AreEqual("3", result.SimData!.ToXml().Descendants("T").Single(element => element.Attribute("name")?.Value == "mood_weight").Value);
    }

    [TestMethod]
    public async Task AChangeThatDoesNotReachSimDataLeavesItAlone()
    {
        var result = await DraftAsync(BuffSad.Replace("0x0000ABCE", "0x0000ABCF", StringComparison.Ordinal), BuffSad);
        Assert.AreEqual(TuningSimDataDraftKind.Unaffected, result.Kind);
        Assert.IsNull(result.SimData);
    }

    [TestMethod]
    public async Task SimDataThatCannotBeReproducedIsNotTrusted()
    {
        var result = await DraftAsync(BuffSad.Replace(">2<", ">3<", StringComparison.Ordinal), BuffSad.Replace(">2<", ">5<", StringComparison.Ordinal));
        Assert.AreEqual(TuningSimDataDraftKind.Failed, result.Kind);
        Assert.IsNull(result.SimData);
    }

    [TestMethod]
    public async Task TuningWithoutSimDataNeedsNone()
    {
        var result = await DraftAsync(BuffSad.Replace(">2<", ">3<", StringComparison.Ordinal), null);
        Assert.AreEqual(TuningSimDataDraftKind.Unaffected, result.Kind);
        Assert.IsNull(result.Key);
    }
}