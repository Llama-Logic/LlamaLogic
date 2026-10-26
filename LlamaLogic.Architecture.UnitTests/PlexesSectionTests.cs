namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class PlexesSectionTests
{
    static byte[] Section(uint version, uint visible = 1, int endOffsetError = 0, bool extraBlocks = false)
    {
        var bytes = new TestBytes().U32(version);
        if (version > 2)
            bytes.U8(0);
        bytes.U32(1).U16(300);
        var endOffsetIndex = bytes.Count;
        if (version >= 2)
            bytes.U32(0);
        bytes.U32(visible).U32(1).Block(36, id: 44);
        if (version > 4)
            bytes.U64(0).U64(0);
        if (version > 5)
        {
            if (version == 6)
                bytes.U32(6).Add("Unit 1"u8.ToArray());
            bytes.U32(25).U8(10).U8(0).U8(0);
        }
        var endOffset = bytes.Count + endOffsetError;
        if (version > 7)
        {
            if (extraBlocks)
                bytes.U32(1).Block(36, id: 1).Block(36, id: 2).U32(1).Block(36, id: 3);
            else
                bytes.U32(0);
        }
        if (version > 8)
            bytes.U16(1).U32(0).U16(3).S8(0).U32(0).S8(1).U32(1).S8(2).U32(2);
        var data = bytes.ToArray();
        if (version >= 2)
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(endOffsetIndex), endOffset);
        return data;
    }

    [TestMethod]
    [DataRow(1U)]
    [DataRow(2U)]
    [DataRow(3U)]
    [DataRow(4U)]
    [DataRow(5U)]
    [DataRow(6U)]
    [DataRow(7U)]
    [DataRow(8U)]
    [DataRow(9U)]
    public void DecodeAndEncodeRoundTrip(uint version)
    {
        var data = Section(version);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Plexes, data));
        Assert.AreEqual(version, architecture.Plexes!.Version);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.Plexes));
    }

    [TestMethod]
    public void DecodeAndEncodeRoundTripWithExtraBlocks()
    {
        var data = Section(9, extraBlocks: true);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Plexes, data));
        var section = architecture.Plexes!;
        Assert.AreEqual((ushort)1, section.Unknown2.Single().Id);
        Assert.AreEqual((ushort)2, section.Unknown3!.Id);
        Assert.AreEqual((ushort)3, section.Unknown4.Single().Id);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.Plexes));
    }

    [TestMethod]
    [DataRow(0U)]
    [DataRow(10U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version)
    {
        var data = Section(9);
        BinaryPrimitives.WriteUInt32LittleEndian(data, version);
        TestBundles.AssertUndecoded(LotArchitectureSectionType.Plexes, data, architecture => architecture.Plexes);
    }

    [TestMethod]
    public void DecodeReadsFields()
    {
        var section = TestBundles.Decode((LotArchitectureSectionType.Plexes, Section(9))).Plexes!;
        Assert.IsFalse(section.Unknown1);
        var plex = section.Plexes.Single();
        Assert.AreEqual((ushort)300, plex.Id);
        Assert.IsTrue(plex.IsVisible);
        Assert.AreEqual((ushort)44, plex.Blocks.Single().Id);
        Assert.AreEqual(25U, plex.TileCount);
        Assert.AreEqual((byte)10, plex.Unknown3);
        Assert.IsNull(section.Unknown3);
        var map = section.GroupMaps.Single();
        Assert.AreEqual(0U, map.Unknown1);
        Assert.AreEqual(3, map.Entries.Count);
        Assert.AreEqual((sbyte)2, map.Entries[2].MapKey);
        Assert.AreEqual(2U, map.Entries[2].GroupId);
    }

    [TestMethod]
    public void DecodeReadsVersion6Name() =>
        Assert.AreEqual("Unit 1", TestBundles.Decode((LotArchitectureSectionType.Plexes, Section(6))).Plexes!.Plexes[0].Name);

    [TestMethod]
    public void DecodeRejectsNameThatIsNotAscii()
    {
        var data = Section(6);
        var index = data.AsSpan().IndexOf("Unit 1"u8);
        data[index] = 0xC3;
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Plexes, data)));
    }

    [TestMethod]
    public void DecodeRejectsNonBooleanVisibility() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Plexes, Section(9, visible: 2))));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Plexes, [.. Section(9), 0])));

    [TestMethod]
    public void DecodeRejectsWrongEndOffset() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Plexes, Section(9, endOffsetError: 1))));

    [TestMethod]
    public void EncodeComputesEndOffset()
    {
        var section = new PlexesSection { Version = 4 };
        section.Plexes.Add(new Plex { Id = 1, IsVisible = true });
        section.Plexes.Add(new Plex { Id = 2 });
        var architecture = new LotArchitecture { Plexes = section };
        var expected = new TestBytes().U32(4).U8(0).U32(2).U16(1).U32(23).U32(1).U32(0).U16(2).U32(37).U32(0).U32(0).ToArray();
        CollectionAssert.AreEqual(expected, TestBundles.Encode(architecture, LotArchitectureSectionType.Plexes));
    }

    [TestMethod]
    public void EncodeRejectsExtraBlocksWithoutUnknown3()
    {
        var section = new PlexesSection();
        section.Unknown2.Add(new WallBlock());
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(new LotArchitecture { Plexes = section }, LotArchitectureSectionType.Plexes));
    }

    [TestMethod]
    public void EncodeRejectsFieldsTheVersionCannotStore()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Plexes, Section(9)));
        architecture.Plexes!.Version = 5;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsNameInOtherVersions()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Plexes, Section(6)));
        architecture.Plexes!.Version = 7;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsNameThatIsNotAscii()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Plexes, Section(6)));
        architecture.Plexes!.Plexes[0].Name = "Unité 1";
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsUnknown3WithoutExtraBlocks()
    {
        var section = new PlexesSection { Unknown3 = new WallBlock() };
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(new LotArchitecture { Plexes = section }, LotArchitectureSectionType.Plexes));
    }

    [TestMethod]
    [DataRow(0U)]
    [DataRow(10U)]
    public void SetVersionRejectsUnsupportedVersions(uint version) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PlexesSection { Version = version });
}
