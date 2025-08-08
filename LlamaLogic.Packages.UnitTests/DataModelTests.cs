namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class DataModelTests :
    TestPackageTests
{
    [TestMethod]
    public void GetNonsenseRelationshipBitSimData()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        var simData = dbpf.GetData("545AC67A:004DFB84:00000000D1D56A53");
        Assert.AreEqual("LlamaLogicPackageTests:NonsenseRelationshipBit", simData.ResourceName);
        var table = simData["LlamaLogicPackageTests:NonsenseRelationshipBit"];
        Assert.AreEqual("RelationshipBit", table.SchemaName);
        Assert.AreEqual(0xf94b8fe1, table.SchemaHash);
        Assert.AreEqual(10, table.ColumnCount);
        var columnsInSchemaOrder = new (string name, DataModelType type, ushort flags)[]
        {
            ("directionality", DataModelType.UINT32, 0),
            ("display_name", DataModelType.LOCKEY, 0),
            ("sim_profile_background", DataModelType.VARIANT, 0),
            ("icon", DataModelType.RESOURCEKEY, 0),
            ("visible", DataModelType.BOOL, 0),
            ("bit_description", DataModelType.LOCKEY, 0),
            ("collection_ids", DataModelType.VECTOR, 0),
            ("group_id", DataModelType.UINT32, 0),
            ("display_priority", DataModelType.INT32, 0),
            ("small_icon", DataModelType.VARIANT, 0)
        };
        for (var c = 0; c < columnsInSchemaOrder.Length; ++c)
        {
            var (name, type, flags) = columnsInSchemaOrder[c];
            Assert.AreEqual(name, table.GetColumnName(c));
            Assert.AreEqual(type, table.GetColumnType(c));
            Assert.AreEqual(flags, table.GetColumnFlags(c));
        }
        Assert.AreEqual(1, table.RowCount);
        Assert.AreEqual(0xdb5ac988, table.Get(0, "bit_description"));
        var collectionIdsVector = (DataModelVector)table.Get(0, "collection_ids")!;
        Assert.AreEqual(4, collectionIdsVector.Count);
        var collectionIds = collectionIdsVector.Values.Cast<long>().ToArray();
        Assert.AreEqual(64L, collectionIds[0]);
        Assert.AreEqual(65L, collectionIds[1]);
        Assert.AreEqual(66L, collectionIds[2]);
        Assert.AreEqual(67L, collectionIds[3]);
        Assert.AreEqual(1U, table.Get(0, "directionality"));
        Assert.AreEqual(0xd4fc0137, table.Get(0, "display_name"));
        Assert.AreEqual(2, table.Get(0, "display_priority"));
        Assert.AreEqual(2U, table.Get(0, "group_id"));
        var icon = (ResourceKey)table.Get(0, "icon")!;
        Assert.AreEqual(ResourceType.DSTImage, icon.Type);
        Assert.AreEqual(0x00000000U, icon.Group);
        Assert.AreEqual(0x297cb1fd39511ef4UL, icon.FullInstance);
        var simProfileBackground = (DataModelVariant)table.Get(0, "sim_profile_background")!;
        Assert.AreEqual(0xf8cbbf4c, simProfileBackground.TypeHash);
        var simProfileBackgroundValue = (ResourceKey)simProfileBackground.Value!;
        Assert.AreEqual(ResourceType.DSTImage, simProfileBackgroundValue.Type);
        Assert.AreEqual(0x00000000U, simProfileBackgroundValue.Group);
        Assert.AreEqual(0xde8e092788735948UL, simProfileBackgroundValue.FullInstance);
        var smallIcon = (DataModelVariant)table.Get(0, "small_icon")!;
        Assert.AreEqual(0xf8cbbf4c, smallIcon.TypeHash);
        var smallIconValue = (ResourceKey)smallIcon.Value!;
        Assert.AreEqual(ResourceType.DSTImage, smallIconValue.Type);
        Assert.AreEqual(0x00000000U, smallIconValue.Group);
        Assert.AreEqual(0x573597ddcdbad0eaUL, smallIconValue.FullInstance);
        Assert.IsFalse((bool)table.Get(0, "visible")!);
    }
}
