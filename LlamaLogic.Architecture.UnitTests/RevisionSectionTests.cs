namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class RevisionSectionTests
{
    [TestMethod]
    public void DecodeAndEncodeRoundTrip()
    {
        var data = BitConverter.GetBytes(587252UL);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Revision, data));
        Assert.AreEqual(587252UL, architecture.Revision!.Value);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.Revision));
    }

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Revision, [.. BitConverter.GetBytes(1UL), 0])));

    [TestMethod]
    public void DecodeRejectsTruncatedData() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Revision, [1, 0, 0, 0])));
}
