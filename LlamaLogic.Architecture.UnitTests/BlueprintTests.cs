namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class BlueprintTests
{
    static byte[] BuildArchitecture() =>
        TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(20, 15, 1))).Encode().ToArray();

    static byte[] BuildContent(byte[] architecture, byte[]? lotObjects = null)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(0xfea1baadU);
        writer.Write(2);
        writer.Write((uint)(lotObjects?.Length ?? 0));
        writer.Write(lotObjects ?? []);
        writer.Write(1U);
        writer.Write(0);
        writer.Write(0);
        writer.Write(architecture);
        writer.Flush();
        return stream.ToArray();
    }

    static byte[] BuildContentWithoutHeader(byte[] architecture)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(0U);
        writer.Write(architecture);
        writer.Flush();
        return stream.ToArray();
    }

    static byte[] BuildFile(byte[] compressed)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(1);
        writer.Write((uint)compressed.Length);
        writer.Write(compressed);
        writer.Flush();
        return stream.ToArray();
    }

    static byte[] BuildLiteralSnappy(byte[] content)
    {
        var result = new List<byte>();
        var length = (uint)content.Length;
        while (length >= 0x80)
        {
            result.Add((byte)(length | 0x80));
            length >>= 7;
        }
        result.Add((byte)length);
        result.Add(61 << 2);
        result.Add((byte)(content.Length - 1));
        result.Add((byte)((content.Length - 1) >> 8));
        result.AddRange(content);
        return [.. result];
    }

    [TestMethod]
    public void DecodeReadsABlueprintWithoutAHeader()
    {
        var file = BuildFile(BuildLiteralSnappy(BuildContentWithoutHeader(BuildArchitecture())));
        var blueprint = Blueprint.Decode(file);
        Assert.IsFalse(blueprint.HasHeader);
        Assert.AreEqual(0, blueprint.LotObjects.LotObjects.Count);
        Assert.AreEqual((byte)20, blueprint.Architecture.LotSize!.SizeX);
        CollectionAssert.AreEqual(file, blueprint.Encode().ToArray());
    }

    [TestMethod]
    public void DecodeReadsALiteralSnappyBlueprint()
    {
        var file = BuildFile(BuildLiteralSnappy(BuildContent(BuildArchitecture())));
        var blueprint = Blueprint.Decode(file);
        Assert.AreEqual(1, blueprint.Version);
        Assert.IsTrue(blueprint.HasHeader);
        Assert.AreEqual(2, blueprint.ContentVersion);
        Assert.AreEqual(1U, blueprint.Unknown1);
        Assert.AreEqual((byte)15, blueprint.Architecture.LotSize!.SizeZ);
        CollectionAssert.AreEqual(file, blueprint.Encode().ToArray());
    }

    [TestMethod]
    public void DecodeRejectsACorruptedObjectMessage() =>
        Assert.ThrowsExactly<InvalidDataException>(() => Blueprint.Decode(BuildFile(BuildLiteralSnappy(BuildContent(BuildArchitecture(), [0x0f, 0x01, 0x02])))));

    [TestMethod]
    public void DecodeRejectsAnUncompressedLengthTheDataCannotHold() =>
        Assert.ThrowsExactly<InvalidDataException>(() => Blueprint.Decode(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0xC0, 0xFF, 0xFF, 0xFF, 0x07 }));

    [TestMethod]
    public void DecodeRejectsCopiesBeforeTheStartOfTheOutput() =>
        Assert.ThrowsExactly<InvalidDataException>(() => Blueprint.Decode(BuildFile([4, 0b000_000_01, 1])));

    [TestMethod]
    public void DecodeRejectsMismatchedCompressedLengths()
    {
        var file = BuildFile(BuildLiteralSnappy(BuildContent(BuildArchitecture())));
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), (uint)file.Length);
        Assert.ThrowsExactly<InvalidDataException>(() => Blueprint.Decode(file));
    }

    [TestMethod]
    public void DecodeRejectsSnappyDataShorterThanItClaims()
    {
        var compressed = BuildLiteralSnappy(BuildContent(BuildArchitecture()));
        compressed[0] = (byte)(compressed[0] + 1);
        Assert.ThrowsExactly<InvalidDataException>(() => Blueprint.Decode(BuildFile(compressed)));
    }

    [TestMethod]
    public void EncodeCompressesRepetitiveContent()
    {
        var blueprint = new Blueprint { Architecture = LotArchitecture.Decode(BuildArchitecture()) };
        for (var i = 0; i < 500; ++i)
            blueprint.ObjectDefinitionMappings.Add(new(1234, 5678));
        var data = blueprint.Encode();
        Assert.IsTrue(data.Length < 500 * 16 / 4);
        Assert.AreEqual(500, Blueprint.Decode(data).ObjectDefinitionMappings.Count);
    }

    [TestMethod]
    public void EncodeRejectsAMissingArchitecture() =>
        Assert.ThrowsExactly<InvalidOperationException>(() => new Blueprint { Architecture = null! }.Encode());

    [TestMethod]
    public void EncodeRejectsImagesWithoutContentVersion2()
    {
        var blueprint = new Blueprint
        {
            Architecture = LotArchitecture.Decode(BuildArchitecture()),
            ContentVersion = 1
        };
        blueprint.Images.Add(new(1, new byte[] { 0xff, 0xd8 }));
        Assert.ThrowsExactly<InvalidOperationException>(() => blueprint.Encode());
    }

    [TestMethod]
    public void EncodeRejectsMissingObjects() =>
        Assert.ThrowsExactly<InvalidOperationException>(() => new Blueprint { LotObjects = null! }.Encode());

    [TestMethod]
    public void EncodeThenDecodeKeepsEveryPart()
    {
        var blueprint = new Blueprint { Architecture = LotArchitecture.Decode(BuildArchitecture()) };
        blueprint.LotObjects.LotObjects.Add(new LotObject { Id = 7, ObjectDefGuid64 = 1234 });
        blueprint.ObjectDefinitionMappings.Add(new(1234, 5678));
        blueprint.Images.Add(new(0x1000000000000001, new byte[] { 0xff, 0xd8, 0xff, 0xe0 }));
        var data = blueprint.Encode();
        var decoded = Blueprint.Decode(data);
        Assert.AreEqual(7UL, decoded.LotObjects.LotObjects.Single().Id);
        Assert.AreEqual(5678UL, decoded.ObjectDefinitionMappings.Single().To);
        Assert.AreEqual(0x1000000000000001UL, decoded.Images.Single().Id);
        CollectionAssert.AreEqual(new byte[] { 0xff, 0xd8, 0xff, 0xe0 }, decoded.Images.Single().Data.ToArray());
        Assert.AreEqual((byte)20, decoded.Architecture.LotSize!.SizeX);
        CollectionAssert.AreEqual(data.ToArray(), decoded.Encode().ToArray());
    }
}
