namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningPythonDraftTests
{
    const string BuffSad = "<I c=\"Buff\" i=\"buff\" m=\"buffs.buff\" n=\"Buff_Sad\" s=\"201\"><T n=\"buff_name\">0x0000ABCE</T><T n=\"mood_type\">100</T><T n=\"mood_weight\">2</T></I>";

    const string MoodHappy = "<I c=\"Mood\" i=\"mood\" m=\"buffs.mood\" n=\"Mood_Happy\" s=\"100\"><L n=\"buffs\"><U><T n=\"buff_type\">200</T></U><U><T n=\"buff_type\">201</T></U></L></I>";

    const string Names = "<M n=\"tests.names\" s=\"12345\"><C n=\"Names\"><L n=\"FIRST_NAMES\"><T>Ann</T></L></C></M>";

    static TuningDescriptionSet OpenNamesDescriptions(string? exportModes = null)
    {
        var descriptions = TuningArchiveFixtures.Descriptions.ToDictionary(pair => pair.Key, pair => pair.Value);
        descriptions["Tuning/tests-names.tdesc"] =
            $"""
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Module name="tests.names" muid="D000000000000001">
                    <Class name="Names" muid="D000000000000002">
                        <TunableList name="FIRST_NAMES" class="TunableList" display="First Names" muid="D000000000000003" Deprecated="False"{(exportModes is null ? string.Empty : $" export_modes=\"{exportModes}\"")} filter="0" group="General">
                            <Tunable type="str" class="Tunable" default="" muid="D000000000000004" Deprecated="False" filter="0" group="General" />
                        </TunableList>
                    </Class>
                </Module>
            </TuningRoot>
            """;
        return TuningArchiveFixtures.OpenDescriptions(descriptions);
    }

    static async Task<TuningDraftResult> DraftAsync(TuningDescriptionSet set, string original, string draft)
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var result = await archive.ImportFolderAsync(directory.WriteFiles("tuning", new Dictionary<string, string> { ["BG/Tuning.xml"] = original }), "1.0.1.1000");
        var entry = archive.GetEntries(result.Snapshot, TuningArchiveResourceKind.Tuning).Single();
        var originalText = archive.GetText(entry);
        var draftText = TuningXml.GetCanonicalText(TuningXml.Parse(draft));
        return TuningPythonDraft.Generate([new(entry, TuningAnalysis.Analyze(originalText, set), TuningAnalysis.Analyze(draftText, set))], "1.0.1.1000", "draft.log");
    }

    [TestMethod]
    public async Task AddingACopyOfAListItemAsksForTwoOfIt()
    {
        using var set = TuningArchiveFixtures.OpenDescriptions();
        var result = await DraftAsync(set, MoodHappy, MoodHappy.Replace("</L>", "<U><T n=\"buff_type\">201</T></U></L>", StringComparison.Ordinal));
        Assert.AreEqual(1, result.Changes);
        StringAssert.Contains(result.Source, "'Mood_Happy', 'append', 'buffs', None, '<U><T n=\"buff_type\">201</T></U>', 2),");
        Assert.AreEqual(TuningDraftSeverity.Information, result.Diagnostics.Single().Severity);
    }

    [TestMethod]
    public async Task ChangingAListItemReplacesItByItsContent()
    {
        using var set = TuningArchiveFixtures.OpenDescriptions();
        var result = await DraftAsync(set, MoodHappy, MoodHappy.Replace(">201<", ">202<", StringComparison.Ordinal));
        Assert.AreEqual(1, result.Changes);
        StringAssert.Contains(result.Source, "'replace_item', 'buffs', '<U><T n=\"buff_type\">201</T></U>', '<U><T n=\"buff_type\">202</T></U>', None),");
        Assert.AreEqual("buffs[1]", result.Diagnostics.Single().Path);
    }

    [TestMethod]
    public async Task ChangingAValueReplacesItOnlyIfItIsAsExpected()
    {
        using var set = TuningArchiveFixtures.OpenDescriptions();
        var result = await DraftAsync(set, BuffSad, BuffSad.Replace(">2<", ">3<", StringComparison.Ordinal));
        Assert.AreEqual(1, result.Changes);
        StringAssert.Contains(result.Source, "(0x6017E896, 201, 'Buff_Sad', 'replace', 'mood_weight', '<T n=\"mood_weight\">2</T>', '<T n=\"mood_weight\">3</T>', None),");
        StringAssert.Contains(result.Source, "if current != expected:");
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(TuningDraftSeverity.Information, diagnostic.Severity);
        Assert.AreEqual("mood_weight", diagnostic.Path);
    }

    [TestMethod]
    public async Task ChangesToTunablesInSimDataAreLeftOut()
    {
        var descriptions = TuningArchiveFixtures.Descriptions.ToDictionary(pair => pair.Key, pair => pair.Value);
        descriptions["Buffs/Descriptions/Buff.tdesc"] = descriptions["Buffs/Descriptions/Buff.tdesc"].Replace("name=\"buff_name\"", "name=\"buff_name\" export_modes=\"client_binary,server_xml\"", StringComparison.Ordinal);
        using var set = TuningArchiveFixtures.OpenDescriptions(descriptions);
        var result = await DraftAsync(set, BuffSad, BuffSad.Replace("0x0000ABCE", "0x0000ABCF", StringComparison.Ordinal).Replace(">2<", ">3<", StringComparison.Ordinal));
        Assert.AreEqual(1, result.Changes);
        Assert.IsFalse(result.Source.Contains("'buff_name'", StringComparison.Ordinal));
        var error = result.Diagnostics.Single(diagnostic => diagnostic.Severity is TuningDraftSeverity.Error);
        Assert.AreEqual("buff_name", error.Path);
        StringAssert.Contains(error.Message, "SimData");
    }

    [TestMethod]
    public async Task AddingToModuleTuningChangesItOnceTuningIsLoaded()
    {
        using var set = OpenNamesDescriptions();
        var result = await DraftAsync(set, Names, Names.Replace("</L>", "<T>Bob</T></L>", StringComparison.Ordinal));
        Assert.AreEqual(1, result.Changes);
        StringAssert.Contains(result.Source, "('tests.names', 'Names', 'append', 'FIRST_NAMES', None, '<T>Bob</T>', 1),");
        StringAssert.Contains(result.Source, "_serialization.finalize_deferred_loads = _finalize_deferred_loads_and_apply_changes");
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(TuningDraftSeverity.Information, diagnostic.Severity);
        Assert.AreEqual("Names.FIRST_NAMES", diagnostic.Path);
    }

    [TestMethod]
    public async Task ChangesToModuleTuningInSimDataAreLeftOut()
    {
        using var set = OpenNamesDescriptions("client_binary,server_binary,server_xml");
        var result = await DraftAsync(set, Names, Names.Replace("</L>", "<T>Bob</T></L>", StringComparison.Ordinal));
        Assert.AreEqual(0, result.Changes);
        Assert.IsFalse(result.Source.Contains("'FIRST_NAMES'", StringComparison.Ordinal));
        var error = result.Diagnostics.Single();
        Assert.AreEqual(TuningDraftSeverity.Error, error.Severity);
        Assert.AreEqual("Names.FIRST_NAMES", error.Path);
        StringAssert.Contains(error.Message, "override");
    }

    [TestMethod]
    public async Task RemovingAListItemRemovesItByItsContent()
    {
        using var set = TuningArchiveFixtures.OpenDescriptions();
        var result = await DraftAsync(set, MoodHappy, MoodHappy.Replace("<U><T n=\"buff_type\">200</T></U>", string.Empty, StringComparison.Ordinal));
        Assert.AreEqual(1, result.Changes);
        StringAssert.Contains(result.Source, "'remove', 'buffs', '<U><T n=\"buff_type\">200</T></U>', None, 0),");
    }

    [TestMethod]
    public async Task UnchangedTuningDraftsNothing()
    {
        using var set = TuningArchiveFixtures.OpenDescriptions();
        var result = await DraftAsync(set, BuffSad, BuffSad);
        Assert.AreEqual(0, result.Changes);
        Assert.AreEqual(0, result.Diagnostics.Count);
    }
}