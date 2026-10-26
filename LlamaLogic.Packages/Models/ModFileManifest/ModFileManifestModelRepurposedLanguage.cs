namespace LlamaLogic.Packages.Models.ModFileManifest;

/// <summary>
/// Represents a repurposed language in a mod file manifest <see cref="ResourceType.SnippetTuning"/> resource (🔓)
/// </summary>
public sealed class ModFileManifestModelRepurposedLanguage :
    IXmlSerializable
{
    /// <summary>
    /// Gets/sets the IETF BCP 47 language tag identifying the game-supported language this mod is repurposing
    /// </summary>
    /// <remarks>
    /// The IETF BCP 47 language tags currently supported by The Sims 4 at time of writing are:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Tag</term>
    ///         <description>Description</description>
    ///     </listheader>
    ///     <item>
    ///         <term>en-US</term>
    ///         <description>American English | English (United States) | Full instance prefix: <c>0x00</c></description>
    ///     </item>
    ///     <item>
    ///         <term>zh-CN</term>
    ///         <description>简体中文 | Chinese (Simplified, Mainland China) | Full instance prefix: <c>0x01</c></description>
    ///     </item>
    ///     <item>
    ///         <term>zh-TW</term>
    ///         <description>繁體中文 | Chinese (Traditional, Taiwan) | Full instance prefix: <c>0x02</c></description>
    ///     </item>
    ///     <item>
    ///         <term>cz</term>
    ///         <description>Čeština | Czech | Full instance prefix: <c>0x03</c></description>
    ///     </item>
    ///     <item>
    ///         <term>da</term>
    ///         <description>Dansk | Danish | Full instance prefix: <c>0x04</c></description>
    ///     </item>
    ///     <item>
    ///         <term>nl</term>
    ///         <description>Nederlands | Dutch | Full instance prefix: <c>0x05</c></description>
    ///     </item>
    ///     <item>
    ///         <term>fi</term>
    ///         <description>Suomi | Finnish | Full instance prefix: <c>0x06</c></description>
    ///     </item>
    ///     <item>
    ///         <term>fr-FR</term>
    ///         <description>Français | French (France) | Full instance prefix: <c>0x07</c></description>
    ///     </item>
    ///     <item>
    ///         <term>de</term>
    ///         <description>Deutsch | German | Full instance prefix: <c>0x08</c></description>
    ///     </item>
    ///     <item>
    ///         <term>it</term>
    ///         <description>Italiano | Italian | Full instance prefix: <c>0x0b</c></description>
    ///     </item>
    ///     <item>
    ///         <term>ja</term>
    ///         <description>日本語 | Japanese | Full instance prefix: <c>0x0c</c></description>
    ///     </item>
    ///     <item>
    ///         <term>ko</term>
    ///         <description>한국어 | Korean | Full instance prefix: <c>0x0d</c></description>
    ///     </item>
    ///     <item>
    ///         <term>nb</term>
    ///         <description>Norsk | Norwegian | Full instance prefix: <c>0x0e</c></description>
    ///     </item>
    ///     <item>
    ///         <term>pl</term>
    ///         <description>Polski | Polish | Full instance prefix: <c>0x0f</c></description>
    ///     </item>
    ///     <item>
    ///         <term>pt-BR</term>
    ///         <description>Português | Portuguese (Brazil) | Full instance prefix: <c>0x11</c></description>
    ///     </item>
    ///     <item>
    ///         <term>ru</term>
    ///         <description>Русский | Russian | Full instance prefix: <c>0x12</c></description>
    ///     </item>
    ///     <item>
    ///         <term>es-ES</term>
    ///         <description>Español | Spanish (Spain) | Full instance prefix: <c>0x13</c></description>
    ///     </item>
    ///     <item>
    ///         <term>sv</term>
    ///         <description>Svenska | Swedish | Full instance prefix: <c>0x15</c></description>
    ///     </item>
    /// </list>
    /// </remarks>
    [YamlMember(Order = 1)]
    public string GameLocale { get; set; } = string.Empty;

    /// <summary>
    /// Gets/sets the IETF BCP 47 language tag identifying the non-game-supported language this mod actually contains in the domain ordinarily reserved to <see cref="GameLocale"/>
    /// </summary>
    [YamlMember(Order = 2)]
    public string ActualLocale { get; set; } = string.Empty;

    #region IXmlSerializable

    XmlSchema? IXmlSerializable.GetSchema() =>
        null;

    void IXmlSerializable.ReadXml(XmlReader reader)
    {
        reader.MoveToContent();
        if (reader.NodeType is not XmlNodeType.Element || reader.Name != "U")
            throw new XmlException("Expected a tunable tuple element");
        var element = XElement.Load(reader.ReadSubtree());
        foreach (var child in element.Elements())
        {
            var (tunableName, isList) = child.ReadTunableDetails();
            if (!isList)
            {
                var tunableValue = child.Value;
                if (tunableName == "game_locale")
                    GameLocale = tunableValue;
                else if (tunableName == "actual_locale")
                    ActualLocale = tunableValue;
            }
        }
    }

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("U");
        writer.WriteTunable("game_locale", GameLocale);
        writer.WriteTunable("actual_locale", ActualLocale);
        writer.WriteEndElement();
    }

    #endregion IXmlSerializable
}
