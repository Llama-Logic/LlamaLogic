using System.Runtime.InteropServices;

namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class WorldDataModelTests
{
    static byte[] Chunk(string tag, params byte[][] contents)
    {
        byte[] content = [.. contents.SelectMany(part => part)];
        var result = new byte[8 + content.Length];
        Encoding.ASCII.GetBytes(tag, result);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(4), content.Length);
        content.CopyTo(result, 8);
        return result;
    }

    static byte[] Value<T>(T value)
        where T : struct
    {
        var result = new byte[Marshal.SizeOf<T>()];
        MemoryMarshal.Write(result, ref value);
        return result;
    }

    static byte[] BuildObject(bool includeOptionalChunks, params byte[][] extraChunks) =>
        Chunk
        (
            "OBJ ",
            [
                Chunk("ID  ", Value(0x0000000000000065UL)),
                Chunk("POS ", Value(new Vector3(1F, 2F, 3F))),
                Chunk("ROT ", Value(new Quaternion(0F, 0.70710677F, 0F, 0.70710677F))),
                Chunk("DGUD", Value(0x0000000000003039UL)),
                Chunk("SCAL", Value(1.5F)),
                Chunk("MLOD", [255]),
                Chunk("PTID", Value(0x0000000000000063UL)),
                Chunk("SLOT", Value(0x12345678U)),
                .. includeOptionalChunks ? (byte[][])[Chunk("SNOW", [1]), Chunk("OPRP", Value(4U))] : [],
                .. extraChunks
            ]
        );

    static byte[] BuildWorldData(params byte[][] objects) =>
        Chunk
        (
            "OMGS",
            Chunk
            (
                "OMGR",
                [
                    Chunk("ID  ", Value(0x0000000000000001UL)),
                    Chunk
                    (
                        "LOT ",
                        Chunk("SIZX", Value(40U)),
                        Chunk("SIZZ", Value(30U)),
                        Chunk("POS ", Value(new Vector3(100F, 0F, 200F))),
                        Chunk("ROT ", Value(1.5707964F))
                    ),
                    .. objects
                ]
            )
        );

    [TestMethod]
    public void DecodeAndEncodeRoundTrip()
    {
        var data = BuildWorldData(BuildObject(true), BuildObject(false));
        Assert.IsTrue(data.AsSpan().SequenceEqual(WorldDataModel.Decode(data).Encode().Span));
    }

    [TestMethod]
    public void DecodeReadsEveryField()
    {
        var model = WorldDataModel.Decode(BuildWorldData(BuildObject(true), BuildObject(false)));
        Assert.AreEqual(1, model.ObjectManagers.Count);
        var manager = model.ObjectManagers[0];
        Assert.AreEqual(1UL, manager.Id);
        Assert.AreEqual(1, manager.Lots.Count);
        var lot = manager.Lots[0];
        Assert.AreEqual(40U, lot.Width);
        Assert.AreEqual(30U, lot.Depth);
        Assert.AreEqual(new Vector3(100F, 0F, 200F), lot.Position);
        Assert.AreEqual(1.5707964F, lot.Rotation);
        Assert.AreEqual(2, manager.Objects.Count);
        var @object = manager.Objects[0];
        Assert.AreEqual(0x0000000000000065UL, @object.ObjectId);
        Assert.AreEqual(new Vector3(1F, 2F, 3F), @object.Position);
        Assert.AreEqual(new Quaternion(0F, 0.70710677F, 0F, 0.70710677F), @object.Orientation);
        Assert.AreEqual(0x0000000000003039UL, @object.ObjectDefinitionId);
        Assert.AreEqual(1.5F, @object.Scale);
        Assert.AreEqual(byte.MaxValue, @object.LevelOfDetail);
        Assert.AreEqual(0x0000000000000063UL, @object.ParentObjectId);
        Assert.AreEqual(0x12345678U, @object.ParentSlotHash);
        Assert.IsTrue(@object.Snow);
        Assert.AreEqual(4U, @object.Properties);
        Assert.IsNull(manager.Objects[1].Snow);
        Assert.IsNull(manager.Objects[1].Properties);
    }

    [TestMethod]
    public void DecodeRejectsChunksBeyondTheirContainer()
    {
        var data = BuildWorldData(BuildObject(true));
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(4), data.Length - 7);
        Assert.ThrowsExactly<InvalidDataException>(() => WorldDataModel.Decode(data));
    }

    [TestMethod]
    public void DecodeRejectsMissingObjectChunks() =>
        Assert.ThrowsExactly<InvalidDataException>(() => WorldDataModel.Decode(BuildWorldData(Chunk("OBJ ", Chunk("ID  ", Value(1UL))))));

    [TestMethod]
    public void DecodeRejectsMoreThanOneTopLevelChunk() =>
        Assert.ThrowsExactly<InvalidDataException>(() => WorldDataModel.Decode((byte[])[.. BuildWorldData(), .. Chunk("OMGS")]));

    [TestMethod]
    public void DecodeRejectsObjectManagersWithoutIds() =>
        Assert.ThrowsExactly<InvalidDataException>(() => WorldDataModel.Decode(Chunk("OMGS", Chunk("OMGR"))));

    [TestMethod]
    public void DecodeRejectsRepeatedObjectChunks() =>
        Assert.ThrowsExactly<InvalidDataException>(() => WorldDataModel.Decode(BuildWorldData(BuildObject(true, Chunk("SCAL", Value(2F))))));

    [TestMethod]
    public void DecodeRejectsUnsupportedChunks() =>
        Assert.ThrowsExactly<InvalidDataException>(() => WorldDataModel.Decode(BuildWorldData(BuildObject(true, Chunk("WHAT", Value(0U))))));

    [TestMethod]
    public void DecodeRejectsValuesOfTheWrongSize() =>
        Assert.ThrowsExactly<InvalidDataException>(() => WorldDataModel.Decode(BuildWorldData(BuildObject(false, Chunk("OPRP", [4])))));

    [TestMethod]
    [DataRow("Llama_Test.world")]
    [DataRow("worlds/LLAMA_TEST.world")]
    public void GetKeyHashesTheFileNameWithoutItsExtension(string worldFileName) =>
        Assert.AreEqual(new ResourceKey(ResourceType.WorldData, 0, 0xf04745da220ca9ab), WorldDataModel.GetKey(worldFileName));

    [TestMethod]
    public void TheGetterOfDataBasePackedFileDecodesTheModel()
    {
        var key = WorldDataModel.GetKey("Llama_Test.world");
        using var dbpf = new DataBasePackedFile();
        dbpf.Set(key, BuildWorldData(BuildObject(true)), CompressionMode.ForceOff);
        Assert.AreEqual(40U, dbpf.GetWorldData(key).ObjectManagers[0].Lots[0].Width);
    }
}
