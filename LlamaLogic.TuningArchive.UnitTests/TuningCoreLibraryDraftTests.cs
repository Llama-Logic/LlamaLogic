namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningCoreLibraryDraftTests
{
    const string GameObject = "<I c=\"GameObject\" i=\"object\" m=\"objects.game_object\" n=\"object_Thing\" s=\"400\"><L n=\"_super_affordances\"><T>500</T></L></I>";

    const string Trait = "<I c=\"Trait\" i=\"trait\" m=\"traits.traits\" n=\"trait_Thing\" s=\"300\"><L n=\"buffs\"><U><T n=\"buff_type\">200</T></U></L><L n=\"super_affordances\"><T>600</T></L></I>";

    static async Task<TuningDraftResult> DraftAsync(string original, string draft, string name = "creator:draft")
    {
        using var set = TuningArchiveFixtures.OpenDescriptions();
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var result = await archive.ImportFolderAsync(directory.WriteFiles("tuning", new Dictionary<string, string> { ["BG/Tuning.xml"] = original }), "1.0.1.1000");
        var entry = archive.GetEntries(result.Snapshot, TuningArchiveResourceKind.Tuning).Single();
        var originalText = archive.GetText(entry);
        var draftText = TuningXml.GetCanonicalText(TuningXml.Parse(draft));
        return TuningCoreLibraryDraft.Generate([new(entry, TuningAnalysis.Analyze(originalText, set), TuningAnalysis.Analyze(draftText, set))], name);
    }

    const string TraitDescription =
        """
        <?xml version="1.0" encoding="iso-8859-1"?>

        <TuningRoot>
            <Instance instance_type="trait" class="Trait" module="traits.traits" muid="9F0C39A1D1E2B3C4" description="A trait." instance_needs_reference="True" instance_subclasses_only="False" path="Traits\Descriptions" use_guid_for_reference="True">
                <TunableList name="ages" class="TunableEnumFlags" default="ADULT" display="Ages" muid="A000000000000001" Deprecated="False" filter="0" group="General" unique_entries="true">
                    <TunableEnum type="Age" class="TunableEnumEntry" default="ADULT" muid="A000000000000002" static_entries="sims-sim_info_types.Age" Deprecated="False" filter="0" group="General" />
                </TunableList>
                <TunableList name="super_affordances" class="TunableList" display="Super Affordances" muid="A000000000000003" Deprecated="False" filter="0" group="General">
                    <Tunable type="interaction" class="TunableReference" muid="A000000000000004" allow_none="False" Deprecated="False" filter="0" group="General" />
                </TunableList>
            </Instance>
        </TuningRoot>
        """;

    const string UnsetTrait = "<I c=\"Trait\" i=\"trait\" m=\"traits.traits\" n=\"trait_Thing\" s=\"300\" />";

    static async Task<TuningDraftResult> DraftWithTraitDescriptionAsync(string original, string draft)
    {
        using var set = TuningArchiveFixtures.OpenDescriptions(new Dictionary<string, string>(TuningArchiveFixtures.Descriptions) { ["Traits/Descriptions/Trait.tdesc"] = TraitDescription });
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var result = await archive.ImportFolderAsync(directory.WriteFiles("tuning", new Dictionary<string, string> { ["BG/Tuning.xml"] = original }), "1.0.1.1000");
        var entry = archive.GetEntries(result.Snapshot, TuningArchiveResourceKind.Tuning).Single();
        var originalText = archive.GetText(entry);
        var draftText = TuningXml.GetCanonicalText(TuningXml.Parse(draft));
        return TuningCoreLibraryDraft.Generate([new(entry, TuningAnalysis.Analyze(originalText, set), TuningAnalysis.Analyze(draftText, set))], "creator:draft");
    }

    const string ActorTest = "<V t=\"sim_info\"><U n=\"sim_info\"><E n=\"who\">Actor</E></U></V>";

    const string MoodTest = "<V t=\"mood\"><U n=\"mood\"><T n=\"mood\">14638</T></U></V>";

    const string TargetTest = "<V t=\"sim_info\"><U n=\"sim_info\"><E n=\"who\">TargetSim</E></U></V>";

    const string TraitTest = "<V t=\"trait\"><U n=\"trait\"><L n=\"whitelist_traits\"><T>16860</T></L></U></V>";

    static string Interaction(string globals, params string[] testLists) =>
        $"<I c=\"SuperInteraction\" i=\"interaction\" m=\"interactions.base.super_interaction\" n=\"sim_Thing\" s=\"700\"><L n=\"test_globals\">{globals}</L><L n=\"tests\">{string.Concat(testLists.Select(tests => $"<L>{tests}</L>"))}</L></I>";

    static XElement Modification(XElement injection, string field) =>
        injection.Elements("V").Single(element => element.Attribute("n")?.Value == field);

    static XElement? Member(XElement modification, string name) =>
        modification.Elements("U").Single().Elements().SingleOrDefault(element => element.Attribute("n")?.Value == name);

    static XElement Injection(TuningDraftResult result, string key) =>
        XDocument.Parse(result.Source).Root!.Elements("L").Single(element => element.Attribute("n")?.Value == key).Elements("U").Single();

    [TestMethod]
    public async Task AddingAnInteractionToAnObjectInjectsByObjectTuning()
    {
        var result = await DraftAsync(GameObject, GameObject.Replace("</L>", "<T>501</T></L>", StringComparison.Ordinal));
        Assert.AreEqual(1, result.Changes);
        var injection = Injection(result, "inject_by_object_tuning");
        Assert.AreEqual("400", injection.Elements("T").Single(element => element.Attribute("n")?.Value == "query").Value);
        Assert.AreEqual("501", injection.Elements("L").Single(element => element.Attribute("n")?.Value == "affordances").Elements("T").Single().Value);
    }

    [TestMethod]
    public async Task AddingAnItemTheListAlreadyHasIsAnError()
    {
        var result = await DraftAsync(GameObject, GameObject.Replace("</L>", "<T>500</T></L>", StringComparison.Ordinal));
        Assert.AreEqual(0, result.Changes);
        Assert.AreEqual(TuningDraftSeverity.Error, result.Diagnostics.Single().Severity);
    }

    [TestMethod]
    public async Task ChangesToOneTuningShareOneInjection()
    {
        var result = await DraftAsync(Trait, Trait.Replace("</U></L>", "</U><U><T n=\"buff_type\">201</T></U></L>", StringComparison.Ordinal).Replace("<T>600</T>", "<T>600</T><T>601</T>", StringComparison.Ordinal));
        Assert.AreEqual(2, result.Changes);
        var injection = Injection(result, "inject_to_traits");
        Assert.AreEqual("300", injection.Elements("T").Single(element => element.Attribute("n")?.Value == "trait").Value);
        Assert.AreEqual("201", injection.Elements("L").Single(element => element.Attribute("n")?.Value == "buffs").Descendants("T").Single().Value);
        Assert.AreEqual("601", injection.Elements("L").Single(element => element.Attribute("n")?.Value == "super_affordances").Elements("T").Single().Value);
    }

    [TestMethod]
    public async Task AddingToAListTheTuningDoesNotSetInjects()
    {
        var result = await DraftWithTraitDescriptionAsync(UnsetTrait, UnsetTrait.Replace(" />", "><L n=\"super_affordances\"><T>600</T></L></I>", StringComparison.Ordinal));
        Assert.AreEqual(1, result.Changes);
        var injection = Injection(result, "inject_to_traits");
        Assert.AreEqual("600", injection.Elements("L").Single(element => element.Attribute("n")?.Value == "super_affordances").Elements("T").Single().Value);
    }

    [TestMethod]
    public async Task SettingAListWhoseDefaultIsNotEmptyIsNotAnAddition()
    {
        var result = await DraftWithTraitDescriptionAsync(UnsetTrait, UnsetTrait.Replace(" />", "><L n=\"ages\"><E>BABY</E></L></I>", StringComparison.Ordinal));
        Assert.AreEqual(0, result.Changes);
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(TuningDraftSeverity.Error, diagnostic.Severity);
        Assert.AreEqual("ages", diagnostic.Path);
        StringAssert.Contains(diagnostic.Message, "can't change", StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task NewTestListsAreAddedWithOr()
    {
        var result = await DraftAsync(Interaction(ActorTest, MoodTest, TraitTest), Interaction(ActorTest, MoodTest, TraitTest, TargetTest));
        Assert.AreEqual(1, result.Changes);
        var injection = Injection(result, "inject_to_affordances");
        Assert.AreEqual("700", injection.Elements("L").Single(element => element.Attribute("n")?.Value == "affordances").Elements("T").Single().Value);
        var modification = Modification(injection, "modify_tests");
        Assert.AreEqual("add_tests", modification.Attribute("t")!.Value);
        Assert.IsNull(Member(modification, "AND"));
        Assert.AreEqual("TargetSim", Member(modification, "OR")!.Elements("L").Single().Descendants("E").Single().Value);
        Assert.AreEqual(TuningDraftSeverity.Information, result.Diagnostics.Single().Severity);
    }

    [TestMethod]
    public async Task ATestAddedToEveryTestListIsAddedWithAnd()
    {
        var result = await DraftAsync(Interaction(ActorTest, MoodTest, TraitTest), Interaction(ActorTest, MoodTest + TargetTest, TraitTest + TargetTest));
        var modification = Modification(Injection(result, "inject_to_affordances"), "modify_tests");
        Assert.AreEqual("add_tests", modification.Attribute("t")!.Value);
        Assert.AreEqual("TargetSim", Member(modification, "AND")!.Elements("V").Single().Descendants("E").Single().Value);
        Assert.IsNull(Member(modification, "OR"));
        Assert.IsNull(Member(modification, "prepend_and"));
    }

    [TestMethod]
    public async Task ATestAddedToTheStartOfEveryTestListIsPrepended()
    {
        var result = await DraftAsync(Interaction(ActorTest, MoodTest, TraitTest), Interaction(ActorTest, TargetTest + MoodTest, TargetTest + TraitTest));
        var modification = Modification(Injection(result, "inject_to_affordances"), "modify_tests");
        Assert.AreEqual("TargetSim", Member(modification, "AND")!.Elements("V").Single().Descendants("E").Single().Value);
        Assert.AreEqual("True", Member(modification, "prepend_and")!.Value);
    }

    [TestMethod]
    public async Task AGlobalTestIsAddedWithAnd()
    {
        var result = await DraftAsync(Interaction(ActorTest, MoodTest), Interaction(ActorTest + TargetTest, MoodTest));
        Assert.AreEqual(1, result.Changes);
        var modification = Modification(Injection(result, "inject_to_affordances"), "modify_global_tests");
        Assert.AreEqual("add_tests", modification.Attribute("t")!.Value);
        Assert.AreEqual("TargetSim", Member(modification, "AND")!.Elements("V").Single().Descendants("E").Single().Value);
    }

    [TestMethod]
    public async Task ATestAddedToOnlySomeTestListsReplacesTheTests()
    {
        var result = await DraftAsync(Interaction(ActorTest, MoodTest, TraitTest), Interaction(ActorTest, MoodTest + TargetTest, TraitTest));
        Assert.AreEqual(1, result.Changes);
        var modification = Modification(Injection(result, "inject_to_affordances"), "modify_tests");
        Assert.AreEqual("replace_tests", modification.Attribute("t")!.Value);
        Assert.AreEqual(2, Member(modification, "tests")!.Elements("L").Count());
        Assert.AreEqual(TuningDraftSeverity.Warning, result.Diagnostics.Single().Severity);
    }

    [TestMethod]
    public async Task RemovingATestListReplacesTheTests()
    {
        var result = await DraftAsync(Interaction(ActorTest, MoodTest, TraitTest), Interaction(ActorTest, MoodTest));
        var modification = Modification(Injection(result, "inject_to_affordances"), "modify_tests");
        Assert.AreEqual("replace_tests", modification.Attribute("t")!.Value);
        Assert.AreEqual("14638", Member(modification, "tests")!.Elements("L").Single().Descendants("T").Single().Value);
        Assert.AreEqual(TuningDraftSeverity.Warning, result.Diagnostics.Single().Severity);
    }

    [TestMethod]
    public async Task ChangesToSeveralTestSetsShareOneInjection()
    {
        var result = await DraftAsync(Interaction(ActorTest, MoodTest), Interaction(ActorTest + TargetTest, MoodTest, TraitTest));
        Assert.AreEqual(2, result.Changes);
        var injection = Injection(result, "inject_to_affordances");
        Assert.AreEqual("add_tests", Modification(injection, "modify_global_tests").Attribute("t")!.Value);
        Assert.AreEqual("add_tests", Modification(injection, "modify_tests").Attribute("t")!.Value);
    }

    [TestMethod]
    public async Task ATestListAddedToATestSetIsAddedWithOr()
    {
        const string testSet = $"<I c=\"TestSetInstance\" i=\"snippet\" m=\"event_testing.tests\" n=\"testSet_Thing\" s=\"800\"><L n=\"test\"><L>{MoodTest}</L></L></I>";
        var result = await DraftAsync(testSet, testSet.Replace("</L></L></I>", $"</L><L>{TargetTest}</L></L></I>", StringComparison.Ordinal));
        Assert.AreEqual(1, result.Changes);
        var injection = Injection(result, "inject_to_test_sets");
        Assert.AreEqual("800", injection.Elements("T").Single(element => element.Attribute("n")?.Value == "test_set").Value);
        var modification = Modification(injection, "modify_test");
        Assert.AreEqual("add_tests", modification.Attribute("t")!.Value);
        Assert.AreEqual("TargetSim", Member(modification, "OR")!.Elements("L").Single().Descendants("E").Single().Value);
    }

    [TestMethod]
    public async Task TheSnippetNamesTheCreatorAndTheMod()
    {
        var result = await DraftAsync(GameObject, GameObject.Replace("</L>", "<T>501</T></L>", StringComparison.Ordinal), "creator:my_mod");
        var snippet = XDocument.Parse(result.Source).Root!;
        Assert.AreEqual("TuningInjector", snippet.Attribute("c")!.Value);
        Assert.AreEqual("lot51_core.snippets.injector", snippet.Attribute("m")!.Value);
        Assert.AreEqual(Fnv64.SetHighBit(Fnv64.GetHash("creator:my_mod")).ToString(CultureInfo.InvariantCulture), snippet.Attribute("s")!.Value);
        Assert.AreEqual("creator", snippet.Elements("T").Single(element => element.Attribute("n")?.Value == "creator_name").Value);
        Assert.AreEqual("my_mod", snippet.Elements("T").Single(element => element.Attribute("n")?.Value == "mod_name").Value);
    }
}