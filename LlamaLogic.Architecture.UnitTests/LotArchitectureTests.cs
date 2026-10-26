namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class LotArchitectureTests
{
    static readonly ResourceKey floorKey = new((ResourceType)0xB4F762C9, 0, 0x21);
    static readonly ResourceKey paintKey = new((ResourceType)0xEBCBB16C, 0, 0x22);
    static readonly ResourceKey patternKey = new((ResourceType)0xF1EDBD86, 0, 0x23);
    static readonly ResourceKey roofKey = new((ResourceType)0x91EDBD3E, 0, 0x24);
    static readonly ResourceKey stairKey = new((ResourceType)0x9A20CD1C, 0, 0x25);
    static readonly ResourceKey styleKey = new((ResourceType)0x9F5CFF10, 0, 0x26);
    static readonly ResourceKey trimKey = new((ResourceType)0xB0311D0F, 0, 0x27);

    static ResourceKey Key(LotArchitectureSectionType type) =>
        new((ResourceType)(uint)type, 0, LotArchitecture.SectionInstance);

    [TestMethod]
    public void DecodeAndEncodeKeepReservedValues()
    {
        var bundle = new RecordBundle { Reserved1 = 5, Reserved2 = 6 };
        bundle.Set(Key(LotArchitectureSectionType.Revision), BitConverter.GetBytes(3UL));
        var architecture = LotArchitecture.Decode(bundle.Encode());
        Assert.AreEqual(5U, architecture.Reserved1);
        Assert.AreEqual(6U, architecture.Reserved2);
        architecture.Reserved1 = 7;
        var encoded = RecordBundle.Decode(architecture.Encode());
        Assert.AreEqual(7U, encoded.Reserved1);
        Assert.AreEqual(6U, encoded.Reserved2);
    }

    [TestMethod]
    public void DecodeKeepsGridsUndecodedWhenTheLotSizeHasNoDimensions()
    {
        var floors = new byte[2 * 2 * 1 * 8];
        floors[0] = 1;
        TestBundles.AssertUndecoded(LotArchitectureSectionType.Floors, floors, architecture => architecture.Floors, (LotArchitectureSectionType.LotSize, BitConverter.GetBytes(2U)));
    }

    [TestMethod]
    [DataRow(1U, LotArchitecture.SectionInstance)]
    [DataRow(0U, 2UL)]
    public void DecodeKeepsRecordsWithAnotherGroupOrInstanceUndecoded(uint group, ulong instance)
    {
        var key = new ResourceKey((ResourceType)(uint)LotArchitectureSectionType.Revision, group, instance);
        var bundle = new RecordBundle();
        bundle.Set(key, BitConverter.GetBytes(3UL));
        var architecture = LotArchitecture.Decode(bundle.Encode());
        Assert.IsNull(architecture.Revision);
        Assert.AreEqual(key, architecture.UndecodedRecords.Single().Key);
        CollectionAssert.AreEqual(BitConverter.GetBytes(3UL), RecordBundle.Decode(architecture.Encode()).Get(key).ToArray());
    }

    [TestMethod]
    public void DecodeKeepsUnknownTypesUndecoded()
    {
        var architecture = TestBundles.Decode(((LotArchitectureSectionType)0x12345678, [1, 2, 3]), (LotArchitectureSectionType.Revision, BitConverter.GetBytes(3UL)));
        Assert.AreEqual(3UL, architecture.Revision!.Value);
        Assert.AreEqual(1, architecture.Sections.Count);
        var record = architecture.UndecodedRecords.Single();
        Assert.AreEqual((ResourceType)0x12345678, record.Key.Type);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, RecordBundle.Decode(architecture.Encode()).Get(record.Key).ToArray());
    }

    [TestMethod]
    public void DecodeKeepsUnsupportedVersionsUndecodedAndDecodesTheRest()
    {
        byte[] weather = [.. BitConverter.GetBytes(2U), .. BitConverter.GetBytes(0.5F)];
        TestBundles.AssertUndecoded(LotArchitectureSectionType.Weather, weather, architecture => architecture.Weather, (LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 2, 1)));
        var decoded = TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 2, 1)), (LotArchitectureSectionType.Weather, weather));
        Assert.AreEqual((byte)2, decoded.LotSize!.SizeX);
    }

    [TestMethod]
    public void EncodeRejectsASectionAndAnUndecodedRecordWithTheSameKey()
    {
        var architecture = new LotArchitecture { Revision = new RevisionSection { Value = 1 } };
        architecture.UndecodedRecords.Add(new RecordBundleRecord(Key(LotArchitectureSectionType.Revision), BitConverter.GetBytes(2UL)));
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.Encode());
    }

    [TestMethod]
    public void EncodeRejectsUndecodedRecordsWithTheSameKey()
    {
        var architecture = new LotArchitecture();
        var key = new ResourceKey((ResourceType)0x12345678, 0, 1);
        architecture.UndecodedRecords.Add(new RecordBundleRecord(key, new byte[] { 1 }));
        architecture.UndecodedRecords.Add(new RecordBundleRecord(key, new byte[] { 2 }));
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.Encode());
    }

    [TestMethod]
    public void GetManifestKeysCollectsKeysInTheGamesSaveOrder()
    {
        var floorKeys = new FloorKeysSection();
        floorKeys.Entries.Add(new FloorKeyEntry { Id = 1, Key = floorKey, ReferenceCount = 4 });
        var roofs = new RoofsSection();
        roofs.Roofs.Add(new Roof { Id = 1, RoofKey = roofKey, FirstTrimKey = trimKey, SecondTrimKey = trimKey, PatternKey = patternKey });
        var stairs = new StairsSection();
        var stair = new Stair { Id = 1, StairsKey = stairKey, SecondRailingKey = styleKey };
        stair.Stages.Add(new StairStage { StepCount = 12 });
        stairs.Stairs.Add(stair);
        var terrainPaint = new TerrainPaintSection();
        terrainPaint.LayerSet0.Add(new TerrainPaintLayer(0, 0) { Key = paintKey });
        var architecture = new LotArchitecture
        {
            Stairs = stairs,
            TerrainPaint = terrainPaint,
            Roofs = roofs,
            FloorKeys = floorKeys,
            LotInfo = new LotInfoSection { StyleKey = styleKey },
            Revision = new RevisionSection { Value = 1 }
        };
        var expected = new[] { styleKey, default, floorKey, roofKey, trimKey, patternKey, stairKey };
        CollectionAssert.AreEqual(expected, architecture.GetManifestKeys().ToArray());
    }

    [TestMethod]
    public void GetManifestKeysRejectsSectionsThatCannotBeEncoded()
    {
        var architecture = new LotArchitecture { LotSize = new LotSizeSection { Version = 2, SizeX = 1 }, Revision = new RevisionSection() };
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.GetManifestKeys());
    }

    [TestMethod]
    public void PropertySetterRejectsASectionOfAnotherType() =>
        Assert.ThrowsExactly<ArgumentException>(() => new LotArchitecture { Ceilings = new FloorIdGridSection(LotArchitectureSectionType.Floors, 1, 1, 1) });

    [TestMethod]
    public void SectionsAreInAscendingOrderOfType()
    {
        var architecture = new LotArchitecture
        {
            Stairs = new StairsSection(),
            Revision = new RevisionSection(),
            LotSize = new LotSizeSection(),
            ObjectFallbacks = new ObjectFallbacksSection(),
            ModularPieces = new ModularPiecesSection()
        };
        var types = architecture.Sections.Select(section => (uint)section.Type).ToArray();
        CollectionAssert.AreEqual(types.OrderBy(type => type).ToArray(), types);
        Assert.AreEqual(LotArchitectureSectionType.ObjectFallbacks, architecture.Sections[0].Type);
        Assert.AreEqual(LotArchitectureSectionType.ModularPieces, architecture.Sections[^1].Type);
    }
}
