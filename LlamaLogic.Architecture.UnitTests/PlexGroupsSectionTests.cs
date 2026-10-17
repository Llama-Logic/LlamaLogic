namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class PlexGroupsSectionTests
{
    static byte[] Section(uint version = 1, ushort groupVersion = 1) =>
        new TestBytes().U32(version).U16(2)
            .U16(groupVersion).U32(0).U8(2).U16(3).U16(4).U16(9).U16(12)
            .U16(groupVersion).U32(1).U8(0).U16(0)
            .ToArray();

    [TestMethod]
    public void DecodeAndEncodeRoundTrip()
    {
        var data = Section();
        var architecture = TestBundles.Decode((LotArchitectureSectionType.PlexGroups, data));
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.PlexGroups));
    }

    [TestMethod]
    [DataRow((ushort)0)]
    [DataRow((ushort)2)]
    public void DecodeKeepsUnsupportedGroupVersionsUndecoded(ushort groupVersion) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.PlexGroups, Section(groupVersion: groupVersion), architecture => architecture.PlexGroups);

    [TestMethod]
    [DataRow(0U)]
    [DataRow(2U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.PlexGroups, Section(version), architecture => architecture.PlexGroups);

    [TestMethod]
    public void DecodeReadsFields()
    {
        var groups = TestBundles.Decode((LotArchitectureSectionType.PlexGroups, Section())).PlexGroups!.Groups;
        Assert.AreEqual(2, groups.Count);
        Assert.AreEqual(0U, groups[0].Id);
        Assert.AreEqual((byte)2, groups[0].Unknown1);
        CollectionAssert.AreEqual(new ushort[] { 4, 9, 12 }, groups[0].Values.ToArray());
        Assert.AreEqual(1U, groups[1].Id);
        Assert.AreEqual(0, groups[1].Values.Count);
    }

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.PlexGroups, [.. Section(), 0])));

    [TestMethod]
    public void EncodeRejectsTooManyValues()
    {
        var section = new PlexGroupsSection();
        var group = new PlexGroup();
        for (var i = 0; i <= ushort.MaxValue; ++i)
            group.Values.Add(0);
        section.Groups.Add(group);
        Assert.ThrowsExactly<InvalidOperationException>(() => TestBundles.Encode(new LotArchitecture { PlexGroups = section }, LotArchitectureSectionType.PlexGroups));
    }
}
