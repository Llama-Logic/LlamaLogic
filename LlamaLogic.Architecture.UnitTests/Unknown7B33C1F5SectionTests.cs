namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class Unknown7B33C1F5SectionTests
{
    static byte[] Build(ushort version, ulong[] first, ulong[] second) =>
    [
        .. BitConverter.GetBytes(version),
        .. BitConverter.GetBytes((ushort)first.Length),
        .. first.SelectMany(BitConverter.GetBytes),
        .. BitConverter.GetBytes((ushort)second.Length),
        .. second.SelectMany(BitConverter.GetBytes)
    ];

    [TestMethod]
    public void DecodeAndEncodeEmptyListsRoundTrip()
    {
        var data = Build(2, [], []);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Unknown7B33C1F5, data));
        Assert.AreEqual(0, architecture.Unknown7B33C1F5!.FirstValues.Count);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.Unknown7B33C1F5));
    }

    [TestMethod]
    public void DecodeAndEncodeRoundTrip()
    {
        var data = Build(2, [10, 20, 30], [40]);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Unknown7B33C1F5, data));
        CollectionAssert.AreEqual(new ulong[] { 10, 20, 30 }, architecture.Unknown7B33C1F5!.FirstValues.ToArray());
        CollectionAssert.AreEqual(new ulong[] { 40 }, architecture.Unknown7B33C1F5.SecondValues.ToArray());
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.Unknown7B33C1F5));
    }

    [TestMethod]
    [DataRow((ushort)1)]
    [DataRow((ushort)3)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(ushort version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.Unknown7B33C1F5, Build(version, [], []), architecture => architecture.Unknown7B33C1F5);

    [TestMethod]
    public void DecodeRejectsCountsBeyondTheData() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Unknown7B33C1F5, [2, 0, 5, 0, 1, 2, 3])));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Unknown7B33C1F5, [.. Build(2, [1], []), 0])));

    [TestMethod]
    public void EncodeRejectsMoreValuesThanTheCountHolds()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Unknown7B33C1F5, Build(2, [], [])));
        for (var i = 0; i <= ushort.MaxValue; ++i)
            architecture.Unknown7B33C1F5!.FirstValues.Add((ulong)i);
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(architecture, LotArchitectureSectionType.Unknown7B33C1F5));
    }
}
