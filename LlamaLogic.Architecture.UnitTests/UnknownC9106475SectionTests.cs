namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class UnknownC9106475SectionTests
{
    static byte[] Build(uint version, int levelCount = 2)
    {
        var keyCount = version >= 3 ? 6 : 3;
        var data = new List<byte>();
        data.AddRange(BitConverter.GetBytes(version));
        data.AddRange(BitConverter.GetBytes(1));
        data.AddRange(BitConverter.GetBytes(0xABCDUL));
        data.AddRange(BitConverter.GetBytes(levelCount));
        for (var level = 0; level < levelCount; ++level)
        {
            data.AddRange(BitConverter.GetBytes(level - 1));
            for (var k = 0; k < keyCount; ++k)
                data.AddRange(Key((ulong)(level * 10 + k + 1), 0x1000U + (uint)k, (uint)level));
        }
        return [.. data];
    }

    static byte[] Key(ulong instance, uint type, uint group) =>
    [
        .. BitConverter.GetBytes(instance),
        .. BitConverter.GetBytes(type),
        .. BitConverter.GetBytes(group)
    ];

    [TestMethod]
    public void DecodeAndEncodeEmptyRoundTrip()
    {
        byte[] data = [.. BitConverter.GetBytes(3U), .. BitConverter.GetBytes(0U)];
        var architecture = TestBundles.Decode((LotArchitectureSectionType.UnknownC9106475, data));
        Assert.AreEqual(3U, architecture.UnknownC9106475!.Version);
        Assert.AreEqual(0, architecture.UnknownC9106475.Entries.Count);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.UnknownC9106475));
    }

    [TestMethod]
    [DataRow(1U, 3)]
    [DataRow(2U, 3)]
    [DataRow(3U, 6)]
    public void DecodeAndEncodeEntriesRoundTrip(uint version, int keyCount)
    {
        var data = Build(version);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.UnknownC9106475, data));
        var entry = architecture.UnknownC9106475!.Entries.Single();
        Assert.AreEqual(0xABCDUL, entry.Id);
        Assert.AreEqual(2, entry.Levels.Count);
        Assert.AreEqual((sbyte)-1, entry.Levels[0].Level);
        Assert.AreEqual((sbyte)0, entry.Levels[1].Level);
        Assert.AreEqual(keyCount, entry.Levels[1].Keys.Count);
        Assert.AreEqual(new ResourceKey((ResourceType)0x1001U, 1, 12), entry.Levels[1].Keys[1]);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.UnknownC9106475));
    }

    [TestMethod]
    [DataRow(0U)]
    [DataRow(4U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.UnknownC9106475, [.. BitConverter.GetBytes(version), .. BitConverter.GetBytes(0U)], architecture => architecture.UnknownC9106475);

    [TestMethod]
    public void DecodeRejectsLevelsThatDoNotFitInASignedByte()
    {
        var data = Build(3, 1);
        BitConverter.GetBytes(300).CopyTo(data, 20);
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.UnknownC9106475, data)));
    }

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.UnknownC9106475, [.. Build(3), 0])));

    [TestMethod]
    public void DecodeRejectsTruncatedEntries() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.UnknownC9106475, Build(3)[..^5])));

    [TestMethod]
    public void EncodeRejectsLevelsWithTooFewKeys()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.UnknownC9106475, Build(2)));
        architecture.UnknownC9106475!.Version = 3;
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(architecture, LotArchitectureSectionType.UnknownC9106475));
    }

    [TestMethod]
    public void EncodeRejectsLevelsWithTooManyKeys()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.UnknownC9106475, Build(3)));
        architecture.UnknownC9106475!.Version = 2;
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(architecture, LotArchitectureSectionType.UnknownC9106475));
    }
}
