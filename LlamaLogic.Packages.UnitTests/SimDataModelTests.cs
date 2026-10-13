namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class SimDataModelTests :
    TestPackageTests
{
    const string nestedModelHex = "44415441010100001800000003000000b000000002000000000000000000000046010000376c7be1980000000d000000100000004c0000000100000000000080c59d1c81940000000d00000004000000400000000300000000000080c59d1c81000000800100000001000000340000001500000000000000000000000000000010000000100000000200000023000000100000001100000012000000000000004465616e004269746500536f6d65004f7474657200000000000000000000000095000000eeff6e0a7b8ffa7c1000000020000000030000008800000053fa066ac3ad4e0d04000000440000000100000056000000e817bc990e00000004000000000000804b0000007d7a86e10b0000000c0000000000008028000000955af7ff0d00000000000000000000802f000000f43b8b2f0b00000000000000000000806368696c64006368696c6472656e006c6162656c004f75746572006e616d6500496e6e6572004f75746572496e7374616e636500";
    const string nonsenseRelationshipBitXml =
        """
        <SimData version="0x00000101" u="0x00000000">
          <Instances>
            <I name="LlamaLogicPackageTests:NonsenseRelationshipBit" schema="RelationshipBit" type="Object">
              <T name="bit_description">0xDB5AC988</T>
              <L name="collection_ids">
                <T type="Int64">64</T>
                <T type="Int64">65</T>
                <T type="Int64">66</T>
                <T type="Int64">67</T>
              </L>
              <T name="directionality">1</T>
              <T name="display_name">0xD4FC0137</T>
              <T name="display_priority">2</T>
              <T name="group_id">2</T>
              <T name="icon">00B2D882-00000000-297CB1FD39511EF4</T>
              <V name="sim_profile_background" variant="0xF8CBBF4C">
                <T type="ResourceKey">00B2D882-00000000-DE8E092788735948</T>
              </V>
              <V name="small_icon" variant="0xF8CBBF4C">
                <T type="ResourceKey">00B2D882-00000000-573597DDCDBAD0EA</T>
              </V>
              <T name="visible">0</T>
            </I>
          </Instances>
          <Schemas>
            <Schema name="RelationshipBit" schema_hash="0xF94B8FE1">
              <Columns>
                <Column name="bit_description" type="LocalizationKey" flags="0x00000000" />
                <Column name="collection_ids" type="Vector" flags="0x00000000" />
                <Column name="directionality" type="UInt32" flags="0x00000000" />
                <Column name="display_name" type="LocalizationKey" flags="0x00000000" />
                <Column name="display_priority" type="Int32" flags="0x00000000" />
                <Column name="group_id" type="UInt32" flags="0x00000000" />
                <Column name="icon" type="ResourceKey" flags="0x00000000" />
                <Column name="sim_profile_background" type="Variant" flags="0x00000000" />
                <Column name="small_icon" type="Variant" flags="0x00000000" />
                <Column name="visible" type="Boolean" flags="0x00000000" />
              </Columns>
            </Schema>
          </Schemas>
        </SimData>
        """;
    const string nestedXml =
        """
        <SimData version="0x00000101" u="0x00000000">
          <Instances>
            <I name="Holder" schema="Holder" type="Object">
              <U name="child" schema="Child">
                <T name="label">Dean</T>
              </U>
              <U name="missing" />
              <L name="children">
                <U type="Object" schema="Child">
                  <T name="label"></T>
                </U>
                <U type="Object" schema="Child">
                  <T name="label" />
                </U>
              </L>
              <L name="nothing" />
              <V name="object_variant" variant="0x12345678" schema="Child">
                <T name="label">Bite</T>
              </V>
              <V name="vector_variant" variant="0x9ABCDEF0">
                <L type="Vector">
                  <T type="Single">1.5</T>
                </L>
              </V>
              <V name="valueless_variant" variant="0x0000002A" />
              <T name="position">1,-2.25</T>
              <T name="flag">1</T>
            </I>
          </Instances>
          <Schemas>
            <Schema name="Holder" schema_hash="0x00000001">
              <Columns>
                <Column name="child" type="Object" flags="0x00000000" />
                <Column name="missing" type="Object" flags="0x00000000" />
                <Column name="children" type="Vector" flags="0x00000000" />
                <Column name="nothing" type="Vector" flags="0x00000000" />
                <Column name="object_variant" type="Variant" flags="0x00000000" />
                <Column name="vector_variant" type="Variant" flags="0x00000000" />
                <Column name="valueless_variant" type="Variant" flags="0x00000000" />
                <Column name="position" type="Float2" flags="0x00000000" />
                <Column name="flag" type="Boolean" flags="0x00000000" />
              </Columns>
            </Schema>
            <Schema name="Child" schema_hash="0x00000002">
              <Columns>
                <Column name="label" type="String" flags="0x00000000" />
              </Columns>
            </Schema>
          </Schemas>
        </SimData>
        """;
    static readonly ResourceKey nonsenseRelationshipBitKey = new(ResourceType.SimData, 0x004dfb84, 0x00000000d1d56a53);

    static void AssertNonsenseRelationshipBit(SimDataModel simData)
    {
        Assert.AreEqual("LlamaLogicPackageTests:NonsenseRelationshipBit", simData.ResourceName);
        Assert.AreEqual(1, simData.Instances.Count);
        var instance = simData.Instances[0];
        Assert.AreEqual("LlamaLogicPackageTests:NonsenseRelationshipBit", instance.Name);
        var relationshipBit = instance.Object;
        Assert.AreEqual("RelationshipBit", relationshipBit.Schema.Name);
        Assert.AreEqual(0xf94b8fe1, relationshipBit.Schema.Hash);
        var columnsInLayoutOrder = new (string name, SimDataType type)[]
        {
            ("bit_description", SimDataType.LOCKEY),
            ("collection_ids", SimDataType.VECTOR),
            ("directionality", SimDataType.UINT32),
            ("display_name", SimDataType.LOCKEY),
            ("display_priority", SimDataType.INT32),
            ("group_id", SimDataType.UINT32),
            ("icon", SimDataType.RESOURCEKEY),
            ("sim_profile_background", SimDataType.VARIANT),
            ("small_icon", SimDataType.VARIANT),
            ("visible", SimDataType.BOOL)
        };
        Assert.AreEqual(columnsInLayoutOrder.Length, relationshipBit.Schema.Columns.Count);
        for (var c = 0; c < columnsInLayoutOrder.Length; ++c)
        {
            var (name, type) = columnsInLayoutOrder[c];
            Assert.AreEqual(name, relationshipBit.Schema.Columns[c].Name);
            Assert.AreEqual(type, relationshipBit.Schema.Columns[c].Type);
            Assert.AreEqual((ushort)0, relationshipBit.Schema.Columns[c].Flags);
        }
        Assert.AreEqual(0xdb5ac988, relationshipBit["bit_description"]);
        var collectionIds = (SimDataVector)relationshipBit["collection_ids"]!;
        Assert.AreEqual(SimDataType.INT64, collectionIds.ElementType);
        CollectionAssert.AreEqual(new object[] { 64L, 65L, 66L, 67L }, collectionIds.ToArray());
        Assert.AreEqual(1U, relationshipBit["directionality"]);
        Assert.AreEqual(0xd4fc0137, relationshipBit["display_name"]);
        Assert.AreEqual(2, relationshipBit["display_priority"]);
        Assert.AreEqual(2U, relationshipBit["group_id"]);
        Assert.AreEqual(new ResourceKey(ResourceType.DSTImage, 0, 0x297cb1fd39511ef4), relationshipBit["icon"]);
        var simProfileBackground = (SimDataVariant)relationshipBit["sim_profile_background"]!;
        Assert.AreEqual(0xf8cbbf4c, simProfileBackground.TypeHash);
        Assert.AreEqual(SimDataType.RESOURCEKEY, simProfileBackground.Type);
        Assert.AreEqual(new ResourceKey(ResourceType.DSTImage, 0, 0xde8e092788735948), simProfileBackground.Value);
        var smallIcon = (SimDataVariant)relationshipBit["small_icon"]!;
        Assert.AreEqual(0xf8cbbf4c, smallIcon.TypeHash);
        Assert.AreEqual(SimDataType.RESOURCEKEY, smallIcon.Type);
        Assert.AreEqual(new ResourceKey(ResourceType.DSTImage, 0, 0x573597ddcdbad0ea), smallIcon.Value);
        Assert.IsFalse((bool)relationshipBit["visible"]!);
    }

    static void AssertValuesEqual(object? expected, object? actual, string path)
    {
        switch (expected)
        {
            case SimDataObject expectedObject:
                var actualObject = actual as SimDataObject;
                Assert.IsNotNull(actualObject, path);
                Assert.AreEqual(expectedObject.Schema.Name, actualObject.Schema.Name, path);
                Assert.AreEqual(expectedObject.Schema.Hash, actualObject.Schema.Hash, path);
                Assert.AreEqual(expectedObject.Schema.Columns.Count, actualObject.Schema.Columns.Count, path);
                for (var c = 0; c < expectedObject.Schema.Columns.Count; ++c)
                {
                    var column = expectedObject.Schema.Columns[c];
                    Assert.AreEqual(column.Name, actualObject.Schema.Columns[c].Name, path);
                    Assert.AreEqual(column.Type, actualObject.Schema.Columns[c].Type, path);
                    Assert.AreEqual(column.Flags, actualObject.Schema.Columns[c].Flags, path);
                    AssertValuesEqual(expectedObject[c], actualObject[c], $"{path}.{column.Name}");
                }
                break;
            case SimDataVector expectedVector:
                var actualVector = actual as SimDataVector;
                Assert.IsNotNull(actualVector, path);
                Assert.AreEqual(expectedVector.ElementType, actualVector.ElementType, path);
                Assert.AreEqual(expectedVector.Count, actualVector.Count, path);
                for (var i = 0; i < expectedVector.Count; ++i)
                    AssertValuesEqual(expectedVector[i], actualVector[i], $"{path}[{i}]");
                break;
            case SimDataVariant expectedVariant:
                var actualVariant = actual as SimDataVariant;
                Assert.IsNotNull(actualVariant, path);
                Assert.AreEqual(expectedVariant.TypeHash, actualVariant.TypeHash, path);
                Assert.AreEqual(expectedVariant.Type, actualVariant.Type, path);
                AssertValuesEqual(expectedVariant.Value, actualVariant.Value, $"{path}.Value");
                break;
            default:
                Assert.AreEqual(expected, actual, path);
                break;
        }
    }

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
            writer.Write((uint)SimDataType.UINT32);
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

    static SimDataModel BuildModelWithEveryType()
    {
        var innerSchema = new SimDataSchema("Inner", 0x1a2b3c4d, [new("name", SimDataType.STRING8), new("weight", SimDataType.FLOAT)]);
        SimDataObject inner(string? name, float weight)
        {
            var @object = new SimDataObject(innerSchema);
            @object["name"] = name;
            @object["weight"] = weight;
            return @object;
        }
        var everythingSchema = new SimDataSchema("Everything", 0x5e6f7081,
        [
            new("bool", SimDataType.BOOL),
            new("char8", SimDataType.CHAR8),
            new("int8", SimDataType.INT8),
            new("uint8", SimDataType.UINT8),
            new("int16", SimDataType.INT16),
            new("uint16", SimDataType.UINT16),
            new("int32", SimDataType.INT32),
            new("uint32", SimDataType.UINT32),
            new("int64", SimDataType.INT64),
            new("uint64", SimDataType.UINT64),
            new("float", SimDataType.FLOAT),
            new("string8", SimDataType.STRING8),
            new("hashedstring8", SimDataType.HASHEDSTRING8),
            new("object", SimDataType.OBJECT),
            new("vector", SimDataType.VECTOR),
            new("float2", SimDataType.FLOAT2),
            new("float3", SimDataType.FLOAT3),
            new("float4", SimDataType.FLOAT4),
            new("tablesetreference", SimDataType.TABLESETREFERENCE),
            new("resourcekey", SimDataType.RESOURCEKEY),
            new("lockey", SimDataType.LOCKEY),
            new("variant", SimDataType.VARIANT),
            new("null_object", SimDataType.OBJECT),
            new("null_string8", SimDataType.STRING8),
            new("null_vector", SimDataType.VECTOR),
            new("valueless_variant", SimDataType.VARIANT),
            new("string_vector", SimDataType.VECTOR),
            new("character_vector", SimDataType.VECTOR),
            new("string_variant", SimDataType.VARIANT)
        ]);
        var everything = new SimDataObject(everythingSchema);
        everything["bool"] = true;
        everything["char8"] = (byte)'L';
        everything["int8"] = (sbyte)-8;
        everything["uint8"] = (byte)8;
        everything["int16"] = (short)-16;
        everything["uint16"] = (ushort)16;
        everything["int32"] = -32;
        everything["uint32"] = 32U;
        everything["int64"] = -64L;
        everything["uint64"] = 64UL;
        everything["float"] = 1.5F;
        everything["string8"] = "Dolejší";
        everything["hashedstring8"] = "Hashed";
        everything["object"] = inner("only", 0.25F);
        everything["vector"] = new SimDataVector(SimDataType.OBJECT, [inner("first", 1F), inner(null, 2F), inner("třetí", 3F)]);
        everything["float2"] = new Vector2(1, 2);
        everything["float3"] = new Vector3(1, 2, 3);
        everything["float4"] = new Vector4(1, 2, 3, 4);
        everything["tablesetreference"] = 0xfedcba9876543210UL;
        everything["resourcekey"] = new ResourceKey(ResourceType.DSTImage, 0x12345678, 0x0123456789abcdef);
        everything["lockey"] = 0xdb5ac988U;
        everything["variant"] = new SimDataVariant(0xf8cbbf4c, SimDataType.VECTOR, new SimDataVector(SimDataType.INT64, [64L, 65L]));
        everything["valueless_variant"] = new SimDataVariant(0x8afe2b6b);
        everything["string_vector"] = new SimDataVector(SimDataType.STRING8, ["alpha", null, "gamma"]);
        everything["character_vector"] = new SimDataVector(SimDataType.CHAR8, [(byte)'h', (byte)'i']);
        everything["string_variant"] = new SimDataVariant(0x5f9b2c1d, SimDataType.STRING8, "variant");
        var other = inner("other instance", -1F);
        var model = new SimDataModel { Unused = 7 };
        model.Instances.Add(new("Everything:Instance", everything));
        model.Instances.Add(new("Inner:Instance", other));
        return model;
    }

    static SimDataModel BuildNestedModel()
    {
        var innerSchema = new SimDataSchema("Inner", 0x0d4eadc3, [new("name", SimDataType.STRING8)]);
        var outerSchema = new SimDataSchema("Outer", 0x7cfa8f7b, [new("child", SimDataType.OBJECT), new("children", SimDataType.VECTOR), new("label", SimDataType.STRING8)]);
        SimDataObject inner(string name)
        {
            var @object = new SimDataObject(innerSchema);
            @object["name"] = name;
            return @object;
        }
        var outer = new SimDataObject(outerSchema);
        outer["child"] = inner("Dean");
        outer["children"] = new SimDataVector(SimDataType.OBJECT, [inner("Bite"), inner("Some")]);
        outer["label"] = "Otter";
        var model = new SimDataModel();
        model.Instances.Add(new("OuterInstance", outer));
        return model;
    }

    static int GetRowPosition(byte[] data, int table) =>
        32 + (table * 28) + 20 + BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(32 + (table * 28) + 20));

    [TestMethod]
    public void DecodeKeepsVersionAndUnused()
    {
        var model = BuildNestedModel();
        model.Version = SimDataVersion.SmartSimOriginalEdition;
        var decoded = SimDataModel.Decode(model.Encode());
        Assert.AreEqual(SimDataVersion.SmartSimOriginalEdition, decoded.Version);
        Assert.AreEqual(7U, SimDataModel.Decode(BuildModelWithEveryType().Encode()).Unused);
    }

    [TestMethod]
    public void DecodeReadsEveryType()
    {
        var model = BuildModelWithEveryType();
        var decoded = SimDataModel.Decode(model.Encode());
        Assert.AreEqual(model.Instances.Count, decoded.Instances.Count);
        for (var i = 0; i < model.Instances.Count; ++i)
        {
            Assert.AreEqual(model.Instances[i].Name, decoded.Instances[i].Name);
            AssertValuesEqual(model.Instances[i].Object, decoded.Instances[i].Object, model.Instances[i].Name);
        }
    }

    [TestMethod]
    public void DecodeReadsZeroOffsetReferencesAsNull()
    {
        var data = BuildNestedModel().Encode().ToArray();
        var childPosition = GetRowPosition(data, 0);
        Assert.AreNotEqual(int.MinValue, BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(childPosition)));
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(childPosition), 0);
        var outer = SimDataModel.Decode(data).Instances[0].Object;
        Assert.IsNull(outer["child"]);
        Assert.AreEqual(2, ((SimDataVector)outer["children"]!).Count);
    }

    [TestMethod]
    public void DecodeRejectsRowsReferredToMoreThanOnce()
    {
        var data = BuildNestedModel().Encode().ToArray();
        var childPosition = GetRowPosition(data, 0);
        var childrenPosition = childPosition + 4;
        var childTarget = childPosition + BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(childPosition));
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(childrenPosition), childTarget - childrenPosition);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(childrenPosition + 4), 1);
        Assert.ThrowsExactly<InvalidDataException>(() => SimDataModel.Decode(data));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("DATA")]
    [DataRow("not SimData at all")]
    public void DecodeRejectsOtherData(string data) =>
        Assert.ThrowsExactly<InvalidDataException>(() => SimDataModel.Decode(Encoding.UTF8.GetBytes(data)));

    [TestMethod]
    [DataRow(16)]
    [DataRow(64)]
    [DataRow(128)]
    [DataRow(-1)]
    public void DecodeRejectsTruncatedData(int length)
    {
        var data = BuildModelWithEveryType().Encode();
        Assert.ThrowsExactly<InvalidDataException>(() => SimDataModel.Decode(data[..(length < 0 ? data.Length + length : length)]));
    }

    [TestMethod]
    public void DecodeRejectsVariantsInSmartSimOriginalEdition()
    {
        var data = BuildModelWithEveryType().Encode().ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), (uint)SimDataVersion.SmartSimOriginalEdition);
        Assert.ThrowsExactly<InvalidDataException>(() => SimDataModel.Decode(data));
    }

    [TestMethod]
    public void EncodeIsStable()
    {
        var encoded = BuildModelWithEveryType().Encode();
        CollectionAssert.AreEqual(encoded.ToArray(), SimDataModel.Decode(encoded).Encode().ToArray());
    }

    [TestMethod]
    public void EncodeLaysOutLikeMaxis() =>
        Assert.AreEqual(nestedModelHex, Convert.ToHexString(BuildNestedModel().Encode().Span).ToLowerInvariant());

    [TestMethod]
    public void EncodeReproducesTestPackageSimData()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        var data = dbpf.Get(nonsenseRelationshipBitKey);
        CollectionAssert.AreEqual(data.ToArray(), SimDataModel.Decode(data).Encode().ToArray());
    }

    [TestMethod]
    public void EncodeRejectsDifferentSchemasWithTheSameHash()
    {
        var model = new SimDataModel();
        model.Instances.Add(new("A", new SimDataObject(new SimDataSchema("A", 1, [new("value", SimDataType.INT32)]))));
        model.Instances.Add(new("B", new SimDataObject(new SimDataSchema("B", 1, [new("value", SimDataType.INT32)]))));
        Assert.ThrowsExactly<InvalidOperationException>(() => model.Encode());
    }

    [TestMethod]
    public void EncodeRejectsObjectsContainingThemselves()
    {
        var schema = new SimDataSchema("Node", 1, [new("next", SimDataType.OBJECT)]);
        var node = new SimDataObject(schema);
        node["next"] = node;
        var model = new SimDataModel();
        model.Instances.Add(new("Node", node));
        Assert.ThrowsExactly<InvalidOperationException>(() => model.Encode());
    }

    [TestMethod]
    public void EncodeRejectsSameSchemaInstancesWithDifferentNames()
    {
        var schema = new SimDataSchema("Thing", 1, [new("value", SimDataType.INT32)]);
        var model = new SimDataModel();
        model.Instances.Add(new("First", new SimDataObject(schema)));
        model.Instances.Add(new("Second", new SimDataObject(schema)));
        Assert.ThrowsExactly<InvalidOperationException>(() => model.Encode());
    }

    [TestMethod]
    public void EncodeRejectsVariantsInSmartSimOriginalEdition()
    {
        var model = BuildModelWithEveryType();
        model.Version = SimDataVersion.SmartSimOriginalEdition;
        Assert.ThrowsExactly<InvalidOperationException>(() => model.Encode());
    }

    [TestMethod]
    public void EncodeRejectsVectorsContainingThemselves()
    {
        var schema = new SimDataSchema("Holder", 1, [new("vectors", SimDataType.VECTOR)]);
        var vector = new SimDataVector(SimDataType.VECTOR);
        vector.Add(vector);
        var holder = new SimDataObject(schema);
        holder["vectors"] = vector;
        var model = new SimDataModel();
        model.Instances.Add(new("Holder", holder));
        Assert.ThrowsExactly<InvalidOperationException>(() => model.Encode());
    }

    [TestMethod]
    public void EncodeWritesNameHashes()
    {
        var model = new SimDataModel();
        var hashes = new SimDataObject(new SimDataSchema("Hashes", 1, [new("Hashed", SimDataType.HASHEDSTRING8), new("unhashed", SimDataType.HASHEDSTRING8)]));
        hashes["Hashed"] = "MixedCase";
        model.Instances.Add(new("Hashes:Instance", hashes));
        var data = model.Encode().ToArray();
        var rowPosition = GetRowPosition(data, 0);
        var schemaPosition = 16 + BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(16));
        Assert.AreEqual(Fnv32.GetHash("Hashes:Instance"), BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(32 + 4)));
        Assert.AreEqual(Fnv32.EmptyStringHash, BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(32 + 28 + 4)));
        Assert.AreEqual(Fnv32.GetHash("MixedCase"), BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(rowPosition + 4)));
        Assert.AreEqual(Fnv32.EmptyStringHash, BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(rowPosition + 12)));
        Assert.AreEqual(Fnv32.GetHash("Hashes"), BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(schemaPosition + 4)));
    }

    [TestMethod]
    public void FromXmlAcceptsSims4ToolkitTypeNames()
    {
        var model = SimDataModel.FromXml(XDocument.Parse("<SimData version=\"0x00000101\" u=\"0x00000000\"><Instances><I name=\"A\" schema=\"A\" type=\"Object\"><T name=\"signed\">-8</T><T name=\"unsigned\">8</T></I></Instances><Schemas><Schema name=\"A\" schema_hash=\"0x00000001\"><Columns><Column name=\"signed\" type=\"Int8\" flags=\"0x00000000\" /><Column name=\"unsigned\" type=\"UInt8\" flags=\"0x00000000\" /></Columns></Schema></Schemas></SimData>"));
        Assert.AreEqual((sbyte)-8, model.Instances[0].Object["signed"]);
        Assert.AreEqual((byte)8, model.Instances[0].Object["unsigned"]);
    }

    [TestMethod]
    public void FromXmlReadsNestedValues()
    {
        var holder = SimDataModel.FromXml(XDocument.Parse(nestedXml)).Instances[0].Object;
        Assert.AreEqual("Dean", ((SimDataObject)holder["child"]!)["label"]);
        Assert.IsNull(holder["missing"]);
        var children = (SimDataVector)holder["children"]!;
        Assert.AreEqual(SimDataType.OBJECT, children.ElementType);
        Assert.AreEqual(string.Empty, ((SimDataObject)children[0]!)["label"]);
        Assert.IsNull(((SimDataObject)children[1]!)["label"]);
        Assert.IsNull(holder["nothing"]);
        var objectVariant = (SimDataVariant)holder["object_variant"]!;
        Assert.AreEqual(0x12345678U, objectVariant.TypeHash);
        Assert.AreEqual(SimDataType.OBJECT, objectVariant.Type);
        Assert.AreEqual("Bite", ((SimDataObject)objectVariant.Value!)["label"]);
        var vectorVariant = (SimDataVariant)holder["vector_variant"]!;
        Assert.AreEqual(SimDataType.VECTOR, vectorVariant.Type);
        CollectionAssert.AreEqual(new object[] { 1.5F }, ((SimDataVector)vectorVariant.Value!).ToArray());
        var valuelessVariant = (SimDataVariant)holder["valueless_variant"]!;
        Assert.AreEqual(0x2AU, valuelessVariant.TypeHash);
        Assert.IsNull(valuelessVariant.Type);
        Assert.AreEqual(new Vector2(1, -2.25F), holder["position"]);
        Assert.AreEqual(true, holder["flag"]);
    }

    [TestMethod]
    public void FromXmlReadsNonsenseRelationshipBit()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        var model = SimDataModel.FromXml(XDocument.Parse(nonsenseRelationshipBitXml));
        AssertNonsenseRelationshipBit(model);
        CollectionAssert.AreEqual(dbpf.Get(nonsenseRelationshipBitKey).ToArray(), model.Encode().ToArray());
    }

    [TestMethod]
    [DataRow("<NotSimData />")]
    [DataRow("<SimData version=\"0x00000102\" u=\"0x00000000\" />")]
    [DataRow("<SimData version=\"0x00000100\" u=\"0x00000000\"><Instances /><Schemas><Schema name=\"A\" schema_hash=\"0x00000001\"><Columns><Column name=\"v\" type=\"Variant\" flags=\"0x00000000\" /></Columns></Schema></Schemas></SimData>")]
    [DataRow("<SimData version=\"0x00000101\" u=\"0x00000000\"><Instances /><Schemas><Schema name=\"A\" schema_hash=\"0x00000001\"><Columns><Column name=\"v\" type=\"Imaginary\" flags=\"0x00000000\" /></Columns></Schema></Schemas></SimData>")]
    [DataRow("<SimData version=\"0x00000101\" u=\"0x00000000\"><Instances /><Schemas><Schema name=\"A\" schema_hash=\"0x00000001\"><Columns><Column name=\"v\" type=\"Int32\" flags=\"0x00000000\" /></Columns></Schema><Schema name=\"A\" schema_hash=\"0x00000002\"><Columns><Column name=\"v\" type=\"Int32\" flags=\"0x00000000\" /></Columns></Schema></Schemas></SimData>")]
    [DataRow("<SimData version=\"0x00000101\" u=\"0x00000000\"><Instances><I name=\"A\" schema=\"B\" type=\"Object\" /></Instances><Schemas><Schema name=\"A\" schema_hash=\"0x00000001\"><Columns><Column name=\"v\" type=\"Int32\" flags=\"0x00000000\" /></Columns></Schema></Schemas></SimData>")]
    [DataRow("<SimData version=\"0x00000101\" u=\"0x00000000\"><Instances><I name=\"A\" schema=\"A\" type=\"Object\" /></Instances><Schemas><Schema name=\"A\" schema_hash=\"0x00000001\"><Columns><Column name=\"v\" type=\"Int32\" flags=\"0x00000000\" /></Columns></Schema></Schemas></SimData>")]
    [DataRow("<SimData version=\"0x00000101\" u=\"0x00000000\"><Instances><I name=\"A\" schema=\"A\" type=\"Object\"><T name=\"v\">1</T><T name=\"w\">2</T></I></Instances><Schemas><Schema name=\"A\" schema_hash=\"0x00000001\"><Columns><Column name=\"v\" type=\"Int32\" flags=\"0x00000000\" /></Columns></Schema></Schemas></SimData>")]
    [DataRow("<SimData version=\"0x00000101\" u=\"0x00000000\"><Instances><I name=\"A\" schema=\"A\" type=\"Object\"><T name=\"v\">1</T><T name=\"v\">2</T></I></Instances><Schemas><Schema name=\"A\" schema_hash=\"0x00000001\"><Columns><Column name=\"v\" type=\"Int32\" flags=\"0x00000000\" /></Columns></Schema></Schemas></SimData>")]
    [DataRow("<SimData version=\"0x00000101\" u=\"0x00000000\"><Instances><I name=\"A\" schema=\"A\" type=\"Object\"><T name=\"v\">one</T></I></Instances><Schemas><Schema name=\"A\" schema_hash=\"0x00000001\"><Columns><Column name=\"v\" type=\"Int32\" flags=\"0x00000000\" /></Columns></Schema></Schemas></SimData>")]
    [DataRow("<SimData version=\"0x00000101\" u=\"0x00000000\"><Instances><I name=\"A\" schema=\"A\" type=\"Object\"><L name=\"v\"><T type=\"Int32\">1</T><T type=\"UInt32\">2</T></L></I></Instances><Schemas><Schema name=\"A\" schema_hash=\"0x00000001\"><Columns><Column name=\"v\" type=\"Vector\" flags=\"0x00000000\" /></Columns></Schema></Schemas></SimData>")]
    [DataRow("<SimData version=\"0x00000101\" u=\"0x00000000\"><Instances><I name=\"A\" schema=\"A\" type=\"Object\"><T name=\"v\">1</T></I><I name=\"B\" schema=\"A\" type=\"Object\"><T name=\"v\">2</T></I></Instances><Schemas><Schema name=\"A\" schema_hash=\"0x00000001\"><Columns><Column name=\"v\" type=\"Int32\" flags=\"0x00000000\" /></Columns></Schema></Schemas></SimData>")]
    public void FromXmlRejectsInvalidDocuments(string xml) =>
        Assert.ThrowsExactly<InvalidDataException>(() => SimDataModel.FromXml(XDocument.Parse(xml)));

    [TestMethod]
    public void FromXmlRoundTripsEveryType()
    {
        var model = BuildModelWithEveryType();
        var imported = SimDataModel.FromXml(XDocument.Parse(model.ToXml().ToString()));
        Assert.AreEqual(model.Unused, imported.Unused);
        Assert.AreEqual(model.Instances.Count, imported.Instances.Count);
        for (var i = 0; i < model.Instances.Count; ++i)
        {
            Assert.AreEqual(model.Instances[i].Name, imported.Instances[i].Name);
            AssertValuesEqual(model.Instances[i].Object, imported.Instances[i].Object, model.Instances[i].Name);
        }
        CollectionAssert.AreEqual(model.Encode().ToArray(), imported.Encode().ToArray());
    }

    [TestMethod]
    public async Task GetNameAsyncToleratesShortReads()
    {
        var data = BuildData(null, "First", "Second");
        using var stream = new ShortReadStream(new MemoryStream(data), 3);
        Assert.AreEqual("First", await SimDataModel.GetNameAsync(stream));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    public async Task GetNameAsyncReadsFirstNamedTable(int unnamedTables)
    {
        var data = BuildData([.. Enumerable.Repeat<string?>(null, unnamedTables), "First", "Second"]);
        using var stream = new MemoryStream(data);
        Assert.AreEqual("First", await SimDataModel.GetNameAsync(stream));
    }

    [TestMethod]
    public async Task GetNameByKeyAsyncReadsSimDataName()
    {
        await using var dbpf = await DataBasePackedFile.FromPathAsync(tempPackagePath);
        Assert.AreEqual("LlamaLogicPackageTests:NonsenseRelationshipBit", await dbpf.GetNameByKeyAsync(nonsenseRelationshipBitKey));
    }

    [TestMethod]
    public void GetNameByKeyReadsSimDataName()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        Assert.AreEqual("LlamaLogicPackageTests:NonsenseRelationshipBit", dbpf.GetNameByKey(nonsenseRelationshipBitKey));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    public void GetNameReadsFirstNamedTable(int unnamedTables)
    {
        var data = BuildData([.. Enumerable.Repeat<string?>(null, unnamedTables), "First", "Second"]);
        using var stream = new MemoryStream(data);
        Assert.AreEqual("First", SimDataModel.GetName(stream));
    }

    [TestMethod]
    public void GetNameMatchesResourceName()
    {
        var data = BuildModelWithEveryType().Encode().ToArray();
        using var stream = new MemoryStream(data);
        Assert.AreEqual(SimDataModel.Decode(data).ResourceName, SimDataModel.GetName(stream));
    }

    [TestMethod]
    public void GetNameReturnsNullWithoutNamedTables()
    {
        using var stream = new MemoryStream(BuildData(null, null));
        Assert.IsNull(SimDataModel.GetName(stream));
    }

    [TestMethod]
    public void GetNameToleratesShortReads()
    {
        var data = BuildData(null, "First", "Second");
        using var stream = new ShortReadStream(new MemoryStream(data), 3);
        Assert.AreEqual("First", SimDataModel.GetName(stream));
    }

    [TestMethod]
    public void GetSimDataReadsNonsenseRelationshipBit()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        AssertNonsenseRelationshipBit(dbpf.GetSimData(nonsenseRelationshipBitKey));
    }

    [TestMethod]
    public async Task GetSimDataAsyncReadsNonsenseRelationshipBit()
    {
        await using var dbpf = await DataBasePackedFile.FromPathAsync(tempPackagePath);
        AssertNonsenseRelationshipBit(await dbpf.GetSimDataAsync(nonsenseRelationshipBitKey));
    }

    [TestMethod]
    public void ObjectRejectsUnknownColumns()
    {
        var @object = new SimDataObject(new SimDataSchema("Thing", 1, [new("value", SimDataType.INT32)]));
        Assert.ThrowsExactly<ArgumentException>(() => @object["missing"] = 1);
    }

    [TestMethod]
    [DataRow(SimDataType.UINT32, 1)]
    [DataRow(SimDataType.INT64, 1)]
    [DataRow(SimDataType.BOOL, null)]
    [DataRow(SimDataType.STRING8, 1)]
    [DataRow(SimDataType.OBJECT, "object")]
    public void ObjectRejectsValuesOfTheWrongType(SimDataType type, object? value)
    {
        var @object = new SimDataObject(new SimDataSchema("Thing", 1, [new("value", type)]));
        Assert.ThrowsExactly<ArgumentException>(() => @object["value"] = value);
    }

    [TestMethod]
    public void SchemaRejectsDuplicateColumns() =>
        Assert.ThrowsExactly<ArgumentException>(() => new SimDataSchema("Thing", 1, [new("value", SimDataType.INT32), new("value", SimDataType.UINT32)]));

    [TestMethod]
    public void SchemaRejectsNoColumns() =>
        Assert.ThrowsExactly<ArgumentException>(() => new SimDataSchema("Thing", 1, []));

    [TestMethod]
    public void ToXmlRejectsCharactersXmlCannotRepresent()
    {
        var @object = new SimDataObject(new SimDataSchema("A", 1, [new("text", SimDataType.STRING8)]));
        @object["text"] = "\u0001";
        var model = new SimDataModel();
        model.Instances.Add(new("A", @object));
        Assert.ThrowsExactly<InvalidOperationException>(() => model.ToXml());
    }

    [TestMethod]
    public void ToXmlRejectsDifferentSchemasWithTheSameName()
    {
        var model = new SimDataModel();
        model.Instances.Add(new("A", new SimDataObject(new SimDataSchema("Same", 1, [new("value", SimDataType.INT32)]))));
        model.Instances.Add(new("B", new SimDataObject(new SimDataSchema("Same", 2, [new("value", SimDataType.INT32)]))));
        Assert.ThrowsExactly<InvalidOperationException>(() => model.ToXml());
    }

    [TestMethod]
    public void ToXmlWritesNestedValues() =>
        Assert.IsTrue(XNode.DeepEquals(XDocument.Parse(nestedXml).Root, SimDataModel.FromXml(XDocument.Parse(nestedXml)).ToXml().Root));

    [TestMethod]
    public void ToXmlWritesNonsenseRelationshipBit()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        var xml = dbpf.GetSimData(nonsenseRelationshipBitKey).ToXml();
        Assert.IsTrue(XNode.DeepEquals(XDocument.Parse(nonsenseRelationshipBitXml).Root, xml.Root), xml.ToString());
    }

    [TestMethod]
    public void VariantRejectsNullObjects() =>
        Assert.ThrowsExactly<ArgumentException>(() => new SimDataVariant(1, SimDataType.OBJECT, null));

    [TestMethod]
    public void VectorRejectsMixedSchemas()
    {
        var vector = new SimDataVector(SimDataType.OBJECT, [new SimDataObject(new SimDataSchema("A", 1, [new("value", SimDataType.INT32)]))]);
        Assert.ThrowsExactly<ArgumentException>(() => vector.Add(new SimDataObject(new SimDataSchema("B", 2, [new("value", SimDataType.INT32)]))));
    }

    [TestMethod]
    public void VectorRejectsNullObjects() =>
        Assert.ThrowsExactly<ArgumentException>(() => new SimDataVector(SimDataType.OBJECT, [null]));

    [TestMethod]
    public void VectorRejectsValuesOfTheWrongType() =>
        Assert.ThrowsExactly<ArgumentException>(() => new SimDataVector(SimDataType.INT64, [64]));
}
