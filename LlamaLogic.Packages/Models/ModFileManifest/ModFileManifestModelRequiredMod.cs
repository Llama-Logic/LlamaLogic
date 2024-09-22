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
    /// Gets the hashes of files that must be present in order for this dependency to be fulfilled
    /// </summary>
    [YamlMember(Order = 6, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections, Description = "hashes of files that must be present in order for this dependency to be fulfilled")]
    public HashSet<byte[]> Files { get; private set; } = [];

    /// <summary>
    /// Gets/sets the <see cref="ResourceKey"/> of the mod manifest <see cref="ResourceType.SnippetTuning"/> of the dependency mod (optional)
    /// </summary>
    [YamlMember(Order = 5)]
    public ResourceKey? ModManifestKey { get; set; }

    /// <summary>
    /// Gets/sets the name of the dependency mod
    /// </summary>
    [YamlMember(Order = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the names of the features of the dependency mod which the dependent mod requires
    /// </summary>
    [YamlMember(Order = 7, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<string> RequiredFeatures { get; private set; } = [];

    /// <summary>
    /// Gets/sets the URL to which players can go to find more information about this dependency mod
    /// </summary>
    [YamlMember(Order = 4, DefaultValuesHandling = DefaultValuesHandling.OmitNull, ScalarStyle = YamlDotNet.Core.ScalarStyle.SingleQuoted)]
    public Uri? Url { get; set; }

    /// <summary>
    /// Gets/sets the version of this dependency mod
    /// </summary>
    [YamlMember(Order = 3, DefaultValuesHandling = DefaultValuesHandling.OmitNull, ScalarStyle = YamlDotNet.Core.ScalarStyle.SingleQuoted)]
    public Version? Version { get; set; }

    /// <summary>
    /// Adds a file based on its <paramref name="path"/>, returning whether the file was added because it was not already present in the list
    /// </summary>
    public bool AddFile(string path) =>
        Files.Add(ModFileManifestModel.GetFileSha256Hash(path));

    /// <summary>
    /// Adds a file based on its <paramref name="path"/> asynchronously, returning whether the file was added because it was not already present in the list
    /// </summary>
    public async Task<bool> AddFileAsync(string path) =>
        Files.Add(await ModFileManifestModel.GetFileSha256HashAsync(path).ConfigureAwait(false));

    /// <summary>
    /// Removes a file based on its <paramref name="path"/>, returning whether the file was removed because it was present in the list
    /// </summary>
    public bool RemoveFile(string path) =>
        Files.Remove(ModFileManifestModel.GetFileSha256Hash(path));

    /// <summary>
    /// Removes a file based on its <paramref name="path"/> asynchronously, returning whether the file was removed because it was present in the list
    /// </summary>
    public async Task<bool> RemoveFileAsync(string path) =>
        Files.Remove(await ModFileManifestModel.GetFileSha256HashAsync(path).ConfigureAwait(false));

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
                else if (tunableName == "files")
                    Files.AddRangeImmediately(reader.ReadTunableList().Select(hex => hex.ToByteArray()));
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
        writer.WriteTunableList("files", Files);
        writer.WriteTunableList("required_features", RequiredFeatures);
        writer.WriteEndElement();
    }

    #endregion IXmlSerializable
}
