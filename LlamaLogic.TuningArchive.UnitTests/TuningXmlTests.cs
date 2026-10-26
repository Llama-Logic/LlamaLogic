namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningXmlTests
{
    static string Normalize(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal);

    [TestMethod]
    public void CanonicalTextIsTheSameHoweverTheTuningWasFormatted()
    {
        var tidy = TuningXml.GetCanonicalText(TuningXml.Parse(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <I c="Buff" i="buff" m="buffs.buff" n="Buff_Happy" s="200">
              <T n="mood_weight">1</T>
            </I>
            """));
        var messy = TuningXml.GetCanonicalText(TuningXml.Parse("<?xml version=\"1.0\"?><!-- hi --><I s=\"200\" n=\"Buff_Happy\" m=\"buffs.buff\" i=\"buff\" c=\"Buff\">\t<T n=\"mood_weight\">1</T>\r\n<!-- bye --></I>"));
        Assert.AreEqual(tidy, messy);
    }

    [TestMethod]
    public void CanonicalTextOrdersAttributesAndIndentsElements()
    {
        var document = XDocument.Parse(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <!-- A comment -->
            <I s="5" n="Thing" i="snippet" m="snippets" c="Snippet"><V t="enabled" n="option"><T n="value">1</T></V><L n="items"><T>2</T><!-- Another comment --><T>3</T></L><T n="empty" /></I>
            """);
        Assert.AreEqual
        (
            Normalize(
                """
                <?xml version="1.0" encoding="utf-8"?>
                <I c="Snippet" i="snippet" m="snippets" n="Thing" s="5">
                  <V n="option" t="enabled">
                    <T n="value">1</T>
                  </V>
                  <L n="items">
                    <T>2</T>
                    <T>3</T>
                  </L>
                  <T n="empty" />
                </I>
                """),
            TuningXml.GetCanonicalText(document)
        );
    }

    [TestMethod]
    [DataRow("buff", ResourceType.BuffTuning)]
    [DataRow("mood", ResourceType.MoodTuning)]
    [DataRow("snippet", ResourceType.SnippetTuning)]
    [DataRow("trait", ResourceType.TraitTuning)]
    [DataRow("notebook_entry", ResourceType.NotebookEntryTuning)]
    [DataRow("relbit", ResourceType.RelationshipBitTuning)]
    [DataRow("tun", ResourceType.Tuning)]
    [DataRow("module", ResourceType.Tuning)]
    public void ResourceTypesAreDerivedTheWayTheGameDerivesThem(string typeName, ResourceType expected) =>
        Assert.AreEqual(expected, TuningXml.GetResourceType(typeName));

    [TestMethod]
    public void ResourceTypeNamesAreCaseSensitive() =>
        Assert.AreNotEqual(TuningXml.GetResourceType("buff"), TuningXml.GetResourceType("Buff"));
}