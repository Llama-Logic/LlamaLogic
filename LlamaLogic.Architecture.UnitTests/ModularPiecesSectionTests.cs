namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class ModularPiecesSectionTests
{
    static byte[] Record(uint version)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(version);
        writer.Write(1);
        writer.Write(0x01_1c_0a_00U);
        foreach (var (type, instance) in new (uint type, ulong instance)[] { (0xa0451cbd, 1), (0xb4f762c9, 2), (0x0418fe2a, 3), (0x3f0c529a, 4), (0, 0) })
        {
            writer.Write(type);
            writer.Write(0U);
            writer.Write(instance);
        }
        if (version > 4)
            for (var i = 0; i < 2; ++i)
            {
                writer.Write(0xd5f0f921U);
                writer.Write(0U);
                writer.Write(5UL + (ulong)i);
            }
        writer.Write((byte)6);
        if (version > 5)
            writer.Write((ushort)0x1700);
        writer.Flush();
        return stream.ToArray();
    }

    [TestMethod]
    [DataRow(4U)]
    [DataRow(5U)]
    [DataRow(6U)]
    public void DecodeAndEncodeRoundTrip(uint version)
    {
        var data = Record(version);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.ModularPieces, data));
        var piece = architecture.ModularPieces!.Pieces.Single();
        Assert.AreEqual(version, architecture.ModularPieces.Version);
        Assert.AreEqual(0x01_1c_0a_00U, piece.Position);
        Assert.AreEqual((sbyte)10, piece.X);
        Assert.AreEqual((sbyte)28, piece.Z);
        Assert.AreEqual((sbyte)1, piece.Level);
        Assert.AreEqual(new ResourceKey((ResourceType)0xa0451cbd, 0, 1), piece.ModularPieceKey);
        Assert.AreEqual(2UL, piece.FloorPatternKey.FullInstance);
        Assert.AreEqual(3UL, piece.FenceKey.FullInstance);
        Assert.AreEqual(4UL, piece.CeilingRailKey.FullInstance);
        Assert.AreEqual(ResourceKey.Empty, piece.Unknown1);
        Assert.AreEqual(version > 4 ? 5UL : 0UL, piece.FirstWallPatternKey.FullInstance);
        Assert.AreEqual(version > 4 ? 6UL : 0UL, piece.SecondWallPatternKey.FullInstance);
        Assert.AreEqual((byte)6, piece.Unknown2);
        Assert.AreEqual(version > 5 ? (ushort)0x1700 : (ushort)0, piece.Unknown3);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.ModularPieces));
    }

    [TestMethod]
    [DataRow(3U)]
    [DataRow(7U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.ModularPieces, Record(version), architecture => architecture.ModularPieces);

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.ModularPieces, [.. Record(6), 0])));

    [TestMethod]
    [DataRow(4U)]
    [DataRow(5U)]
    public void EncodeRejectsFieldsTheVersionCannotStore(uint version)
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.ModularPieces, Record(6)));
        architecture.ModularPieces!.Version = version;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeWritesVersion4WhenTheLaterFieldsHoldDefaults()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.ModularPieces, Record(6)));
        var piece = architecture.ModularPieces!.Pieces[0];
        piece.FirstWallPatternKey = default;
        piece.SecondWallPatternKey = default;
        piece.Unknown3 = 0;
        architecture.ModularPieces.Version = 4;
        CollectionAssert.AreEqual(Record(4), TestBundles.Encode(architecture, LotArchitectureSectionType.ModularPieces));
    }

    [TestMethod]
    public void PositionBytesCanBeSetSeparately()
    {
        var piece = new ModularPiece { X = 3, Z = -2, Level = -1 };
        Assert.AreEqual(0xff_fe_03_00U, piece.Position);
    }

    [TestMethod]
    public void VersionRejectsUnsupportedValues() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ModularPiecesSection { Version = 7 });
}
