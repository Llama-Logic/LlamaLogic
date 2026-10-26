namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class AnimationStateMachineModelTests
{
    const string maxisXml = "<ASM name=\"llamalogic_Test\" type=\"Jazz\"><Actor name=\"x\" type=\"Sim\" /><Parameter name=\"caf&#233;\" type=\"enum\" labels=\"It&apos;s &amp; more\" /><State name=\"entry\" type=\"entry\"><Transitions><Transition to=\"exit\" /></Transitions></State><State name=\"exit\" type=\"exit\" /></ASM>";
    static readonly ResourceKey animationStateMachineKey = new(ResourceType.AnimationStateMachine, 0, 0x8b12a7c35f0e4d61);

    static void AssertDocumentIsMaxisXml(AnimationStateMachineModel model)
    {
        Assert.IsTrue(XNode.DeepEquals(XElement.Parse(maxisXml), model.Document.Root), model.Document.ToString(SaveOptions.DisableFormatting));
        Assert.AreEqual("café", model.Document.Root!.Element("Parameter")!.Attribute("name")!.Value);
    }

    [TestMethod]
    public void DecodeReadsBinaryStateMachine() =>
        AssertDocumentIsMaxisXml(AnimationStateMachineModel.Decode(PackedXmlBuilder.Build(XElement.Parse(maxisXml))));

    [TestMethod]
    public void DecodeReadsXmlStateMachine() =>
        AssertDocumentIsMaxisXml(AnimationStateMachineModel.Decode(Encoding.ASCII.GetBytes(maxisXml)));

    [TestMethod]
    public void DecodeRejectsBinaryNodeContainingItself() =>
        Assert.ThrowsExactly<InvalidDataException>(() => AnimationStateMachineModel.Decode(PackedXmlBuilder.Build(XElement.Parse(maxisXml), rootContainsItself: true)));

    [TestMethod]
    [DataRow("not XML at all")]
    [DataRow("<ASM name=\"unclosed\">")]
    [DataRow("")]
    public void DecodeRejectsOtherData(string data) =>
        Assert.ThrowsExactly<InvalidDataException>(() => AnimationStateMachineModel.Decode(Encoding.UTF8.GetBytes(data)));

    [TestMethod]
    [DataRow(8)]
    [DataRow(64)]
    [DataRow(256)]
    [DataRow(-11)]
    public void DecodeRejectsTruncatedBinary(int length)
    {
        var data = PackedXmlBuilder.Build(XElement.Parse(maxisXml));
        Assert.ThrowsExactly<InvalidDataException>(() => AnimationStateMachineModel.Decode(data.AsMemory(0, length < 0 ? data.Length + length : length)));
    }

    [TestMethod]
    public void EncodeWritesBinaryStateMachineAsMaxisXml() =>
        CollectionAssert.AreEqual(Encoding.ASCII.GetBytes(maxisXml), AnimationStateMachineModel.Decode(PackedXmlBuilder.Build(XElement.Parse(maxisXml))).Encode().ToArray());

    [TestMethod]
    public void EncodeWritesXmlStateMachineUnchanged() =>
        CollectionAssert.AreEqual(Encoding.ASCII.GetBytes(maxisXml), AnimationStateMachineModel.Decode(Encoding.ASCII.GetBytes(maxisXml)).Encode().ToArray());

    [TestMethod]
    public async Task GetAnimationStateMachineAsyncReadsResource()
    {
        await using var dbpf = new DataBasePackedFile();
        await dbpf.SetAsync(animationStateMachineKey, PackedXmlBuilder.Build(XElement.Parse(maxisXml)));
        AssertDocumentIsMaxisXml(await dbpf.GetAnimationStateMachineAsync(animationStateMachineKey));
    }

    [TestMethod]
    public void GetAnimationStateMachineReadsResource()
    {
        using var dbpf = new DataBasePackedFile();
        dbpf.Set(animationStateMachineKey, PackedXmlBuilder.Build(XElement.Parse(maxisXml)));
        AssertDocumentIsMaxisXml(dbpf.GetAnimationStateMachine(animationStateMachineKey));
    }

    [TestMethod]
    public async Task GetNameAsyncReadsTheRootsName()
    {
        using var stream = new MemoryStream(PackedXmlBuilder.Build(XElement.Parse(maxisXml)));
        Assert.AreEqual("llamalogic_Test", await AnimationStateMachineModel.GetNameAsync(stream));
    }

    [TestMethod]
    public void GetNameIsNullForOtherData()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not XML at all"));
        Assert.IsNull(AnimationStateMachineModel.GetName(stream));
    }

    [TestMethod]
    public void GetNameReadsTheRootsName()
    {
        using var stream = new MemoryStream(PackedXmlBuilder.Build(XElement.Parse(maxisXml)));
        Assert.AreEqual("llamalogic_Test", AnimationStateMachineModel.GetName(stream));
    }

    [TestMethod]
    public void ResourceNameIsNullWithoutANameAttribute() =>
        Assert.IsNull(AnimationStateMachineModel.Decode("<ASM type=\"Jazz\" />"u8.ToArray()).ResourceName);

    [TestMethod]
    public void ResourceNameIsTheRootsName() =>
        Assert.AreEqual("llamalogic_Test", AnimationStateMachineModel.Decode(Encoding.ASCII.GetBytes(maxisXml)).ResourceName);
}
