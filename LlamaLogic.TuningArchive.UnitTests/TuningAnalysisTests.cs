namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningAnalysisTests
{
    static TuningAnalysis Analyze(TuningDescriptionSet set, string xml) =>
        TuningAnalysis.Analyze(TuningXml.GetCanonicalText(TuningXml.Parse(xml)), set);

    [TestMethod]
    public void EnumsAreResolvedThroughTheirStaticEntries()
    {
        using var set = TuningArchiveFixtures.OpenDescriptions();
        var analysis = Analyze(set, "<I c=\"Mood\" i=\"mood\" m=\"buffs.mood\" n=\"Mood_Happy\" s=\"100\"><L n=\"buffs\"><U><T n=\"buff_type\">200</T><E n=\"age\">ADULT</E></U></L></I>");
        var age = analysis.Nodes.Single(node => node.Path == "buffs[0].age");
        Assert.AreEqual(TuningValueKind.Enum, age.ValueKind);
        Assert.AreEqual("Age", age.Enum!.Name);
        Assert.AreEqual(32L, age.EnumValue);
        Assert.AreEqual(0, analysis.UnrecognizedPaths.Count);
    }

    [TestMethod]
    public void DeclaredTunablesAreAvailableForTheInstanceAndItsTuples()
    {
        using var set = TuningArchiveFixtures.OpenDescriptions();
        var buff = Analyze(set, "<I c=\"Buff\" i=\"buff\" m=\"buffs.buff\" n=\"Buff_Sad\" s=\"201\"><T n=\"mood_weight\">2</T></I>");
        CollectionAssert.AreEqual(new[] { "buff_name", "mood_type", "mood_weight" }, buff.InstanceMembers.Select(member => member.Name).ToArray());
        Assert.AreEqual(0, buff.Nodes.Single().Members.Count);
        var mood = Analyze(set, "<I c=\"Mood\" i=\"mood\" m=\"buffs.mood\" n=\"Mood_Happy\" s=\"100\"><L n=\"buffs\"><U><T n=\"buff_type\">200</T></U></L></I>");
        CollectionAssert.AreEqual(new[] { "buff_type", "age" }, mood.Nodes.Single(node => node.Path == "buffs[0]").Members.Select(member => member.Name).ToArray());
    }

    [TestMethod]
    public void ReferencesAreFoundInsideListsAndTuples()
    {
        using var set = TuningArchiveFixtures.OpenDescriptions();
        var analysis = Analyze(set, "<I c=\"Mood\" i=\"mood\" m=\"buffs.mood\" n=\"Mood_Happy\" s=\"100\"><L n=\"buffs\"><U><T n=\"buff_type\">200</T></U><U><T n=\"buff_type\">999</T></U></L></I>");
        var references = analysis.References.ToList();
        CollectionAssert.AreEqual(new[] { "buffs[0].buff_type", "buffs[1].buff_type" }, references.Select(node => node.Path).ToArray());
        CollectionAssert.AreEqual(new ulong?[] { 200, 999 }, references.Select(node => node.ReferenceInstance).ToArray());
        Assert.IsTrue(references.All(node => node.ReferenceType == ResourceType.BuffTuning && node.ReferenceTypeName == "buff"));
    }

    [TestMethod]
    public void ReferencesByNameAreRecognized()
    {
        using var set = TuningArchiveFixtures.OpenDescriptions();
        var reference = Analyze(set, "<I c=\"Buff\" i=\"buff\" m=\"buffs.buff\" n=\"Buff_Sad\" s=\"201\"><T n=\"mood_type\">Mood_Happy</T></I>").References.Single();
        Assert.AreEqual(ResourceType.MoodTuning, reference.ReferenceType);
        Assert.AreEqual("Mood_Happy", reference.ReferenceName);
        Assert.IsNull(reference.ReferenceInstance);
    }

    [TestMethod]
    public void TheElementsOfInstanceTuningAreDescribed()
    {
        using var set = TuningArchiveFixtures.OpenDescriptions();
        var analysis = Analyze(set, "<I c=\"Buff\" i=\"buff\" m=\"buffs.buff\" n=\"Buff_Happy\" s=\"200\"><T n=\"buff_name\">0x0000ABCD</T><T n=\"mood_type\">100</T><T n=\"mood_weight\">1</T><T n=\"mystery\">7</T></I>");
        Assert.AreEqual("Buff", analysis.Instance!.Class);
        Assert.AreEqual("buffs.buff", analysis.PythonModule);
        CollectionAssert.AreEqual(new[] { "buff_name", "mood_type", "mood_weight", "mystery" }, analysis.Nodes.Select(node => node.Path).ToArray());
        var name = analysis.Nodes[0];
        Assert.AreEqual(TuningValueKind.LocalizedString, name.ValueKind);
        Assert.AreEqual(0xABCDu, name.StringKey);
        Assert.AreEqual("The name of the buff.", name.Description!.Description);
        Assert.AreEqual(3, name.Line);
        Assert.AreEqual(20, name.ValueStartColumn);
        Assert.AreEqual(30, name.ValueEndColumn);
        var mood = analysis.Nodes[1];
        Assert.AreEqual(TuningValueKind.Reference, mood.ValueKind);
        Assert.AreEqual(ResourceType.MoodTuning, mood.ReferenceType);
        Assert.AreEqual(100UL, mood.ReferenceInstance);
        Assert.AreEqual(TuningValueKind.Scalar, analysis.Nodes[2].ValueKind);
        Assert.IsNull(analysis.Nodes[3].Description);
        CollectionAssert.AreEqual(new[] { "mystery" }, analysis.UnrecognizedPaths.ToArray());
    }

    [TestMethod]
    [DataRow("buff", true, (uint)ResourceType.BuffTuning)]
    [DataRow("object", false, (uint)ResourceType.ObjectDefinition)]
    [DataRow("caspart", false, 0x034AEECBu)]
    public void ReferenceTypeNamesAreMappedToResourceTypes(string typeName, bool isTuning, uint resourceType)
    {
        Assert.AreEqual(isTuning, TuningAnalysis.IsTuningReferenceType(typeName));
        Assert.AreEqual((ResourceType)resourceType, TuningAnalysis.GetReferenceResourceType(typeName));
    }

    [TestMethod]
    public void TuningOfAnUndescribedClassIsUnrecognized()
    {
        using var set = TuningArchiveFixtures.OpenDescriptions();
        var analysis = Analyze(set, "<I c=\"Nonexistent\" i=\"snippet\" m=\"snippets\" n=\"Thing\" s=\"5\"><T n=\"a\">1</T><T n=\"b\">2</T></I>");
        Assert.IsNull(analysis.Instance);
        CollectionAssert.AreEqual(new[] { "a", "b" }, analysis.UnrecognizedPaths.ToArray());
    }
}