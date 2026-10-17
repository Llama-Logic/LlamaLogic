namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class LotObjectsSectionTests
{
    static byte[] Build(uint version, params byte[] payload) =>
    [
        .. BitConverter.GetBytes(version),
        .. BitConverter.GetBytes(payload.Length),
        .. payload
    ];

    [TestMethod]
    public void DecodeAndEncodeEmptyMessageRoundTrip()
    {
        var data = Build(1);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotObjects, data));
        Assert.AreEqual(0, architecture.LotObjects!.Objects.LotObjects.Count);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.LotObjects));
    }

    [TestMethod]
    public void DecodeAndEncodeRoundTrip()
    {
        var data = Build(1, 0x08, 0x07);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotObjects, data));
        Assert.AreEqual(7U, architecture.LotObjects!.Objects.LotId);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.LotObjects));
    }

    [TestMethod]
    [DataRow(0U)]
    [DataRow(2U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.LotObjects, Build(version), architecture => architecture.LotObjects);

    [TestMethod]
    public void DecodeRejectsLengthBeyondTheData() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.LotObjects, [.. BitConverter.GetBytes(1U), .. BitConverter.GetBytes(10), 0x08])));

    [TestMethod]
    public void DecodeRejectsMalformedMessage() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.LotObjects, Build(1, 0x08))));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.LotObjects, [.. Build(1, 0x08, 0x07), 0])));

    [TestMethod]
    public void ObjectsRejectsNull() =>
        Assert.ThrowsExactly<ArgumentNullException>(() => new LotObjectsSection { Objects = null! });
}
