namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class TerrainPaintSectionTests
{
    const int maskSizeX = 2;
    const int maskSizeZ = 3;

    static byte[] Record(uint version, ulong[][] storedSets, byte hasMask = 1)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(version);
        foreach (var set in storedSets)
        {
            writer.Write(set.Length);
            foreach (var instance in set)
            {
                writer.Write(0xebcbb16cU);
                writer.Write(0U);
                writer.Write(instance);
                writer.Write((uint)instance + 10);
            }
        }
        if (version < 3)
            writer.Write(storedSets.Sum(set => set.Length));
        writer.Write(maskSizeX);
        writer.Write(maskSizeZ);
        foreach (var set in storedSets)
            foreach (var instance in set)
            {
                if (version > 2)
                    writer.Write(hasMask);
                for (var i = 0; i < maskSizeX * maskSizeZ; ++i)
                    writer.Write((byte)(instance * 16 + (ulong)i));
            }
        writer.Flush();
        return stream.ToArray();
    }

    [TestMethod]
    [DataRow(1U)]
    [DataRow(5U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version)
    {
        var data = Record(4, [[], []]);
        BinaryPrimitives.WriteUInt32LittleEndian(data, version);
        TestBundles.AssertUndecoded(LotArchitectureSectionType.TerrainPaint, data, architecture => architecture.TerrainPaint);
    }

    [TestMethod]
    public void DecodeReadsAnyMaskSizeWithoutTheLotsDimensions()
    {
        var data = Record(4, [[1], []]);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, BitConverter.GetBytes(2U)), (LotArchitectureSectionType.TerrainPaint, data));
        Assert.AreEqual(maskSizeX, architecture.TerrainPaint!.MaskSizeX);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.TerrainPaint));
    }

    [TestMethod]
    public void DecodeRejectsMaskSizeThatDoesNotFitTheLot() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(1, 1, 1)), (LotArchitectureSectionType.TerrainPaint, Record(4, [[1], []]))));

    [TestMethod]
    public void DecodeRejectsNonBooleanHasMask() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.TerrainPaint, Record(4, [[1], []], 2))));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.TerrainPaint, [.. Record(4, [[1], []]), 0])));

    [TestMethod]
    public void EncodeRejectsLayerMaskOfAnotherSize()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.TerrainPaint, Record(4, [[1], []])));
        architecture.TerrainPaint!.LayerSet1.Add(new TerrainPaintLayer(maskSizeZ, maskSizeX));
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsMaskSizeThatDoesNotFitTheLot()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.TerrainPaint, Record(4, [[1], []])));
        architecture.LotSize = new LotSizeSection { SizeX = 1, SizeZ = 1, LevelCount = 1 };
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsSet0InVersion2()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.TerrainPaint, Record(2, [[1]])));
        architecture.TerrainPaint!.LayerSet0.Add(new TerrainPaintLayer(maskSizeX, maskSizeZ));
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsUnknown1InLaterVersions()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.TerrainPaint, Record(2, [[1]])));
        architecture.TerrainPaint!.Version = 4;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsVersion2LayerWithoutMask()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.TerrainPaint, Record(4, [[], [1]], 0)));
        architecture.TerrainPaint!.Version = 2;
        architecture.TerrainPaint.Unknown1 = 1;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void HasMaskFalseStillHasMask()
    {
        var data = Record(4, [[1], []], 0);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.TerrainPaint, data));
        Assert.IsFalse(architecture.TerrainPaint!.LayerSet0[0].HasMask);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.TerrainPaint));
    }

    [TestMethod]
    public void LayerConstructorAcceptsTheLargestLot() =>
        Assert.AreEqual(1021, new TerrainPaintLayer(1021, 1021).SizeZ);

    [TestMethod]
    [DataRow(-1, 5)]
    [DataRow(5, 1022)]
    [DataRow(int.MaxValue, int.MaxValue)]
    public void LayerConstructorRejectsSizesNoLotNeeds(int sizeX, int sizeZ) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TerrainPaintLayer(sizeX, sizeZ));

    [TestMethod]
    public void NewLayerEncodes()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(1, 1, 1)));
        var section = new TerrainPaintSection { MaskSizeX = 5, MaskSizeZ = 5 };
        var layer = new TerrainPaintLayer(5, 5) { Key = new ResourceKey((ResourceType)0xebcbb16c, 0, 7), UseCount = 1 };
        layer[4, 3] = 255;
        section.LayerSet1.Add(layer);
        architecture.TerrainPaint = section;
        var encoded = TestBundles.Encode(architecture, LotArchitectureSectionType.TerrainPaint);
        Assert.AreEqual(4 + 4 + 4 + 20 + 8 + 1 + 25, encoded.Length);
        Assert.AreEqual((byte)1, encoded[40]);
        Assert.AreEqual((byte)255, encoded[41 + 4 * 5 + 3]);
    }

    [TestMethod]
    public void Version2LayersGoToSet1()
    {
        var data = Record(2, [[1, 2]]);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.TerrainPaint, data));
        var section = architecture.TerrainPaint!;
        Assert.AreEqual(2U, section.Version);
        Assert.AreEqual(0, section.LayerSet0.Count);
        Assert.AreEqual(2, section.LayerSet1.Count);
        Assert.AreEqual(2U, section.Unknown1);
        Assert.AreEqual(new ResourceKey((ResourceType)0xebcbb16c, 0, 2), section.LayerSet1[1].Key);
        Assert.AreEqual(12U, section.LayerSet1[1].UseCount);
        Assert.AreEqual((byte)(2 * 16 + 1 * maskSizeZ + 2), section.LayerSet1[1][1, 2]);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.TerrainPaint));
    }

    [TestMethod]
    public void Version3StoresSet1First()
    {
        var data = Record(3, [[1], [2, 3]]);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.TerrainPaint, data));
        var section = architecture.TerrainPaint!;
        Assert.AreEqual(1UL, section.LayerSet1.Single().Key.FullInstance);
        Assert.AreEqual(2, section.LayerSet0.Count);
        Assert.AreEqual((byte)(3 * 16), section.LayerSet0[1][0, 0]);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.TerrainPaint));
    }

    [TestMethod]
    public void Version4StoresSet0First()
    {
        var data = Record(4, [[1], [2, 3]]);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.TerrainPaint, data));
        var section = architecture.TerrainPaint!;
        Assert.AreEqual(1UL, section.LayerSet0.Single().Key.FullInstance);
        Assert.AreEqual(2, section.LayerSet1.Count);
        Assert.AreEqual(maskSizeX, section.MaskSizeX);
        Assert.AreEqual(maskSizeZ, section.MaskSizeZ);
        Assert.IsTrue(section.LayerSet1[0].HasMask);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.TerrainPaint));
    }
}
