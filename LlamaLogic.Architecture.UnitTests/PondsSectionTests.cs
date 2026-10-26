namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class PondsSectionTests
{
    static byte[] Record(uint version, ushort[] values)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(version);
        if (version > 1)
        {
            writer.Write((ushort)1);
            writer.Write((ushort)11);
            writer.Write(-2);
            writer.Write(0xb4f762c9U);
            writer.Write(0U);
            writer.Write(0xe7fcUL);
            writer.Write((ushort)2);
            writer.Write(new byte[] { 3, 4, 15, 0xff });
            if (version > 2)
                writer.Write((byte)3);
            writer.Write(new byte[] { 5, 6, 2, 0 });
            if (version > 2)
                writer.Write((byte)7);
            if (version > 2)
            {
                writer.Write((ushort)values.Length);
                foreach (var value in values)
                    writer.Write(value);
            }
        }
        writer.Flush();
        return stream.ToArray();
    }

    [TestMethod]
    public void DecodeKeepsUnsupportedVersionsUndecoded() =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.Ponds, Record(4, []), architecture => architecture.Ponds);

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Ponds, [.. Record(3, []), 0])));

    [TestMethod]
    public void EncodeRejectsPondsInVersion1()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Ponds, Record(3, [])));
        architecture.Ponds!.Version = 1;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsVersion3FieldsInVersion2()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Ponds, Record(3, [])));
        architecture.Ponds!.Version = 2;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeWritesVersion2WhenTheVersion3FieldsHoldDefaults()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Ponds, Record(3, [])));
        architecture.Ponds!.Ponds[0].Cells[1].Unknown1 = 3;
        architecture.Ponds.Version = 2;
        CollectionAssert.AreEqual(Record(2, []), TestBundles.Encode(architecture, LotArchitectureSectionType.Ponds));
    }

    [TestMethod]
    public void Version2RoundTrip()
    {
        var data = Record(2, []);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Ponds, data));
        var pond = architecture.Ponds!.Ponds.Single();
        Assert.AreEqual((byte)3, pond.Cells[1].Unknown1);
        Assert.AreEqual((byte)2, pond.Cells[1].QuarterMask);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.Ponds));
    }

    [TestMethod]
    public void Version3RoundTrip()
    {
        var data = Record(3, [9, 10]);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Ponds, data));
        var pond = architecture.Ponds!.Ponds.Single();
        Assert.AreEqual((ushort)11, pond.Id);
        Assert.AreEqual(-2, pond.WaterLevel);
        Assert.AreEqual(new ResourceKey((ResourceType)0xb4f762c9, 0, 0xe7fc), pond.FloorPatternKey);
        Assert.AreEqual(2, pond.Cells.Count);
        Assert.AreEqual((byte)3, pond.Cells[0].X);
        Assert.AreEqual((byte)4, pond.Cells[0].Z);
        Assert.AreEqual((byte)15, pond.Cells[0].QuarterMask);
        Assert.AreEqual((sbyte)-1, pond.Cells[0].Level);
        Assert.AreEqual((byte)7, pond.Cells[1].Unknown1);
        CollectionAssert.AreEqual(new ushort[] { 9, 10 }, pond.Unknown2.ToArray());
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.Ponds));
    }

    [TestMethod]
    public void VersionRejectsValuesAbove3() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PondsSection { Version = 4 });

    [TestMethod]
    [DataRow(0U)]
    [DataRow(1U)]
    public void VersionsBefore2HoldNoPonds(uint version)
    {
        var data = Record(version, []);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Ponds, data));
        Assert.AreEqual(0, architecture.Ponds!.Ponds.Count);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.Ponds));
    }
}
