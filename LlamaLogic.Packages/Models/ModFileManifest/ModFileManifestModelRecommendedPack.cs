namespace LlamaLogic.Packages.Models.ModFileManifest;

/// <summary>
/// Represents a recommended mod in a mod file manifest <see cref="ResourceType.SnippetTuning"/> resource (🔓)
/// </summary>
public sealed class ModFileManifestModelRecommendedPack :
    IXmlSerializable
{
    /// <summary>
    /// Gets/sets the pack code indicating the pack that is recommended (e.g. "EP01" for Get to Work)
    /// </summary>
    [YamlMember(Order = 1)]
    public string PackCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets/sets the reason for the pack recommendation
    /// </summary>
    [YamlMember(Order = 2)]
    public string Reason { get; set; } = string.Empty;

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
                if (tunableName == "pack_code")
                    PackCode = tunableValue;
                else if (tunableName == "reason")
                    Reason = tunableValue;
            }
        }
    }

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("U");
        writer.WriteTunable("pack_code", PackCode);
        writer.WriteTunable("reason", Reason);
        writer.WriteEndElement();
    }

    #endregion IXmlSerializable
}
