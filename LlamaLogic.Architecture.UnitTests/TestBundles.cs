namespace LlamaLogic.Architecture.UnitTests;

static class TestBundles
{
    public static void AssertUndecoded(LotArchitectureSectionType type, byte[] data, Func<LotArchitecture, LotArchitectureSection?> section, params (LotArchitectureSectionType type, byte[] data)[] others)
    {
        var architecture = Decode([.. others, (type, data)]);
        Assert.IsNull(section(architecture));
        var key = new ResourceKey((ResourceType)(uint)type, 0, LotArchitecture.SectionInstance);
        CollectionAssert.AreEqual(data, architecture.UndecodedRecords.Single(record => record.Key.Equals(key)).Data.ToArray());
        CollectionAssert.AreEqual(data, RecordBundle.Decode(architecture.Encode()).Get(key).ToArray());
    }

    public static LotArchitecture Decode(params (LotArchitectureSectionType type, byte[] data)[] records)
    {
        var bundle = new RecordBundle();
        foreach (var (type, data) in records)
            bundle.Set(new ResourceKey((ResourceType)(uint)type, 0, LotArchitecture.SectionInstance), data);
        return LotArchitecture.Decode(bundle.Encode());
    }

    public static byte[] Encode(LotArchitecture architecture, LotArchitectureSectionType type) =>
        architecture.ToRecordBundle().Get(new ResourceKey((ResourceType)(uint)type, 0, LotArchitecture.SectionInstance)).ToArray();

    public static byte[] LotSize(byte sizeX, byte sizeZ, byte levelCount, uint version = 13) =>
        [.. BitConverter.GetBytes(version), sizeX, sizeZ, levelCount];
}
