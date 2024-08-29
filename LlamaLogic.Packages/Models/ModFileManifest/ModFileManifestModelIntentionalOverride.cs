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
    /// Gets the hashes of the files for the mod in which the resource is being overridden (optional)
    /// </summary>
    [YamlMember(Order = 6, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections, Description = "hashes of files of which this mod intends to override this resource")]
    public HashSet<byte[]> ModFiles { get; private set; } = [];

    /// <summary>
    /// Gets/sets the name of the mod in which the resource is being overridden (optional)
    /// </summary>
    [YamlMember(Order = 3)]
    public string? ModName { get; set; }

    /// <summary>
    /// Gets/sets the <see cref="ResourceKey"/> of the mod manifest <see cref="ResourceType.SnippetTuning"/> of the mod in which the resource is being overridden (optional)
    /// </summary>
    [YamlMember(Order = 5)]
    public ResourceKey? ModManifestKey { get; set; }

    /// <summary>
    /// Gets/sets the name of the resource in the mod that is being overridden (optional)
    /// </summary>
    [YamlMember(Order = 2)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets/sets the version of the mod in which the resource is being overridden (optional)
    /// </summary>
    [YamlMember(Order = 4)]
    public Version? ModVersion { get; set; }

    /// <summary>
    /// Adds a mod file based on its <paramref name="path"/>, returning whether the file was added because it was not already present in the list
    /// </summary>
    public bool AddModFile(string path) =>
        ModFiles.Add(ModFileManifestModel.GetFileSha256Hash(path));

    /// <summary>
    /// Adds a mod file based on its <paramref name="path"/> asynchronously, returning whether the file was added because it was not already present in the list
    /// </summary>
    public async Task<bool> AddModFileAsync(string path) =>
        ModFiles.Add(await ModFileManifestModel.GetFileSha256HashAsync(path).ConfigureAwait(false));

    /// <summary>
    /// Removes a mod file based on its <paramref name="path"/>, returning whether the file was removed because it was present in the list
    /// </summary>
    public bool RemoveModFile(string path) =>
        ModFiles.Remove(ModFileManifestModel.GetFileSha256Hash(path));

    /// <summary>
    /// Removes a mod file based on its <paramref name="path"/> asynchronously, returning whether the file was removed because it was present in the list
    /// </summary>
    public async Task<bool> RemoveModFileAsync(string path) =>
        ModFiles.Remove(await ModFileManifestModel.GetFileSha256HashAsync(path).ConfigureAwait(false));

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
                if (tunableName == "files")
                    ModFiles.AddRangeImmediately(reader.ReadTunableList().Select(hex => hex.ToByteArray()));
            }
            else
            {
                var tunableValue = reader.ReadElementContentAsString();
                if (tunableName == "key")
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
        writer.WriteTunableList("mod_files", ModFiles);
        writer.WriteEndElement();
    }

    #endregion IXmlSerializable
}
