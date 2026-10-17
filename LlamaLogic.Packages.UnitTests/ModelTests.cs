namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class ModelTests :
    TestPackageTests
{
    static readonly ResourceKey relationshipBitTuningKey = new(ResourceType.RelationshipBitTuning, 0, 0x00000000d1d56a53);
    static readonly ResourceKey simDataKey = new(ResourceType.SimData, 0x004dfb84, 0x00000000d1d56a53);
    static readonly ResourceKey stringTableKey = new(ResourceType.StringTable, 0x80000000, 0x002100b7f5cfd646);

    [TestMethod]
    public void DecodeRejectsTypesWithoutAModel() =>
        Assert.ThrowsExactly<NotSupportedException>(() => Model.Decode(ResourceType.SnippetTuning, "<I />"u8.ToArray()));

    [TestMethod]
    public void DecodeUsesTheModelForTheType()
    {
        var model = Model.Decode(ResourceType.CombinedTuning, "<combined><g s=\"merged\" /></combined>"u8.ToArray());
        Assert.IsInstanceOfType<CombinedTuningModel>(model);
    }

    [TestMethod]
    public async Task GetModelAsyncDecodesWithTheModelForTheType()
    {
        await using var dbpf = await DataBasePackedFile.FromPathAsync(tempPackagePath);
        Assert.AreEqual("LlamaLogicPackageTests:NonsenseRelationshipBit", ((SimDataModel)await dbpf.GetModelAsync(simDataKey)).ResourceName);
        Assert.IsInstanceOfType<StringTableModel>(await dbpf.GetModelAsync(stringTableKey));
    }

    [TestMethod]
    public async Task GetModelAsyncRejectsTypesWithoutAModel()
    {
        await using var dbpf = await DataBasePackedFile.FromPathAsync(tempPackagePath);
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => dbpf.GetModelAsync(relationshipBitTuningKey));
    }

    [TestMethod]
    public void GetModelDecodesWithTheModelForTheType()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        Assert.AreEqual("LlamaLogicPackageTests:NonsenseRelationshipBit", ((SimDataModel)dbpf.GetModel(simDataKey)).ResourceName);
        Assert.IsInstanceOfType<StringTableModel>(dbpf.GetModel(stringTableKey));
    }

    [TestMethod]
    public void GetModelRejectsTypesWithoutAModel()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        Assert.ThrowsExactly<ArgumentException>(() => dbpf.GetModel(relationshipBitTuningKey));
    }

    [TestMethod]
    [DataRow(ResourceType.CombinedTuning, typeof(CombinedTuningModel))]
    [DataRow(ResourceType.SimData, typeof(SimDataModel))]
    [DataRow(ResourceType.StringTable, typeof(StringTableModel))]
    [DataRow(ResourceType.WorldData, typeof(WorldDataModel))]
    [DataRow(ResourceType.WorldLotArchitecture, typeof(WorldLotArchitectureModel))]
    [DataRow(ResourceType.WorldObjectData, typeof(WorldObjectDataModel))]
    public void GetModelTypeGivesTheModelForTheType(ResourceType type, Type modelType) =>
        Assert.AreEqual(modelType, Model.GetModelType(type));

    [TestMethod]
    [DataRow(ResourceType.DSTImage)]
    [DataRow(ResourceType.RelationshipBitTuning)]
    [DataRow(ResourceType.SnippetTuning)]
    [DataRow((ResourceType)0x12345678)]
    public void GetModelTypeIsNullForTypesWithoutAModel(ResourceType type) =>
        Assert.IsNull(Model.GetModelType(type));
}
