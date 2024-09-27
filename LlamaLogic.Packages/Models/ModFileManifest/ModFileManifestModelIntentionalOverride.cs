namespace LlamaLogic.Packages.Models.ModFileManifest;

/// <summary>
/// Represents a resource the mod in a mod file manifest <see cref="ResourceType.SnippetTuning"/> resource is overriding intentionally (🔓)
/// </summary>
public sealed class ModFileManifestModelIntentionalOverride :
    IXmlSerializable
{
    /// <summary>
    /// Gets/sets the <see cref="ResourceKey"/> of the resource this mod is overriding
    /// </summary>
    [YamlMember(Order = 1)]
    public ResourceKey Key { get; set; }

    /// <summary>
    /// Gets the hash of the mod file in which the resource is being overridden (optional)
    /// </summary>
    [YamlMember(Order = 6, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public ImmutableArray<byte> Hash { get; set; }

    /// <summary>
    /// Gets/sets the name of the mod in which the resource is being overridden (optional)
    /// </summary>
    [YamlMember(Order = 3, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? ModName { get; set; }

    /// <summary>
    /// Gets/sets the <see cref="ResourceKey"/> of the mod manifest <see cref="ResourceType.SnippetTuning"/> of the mod in which the resource is being overridden (optional)
    /// </summary>
    [YamlMember(Order = 5, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public ResourceKey? ModManifestKey { get; set; }

    /// <summary>
    /// Gets/sets the name of the resource in the mod that is being overridden (optional)
    /// </summary>
    [YamlMember(Order = 2, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets/sets the version of the mod in which the resource is being overridden (optional)
    /// </summary>
    [YamlMember(Order = 4, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public Version? ModVersion { get; set; }

    #region IXmlSerializable

    XmlSchema? IXmlSerializable.GetSchema() =>
        null;

    void IXmlSerializable.ReadXml(XmlReader reader)
    {
        reader.MoveToContent();
        if (reader.NodeType is not XmlNodeType.Element || reader.Name != "U")
            throw new XmlException("Expected a tunable tuple element");
        while (reader.Read() && reader.NodeType is not XmlNodeType.EndElement)
        {
            if (reader.NodeType is not XmlNodeType.Element)
                continue;
            var (tunableName, isList) = reader.ReadTunableDetails();
            if (!isList)
            {
                var tunableValue = reader.ReadElementContentAsString();
                if (tunableName == "hash")
                    Hash = tunableValue.ToByteSequence().ToImmutableArray();
                else if (tunableName == "key")
                    Key = ResourceKey.Parse(tunableValue);
                else if (tunableName == "mod_manifest_key")
                    ModManifestKey = ResourceKey.Parse(tunableValue);
                else if (tunableName == "mod_name")
                    ModName = tunableValue;
                else if (tunableName == "mod_version")
                    ModVersion = Version.Parse(tunableValue);
                else if (tunableName == "name")
                    Name = tunableValue;
            }
        }
        reader.ReadEndElement();
    }

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("U");
        writer.WriteTunable("key", Key);
        writer.WriteTunable("name", Name);
        writer.WriteTunable("mod_name", ModName);
        writer.WriteTunable("mod_version", ModVersion);
        writer.WriteTunable("mod_manifest_key", ModManifestKey);
        writer.WriteTunable("hash", Hash);
        writer.WriteEndElement();
    }

    #endregion IXmlSerializable
}
