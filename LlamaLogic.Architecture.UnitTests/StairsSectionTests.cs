namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class StairsSectionTests
{
    static readonly ResourceKey stairKey = new((ResourceType)0x9A20CD1C, 0, 0x11);
    static readonly ResourceKey ladderKey = new((ResourceType)0xCC09004D, 0, 0x12);
    static readonly ResourceKey railingKey = new((ResourceType)0x1C1CF1F7, 0, 0x13);
    static readonly ResourceKey wallKey = new((ResourceType)0xD5F0F921, 0, 0x14);

    static byte[] Record(uint version, int stageCount = 2, int legacyBottomLevel = -1)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(version);
        if (version > 8)
        {
            writer.Write(true);
            writer.Write((ushort)1);
        }
        else
            writer.Write((byte)1);
        WriteStair(writer, version, stairKey, stageCount, legacyBottomLevel);
        if (version > 10)
        {
            writer.Write((ushort)1);
            WriteStair(writer, version, ladderKey, stageCount, legacyBottomLevel);
        }
        writer.Flush();
        return stream.ToArray();
    }

    static void WriteKey(BinaryWriter writer, ResourceKey key)
    {
        writer.Write((uint)key.Type);
        writer.Write(key.Group);
        writer.Write(key.FullInstance);
    }

    static void WriteStair(BinaryWriter writer, uint version, ResourceKey key, int stageCount, int legacyBottomLevel)
    {
        if (version < 7)
        {
            writer.Write(1.5F);
            writer.Write(2.5F);
            writer.Write(legacyBottomLevel);
        }
        writer.Write(8.5F);
        writer.Write(9F);
        writer.Write(1);
        if (version > 6)
        {
            writer.Write((sbyte)-1);
            writer.Write(3.25F);
        }
        writer.Write((byte)2);
        if (version > 6)
        {
            if (version > 9)
                writer.Write((ushort)stageCount);
            for (var i = 0; i < stageCount; ++i)
            {
                writer.Write((ushort)(11 + i));
                writer.Write((byte)(2 * i));
            }
        }
        writer.Write((ushort)7);
        writer.Write(0x1234UL);
        WriteKey(writer, key);
        WriteKey(writer, railingKey);
        WriteKey(writer, wallKey);
        if (version > 11)
            WriteKey(writer, railingKey);
        if (version < 8)
        {
            writer.Write(new byte[] { 1, 0, 1, 1, 0 });
            if (version is 5 or 6)
                writer.Write(0.75F);
            if (version > 5)
                writer.Write(new byte[] { 1, 0 });
            return;
        }
        writer.Write((ushort)0x21);
        writer.Write(true);
        if (version > 12)
        {
            if (version < 15)
                writer.Write((byte)9);
            writer.Write(0.5F);
            writer.Write(1F);
            writer.Write(0F);
            writer.Write(0F);
            if (version > 14)
                writer.Write((byte)4);
            for (var i = 0; i < stageCount; ++i)
            {
                writer.Write((ushort)1);
                writer.Write((ushort)10);
                if (version > 14)
                    writer.Write((byte)3);
            }
        }
        if (version > 13)
        {
            writer.Write((ushort)0);
            writer.Write((ushort)21);
        }
    }

    [TestMethod]
    [DataRow(4U)]
    [DataRow(5U)]
    [DataRow(6U)]
    [DataRow(7U)]
    [DataRow(8U)]
    [DataRow(9U)]
    [DataRow(10U)]
    [DataRow(11U)]
    [DataRow(12U)]
    [DataRow(13U)]
    [DataRow(14U)]
    [DataRow(15U)]
    public void DecodeAndEncodeRoundTrip(uint version)
    {
        var data = Record(version, version is > 6 and < 10 ? 1 : 2);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Stairs, data));
        Assert.AreEqual(version, architecture.Stairs!.Version);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.Stairs));
    }

    [TestMethod]
    [DataRow(3U)]
    [DataRow(16U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.Stairs, [.. BitConverter.GetBytes(version), .. Record(15).Skip(4)], architecture => architecture.Stairs);

    [TestMethod]
    public void DecodeReadsCurrentLayout()
    {
        var section = TestBundles.Decode((LotArchitectureSectionType.Stairs, Record(15))).Stairs!;
        Assert.IsTrue(section.Unknown1);
        Assert.AreEqual(ladderKey, section.Ladders.Single().StairsKey);
        var stair = section.Stairs.Single();
        Assert.AreEqual(8.5F, stair.AnchorX);
        Assert.AreEqual(9F, stair.AnchorZ);
        Assert.AreEqual(1, stair.TopLevel);
        Assert.AreEqual((sbyte)-1, stair.BottomLevel);
        Assert.AreEqual(3.25F, stair.TopElevation);
        Assert.AreEqual((byte)2, stair.LaneCount);
        Assert.AreEqual(2, stair.Stages.Count);
        Assert.AreEqual((ushort)12, stair.Stages[1].StepCount);
        Assert.AreEqual((byte)2, stair.Stages[1].Unknown1);
        Assert.AreEqual((ushort)10, stair.Stages[1].Unknown3);
        Assert.AreEqual((byte)3, stair.Stages[1].Unknown4);
        Assert.AreEqual((ushort)7, stair.Id);
        Assert.AreEqual(0x1234UL, stair.ObjectId);
        Assert.AreEqual(stairKey, stair.StairsKey);
        Assert.AreEqual(wallKey, stair.WallKey);
        Assert.AreEqual(railingKey, stair.SecondRailingKey);
        Assert.AreEqual((ushort)0x21, stair.Flags);
        Assert.AreEqual(1F, stair.Unknown5);
        Assert.AreEqual((byte)4, stair.Unknown8);
        Assert.AreEqual((ushort)21, stair.LastStep);
    }

    [TestMethod]
    public void DecodeReadsLegacyLayout()
    {
        var stair = TestBundles.Decode((LotArchitectureSectionType.Stairs, Record(6))).Stairs!.Stairs.Single();
        Assert.AreEqual(1.5F, stair.LegacyX);
        Assert.AreEqual(2.5F, stair.LegacyZ);
        Assert.AreEqual((sbyte)-1, stair.BottomLevel);
        Assert.AreEqual(1, stair.TopLevel);
        Assert.AreEqual(0, stair.Stages.Count);
        Assert.AreEqual((ushort)0x15, stair.Flags);
        Assert.IsTrue(stair.Unknown1);
        Assert.AreEqual(0.75F, stair.Unknown2);
    }

    [TestMethod]
    public void DecodeReadsVersion9StageAsOneStage()
    {
        var stage = TestBundles.Decode((LotArchitectureSectionType.Stairs, Record(9, 1))).Stairs!.Stairs.Single().Stages.Single();
        Assert.AreEqual((ushort)11, stage.StepCount);
        Assert.AreEqual((byte)0, stage.Unknown1);
    }

    [TestMethod]
    public void DecodeRejectsLegacyBottomLevelOutOfRange() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Stairs, Record(5, legacyBottomLevel: 200))));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Stairs, [.. Record(15), 0])));

    [TestMethod]
    [DataRow(5U, (ushort)0x10)]
    [DataRow(6U, (ushort)0x40)]
    public void EncodeRejectsFlagsTheVersionCannotStore(uint version, ushort flags)
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Stairs, Record(version)));
        architecture.Stairs!.Stairs[0].Flags = flags;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsLaddersInVersionsWithoutThem()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Stairs, Record(11)));
        architecture.Stairs!.Version = 10;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsSecondStageInSingleStageVersions()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Stairs, Record(9, 1)));
        architecture.Stairs!.Stairs[0].Stages.Add(new StairStage { StepCount = 4 });
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsStagesInVersionsWithoutThem()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Stairs, Record(7, 1)));
        var section = architecture.Stairs!;
        section.Version = 6;
        section.Stairs[0].TopElevation = 0F;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    [DataRow(12U)]
    [DataRow(14U)]
    public void EncodeRejectsStairFieldsTheVersionCannotStore(uint version)
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Stairs, Record(15)));
        architecture.Stairs!.Version = version;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeWritesAnEarlierVersionWhenTheLaterFieldsHoldDefaults()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Stairs, Record(11)));
        var section = architecture.Stairs!;
        section.Ladders.Clear();
        section.Version = 10;
        CollectionAssert.AreEqual(Record(10), TestBundles.Encode(architecture, LotArchitectureSectionType.Stairs));
    }

    [TestMethod]
    public void StepCountKeepsTheHighBits()
    {
        var stage = new StairStage { StoredStepCount = 0x8401 };
        stage.StepCount = 12;
        Assert.AreEqual((ushort)0x840C, stage.StoredStepCount);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => stage.StepCount = 1024);
    }

    [TestMethod]
    public void VersionRejectsUnsupportedValues() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new StairsSection { Version = 3 });
}
