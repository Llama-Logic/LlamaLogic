namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class FloorFeaturesSectionTests
{
    const byte sizeX = 2;
    const byte sizeZ = 3;

    static LotArchitecture Decode(byte[] data) =>
        TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(sizeX, sizeZ, 2)), (LotArchitectureSectionType.FloorFeatures, data));

    static void UndecodedWithLotSize(byte[] data) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.FloorFeatures, data, architecture => architecture.FloorFeatures, (LotArchitectureSectionType.LotSize, TestBundles.LotSize(sizeX, sizeZ, 2)));

    static byte[] Layer(byte type, uint layerVersion, int lengthAdjustment, params sbyte[] levels)
    {
        using var payloadStream = new MemoryStream();
        using var payload = new BinaryWriter(payloadStream);
        payload.Write(layerVersion);
        var valueCount = type == 2 ? 1 : sizeX * sizeZ;
        foreach (var level in levels)
        {
            payload.Write(level);
            for (var i = 0; i < valueCount; ++i)
                payload.Write((byte)(level * 32 + i + 64));
        }
        payload.Write(sbyte.MinValue);
        payload.Flush();
        var bytes = payloadStream.ToArray();
        return [type, .. BitConverter.GetBytes(bytes.Length + lengthAdjustment), .. bytes];
    }

    static byte[] Record(uint version, params byte[][] layers) =>
        [.. BitConverter.GetBytes(version), (byte)layers.Length, .. layers.SelectMany(layer => layer)];

    [TestMethod]
    public void DecodeAndEncodeRoundTrip()
    {
        var data = Record(1, Layer(0, 1, 0, -1, 0), Layer(1, 0, 0), Layer(2, 1, 0, 0));
        var architecture = Decode(data);
        var layers = architecture.FloorFeatures!.Layers;
        Assert.AreEqual(3, layers.Count);
        Assert.AreEqual((byte)0, layers[0].Type);
        Assert.AreEqual(2, layers[0].Levels.Count);
        Assert.AreEqual((sbyte)-1, layers[0].Levels[0].Level);
        Assert.AreEqual(sizeX, layers[0].Levels[0].SizeX);
        Assert.AreEqual(sizeZ, layers[0].Levels[0].SizeZ);
        Assert.AreEqual((byte)(0 * 32 + 2 * sizeX + 1 + 64), layers[0].Levels[1][1, 2]);
        Assert.AreEqual(0U, layers[1].Version);
        Assert.AreEqual(0, layers[1].Levels.Count);
        Assert.AreEqual(1, layers[2].Levels[0].SizeX);
        Assert.AreEqual((byte)64, layers[2].Levels[0][0, 0]);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.FloorFeatures));
    }

    [TestMethod]
    public void DecodeKeepsUnsupportedLayerTypesUndecoded() =>
        UndecodedWithLotSize(Record(1, Layer(3, 1, 0)));

    [TestMethod]
    public void DecodeKeepsUnsupportedLayerVersionsUndecoded() =>
        UndecodedWithLotSize(Record(1, Layer(0, 2, 0, 0)));

    [TestMethod]
    [DataRow(2U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        UndecodedWithLotSize(Record(version, Layer(0, 1, 0, 0)));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => Decode([.. Record(1, Layer(0, 1, 0, 0)), 0]));

    [TestMethod]
    public void DecodeRejectsWrongLayerLength() =>
        Assert.ThrowsExactly<InvalidDataException>(() => Decode(Record(1, Layer(0, 1, 1, 0))));

    [TestMethod]
    public void EncodeComputesLayerLength()
    {
        var architecture = Decode(Record(1));
        var layer = new FloorFeatureLayer { Type = 2 };
        layer.Levels.Add(new FloorFeatureLevel(1, 1) { Level = 1, [0, 0] = 9 });
        architecture.FloorFeatures!.Layers.Add(layer);
        CollectionAssert.AreEqual(new byte[] { 1, 0, 0, 0, 1, 2, 7, 0, 0, 0, 1, 0, 0, 0, 1, 9, 0x80 }, TestBundles.Encode(architecture, LotArchitectureSectionType.FloorFeatures));
    }

    [TestMethod]
    public void EncodeRejectsLevelOfWrongSize()
    {
        var architecture = Decode(Record(1));
        var layer = new FloorFeatureLayer();
        layer.Levels.Add(new FloorFeatureLevel(1, 1));
        architecture.FloorFeatures!.Layers.Add(layer);
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void LevelRejectsTheTerminator() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FloorFeatureLevel(sizeX, sizeZ) { Level = sbyte.MinValue });

    [TestMethod]
    public void TypeRejectsValuesAbove2() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FloorFeatureLayer { Type = 3 });
}
