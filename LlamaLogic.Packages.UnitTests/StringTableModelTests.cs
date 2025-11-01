namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class StringTableModelTests :
    TestPackageTests
{
    [TestMethod]
    public void StringTableManipulation()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        var stringTable = dbpf.GetStringTable("220557da:80000000:002100b7f5cfd646");
        Assert.AreEqual("TSHI", stringTable[0xd4fc0137]);
        Assert.AreEqual("Disappointed in the cooking of the duck meat.", stringTable[0xdb5ac988]);
        var howBadKey = stringTable.AddNew("How bad could it really be?");
        dbpf.Set("220557da:80000000:002100b7f5cfd646", stringTable);
        stringTable = dbpf.GetStringTable("220557da:80000000:002100b7f5cfd646");
        Assert.AreEqual("How bad could it really be?", stringTable[howBadKey]);
        Assert.ThrowsExactly<ArgumentException>(() => stringTable.AddNew("How bad could it really be?"));
    }

    [TestMethod]
    public void StringTableKeyUtilities()
    {
        var englishKey = ResourceKey.Parse("220557da:80000000:002100b7f5cfd646");
        var english = englishKey.GetStringTableLocale();
        Assert.AreEqual("English (United States)", english.EnglishName);
        var polishKey = englishKey.GetResourceKeyForLocale(new CultureInfo("pl"));
        Assert.AreEqual(0x0f, (byte)((polishKey.FullInstance & 0xFF00000000000000) >> 56));
        var polish = polishKey.GetStringTableLocale();
        Assert.AreEqual("Polish", polish.EnglishName);
    }
}
