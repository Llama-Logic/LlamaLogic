namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningXmlInjectorDraftTests
{
    const string GameObject = "<I c=\"GameObject\" i=\"object\" m=\"objects.game_object\" n=\"object_Thing\" s=\"400\"><L n=\"_super_affordances\"><T>500</T></L></I>";

    const string Trait = "<I c=\"Trait\" i=\"trait\" m=\"traits.traits\" n=\"trait_Thing\" s=\"300\"><L n=\"buffs\"><U><T n=\"buff_type\">200</T></U></L><T n=\"ages\">ADULT</T></I>";

    static async Task<TuningDraftResult> DraftAsync(string original, string draft, string name = "creator:draft")
    {
        using var set = TuningArchiveFixtures.OpenDescriptions();
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var result = await archive.ImportFolderAsync(directory.WriteFiles("tuning", new Dictionary<string, string> { ["BG/Tuning.xml"] = original }), "1.0.1.1000");
        var entry = archive.GetEntries(result.Snapshot, TuningArchiveResourceKind.Tuning).Single();
        var originalText = archive.GetText(entry);
        var draftText = TuningXml.GetCanonicalText(TuningXml.Parse(draft));
        return TuningXmlInjectorDraft.Generate([new(entry, TuningAnalysis.Analyze(originalText, set), TuningAnalysis.Analyze(draftText, set))], name);
    }

    [TestMethod]
    public async Task AddingABuffToATraitAddsBuffsToTheTrait()
    {
        var result = await DraftAsync(Trait, Trait.Replace("</L>", "<U><T n=\"buff_type\">201</T></U></L>", StringComparison.Ordinal));
        Assert.AreEqual(1, result.Changes);
        var snippet = XDocument.Parse(result.Source).Root!;
        Assert.AreEqual("2", snippet.Elements("T").Single(element => element.Attribute("n")?.Value == "xml_injector_minimum_version").Value);
        var entry = snippet.Elements("L").Single(element => element.Attribute("n")?.Value == "add_buffs_to_trait").Elements("U").Single();
        Assert.AreEqual("300", entry.Elements("T").Single(element => element.Attribute("n")?.Value == "trait").Value);
        Assert.AreEqual("201", entry.Elements("L").Single().Elements("U").Single().Elements("T").Single().Value);
        Assert.AreEqual(TuningDraftSeverity.Information, result.Diagnostics.Single().Severity);
    }

    [TestMethod]
    public async Task AddingAnInteractionToAnObjectSelectsTheObjectByInstance()
    {
        var result = await DraftAsync(GameObject, GameObject.Replace("</L>", "<T>501</T></L>", StringComparison.Ordinal));
        Assert.AreEqual(1, result.Changes);
        var snippet = XDocument.Parse(result.Source).Root!;
        Assert.AreEqual("1", snippet.Elements("T").Single(element => element.Attribute("n")?.Value == "xml_injector_minimum_version").Value);
        var entry = snippet.Elements("L").Single(element => element.Attribute("n")?.Value == "add_interactions_to_objects").Elements("U").Single();
        Assert.AreEqual("object_list", entry.Element("V")!.Attribute("t")!.Value);
        Assert.AreEqual("400", entry.Element("V")!.Descendants("T").Single().Value);
        Assert.AreEqual("501", entry.Elements("L").Single(element => element.Attribute("n")?.Value == "_super_affordances").Elements("T").Single().Value);
    }

    [TestMethod]
    public async Task RemovingItemsAndChangingValuesAreLeftOut()
    {
        var result = await DraftAsync(Trait, Trait.Replace("<U><T n=\"buff_type\">200</T></U>", string.Empty, StringComparison.Ordinal).Replace(">ADULT<", ">CHILD<", StringComparison.Ordinal));
        Assert.AreEqual(0, result.Changes);
        Assert.AreEqual(2, result.Diagnostics.Count(diagnostic => diagnostic.Severity is TuningDraftSeverity.Error));
        CollectionAssert.AreEquivalent(new[] { "ages", "buffs" }, result.Diagnostics.Select(diagnostic => diagnostic.Path).ToArray());
        Assert.IsNull(XDocument.Parse(result.Source).Root!.Element("L"));
    }

    [TestMethod]
    public async Task TheSnippetIsNamedAndNumberedAsCustomTuning()
    {
        var result = await DraftAsync(Trait, Trait.Replace("</L>", "<U><T n=\"buff_type\">201</T></U></L>", StringComparison.Ordinal), "creator:my_injections");
        var snippet = XDocument.Parse(result.Source).Root!;
        Assert.AreEqual("XmlInjector", snippet.Attribute("c")!.Value);
        Assert.AreEqual("snippet", snippet.Attribute("i")!.Value);
        Assert.AreEqual("xml_injector.snippet", snippet.Attribute("m")!.Value);
        Assert.AreEqual("creator:my_injections", snippet.Attribute("n")!.Value);
        Assert.AreEqual(Fnv64.SetHighBit(Fnv64.GetHash("creator:my_injections")).ToString(CultureInfo.InvariantCulture), snippet.Attribute("s")!.Value);
    }
}