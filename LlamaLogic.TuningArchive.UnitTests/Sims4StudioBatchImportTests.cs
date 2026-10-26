namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class Sims4StudioBatchImportTests
{
    static ResourceKey ParseLikeSims4Studio(string fileName)
    {
        var split = fileName.Split('.')[0].Split('!');
        return new(
            (ResourceType)Convert.ToUInt32(split[0], 16),
            Convert.ToUInt32(split[1], 16),
            Convert.ToUInt64(split[2], 16));
    }

    [TestMethod]
    public void FileNameStartsWithTheKeyThenTheNameAndType()
    {
        var key = new ResourceKey(ResourceType.BuffTuning, 0, 0x12345);
        var fileName = Sims4StudioBatchImport.GetFileName(key, "buff_Example");
        Assert.AreEqual("6017E896!00000000!0000000000012345.buff_Example.BuffTuning.xml", fileName);
        Assert.AreEqual(key, ParseLikeSims4Studio(fileName));
    }

    [TestMethod]
    public void FileNameReplacesCharactersWindowsDoesNotAllow()
    {
        var key = new ResourceKey(ResourceType.SnippetTuning, 0, 0x8000000000000001);
        var fileName = Sims4StudioBatchImport.GetFileName(key, "creator:mod/one?");
        Assert.AreEqual("7DF2169C!00000000!8000000000000001.creator_mod_one_.SnippetTuning.xml", fileName);
        Assert.AreEqual(key, ParseLikeSims4Studio(fileName));
    }

    [TestMethod]
    public void FileNameKeepsTheKeyWhenTheNameHasPeriods()
    {
        var key = new ResourceKey(ResourceType.BuffTuning, 0, 7);
        Assert.AreEqual(key, ParseLikeSims4Studio(Sims4StudioBatchImport.GetFileName(key, "buff.with.periods")));
    }

    [TestMethod]
    public void FileNameOfATuningComesFromItsRootElement()
    {
        var fileName = Sims4StudioBatchImport.GetFileName("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<I c=\"XmlInjector\" i=\"snippet\" m=\"xml_injector.snippet\" n=\"creator:mod_xml_injector\" s=\"17293822569102704640\" />");
        Assert.AreEqual("7DF2169C!00000000!F000000000000000.creator_mod_xml_injector.SnippetTuning.xml", fileName);
    }

    [TestMethod]
    public void FileNameOfATuningWithoutAnInstanceThrows() =>
        Assert.ThrowsExactly<ArgumentException>(() => Sims4StudioBatchImport.GetFileName("<I i=\"snippet\" n=\"x\" />"));
}