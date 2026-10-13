namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class CombinedTuningModelTests
{
    const string maxisXml = "<combined><g s=\"merged\"><L x=\"0\"><T>Dolej&#353;&#237;</T><T>Ann's</T></L><U x=\"1\"><T n=\"a\">1</T><r x=\"0\" n=\"names\" /></U><T x=\"2\" p=\"It&apos;s &amp; more\">5</T></g><R n=\"buff\"><I c=\"Buff\" i=\"buff\" m=\"buffs.buff\" n=\"Buff_A\" s=\"12345\"><r x=\"1\" n=\"data\" /><T n=\"value\">x</T><E n=\"empty\" /><B /></I></R><R n=\"tun\"><M n=\"some.module\" s=\"67890\"><r x=\"2\" n=\"tunable\" /><r x=\"2\" /></M></R></combined>";
    static readonly ResourceKey combinedTuningKey = new(ResourceType.CombinedTuning, 0, 0xf1352e740c4710ba);

    static void AssertDocumentIsMaxisXml(CombinedTuningModel model)
    {
        Assert.IsTrue(XNode.DeepEquals(XElement.Parse(maxisXml), model.Document.Root), model.Document.ToString(SaveOptions.DisableFormatting));
        Assert.AreEqual("Dolejší", model.Document.Root!.Element("g")!.Element("L")!.Elements("T").First().Value);
    }

    [TestMethod]
    public void DecodeReadsBinaryCombinedTuning() =>
        AssertDocumentIsMaxisXml(CombinedTuningModel.Decode(PackedXmlBuilder.Build(XElement.Parse(maxisXml))));

    [TestMethod]
    public void DecodeReadsXmlCombinedTuning() =>
        AssertDocumentIsMaxisXml(CombinedTuningModel.Decode(Encoding.ASCII.GetBytes(maxisXml)));

    [TestMethod]
    public void DecodeRejectsBinaryNodeContainingItself() =>
        Assert.ThrowsExactly<InvalidDataException>(() => CombinedTuningModel.Decode(PackedXmlBuilder.Build(XElement.Parse(maxisXml), rootContainsItself: true)));

    [TestMethod]
    [DataRow("<I n=\"not_combined\" s=\"1\" />")]
    [DataRow("not XML at all")]
    public void DecodeRejectsOtherData(string data) =>
        Assert.ThrowsExactly<InvalidDataException>(() => CombinedTuningModel.Decode(Encoding.UTF8.GetBytes(data)));

    [TestMethod]
    [DataRow(8)]
    [DataRow(64)]
    [DataRow(256)]
    [DataRow(1024)]
    [DataRow(-11)]
    public void DecodeRejectsTruncatedBinary(int length)
    {
        var data = PackedXmlBuilder.Build(XElement.Parse(maxisXml));
        Assert.ThrowsExactly<InvalidDataException>(() => CombinedTuningModel.Decode(data.AsMemory(0, length < 0 ? data.Length + length : length)));
    }

    [TestMethod]
    public void EncodeWritesBinaryCombinedTuningAsMaxisXml() =>
        CollectionAssert.AreEqual(Encoding.ASCII.GetBytes(maxisXml), CombinedTuningModel.Decode(PackedXmlBuilder.Build(XElement.Parse(maxisXml))).Encode().ToArray());

    [TestMethod]
    public void EncodeWritesXmlCombinedTuningUnchanged() =>
        CollectionAssert.AreEqual(Encoding.ASCII.GetBytes(maxisXml), CombinedTuningModel.Decode(Encoding.ASCII.GetBytes(maxisXml)).Encode().ToArray());

    [TestMethod]
    public async Task GetCombinedTuningAsyncReadsResource()
    {
        await using var dbpf = new DataBasePackedFile();
        await dbpf.SetAsync(combinedTuningKey, PackedXmlBuilder.Build(XElement.Parse(maxisXml)));
        AssertDocumentIsMaxisXml(await dbpf.GetCombinedTuningAsync(combinedTuningKey));
    }

    [TestMethod]
    public void GetCombinedTuningReadsResource()
    {
        using var dbpf = new DataBasePackedFile();
        dbpf.Set(combinedTuningKey, PackedXmlBuilder.Build(XElement.Parse(maxisXml)));
        AssertDocumentIsMaxisXml(dbpf.GetCombinedTuning(combinedTuningKey));
    }

    [TestMethod]
    [DataRow("<combined><g s=\"merged\" /><R n=\"buff\"><I n=\"a\" s=\"1\"><r x=\"9\" /></I></R></combined>")]
    [DataRow("<combined><g s=\"merged\"><L x=\"0\"><r x=\"0\" /></L></g><R n=\"buff\"><I n=\"a\" s=\"1\"><r x=\"0\" /></I></R></combined>")]
    [DataRow("<combined><R n=\"buff\"><I n=\"a\" s=\"1\" /></R></combined>")]
    [DataRow("<combined><g s=\"merged\" /><R n=\"buff\"><I n=\"a\" /></R></combined>")]
    public void GetTuningsRejectsMalformedDocuments(string xml)
    {
        var model = CombinedTuningModel.Decode(Encoding.ASCII.GetBytes(xml));
        Assert.ThrowsExactly<InvalidDataException>(() => model.GetTunings().ToList());
    }

    [TestMethod]
    public void GetTuningsResolvesSharedNodes()
    {
        var model = CombinedTuningModel.Decode(PackedXmlBuilder.Build(XElement.Parse(maxisXml)));
        var tunings = model.GetTunings().ToList();
        Assert.AreEqual(2, tunings.Count);
        Assert.AreEqual("buff", tunings[0].TypeName);
        Assert.AreEqual(12345UL, tunings[0].Instance);
        Assert.AreEqual("Buff_A", tunings[0].Name);
        Assert.IsTrue(XNode.DeepEquals(XElement.Parse("<I c=\"Buff\" i=\"buff\" m=\"buffs.buff\" n=\"Buff_A\" s=\"12345\"><U n=\"data\"><T n=\"a\">1</T><L n=\"names\"><T>Dolej&#353;&#237;</T><T>Ann's</T></L></U><T n=\"value\">x</T><E n=\"empty\" /><B /></I>"), tunings[0].Document.Root), tunings[0].Document.ToString(SaveOptions.DisableFormatting));
        Assert.AreEqual("tun", tunings[1].TypeName);
        Assert.AreEqual(67890UL, tunings[1].Instance);
        Assert.AreEqual("some.module", tunings[1].Name);
        Assert.IsTrue(XNode.DeepEquals(XElement.Parse("<M n=\"some.module\" s=\"67890\"><T n=\"tunable\" p=\"It&apos;s &amp; more\">5</T><T p=\"It&apos;s &amp; more\">5</T></M>"), tunings[1].Document.Root), tunings[1].Document.ToString(SaveOptions.DisableFormatting));
        Assert.IsTrue(XNode.DeepEquals(XElement.Parse(maxisXml), model.Document.Root));
    }
}
