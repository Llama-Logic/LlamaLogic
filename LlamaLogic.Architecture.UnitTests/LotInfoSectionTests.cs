namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class LotInfoSectionTests
{
    static readonly ResourceKey styleKey = new((ResourceType)0x9F5CFF10, 0, 0x55);

    static byte[] Record(uint version, sbyte lowestLevel = -1, sbyte highestLevel = 1)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(version);
        if (version > 12)
            writer.Write(1U);
        writer.Write(lowestLevel);
        writer.Write(highestLevel);
        for (var level = lowestLevel; level <= highestLevel; ++level)
            writer.Write(level < 1 ? 3F : 4F);
        if (version > 5)
            writer.Write(0.25F);
        if (version > 15)
            writer.Write(0.5F);
        if (version > 16)
            writer.Write(true);
        if (version > 17)
            writer.Write(false);
        if (version > 6)
        {
            writer.Write((uint)styleKey.Type);
            writer.Write(styleKey.Group);
            writer.Write(styleKey.FullInstance);
        }
        if (version > 10)
        {
            writer.Write(0xABCDEFUL);
            writer.Write(10UL);
            writer.Write(20UL);
            writer.Write(16);
            writer.Write(Enumerable.Range(0x40, 16).Select(i => (byte)i).ToArray());
            WriteString(writer, "creator");
            WriteString(writer, "modifier");
            if (version > 18)
            {
                writer.Write(100U);
                WriteString(writer, "creator");
                writer.Write(10UL);
                writer.Write(0U);
                WriteString(writer, string.Empty);
                writer.Write(20UL);
            }
        }
        if (version is > 11 and < 15)
        {
            writer.Write(960U);
            writer.Write(true);
        }
        else if (version > 14)
        {
            writer.Write(2U);
            writer.Write(0U);
            writer.Write(960U);
            writer.Write(true);
            writer.Write(1U);
            writer.Write(0U);
            writer.Write(false);
        }
        writer.Flush();
        return stream.ToArray();
    }

    static void WriteString(BinaryWriter writer, string value)
    {
        writer.Write(value.Length);
        writer.Write(Encoding.Unicode.GetBytes(value));
    }

    [TestMethod]
    public void DecodeAllowsAnEmptyLevelRange()
    {
        var data = Record(20, 0, -1);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotInfo, data));
        Assert.AreEqual(0, architecture.LotInfo!.LevelHeights.Count);
        Assert.AreEqual(-1, architecture.LotInfo.HighestLevel);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.LotInfo));
    }

    [TestMethod]
    [DataRow(5U)]
    [DataRow(6U)]
    [DataRow(7U)]
    [DataRow(10U)]
    [DataRow(11U)]
    [DataRow(12U)]
    [DataRow(13U)]
    [DataRow(15U)]
    [DataRow(16U)]
    [DataRow(17U)]
    [DataRow(18U)]
    [DataRow(19U)]
    [DataRow(20U)]
    public void DecodeAndEncodeRoundTrip(uint version)
    {
        var data = Record(version);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotInfo, data));
        Assert.AreEqual(version, architecture.LotInfo!.Version);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.LotInfo));
    }

    [TestMethod]
    [DataRow(4U)]
    [DataRow(21U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.LotInfo, [.. BitConverter.GetBytes(version), .. Record(20).Skip(4)], architecture => architecture.LotInfo);

    [TestMethod]
    public void DecodeReadsCurrentLayout()
    {
        var section = TestBundles.Decode((LotArchitectureSectionType.LotInfo, Record(20))).LotInfo!;
        Assert.AreEqual(1U, section.Unknown1);
        Assert.AreEqual((sbyte)-1, section.LowestLevel);
        Assert.AreEqual(1, section.HighestLevel);
        CollectionAssert.AreEqual(new[] { 3F, 3F, 4F }, section.LevelHeights.ToArray());
        Assert.AreEqual(0.25F, section.LevelElevationOffset);
        Assert.AreEqual(0.5F, section.NegativeLevelElevationOffset);
        Assert.IsTrue(section.Unknown2);
        Assert.IsFalse(section.Unknown3);
        Assert.AreEqual(styleKey, section.StyleKey);
        Assert.AreEqual(0xABCDEFUL, section.BlueprintInstance);
        Assert.AreEqual(16, section.Uuid.Length);
        Assert.AreEqual(10UL, section.FirstUser.Id);
        Assert.AreEqual("creator", section.FirstUser.Name);
        Assert.AreEqual(100U, section.FirstUser.Unknown1);
        Assert.AreEqual("creator", section.FirstUser.Unknown2);
        Assert.AreEqual(10UL, section.FirstUser.RepeatedId);
        Assert.AreEqual("modifier", section.SecondUser.Name);
        Assert.AreEqual(20UL, section.SecondUser.RepeatedId);
        Assert.AreEqual(2, section.PlexValues.Count);
        Assert.AreEqual(960U, section.PlexValues[0].Value);
        Assert.IsTrue(section.PlexValues[0].HasValue);
        Assert.AreEqual(1U, section.PlexValues[1].UnitId);
        Assert.IsFalse(section.PlexValues[1].HasValue);
    }

    [TestMethod]
    public void DecodeReadsSingleEntryPlexMap()
    {
        var section = TestBundles.Decode((LotArchitectureSectionType.LotInfo, Record(13))).LotInfo!;
        var plexValue = section.PlexValues.Single();
        Assert.AreEqual(0U, plexValue.UnitId);
        Assert.AreEqual(960U, plexValue.Value);
        Assert.AreEqual("modifier", section.SecondUser.Name);
    }

    [TestMethod]
    public void DecodeRejectsInvertedLevelRange() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.LotInfo, [.. BitConverter.GetBytes(20U), .. BitConverter.GetBytes(1U), 1, unchecked((byte)-1)])));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.LotInfo, [.. Record(20), 0])));

    [TestMethod]
    public void EncodeRejectsFieldsTheVersionCannotStore()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotInfo, Record(20)));
        architecture.LotInfo!.Version = 5;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsLevelHeightsThatDoNotMatchTheLot()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 2, 2)), (LotArchitectureSectionType.LotInfo, Record(20)));
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsPlexValuesInVersionsWithoutThem()
    {
        var section = new LotInfoSection { Version = 11 };
        section.PlexValues.Add(new LotInfoPlexValue { Value = 960, HasValue = true });
        Assert.ThrowsExactly<InvalidOperationException>(() => new LotArchitecture { LotInfo = section }.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsSecondPlexValueInSingleEntryVersions()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotInfo, Record(13)));
        architecture.LotInfo!.PlexValues.Add(new LotInfoPlexValue { UnitId = 1 });
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsTooManyLevels()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotInfo, Record(20)));
        for (var i = 0; i < 126; ++i)
            architecture.LotInfo!.LevelHeights.Add(3F);
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsUnitIdInSingleEntryVersions()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotInfo, Record(13)));
        architecture.LotInfo!.PlexValues[0].UnitId = 1;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeWritesLevelHeightsThatMatchTheLot()
    {
        var data = Record(20);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(2, 2, 3)), (LotArchitectureSectionType.LotInfo, data));
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.LotInfo));
    }

    [TestMethod]
    public void EncodeWritesTheEarliestVersionWithDefaults()
    {
        var section = new LotInfoSection { Version = 5 };
        section.LevelHeights.Add(3F);
        var expected = new TestBytes().U32(5).S8(0).S8(0).F32(3F).ToArray();
        CollectionAssert.AreEqual(expected, TestBundles.Encode(new LotArchitecture { LotInfo = section }, LotArchitectureSectionType.LotInfo));
    }

    [TestMethod]
    public void VersionRejectsUnsupportedValues() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new LotInfoSection { Version = 21 });
}
