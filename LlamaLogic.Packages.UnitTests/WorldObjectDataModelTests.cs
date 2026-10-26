namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class WorldObjectDataModelTests
{
    static readonly ResourceKey footprintKey = new(ResourceType.Footprint, 0x00000001, 0x0000000000000002);
    static readonly ResourceKey lotFootprintKey = new(ResourceType.Footprint, 0, 0x0000000000000003);

    static void WriteKey(BinaryWriter writer, ResourceKey key)
    {
        writer.Write((uint)key.Type);
        writer.Write(key.Group);
        writer.Write(key.FullInstance);
    }

    static void WriteLocator(BinaryWriter writer, ulong objectDefinitionId)
    {
        WriteKey(writer, footprintKey);
        writer.Write(1F);
        writer.Write(2F);
        writer.Write(3F);
        writer.Write(0F);
        writer.Write(0.70710677F);
        writer.Write(0F);
        writer.Write(0.70710677F);
        writer.Write(1.5F);
        writer.Write(objectDefinitionId);
    }

    static void WriteObject(BinaryWriter writer, uint version, ulong objectId, byte snow = 1)
    {
        WriteLocator(writer, 0x0000000000003039);
        writer.Write(objectId);
        writer.Write((byte)255);
        writer.Write(0x0000000000000063UL);
        writer.Write(0x12345678U);
        if (version >= 8)
            writer.Write(snow);
        if (version >= 10)
            writer.Write(1U);
    }

    static byte[] BuildWorldObjectData(uint version, byte snow = 1)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(version);
        writer.Write(1U);
        writer.Write(7U);
        WriteKey(writer, lotFootprintKey);
        writer.Write(2U);
        WriteObject(writer, version, 0x0000000000000065, snow);
        WriteObject(writer, version, 0x0000000000000066, snow);
        writer.Write(1U);
        WriteObject(writer, version, 0x0000000000000067, snow);
        writer.Write(1U);
        WriteLocator(writer, 0x0000000000000068);
        if (version >= 9)
        {
            writer.Write(1U);
            WriteLocator(writer, 0x0000000000000069);
            writer.Write(2U);
        }
        writer.Flush();
        return stream.ToArray();
    }

    [TestMethod]
    [DataRow(7U)]
    [DataRow(8U)]
    [DataRow(9U)]
    [DataRow(10U)]
    public void DecodeAndEncodeRoundTripEachVersion(uint version)
    {
        var data = BuildWorldObjectData(version);
        var model = WorldObjectDataModel.Decode(data);
        Assert.AreEqual(version, model.Version);
        Assert.IsTrue(data.AsSpan().SequenceEqual(model.Encode().Span));
    }

    [TestMethod]
    public void DecodeReadsEveryField()
    {
        var model = WorldObjectDataModel.Decode(BuildWorldObjectData(10));
        Assert.AreEqual(1, model.Lots.Count);
        var lot = model.Lots[0];
        Assert.AreEqual(7U, lot.LotId);
        Assert.AreEqual(lotFootprintKey, lot.FootprintKey);
        Assert.AreEqual(2, lot.Objects.Count);
        Assert.AreEqual(1, lot.FootprintObjects.Count);
        Assert.AreEqual(1, lot.SimLocators.Count);
        Assert.AreEqual(1, lot.StairLocators.Count);
        var @object = lot.Objects[1];
        Assert.AreEqual(footprintKey, @object.FootprintKey);
        Assert.AreEqual(new Vector3(1F, 2F, 3F), @object.Position);
        Assert.AreEqual(new Quaternion(0F, 0.70710677F, 0F, 0.70710677F), @object.Orientation);
        Assert.AreEqual(1.5F, @object.Scale);
        Assert.AreEqual(0x0000000000003039UL, @object.ObjectDefinitionId);
        Assert.AreEqual(0x0000000000000066UL, @object.ObjectId);
        Assert.AreEqual(byte.MaxValue, @object.LevelOfDetail);
        Assert.AreEqual(0x0000000000000063UL, @object.ParentObjectId);
        Assert.AreEqual(0x12345678U, @object.ParentSlotHash);
        Assert.IsTrue(@object.Snow);
        Assert.AreEqual(1U, @object.Flags);
        Assert.AreEqual(0x0000000000000067UL, lot.FootprintObjects[0].ObjectId);
        Assert.AreEqual(0x0000000000000068UL, lot.SimLocators[0].ObjectDefinitionId);
        Assert.AreEqual(0x0000000000000069UL, lot.StairLocators[0].ObjectDefinitionId);
        Assert.AreEqual(2U, lot.StairLocators[0].Width);
    }

    [TestMethod]
    public void DecodeRejectsInvalidSnowFlags() =>
        Assert.ThrowsExactly<InvalidDataException>(() => WorldObjectDataModel.Decode(BuildWorldObjectData(10, 2)));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => WorldObjectDataModel.Decode((byte[])[.. BuildWorldObjectData(10), 0]));

    [TestMethod]
    public void DecodeRejectsTruncatedData() =>
        Assert.ThrowsExactly<InvalidDataException>(() => WorldObjectDataModel.Decode(BuildWorldObjectData(10)[..^1]));

    [TestMethod]
    [DataRow(6U)]
    [DataRow(11U)]
    public void DecodeRejectsUnsupportedVersions(uint version)
    {
        var data = BuildWorldObjectData(10);
        BinaryPrimitives.WriteUInt32LittleEndian(data, version);
        Assert.ThrowsExactly<InvalidDataException>(() => WorldObjectDataModel.Decode(data));
    }

    [TestMethod]
    public void EncodeRejectsStairLocatorsBeforeVersion9()
    {
        var model = WorldObjectDataModel.Decode(BuildWorldObjectData(10));
        model.Version = 8;
        Assert.ThrowsExactly<InvalidOperationException>(() => model.Encode());
    }

    [TestMethod]
    public void EncodeWritesAnEarlierVersionWithoutItsLaterFields()
    {
        var model = WorldObjectDataModel.Decode(BuildWorldObjectData(10));
        model.Version = 9;
        Assert.IsTrue(BuildWorldObjectData(9).AsSpan().SequenceEqual(model.Encode().Span));
    }

    [TestMethod]
    public void NewModelsUseTheLatestVersion() =>
        Assert.AreEqual(10U, new WorldObjectDataModel().Version);

    [TestMethod]
    public void TheGetterOfDataBasePackedFileDecodesTheModel()
    {
        var key = new ResourceKey(ResourceType.WorldObjectData, 0, 1);
        using var dbpf = new DataBasePackedFile();
        dbpf.Set(key, BuildWorldObjectData(10), CompressionMode.ForceOff);
        Assert.AreEqual(7U, dbpf.GetWorldObjectData(key).Lots[0].LotId);
    }

    [TestMethod]
    [DataRow(6U)]
    [DataRow(11U)]
    public void VersionRejectsUnsupportedVersions(uint version) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new WorldObjectDataModel().Version = version);
}
