namespace LlamaLogic.Packages.Models.ModFileManifest;

/// <summary>
/// Represents a required mod in a mod file manifest <see cref="ResourceType.SnippetTuning"/> resource (🔓)
/// </summary>
public sealed class ModFileManifestModelRequiredMod :
    IXmlSerializable
{
    /// <summary>
    /// Gets the names of the creators of the dependency mod
    /// </summary>
    [YamlMember(Order = 2, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<string> Creators { get; private set; } = [];

    /// <summary>
    /// Gets the hashes of the mod files that must be present in order for this dependency to be fulfilled
    /// </summary>
    [YamlMember(Order = 8)]
    public HashSet<ImmutableArray<byte>> Hashes { get; private set; } = [];

    /// <summary>
    /// Gets/sets the <see cref="ResourceKey"/> of the mod manifest <see cref="ResourceType.SnippetTuning"/> of the dependency mod (optional)
    /// </summary>
    [YamlMember(Order = 5, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public ResourceKey? ModManifestKey { get; set; }

    /// <summary>
    /// Gets/sets the name of the dependency mod
    /// </summary>
    [YamlMember(Order = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the names of the features of the dependency mod which the dependent mod requires
    /// </summary>
    [YamlMember(Order = 6, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<string> RequiredFeatures { get; private set; } = [];

    /// <summary>
    /// Gets/sets an identifier that, when shared with a group of other dependency mods for a single dependent, indicates that only one member of the group need be present for the dependent to be satisfied
    /// </summary>
    [YamlMember(Order = 7, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? RequirementIdentifier { get; set; }

    /// <summary>
    /// Gets/sets the URL to which players can go to find more information about this dependency mod
    /// </summary>
    [YamlMember(Order = 4, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public Uri? Url { get; set; }

    /// <summary>
    /// Gets/sets the version of this dependency mod
    /// </summary>
    [YamlMember(Order = 3, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public Version? Version { get; set; }

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
            if (isList)
            {
                if (tunableName == "creators")
                    Creators.AddRange(reader.ReadTunableList());
                else if (tunableName == "hashes")
                    Hashes.AddRangeImmediately(reader.ReadTunableList().Select(hex => hex.ToByteSequence().ToImmutableArray()));
                else if (tunableName == "required_features")
                    RequiredFeatures.AddRange(reader.ReadTunableList());
            }
            else
            {
                var tunableValue = reader.ReadElementContentAsString();
                if (tunableName == "mod_manifest_key")
                    ModManifestKey = ResourceKey.Parse(tunableValue);
                else if (tunableName == "name")
                    Name = tunableValue;
                else if (tunableName == "requirement_identifier")
                    RequirementIdentifier = tunableValue;
                else if (tunableName == "version")
                    Version = Version.Parse(tunableValue);
                else if (tunableName == "url")
                    Url = new Uri(tunableValue, UriKind.Absolute);
            }
        }
        reader.ReadEndElement();
    }

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("U");
        writer.WriteTunable("name", Name);
        writer.WriteTunableList("creators", Creators);
        writer.WriteTunable("version", Version);
        writer.WriteTunable("url", Url);
        writer.WriteTunable("mod_manifest_key", ModManifestKey);
        writer.WriteTunableList("required_features", RequiredFeatures);
        writer.WriteTunable("requirement_identifier", RequirementIdentifier);
        writer.WriteTunableList("hashes", Hashes);
        writer.WriteEndElement();
    }

    #endregion IXmlSerializable
}
