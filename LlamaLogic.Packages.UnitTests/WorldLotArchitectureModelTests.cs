namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class WorldLotArchitectureModelTests
{
    static readonly ResourceKey recordKey = new((ResourceType)0x00000001, 0, 0x2222222222222222);

    static WorldLotArchitectureModel BuildModel()
    {
        var model = new WorldLotArchitectureModel();
        model.Bundle.Set(recordKey, "section"u8.ToArray());
        return model;
    }

    [TestMethod]
    public void DecodeReadsWhatEncodeWrites()
    {
        var data = BuildModel().Encode();
        var model = WorldLotArchitectureModel.Decode(data);
        Assert.AreEqual("section", Encoding.ASCII.GetString(model.Bundle.Get(recordKey).Span));
        Assert.IsTrue(data.Span.SequenceEqual(model.Encode().Span));
    }

    [TestMethod]
    public void DecodeRejectsMismatchedLengths()
    {
        var data = BuildModel().Encode().ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(data, BinaryPrimitives.ReadUInt32LittleEndian(data) - 1);
        Assert.ThrowsExactly<InvalidDataException>(() => WorldLotArchitectureModel.Decode(data));
    }

    [TestMethod]
    public void DecodeRejectsTooFewBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => WorldLotArchitectureModel.Decode(new byte[3]));

    [TestMethod]
    public void EncodePrefixesTheBundleWithItsLength()
    {
        var model = BuildModel();
        var data = model.Encode();
        var bundle = model.Bundle.Encode();
        Assert.AreEqual((uint)bundle.Length, BinaryPrimitives.ReadUInt32LittleEndian(data.Span));
        Assert.IsTrue(data.Span[4..].SequenceEqual(bundle.Span));
    }

    [TestMethod]
    [DataRow(0x0000abcdU, 0xf3427520489192dfUL)]
    [DataRow(0x0001e240U, 0xb1d5781949f91e43UL)]
    public void GetKeyHashesTheLotTemplateId(uint lotTemplateId, ulong instance)
    {
        Assert.AreEqual(new ResourceKey(ResourceType.WorldLotArchitecture, 0, instance), WorldLotArchitectureModel.GetKey(lotTemplateId));
        Assert.AreEqual(instance, WorldLotArchitectureModel.GetLotTemplateInstance(lotTemplateId));
    }

    [TestMethod]
    [DataRow(ResourceType.WorldData, "dworld")]
    [DataRow(ResourceType.WorldLotArchitecture, "dat")]
    [DataRow(ResourceType.WorldLotObjects, "lottemp")]
    public void ResourceTypesAreHashesOfTheirExtensions(ResourceType type, string extension) =>
        Assert.AreEqual((uint)type, Fnv32.GetHash(extension));

    [TestMethod]
    public void TheGetterOfDataBasePackedFileDecodesTheModel()
    {
        var key = WorldLotArchitectureModel.GetKey(0x0000abcd);
        using var dbpf = new DataBasePackedFile();
        dbpf.Set(key, BuildModel());
        Assert.AreEqual("section", Encoding.ASCII.GetString(dbpf.GetWorldLotArchitecture(key).Bundle.Get(recordKey).Span));
    }

    [TestMethod]
    public async Task TheGetterOfDataBasePackedFileDecodesTheModelAsynchronously()
    {
        var key = WorldLotArchitectureModel.GetKey(0x0000abcd);
        await using var dbpf = new DataBasePackedFile();
        await dbpf.SetAsync(key, BuildModel());
        Assert.AreEqual("section", Encoding.ASCII.GetString((await dbpf.GetWorldLotArchitectureAsync(key)).Bundle.Get(recordKey).Span));
    }
}
