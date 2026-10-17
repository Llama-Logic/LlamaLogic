namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class WallBlocksSectionTests
{
    public static IEnumerable<object[]> BlockVersions =>
        WallBlockTestData.Versions.Select(version => new object[] { version });

    static byte[] Section(ushort blockVersion, uint version = 2, ushort? mapKey = null, int loopCount = 1, bool hasFloor = true, byte segmentKind = 0)
    {
        var bytes = new TestBytes().U32(version);
        if (version > 1)
            bytes.U32(42);
        return bytes.U16(1).U16(mapKey ?? 7).Block(blockVersion, loopCount: loopCount, hasFloor: hasFloor, segmentKind: segmentKind).ToArray();
    }

    [TestMethod]
    [DynamicData(nameof(BlockVersions))]
    public void DecodeAndEncodeRoundTrip(ushort blockVersion)
    {
        var data = Section(blockVersion);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.WallBlocks, data));
        Assert.AreEqual(blockVersion, architecture.WallBlocks!.Blocks[0].Version);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.WallBlocks));
    }

    [TestMethod]
    [DataRow(0U)]
    [DataRow(1U)]
    public void DecodeAndEncodeRoundTripWithoutStampCounter(uint version)
    {
        var data = Section(36, version);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.WallBlocks, data));
        Assert.AreEqual(0U, architecture.WallBlocks!.StampCounter);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.WallBlocks));
    }

    [TestMethod]
    public void DecodeKeepsSegmentKind()
    {
        var segment = TestBundles.Decode((LotArchitectureSectionType.WallBlocks, Section(36, segmentKind: 3))).WallBlocks!.Blocks[0].Loops[0].Segments[0];
        Assert.AreEqual(WallBlockSegmentKind.ModularPieceWithWall, segment.Kind);
    }

    [TestMethod]
    [DataRow((ushort)16)]
    [DataRow((ushort)37)]
    public void DecodeKeepsUnsupportedBlockVersionsUndecoded(ushort blockVersion)
    {
        var data = Section(36);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(12), blockVersion);
        TestBundles.AssertUndecoded(LotArchitectureSectionType.WallBlocks, data, architecture => architecture.WallBlocks);
    }

    [TestMethod]
    public void DecodeKeepsUnsupportedVersionsUndecoded() =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.WallBlocks, Section(36, 3), architecture => architecture.WallBlocks);

    [TestMethod]
    public void DecodeReadsVersion17Fields()
    {
        var block = TestBundles.Decode((LotArchitectureSectionType.WallBlocks, Section(17))).WallBlocks!.Blocks.Single();
        Assert.AreEqual((byte)1, block.Unknown1);
        Assert.AreEqual(15, block.LegacyKeys.Count);
        Assert.AreEqual((byte)3, block.Unknown9);
        Assert.AreEqual(0, block.Products.Entries.Count);
    }

    [TestMethod]
    public void DecodeReadsVersion22Fields()
    {
        var block = TestBundles.Decode((LotArchitectureSectionType.WallBlocks, Section(22))).WallBlocks!.Blocks.Single();
        Assert.AreEqual(WallBlockType.Standard, block.BlockType);
        Assert.AreEqual((byte)2, block.Unknown5);
        Assert.AreEqual(9, block.LegacyKeys.Count);
        Assert.AreEqual(new ResourceKey((ResourceType)WallBlockTestData.FoundationType, 0, 0x106), block.LegacyKeys[6]);
        Assert.IsTrue(block.Unknown7);
        Assert.IsFalse(block.Unknown8);
        Assert.AreEqual((byte)9, block.Loops.Single().Segments[0].Unknown1);
        Assert.AreEqual(1U, block.Loops.Single().Segments[0].SideAWallPatternId);
        Assert.AreEqual(13U, block.LegacyStamp);
        Assert.AreEqual((ushort)4, block.Unknown10);
        Assert.AreEqual((byte)1, block.Unknown11);
        Assert.AreEqual((byte)1, block.Unknown12);
    }

    [TestMethod]
    public void DecodeReadsVersion36Fields()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.WallBlocks, Section(36, loopCount: 2)));
        var section = architecture.WallBlocks!;
        Assert.AreEqual(42U, section.StampCounter);
        var block = section.Blocks.Single();
        Assert.AreEqual((ushort)7, block.Id);
        Assert.AreEqual((sbyte)1, block.Level);
        Assert.AreEqual(WallBlockType.Pool, block.BlockType);
        Assert.AreEqual((ushort)300, block.PlexId);
        Assert.IsTrue(block.HasFloor);
        Assert.IsTrue(block.Unknown3);
        Assert.AreEqual(3.25f, block.BaseElevation);
        Assert.AreEqual(0.25f, block.FloorHeightOffset);
        Assert.AreEqual(2, block.Loops.Count);
        var segment = block.Loops[1].Segments[1];
        Assert.AreEqual((byte)8, segment.X);
        Assert.AreEqual((byte)5, segment.Z);
        Assert.AreEqual(WallBlockSegmentKind.Wall, segment.Kind);
        Assert.AreEqual(1U, segment.SideAWallPatternId);
        Assert.AreEqual(1U, segment.FoundationProductId);
        Assert.AreEqual(0U, segment.PlatformTrimProductId);
        Assert.AreEqual(11U, segment.FenceStamp);
        Assert.AreEqual(12U, segment.CeilingRailStamp);
        Assert.AreEqual(new ResourceKey((ResourceType)0, 0, 0), block.Products.DefaultKey);
        var entry = block.Products.Entries.Single();
        Assert.AreEqual(new ResourceKey((ResourceType)WallBlockTestData.WallPatternType, 0, 0x1234), entry.Key);
        Assert.AreEqual(1U, entry.Id);
        Assert.AreEqual(2U, entry.ReferenceCount);
        Assert.AreEqual(0, block.LegacyKeys.Count);
    }

    [TestMethod]
    public void DecodeRejectsMismatchedMapKey() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.WallBlocks, Section(36, mapKey: 8))));

    [TestMethod]
    public void DecodeRejectsNonBooleanFlag()
    {
        var data = Section(36);
        data[4 + 4 + 2 + 2 + 2 + 2 + 1 + 1 + 2] = 2;
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.WallBlocks, data)));
    }

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.WallBlocks, [.. Section(36), 0])));

    [TestMethod]
    public void EncodeRejectsLegacyKeyCountMismatch()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.WallBlocks, Section(22)));
        architecture.WallBlocks!.Blocks[0].LegacyKeys.RemoveAt(0);
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(architecture, LotArchitectureSectionType.WallBlocks));
    }

    [TestMethod]
    public void EncodeRejectsLegacyKeysInVersionsWithoutThem()
    {
        var section = new WallBlocksSection();
        var block = new WallBlock { Version = 27, Id = 1 };
        block.LegacyKeys.Add(new ResourceKey((ResourceType)WallBlockTestData.FoundationType, 0, 1));
        section.Blocks.Add(block);
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(new LotArchitecture { WallBlocks = section }, LotArchitectureSectionType.WallBlocks));
    }

    [TestMethod]
    public void EncodeRejectsProductsInVersionsWithoutThem()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.WallBlocks, Section(20)));
        var block = architecture.WallBlocks!.Blocks[0];
        foreach (var segment in block.Loops[0].Segments)
            segment.SideAWallPatternId = 0;
        block.Version = 19;
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(architecture, LotArchitectureSectionType.WallBlocks));
        block.Products.Entries.Clear();
        TestBundles.Encode(architecture, LotArchitectureSectionType.WallBlocks);
    }

    [TestMethod]
    public void EncodeRejectsSecondLoopBeforeVersion24()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.WallBlocks, Section(23)));
        architecture.WallBlocks!.Blocks[0].Loops.Add(new WallBlockLoop());
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(architecture, LotArchitectureSectionType.WallBlocks));
    }

    [TestMethod]
    public void EncodeRejectsSlotsTheVersionCannotStore()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.WallBlocks, Section(36)));
        architecture.WallBlocks!.Blocks[0].Loops[0].Segments[0].PlatformTrimProductId = 1;
        architecture.WallBlocks.Blocks[0].PlexId = 0;
        architecture.WallBlocks.Blocks[0].Version = 34;
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(architecture, LotArchitectureSectionType.WallBlocks));
    }

    [TestMethod]
    public void EncodeRejectsStampCounterInVersionsWithoutIt()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.WallBlocks, Section(36)));
        architecture.WallBlocks!.Version = 1;
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(architecture, LotArchitectureSectionType.WallBlocks));
    }

    [TestMethod]
    public void EncodeWritesALaterBlockVersionWhenTheLegacyFieldsHoldDefaults()
    {
        var section = new WallBlocksSection();
        section.Blocks.Add(new WallBlock { Version = 27, Id = 1 });
        var encoded = TestBundles.Encode(new LotArchitecture { WallBlocks = section }, LotArchitectureSectionType.WallBlocks);
        Assert.AreEqual((ushort)27, TestBundles.Decode((LotArchitectureSectionType.WallBlocks, encoded)).WallBlocks!.Blocks.Single().Version);
    }

    [TestMethod]
    public void EncodeWritesNewSection()
    {
        var block = new WallBlock { Id = 3, Level = -1, BlockType = WallBlockType.Fence, BaseElevation = 1.5f };
        var loop = new WallBlockLoop();
        loop.Segments.Add(new WallBlockSegment { X = 1, Z = 2, Kind = WallBlockSegmentKind.Open, FenceProductId = 1 });
        block.Loops.Add(loop);
        block.Products.Entries.Add(new ProductTableEntry { Key = new ResourceKey((ResourceType)0x0418FE2A, 0, 5), Id = 1, ReferenceCount = 1 });
        var section = new WallBlocksSection { StampCounter = 9 };
        section.Blocks.Add(block);
        var architecture = new LotArchitecture { WallBlocks = section };
        var expected = new TestBytes()
            .U32(2).U32(9).U16(1).U16(3)
            .U16(36).U16(3).S8(-1).U8(1).U16(0).U8(0).U8(0).F32(1.5f).F32(0)
            .U16(1).U16(1).U8(1).U8(2)
            .U8(1).U32(0).U32(0).U32(1).U32(0).U32(0).U32(0).U32(0).U32(0).U32(0).U32(0).U32(0)
            .KeyInstanceTypeGroup(0, 0, 0).U32(1).KeyInstanceTypeGroup(0x0418FE2A, 0, 5).U32(1).U32(1)
            .ToArray();
        CollectionAssert.AreEqual(expected, TestBundles.Encode(architecture, LotArchitectureSectionType.WallBlocks));
    }

    [TestMethod]
    [DataRow((ushort)16)]
    [DataRow((ushort)37)]
    public void SetBlockVersionRejectsUnsupportedVersions(ushort version) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new WallBlock { Version = version });

    [TestMethod]
    public void SetVersionRejectsUnsupportedVersions() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new WallBlocksSection { Version = 3 });
}
