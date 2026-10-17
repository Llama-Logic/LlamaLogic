namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class ObjectFallbacksSectionTests
{
    static byte[] Build(uint version, byte first, params ulong[][] categories) =>
    [
        .. BitConverter.GetBytes(version),
        first,
        .. categories.SelectMany(category => BitConverter.GetBytes((uint)category.Length).Concat(category.SelectMany(BitConverter.GetBytes)))
    ];

    [TestMethod]
    public void DecodeAndEncodeRoundTrip()
    {
        var data = Build(2, 4, [], [0x0A2C1737UL, 0x0A2C1738UL], [5UL], []);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.ObjectFallbacks, data));
        var section = architecture.ObjectFallbacks!;
        Assert.AreEqual((byte)4, section.Unknown1);
        Assert.AreEqual(4, section.Categories.Count);
        Assert.AreEqual(0, section.Categories[0].Count);
        CollectionAssert.AreEqual(new ulong[] { 0x0A2C1737UL, 0x0A2C1738UL }, section.Categories[1].ToArray());
        CollectionAssert.AreEqual(new ulong[] { 5UL }, section.Categories[2].ToArray());
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.ObjectFallbacks));
    }

    [TestMethod]
    public void DecodeKeepsAnyFirstByte()
    {
        var data = Build(2, 9, [], [], [], []);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.ObjectFallbacks, data));
        Assert.AreEqual((byte)9, architecture.ObjectFallbacks!.Unknown1);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.ObjectFallbacks));
    }

    [TestMethod]
    [DataRow(0U)]
    [DataRow(1U)]
    [DataRow(3U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.ObjectFallbacks, Build(version, 4, [], [], [], []), architecture => architecture.ObjectFallbacks);

    [TestMethod]
    public void DecodeRejectsCountsBeyondTheData() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.ObjectFallbacks, [.. BitConverter.GetBytes(2U), 4, 9, 0, 0, 0])));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.ObjectFallbacks, [.. Build(2, 4, [], [], [], []), 0])));
}
