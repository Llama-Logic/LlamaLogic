namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class PendingPlacementSectionTests
{
    static byte[] Record(uint version, byte state = 1)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(version);
        writer.Write((byte)20);
        writer.Write((byte)30);
        if (version > 2)
            writer.Write((byte)3);
        writer.Write(state);
        if (version > 1)
        {
            writer.Write(true);
            writer.Write(0x77UL);
            if (version > 3)
                writer.Write(true);
            if (version > 4)
                writer.Write(false);
        }
        writer.Flush();
        return stream.ToArray();
    }

    [TestMethod]
    [DataRow(1U)]
    [DataRow(2U)]
    [DataRow(3U)]
    [DataRow(4U)]
    [DataRow(5U)]
    public void DecodeAndEncodeRoundTrip(uint version)
    {
        var data = Record(version);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.PendingPlacement, data));
        Assert.AreEqual(version, architecture.PendingPlacement!.Version);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.PendingPlacement));
    }

    [TestMethod]
    [DataRow(0U)]
    [DataRow(6U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.PendingPlacement, [.. BitConverter.GetBytes(version), .. Record(5).Skip(4)], architecture => architecture.PendingPlacement);

    [TestMethod]
    public void DecodeReadsCurrentLayout()
    {
        var section = TestBundles.Decode((LotArchitectureSectionType.PendingPlacement, Record(5, 2))).PendingPlacement!;
        Assert.AreEqual((byte)20, section.Width);
        Assert.AreEqual((byte)30, section.Depth);
        Assert.AreEqual((byte)3, section.Rotation);
        Assert.AreEqual((byte)2, section.State);
        Assert.IsTrue(section.Unknown1);
        Assert.AreEqual(0x77UL, section.Unknown2);
        Assert.IsTrue(section.Unknown3);
        Assert.IsFalse(section.Unknown4);
    }

    [TestMethod]
    public void DecodeRejectsNonBooleanVersion1State() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.PendingPlacement, Record(1, 2))));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.PendingPlacement, [.. Record(5), 0])));

    [TestMethod]
    [DataRow(1U)]
    [DataRow(2U)]
    [DataRow(3U)]
    public void EncodeRejectsFieldsTheVersionCannotStore(uint version)
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.PendingPlacement, Record(5)));
        architecture.PendingPlacement!.Version = version;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsNonBooleanVersion1State()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.PendingPlacement, Record(1)));
        architecture.PendingPlacement!.State = 2;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeWritesVersion1WhenTheLaterFieldsHoldDefaults()
    {
        var section = new PendingPlacementSection { Version = 1, Width = 2, Depth = 3, State = 1 };
        CollectionAssert.AreEqual(new byte[] { 1, 0, 0, 0, 2, 3, 1 }, TestBundles.Encode(new LotArchitecture { PendingPlacement = section }, LotArchitectureSectionType.PendingPlacement));
    }

    [TestMethod]
    public void VersionRejectsUnsupportedValues() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PendingPlacementSection { Version = 6 });
}
