namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class DataModelTests :
    TestPackageTests
{
    static byte[] BuildData(params string?[] tableNames)
    {
        var tableCount = tableNames.Length;
        var tableHeaderPosition = 32;
        var rowsPosition = (tableHeaderPosition + (tableCount * 28) + 15) & ~15;
        var namesPosition = (rowsPosition + (tableCount * 4) + 15) & ~15;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("DATA"u8);
        writer.Write(0x101U);
        writer.Write(tableHeaderPosition - 8);
        writer.Write(tableCount);
        writer.Write(namesPosition - 16);
        writer.Write(0);
        writer.Write(0U);
        var namePosition = namesPosition;
        for (var i = 0; i < tableCount; ++i)
        {
            var fieldPosition = tableHeaderPosition + (i * 28);
            stream.Position = fieldPosition;
            if (tableNames[i] is { } tableName)
            {
                writer.Write(namePosition - fieldPosition);
                writer.Write(Fnv32.GetHash(tableName));
                var nameBytes = Encoding.ASCII.GetBytes(tableName);
                var tableInfoPosition = stream.Position;
                stream.Position = namePosition;
                writer.Write(nameBytes);
                writer.Write((byte)0);
                namePosition += nameBytes.Length + 1;
                stream.Position = tableInfoPosition;
            }
            else
            {
                writer.Write(int.MinValue);
                writer.Write(Fnv32.EmptyStringHash);
            }
            writer.Write(int.MinValue);
            writer.Write((uint)DataModelType.UINT32);
            writer.Write(4U);
            writer.Write(rowsPosition + (i * 4) - (fieldPosition + 20));
            writer.Write(1U);
            stream.Position = rowsPosition + (i * 4);
            writer.Write((uint)i);
        }
        if (stream.Length < namesPosition)
            stream.SetLength(namesPosition);
        writer.Flush();
        return stream.ToArray();
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    public async Task GetNameAsyncReadsFirstNamedTable(int unnamedTables)
    {
        var data = BuildData([.. Enumerable.Repeat<string?>(null, unnamedTables), "First", "Second"]);
        Assert.AreEqual("First", DataModel.Decode(data).ResourceName);
        using var stream = new MemoryStream(data);
        Assert.AreEqual("First", await DataModel.GetNameAsync(stream));
    }

    [TestMethod]
    public async Task GetNameAsyncToleratesShortReads()
    {
        var data = BuildData(null, "First", "Second");
        using var stream = new ShortReadStream(new MemoryStream(data), 3);
        Assert.AreEqual("First", await DataModel.GetNameAsync(stream));
    }

    [TestMethod]
    public async Task GetNameByKeyAsyncReadsSimDataName()
    {
        await using var dbpf = await DataBasePackedFile.FromPathAsync(tempPackagePath);
        Assert.AreEqual("LlamaLogicPackageTests:NonsenseRelationshipBit", await dbpf.GetNameByKeyAsync("545AC67A:004DFB84:00000000D1D56A53"));
    }

    [TestMethod]
    public void GetNameByKeyReadsSimDataName()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        Assert.AreEqual("LlamaLogicPackageTests:NonsenseRelationshipBit", dbpf.GetNameByKey("545AC67A:004DFB84:00000000D1D56A53"));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    public void GetNameReadsFirstNamedTable(int unnamedTables)
    {
        var data = BuildData([.. Enumerable.Repeat<string?>(null, unnamedTables), "First", "Second"]);
        Assert.AreEqual("First", DataModel.Decode(data).ResourceName);
        using var stream = new MemoryStream(data);
        Assert.AreEqual("First", DataModel.GetName(stream));
    }

    [TestMethod]
    public void GetNameReturnsNullWithoutNamedTables()
    {
        var data = BuildData(null, null);
        Assert.IsNull(DataModel.Decode(data).ResourceName);
        using var stream = new MemoryStream(data);
        Assert.IsNull(DataModel.GetName(stream));
    }

    [TestMethod]
    public void GetNameToleratesShortReads()
    {
        var data = BuildData(null, "First", "Second");
        using var stream = new ShortReadStream(new MemoryStream(data), 3);
        Assert.AreEqual("First", DataModel.GetName(stream));
    }

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

    //[TestMethod]
    //public async Task GetCombinedBinaryTuningAsync()
    //{
    //    foreach (var gamePackageFile in new DirectoryInfo(@"C:\Program Files\EA Games\The Sims 4").GetFiles("*.package", SearchOption.AllDirectories))
    //    {
    //        var dbpf = await DataBasePackedFile.FromPathAsync(gamePackageFile.FullName);
    //        foreach (var key in await dbpf.GetKeysAsync().ConfigureAwait(false))
    //            if (key.Type is ResourceType.CombinedTuning)
    //            {
    //                var combinedTuning = await dbpf.GetModelAsync<DataModel>(key);
    //                combinedTuning.ToString();
    //            }
    //    }
    //}
}
