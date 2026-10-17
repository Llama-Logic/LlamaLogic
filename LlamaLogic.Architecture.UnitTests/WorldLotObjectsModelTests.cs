namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class WorldLotObjectsModelTests
{
    const string maxisText =
        "lot_objects {\r\n" +
        "  id: 140741783479633\r\n" +
        "  quat_pos {\r\n" +
        "    translation {\r\n" +
        "      x: 1.5\r\n" +
        "      y: -4.371139E-08\r\n" +
        "      z: 1\r\n" +
        "    }\r\n" +
        "    orientation {\r\n" +
        "      x: 0\r\n" +
        "      y: -0.707106769\r\n" +
        "      z: 0\r\n" +
        "      w: 0.707106769\r\n" +
        "    }\r\n" +
        "  }\r\n" +
        "  level: -1\r\n" +
        "  parent_id: 0\r\n" +
        "  scale: 1\r\n" +
        "  object_def_guid64: 17650\r\n" +
        "  geo_state_id: 486196806\r\n" +
        "  attributes: \"a\\000\\\"\\377\"\r\n" +
        "}\r\n";

    [TestMethod]
    public void DecodeReadsEveryField()
    {
        var lotObject = WorldLotObjectsModel.Decode(Encoding.ASCII.GetBytes(maxisText)).LotObjects.LotObjects.Single();
        Assert.AreEqual(140741783479633UL, lotObject.Id);
        Assert.AreEqual(1.5F, lotObject.QuatPos.Translation.X);
        Assert.AreEqual(-4.371139E-08F, lotObject.QuatPos.Translation.Y);
        Assert.AreEqual(-0.707106769F, lotObject.QuatPos.Orientation.Y);
        Assert.AreEqual(-1, lotObject.Level);
        Assert.IsTrue(lotObject.ShouldSerializeParentId());
        Assert.IsFalse(lotObject.ShouldSerializeSlotHash());
        Assert.AreEqual(17650UL, lotObject.ObjectDefGuid64);
        CollectionAssert.AreEqual(new byte[] { (byte)'a', 0, (byte)'"', 255 }, lotObject.Attributes);
    }

    [TestMethod]
    public void DecodeReadsOtherDialects()
    {
        var text = "lot_id: 3,\n    lot_objects: {\n        scale: 2.5f,\n        id: 0x10, # comment\n        multicolor { x: 1 y: 2 z: 3 }\n    }\n";
        var lotObjects = WorldLotObjectsModel.Decode(Encoding.ASCII.GetBytes(text)).LotObjects;
        Assert.AreEqual(3U, lotObjects.LotId);
        Assert.AreEqual(16UL, lotObjects.LotObjects.Single().Id);
        Assert.AreEqual(2.5F, lotObjects.LotObjects.Single().Scale);
        Assert.AreEqual(2F, lotObjects.LotObjects.Single().Multicolors.Single().Y);
    }

    [TestMethod]
    public void DecodeRejectsUnknownFields() =>
        Assert.ThrowsExactly<InvalidDataException>(() => WorldLotObjectsModel.Decode(Encoding.ASCII.GetBytes("lot_objects {\r\n  colour: 1\r\n}\r\n")));

    [TestMethod]
    public void DecodeRejectsUnterminatedMessages() =>
        Assert.ThrowsExactly<InvalidDataException>(() => WorldLotObjectsModel.Decode(Encoding.ASCII.GetBytes("lot_objects {\r\n  id: 1\r\n")));

    [TestMethod]
    public void EncodeKeepsTheDecodedBytesWhileUnchanged()
    {
        var text = Encoding.ASCII.GetBytes("lot_objects { id: 1, scale: 1.0 }");
        CollectionAssert.AreEqual(text, WorldLotObjectsModel.Decode(text).Encode().ToArray());
    }

    [TestMethod]
    public void EncodeWritesAnEmptyListAsNothing() =>
        Assert.AreEqual(0, new WorldLotObjectsModel().Encode().Length);

    [TestMethod]
    [DataRow(0.1F, "0.1")]
    [DataRow(-0F, "0")]
    [DataRow(12.5F, "12.5")]
    [DataRow(1E-08F, "1E-08")]
    [DataRow(0.0001F, "0.0001")]
    [DataRow(1234567F, "1234567")]
    [DataRow(12345678F, "12345678")]
    [DataRow(16777216F, "16777216")]
    [DataRow(1E+10F, "1E+10")]
    [DataRow(2.98023224E-08F, "2.98023224E-08")]
    [DataRow(-4.371139E-08F, "-4.371139E-08")]
    public void EncodeWritesFloatsInTheRoundTripFormat(float value, string expected)
    {
        var lotObjects = new LotObjectList();
        lotObjects.LotObjects.Add(new LotObject { Scale = value });
        var text = Encoding.ASCII.GetString(new WorldLotObjectsModel { LotObjects = lotObjects }.Encode().Span);
        Assert.AreEqual($"lot_objects {{\r\n  scale: {expected}\r\n}}\r\n", text);
    }

    [TestMethod]
    [DataRow(new byte[] { (byte)'\n' }, "\\n")]
    [DataRow(new byte[] { (byte)'\r' }, "\\r")]
    [DataRow(new byte[] { (byte)'\t' }, "\\t")]
    [DataRow(new byte[] { (byte)'"' }, "\\\"")]
    [DataRow(new byte[] { (byte)'\'' }, "\\'")]
    [DataRow(new byte[] { (byte)'\\' }, "\\\\")]
    [DataRow(new byte[] { 0 }, "\\000")]
    [DataRow(new byte[] { 127 }, "\\177")]
    [DataRow(new byte[] { 255 }, "\\377")]
    [DataRow(new byte[] { (byte)'a' }, "a")]
    public void EncodeEscapesBytesLikeTheProtobufTextFormat(byte[] value, string expected)
    {
        var lotObjects = new LotObjectList();
        lotObjects.LotObjects.Add(new LotObject { Attributes = value });
        var text = Encoding.ASCII.GetString(new WorldLotObjectsModel { LotObjects = lotObjects }.Encode().Span);
        Assert.AreEqual($"lot_objects {{\r\n  attributes: \"{expected}\"\r\n}}\r\n", text);
    }

    [TestMethod]
    public void EncodeWritesTheMaxisDialect()
    {
        var decoded = WorldLotObjectsModel.Decode(Encoding.ASCII.GetBytes(maxisText));
        var model = new WorldLotObjectsModel { LotObjects = decoded.LotObjects };
        Assert.AreEqual(maxisText, Encoding.ASCII.GetString(model.Encode().Span));
    }

    [TestMethod]
    public void GetKeyUsesTheLotTemplateInstance() =>
        Assert.AreEqual(new ResourceKey(ResourceType.WorldLotObjects, 0, 0xf3427520489192df), WorldLotObjectsModel.GetKey(0x0000abcd));

    [TestMethod]
    [DataRow("nan", float.NaN, "NaN")]
    [DataRow("inf", float.PositiveInfinity, "Infinity")]
    [DataRow("-Infinity", float.NegativeInfinity, "-Infinity")]
    [DataRow("0x10", 16F, "16")]
    [DataRow("0x1F", 31F, "31")]
    [DataRow("2.5f", 2.5F, "2.5")]
    [DataRow("-0x8", -8F, "-8")]
    public void SpecialFloatsRoundTrip(string text, float value, string expected)
    {
        var decoded = WorldLotObjectsModel.Decode(Encoding.ASCII.GetBytes($"lot_objects {{\r\n  scale: {text}\r\n}}\r\n"));
        Assert.AreEqual(value, decoded.LotObjects.LotObjects.Single().Scale);
        var encoded = new WorldLotObjectsModel { LotObjects = decoded.LotObjects }.Encode();
        Assert.AreEqual($"lot_objects {{\r\n  scale: {expected}\r\n}}\r\n", Encoding.ASCII.GetString(encoded.Span));
        Assert.AreEqual(value, WorldLotObjectsModel.Decode(encoded).LotObjects.LotObjects.Single().Scale);
    }
}
