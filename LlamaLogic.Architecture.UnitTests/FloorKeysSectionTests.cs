namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class FloorKeysSectionTests
{
    static byte[] Record(uint count, params (ulong instance, uint id, uint referenceCount)[] entries)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(new byte[16]);
        writer.Write(count);
        foreach (var (instance, id, referenceCount) in entries)
        {
            writer.Write(instance);
            writer.Write(0xb4f762c9U);
            writer.Write(0x80000000U);
            writer.Write(id);
            writer.Write(referenceCount);
        }
        writer.Flush();
        return stream.ToArray();
    }

    [TestMethod]
    public void DecodeAndEncodeRoundTrip()
    {
        var data = Record(2, (0x1122334455667788, 3, 64), (0x99, 1, 4));
        var architecture = TestBundles.Decode((LotArchitectureSectionType.FloorKeys, data));
        var section = architecture.FloorKeys!;
        Assert.AreEqual(ResourceKey.Empty, section.DefaultKey);
        Assert.AreEqual(2, section.Entries.Count);
        Assert.AreEqual(new ResourceKey((ResourceType)0xb4f762c9, 0x80000000, 0x1122334455667788), section.Entries[0].Key);
        Assert.AreEqual(3U, section.Entries[0].Id);
        Assert.AreEqual(64U, section.Entries[0].ReferenceCount);
        Assert.AreEqual(1U, section.Entries[1].Id);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.FloorKeys));
    }

    [TestMethod]
    public void DecodeRejectsCountBeyondData() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.FloorKeys, Record(2, (1, 1, 1)))));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.FloorKeys, [.. Record(1, (1, 1, 1)), 0])));

    [TestMethod]
    public void ManifestKeysIncludeDefaultKeyFirst()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.FloorKeys, Record(2, (5, 1, 1), (6, 2, 1))));
        CollectionAssert.AreEqual(new[] { ResourceKey.Empty, new ResourceKey((ResourceType)0xb4f762c9, 0x80000000, 5), new ResourceKey((ResourceType)0xb4f762c9, 0x80000000, 6) }, architecture.GetManifestKeys().ToArray());
    }
}
