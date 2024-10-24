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
    [YamlMember(Order = 7)]
    public HashSet<ImmutableArray<byte>> Hashes { get; private set; } = [];

    /// <summary>
    /// Instructs agents to ignore fulfilling this dependency if this hash is present in the player's catalog of mods
    /// </summary>
    [YamlMember(Order = 8, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public ImmutableArray<byte> IgnoreIfHashAvailable { get; set; }

    /// <summary>
    /// Instructs agents to ignore fulfilling this dependency if this hash is not present in the player's catalog of mods
    /// </summary>
    [YamlMember(Order = 9, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public ImmutableArray<byte> IgnoreIfHashUnavailable { get; set; }

    /// <summary>
    /// Instructs agents to ignore fulfilling this dependency if the pack identified by this pack code is present (e.g. "EP01" for Get to Work)
    /// </summary>
    [YamlMember(Order = 10, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? IgnoreIfPackAvailable { get; set; }

    /// <summary>
    /// Instructs agents to ignore fulfilling this dependency if the pack identified by this pack code is not present (e.g. "EP01" for Get to Work)
    /// </summary>
    [YamlMember(Order = 11, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? IgnoreIfPackUnavailable { get; set; }

    /// <summary>
    /// Gets/sets the name of the dependency mod
    /// </summary>
    [YamlMember(Order = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the names of the features of the dependency mod which the dependent mod requires
    /// </summary>
    [YamlMember(Order = 5, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<string> RequiredFeatures { get; private set; } = [];

    /// <summary>
    /// Gets/sets an identifier that, when shared with a group of other dependency mods for a single dependent, indicates that only one member of the group need be present for the dependent to be satisfied
    /// </summary>
    [YamlMember(Order = 6, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
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
    public string? Version { get; set; }

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
                if (tunableName == "ignore_if_hash_available")
                    IgnoreIfHashAvailable = tunableValue.ToByteSequence().ToImmutableArray();
                else if (tunableName == "ignore_if_hash_unavailable")
                    IgnoreIfHashUnavailable = tunableValue.ToByteSequence().ToImmutableArray();
                else if (tunableName == "ignore_if_pack_available")
                    IgnoreIfPackAvailable = tunableValue;
                else if (tunableName == "ignore_if_pash_unavailable")
                    IgnoreIfPackUnavailable = tunableValue;
                else if (tunableName == "name")
                    Name = tunableValue;
                else if (tunableName == "requirement_identifier")
                    RequirementIdentifier = tunableValue;
                else if (tunableName == "version")
                    Version = tunableValue;
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
        writer.WriteTunableList("required_features", RequiredFeatures);
        writer.WriteTunable("requirement_identifier", RequirementIdentifier);
        writer.WriteTunableList("hashes", Hashes);
        writer.WriteTunable("ignore_if_hash_available", IgnoreIfHashAvailable);
        writer.WriteTunable("ignore_if_hash_unavailable", IgnoreIfHashUnavailable);
        writer.WriteTunable("ignore_if_pack_available", IgnoreIfPackAvailable);
        writer.WriteTunable("ignore_if_pash_unavailable", IgnoreIfPackUnavailable);
        writer.WriteEndElement();
    }

    #endregion IXmlSerializable
}
