namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class ZoneArchitectureTests
{
    static byte[] BuildArchitecture() =>
        TestBundles.Decode((LotArchitectureSectionType.LotSize, TestBundles.LotSize(20, 15, 1))).Encode().ToArray();

    static byte[] BuildZoneObjectData(byte[] firstArchitecture, byte[] secondArchitecture) =>
    [
        0x09, 1, 2, 3, 4, 5, 6, 7, 8,
        0x12, .. Varint(firstArchitecture.Length), .. firstArchitecture,
        0x20, 0x00,
        0x12, .. Varint(secondArchitecture.Length), .. secondArchitecture,
        0x30, 0x01
    ];

    static byte[] Varint(int value)
    {
        var result = new List<byte>();
        var remaining = (uint)value;
        while (remaining >= 0x80)
        {
            result.Add((byte)(remaining | 0x80));
            remaining >>= 7;
        }
        result.Add((byte)remaining);
        return [.. result];
    }

    [TestMethod]
    public void GetArchitectureDataRejectsAnotherWireTypeForTheField() =>
        Assert.ThrowsExactly<InvalidDataException>(() => ZoneArchitecture.GetArchitectureData(new byte[] { 0x10, 0x01 }));

    [TestMethod]
    public void GetArchitectureDataRejectsTruncatedMessages()
    {
        var message = BuildZoneObjectData(BuildArchitecture(), [1, 2, 3]);
        Assert.ThrowsExactly<InvalidDataException>(() => ZoneArchitecture.GetArchitectureData(message[..^4]));
    }

    [TestMethod]
    public void GetArchitectureDataReturnsTheEntries()
    {
        var first = BuildArchitecture();
        var data = ZoneArchitecture.GetArchitectureData(BuildZoneObjectData(first, [1, 2, 3]));
        Assert.AreEqual(2, data.Count);
        CollectionAssert.AreEqual(first, data[0].ToArray());
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, data[1].ToArray());
    }

    [TestMethod]
    public void GetArchitectureDecodesEntriesAndSkipsEmptyOnes()
    {
        var architecture = ZoneArchitecture.GetArchitecture(BuildZoneObjectData(BuildArchitecture(), []));
        Assert.AreEqual(2, architecture.Count);
        Assert.AreEqual((byte)15, architecture[0]!.LotSize!.SizeZ);
        Assert.IsNull(architecture[1]);
    }

    [TestMethod]
    public void SetArchitectureDataKeepsEverythingElse()
    {
        var message = BuildZoneObjectData([9, 9], [1, 2, 3]);
        var replacement = new byte[200];
        replacement[0] = 42;
        var result = ZoneArchitecture.SetArchitectureData(message, [replacement, new byte[] { 1, 2, 3 }]).ToArray();
        CollectionAssert.AreEqual(BuildZoneObjectData(replacement, [1, 2, 3]), result);
    }

    [TestMethod]
    public void SetArchitectureDataRejectsAnotherNumberOfEntries() =>
        Assert.ThrowsExactly<ArgumentException>(() => ZoneArchitecture.SetArchitectureData(BuildZoneObjectData([9], [8]), [new byte[] { 1 }]));

    [TestMethod]
    public void SetArchitectureReproducesTheMessage()
    {
        var message = BuildZoneObjectData(BuildArchitecture(), []);
        var architecture = ZoneArchitecture.GetArchitecture(message);
        CollectionAssert.AreEqual(message, ZoneArchitecture.SetArchitecture(message, architecture).ToArray());
    }
}
