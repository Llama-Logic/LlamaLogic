namespace LlamaLogic.Packages.Models.ModFileManifest;

/// <summary>
/// Represents the contribution of a translator in a mod file manifest <see cref="ResourceType.SnippetTuning"/> resource (🔓)
/// </summary>
public sealed class ModFileManifestModelTranslator :
    IXmlSerializable
{
    /// <summary>
    /// Gets/sets the value of the <see cref="Language"/> property using a <see cref="CultureInfo"/> object
    /// </summary>
    [YamlIgnore]
    public CultureInfo? Culture
    {
        get
        {
            try
            {
                return CultureInfo.GetCultureInfo(Language);
            }
            catch (CultureNotFoundException)
            {
                return null;
            }
        }
        set => Language = value?.Name ?? string.Empty;
    }

    /// <summary>
    /// Gets/sets the IETF BCP 47 language tag identifying the language to which this translator contributed
    /// </summary>
    [YamlMember(Order = 2)]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Gets/sets the name of the translator
    /// </summary>
    [YamlMember(Order = 1)]
    public string Name { get; set; } = string.Empty;

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
                if (tunableName == "language")
                    Language = tunableValue;
                else if (tunableName == "name")
                    Name = tunableValue;
            }
        }
    }

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("U");
        writer.WriteTunable("name", Name);
        writer.WriteTunable("language", Language);
        writer.WriteEndElement();
    }

    #endregion IXmlSerializable
}
