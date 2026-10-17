namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class ObjectTileBoundsSectionTests
{
    static byte[] Record(uint version)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(version);
        writer.Write(2U);
        writer.Write(0x9000000000000002UL);
        if (version > 1)
            writer.Write(new byte[] { 3, 5, 7, 9 });
        writer.Write((sbyte)-1);
        writer.Write(0x9000000000000001UL);
        if (version > 1)
            writer.Write(new byte[] { 0, 1, 0, 1 });
        writer.Write((sbyte)2);
        writer.Flush();
        return stream.ToArray();
    }

    [TestMethod]
    [DataRow(1U)]
    [DataRow(2U)]
    public void DecodeAndEncodeRoundTrip(uint version)
    {
        var data = Record(version);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.ObjectTileBounds, data));
        Assert.AreEqual(version, architecture.ObjectTileBounds!.Version);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.ObjectTileBounds));
    }

    [TestMethod]
    [DataRow(0U)]
    [DataRow(3U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.ObjectTileBounds, [.. BitConverter.GetBytes(version), .. Record(2).Skip(4)], architecture => architecture.ObjectTileBounds);

    [TestMethod]
    public void DecodeReadsEntriesInStoredOrder()
    {
        var entries = TestBundles.Decode((LotArchitectureSectionType.ObjectTileBounds, Record(2))).ObjectTileBounds!.Entries;
        Assert.AreEqual(2, entries.Count);
        Assert.AreEqual(0x9000000000000002UL, entries[0].ObjectId);
        Assert.AreEqual((byte)3, entries[0].MinimumZ);
        Assert.AreEqual((byte)5, entries[0].MaximumZ);
        Assert.AreEqual((byte)7, entries[0].MinimumX);
        Assert.AreEqual((byte)9, entries[0].MaximumX);
        Assert.AreEqual((sbyte)-1, entries[0].Level);
        Assert.AreEqual((sbyte)2, entries[1].Level);
    }

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.ObjectTileBounds, [.. Record(2), 0])));

    [TestMethod]
    public void DecodeRejectsTruncatedEntries() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.ObjectTileBounds, Record(2)[..^1])));

    [TestMethod]
    public void EncodeRejectsRectanglesInVersion1()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.ObjectTileBounds, Record(2)));
        architecture.ObjectTileBounds!.Version = 1;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeWritesVersion1WithoutRectangles()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.ObjectTileBounds, Record(2)));
        foreach (var entry in architecture.ObjectTileBounds!.Entries)
        {
            entry.MinimumX = 0;
            entry.MaximumX = 0;
            entry.MinimumZ = 0;
            entry.MaximumZ = 0;
        }
        architecture.ObjectTileBounds.Version = 1;
        CollectionAssert.AreEqual(Record(1), TestBundles.Encode(architecture, LotArchitectureSectionType.ObjectTileBounds));
    }

    [TestMethod]
    public void VersionRejectsUnsupportedValues() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ObjectTileBoundsSection { Version = 3 });
}
