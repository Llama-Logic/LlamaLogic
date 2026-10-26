namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class VenueSectionTests
{
    static byte[] Build(uint version, bool entry = true, bool householdAndFlag = true)
    {
        var data = new List<byte>();
        data.AddRange(BitConverter.GetBytes(version));
        data.AddRange(BitConverter.GetBytes(0x6FC6UL));
        data.AddRange([1, 0]);
        if (version >= 4)
        {
            data.AddRange(BitConverter.GetBytes(entry ? 1 : 0));
            if (entry)
            {
                data.AddRange(BitConverter.GetBytes(0x1FE99UL));
                data.AddRange(BitConverter.GetBytes(0x1FE99UL));
                data.AddRange(BitConverter.GetBytes(3));
                data.AddRange([0x08, 0x01, 0x10]);
            }
        }
        if (version >= 5)
            data.AddRange(BitConverter.GetBytes(householdAndFlag ? 77U : 0U));
        if (version >= 6)
            data.AddRange(BitConverter.GetBytes(householdAndFlag ? 99UL : 0UL));
        return [.. data];
    }

    [TestMethod]
    [DataRow(3U)]
    [DataRow(4U)]
    [DataRow(5U)]
    [DataRow(6U)]
    public void DecodeAndEncodeRoundTrip(uint version)
    {
        var data = Build(version);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Venue, data));
        var section = architecture.Venue!;
        Assert.AreEqual(version, section.Version);
        Assert.AreEqual(0x6FC6UL, section.VenueTuning);
        Assert.IsTrue(section.Unknown1);
        Assert.IsFalse(section.Unknown2);
        Assert.AreEqual(version >= 4 ? 1 : 0, section.BusinessInfos.Count);
        Assert.AreEqual(version >= 5 ? 77U : 0U, section.Unknown3);
        Assert.AreEqual(version >= 6 ? 99UL : 0UL, section.HouseholdId);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.Venue));
    }

    [TestMethod]
    public void DecodeAndEncodeWithoutEntriesRoundTrip()
    {
        var data = Build(6, false, false);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Venue, data));
        Assert.AreEqual(0, architecture.Venue!.BusinessInfos.Count);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.Venue));
    }

    [TestMethod]
    public void DecodeBusinessInfo()
    {
        var info = TestBundles.Decode((LotArchitectureSectionType.Venue, Build(6))).Venue!.BusinessInfos.Single();
        Assert.AreEqual(0x1FE99UL, info.Key);
        Assert.AreEqual(0x1FE99UL, info.VenueTuning);
        CollectionAssert.AreEqual(new byte[] { 0x08, 0x01, 0x10 }, info.Payload.ToArray());
    }

    [TestMethod]
    [DataRow(2U)]
    [DataRow(7U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.Venue, Build(version), architecture => architecture.Venue);

    [TestMethod]
    public void DecodeRejectsFlagsThatAreNotBooleans()
    {
        var data = Build(3);
        data[12] = 2;
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Venue, data)));
    }

    [TestMethod]
    public void DecodeRejectsPayloadLengthBeyondTheData()
    {
        var data = Build(4);
        data[34] = 100;
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Venue, data)));
    }

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Venue, [.. Build(6), 0])));

    [TestMethod]
    [DataRow(3U)]
    [DataRow(4U)]
    [DataRow(5U)]
    public void EncodeRejectsFieldsTheVersionCannotStore(uint version)
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Venue, Build(6)));
        architecture.Venue!.Version = version;
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(architecture, LotArchitectureSectionType.Venue));
    }

    [TestMethod]
    public void EncodeWritesAnEarlierVersionWhenTheLaterFieldsHoldDefaults()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Venue, Build(6, false, false)));
        architecture.Venue!.Version = 3;
        CollectionAssert.AreEqual(Build(3), TestBundles.Encode(architecture, LotArchitectureSectionType.Venue));
    }

    [TestMethod]
    [DataRow(2U)]
    [DataRow(7U)]
    public void VersionRejectsUnsupportedValues(uint version) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new VenueSection { Version = version });
}
