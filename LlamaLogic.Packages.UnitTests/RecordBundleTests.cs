namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class RecordBundleTests
{
    static readonly ResourceKey firstKey = new((ResourceType)0x00000002, 0, 0x1111111111111111);
    static readonly ResourceKey secondKey = new((ResourceType)0x00000001, 0, 0x2222222222222222);
    static readonly ResourceKey thirdKey = new((ResourceType)0x00000001, 0, 0x0000000000000003);

    static RecordBundle BuildBundle()
    {
        var bundle = new RecordBundle();
        bundle.Set(firstKey, "first"u8.ToArray());
        bundle.Set(secondKey, "second"u8.ToArray());
        bundle.Set(thirdKey, "third"u8.ToArray());
        return bundle;
    }

    [TestMethod]
    public void DecodeReadsWhatEncodeWrites()
    {
        var data = BuildBundle().Encode();
        var bundle = RecordBundle.Decode(data);
        Assert.AreEqual(3, bundle.Count);
        Assert.AreEqual("first", Encoding.ASCII.GetString(bundle.Get(firstKey).Span));
        Assert.AreEqual("second", Encoding.ASCII.GetString(bundle.Get(secondKey).Span));
        Assert.AreEqual("third", Encoding.ASCII.GetString(bundle.Get(thirdKey).Span));
        Assert.IsTrue(data.Span.SequenceEqual(bundle.Encode().Span));
    }

    [TestMethod]
    public void DecodeRejectsBigEndianBundles()
    {
        var data = BuildBundle().Encode().ToArray();
        "BNDL"u8.CopyTo(data);
        Assert.ThrowsExactly<InvalidDataException>(() => RecordBundle.Decode(data));
    }

    [TestMethod]
    public void DecodeRejectsDuplicateKeys()
    {
        var data = BuildBundle().Encode().ToArray();
        var instancesOffset = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(20));
        BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(instancesOffset + 8), BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(instancesOffset)));
        Assert.ThrowsExactly<InvalidDataException>(() => RecordBundle.Decode(data));
    }

    [TestMethod]
    public void DecodeRejectsGroupsThatDoNotCoverEveryRecord()
    {
        var data = BuildBundle().Encode().ToArray();
        var groupsOffset = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(36));
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(groupsOffset + 24 + 8), 2);
        Assert.ThrowsExactly<InvalidDataException>(() => RecordBundle.Decode(data));
    }

    [TestMethod]
    public void DecodeRejectsOtherData() =>
        Assert.ThrowsExactly<InvalidDataException>(() => RecordBundle.Decode("DATA and then enough more bytes to fill a header"u8.ToArray()));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => RecordBundle.Decode((byte[])[.. BuildBundle().Encode().ToArray(), 0]));

    [TestMethod]
    public void DecodeRejectsTruncatedBundles() =>
        Assert.ThrowsExactly<InvalidDataException>(() => RecordBundle.Decode(BuildBundle().Encode()[..^1]));

    [TestMethod]
    [DataRow(2U)]
    [DataRow(4U)]
    public void DecodeRejectsUnsupportedVersions(uint version)
    {
        var data = BuildBundle().Encode().ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), version);
        Assert.ThrowsExactly<InvalidDataException>(() => RecordBundle.Decode(data));
    }

    [TestMethod]
    public void EncodeLaysOutAnEmptyBundle()
    {
        var data = new RecordBundle().Encode().Span;
        Assert.AreEqual(55, data.Length);
        Assert.IsTrue(data[..4].SequenceEqual("LDNB"u8));
        Assert.AreEqual(3U, BinaryPrimitives.ReadUInt32LittleEndian(data[4..]));
        Assert.AreEqual(0, BinaryPrimitives.ReadInt32LittleEndian(data[16..]));
        Assert.AreEqual(40, BinaryPrimitives.ReadInt32LittleEndian(data[20..]));
        Assert.AreEqual(52, BinaryPrimitives.ReadInt32LittleEndian(data[24..]));
        Assert.AreEqual(40, BinaryPrimitives.ReadInt32LittleEndian(data[28..]));
        Assert.AreEqual(0, BinaryPrimitives.ReadInt32LittleEndian(data[32..]));
        Assert.AreEqual(40, BinaryPrimitives.ReadInt32LittleEndian(data[36..]));
    }

    [TestMethod]
    public void EncodeLaysOutRecords()
    {
        var data = BuildBundle().Encode().Span;
        Assert.IsTrue(data[40..56].SequenceEqual("secondthirdfirst"u8));
        Assert.AreEqual(56, BinaryPrimitives.ReadInt32LittleEndian(data[20..]));
        Assert.AreEqual(2, BinaryPrimitives.ReadInt32LittleEndian(data[32..]));
        var groupsOffset = BinaryPrimitives.ReadInt32LittleEndian(data[36..]);
        Assert.AreEqual(80, groupsOffset);
        Assert.AreEqual(1U, BinaryPrimitives.ReadUInt32LittleEndian(data[groupsOffset..]));
        Assert.AreEqual(0, BinaryPrimitives.ReadInt32LittleEndian(data[(groupsOffset + 8)..]));
        Assert.AreEqual(2U, BinaryPrimitives.ReadUInt32LittleEndian(data[(groupsOffset + 12)..]));
        Assert.AreEqual(2, BinaryPrimitives.ReadInt32LittleEndian(data[(groupsOffset + 20)..]));
        Assert.AreEqual(3, BinaryPrimitives.ReadInt32LittleEndian(data[(groupsOffset + 32)..]));
        var dataOffsetsOffset = BinaryPrimitives.ReadInt32LittleEndian(data[24..]);
        Assert.AreEqual(116, dataOffsetsOffset);
        Assert.IsTrue(data[dataOffsetsOffset..].SequenceEqual((byte[])[0, 0, 0, 0, 0, 6, 0, 0, 11, 0, 0, 16]));
    }

    [TestMethod]
    public void EncodeRejectsMoreDataThanTheFormatAddresses()
    {
        var bundle = new RecordBundle();
        bundle.Set(firstKey, new byte[0x1000000]);
        Assert.ThrowsExactly<InvalidOperationException>(() => bundle.Encode());
    }

    [TestMethod]
    public void GetRejectsMissingKeys() =>
        Assert.ThrowsExactly<KeyNotFoundException>(() => new RecordBundle().Get(firstKey));

    [TestMethod]
    public void RecordsAreOrderedByTypeAndGroupKeepingTheirOrderWithinAGroup() =>
        CollectionAssert.AreEqual(new[] { secondKey, thirdKey, firstKey }, BuildBundle().Records.Select(record => record.Key).ToArray());

    [TestMethod]
    public void RemoveRemovesOnlyTheRecordWithTheKey()
    {
        var bundle = BuildBundle();
        Assert.IsTrue(bundle.Remove(secondKey));
        Assert.IsFalse(bundle.Remove(secondKey));
        Assert.AreEqual(2, bundle.Count);
        Assert.IsFalse(bundle.TryGet(secondKey, out _));
    }

    [TestMethod]
    public void ReservedValuesRoundTrip()
    {
        var bundle = BuildBundle();
        bundle.Reserved1 = 0x01020304;
        bundle.Reserved2 = 0x05060708;
        var decoded = RecordBundle.Decode(bundle.Encode());
        Assert.AreEqual(0x01020304U, decoded.Reserved1);
        Assert.AreEqual(0x05060708U, decoded.Reserved2);
    }

    [TestMethod]
    public void SetReplacesTheContentOfAnExistingRecord()
    {
        var bundle = BuildBundle();
        bundle.Set(secondKey, "replaced"u8.ToArray());
        Assert.AreEqual(3, bundle.Count);
        Assert.AreEqual("replaced", Encoding.ASCII.GetString(RecordBundle.Decode(bundle.Encode()).Get(secondKey).Span));
    }
}
