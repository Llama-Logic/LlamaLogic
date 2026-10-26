namespace LlamaLogic.Packages.Models.GlobalManifest;

/// <summary>
/// Represents a manifested mod in a global mods manifest <see cref="ResourceType.SnippetTuning"/> resource (🔓)
/// </summary>
public sealed class GlobalModsManifestModelManifestedModFile :
    IXmlSerializable
{
    /// <summary>
    /// Gets the union of <see cref="ModFileManifestModel.SubsumedHashes"/> and the agent's calculated manifest hash for the mod file
    /// </summary>
    public HashSet<ImmutableArray<byte>> Hashes { get; private set; } = [];

    /// <summary>
    /// Gets/sets the <see cref="ResourceKey"/> of the mod manifest <see cref="ResourceType.SnippetTuning"/> of the mod file if it's a package
    /// </summary>
    public ResourceKey? ManifestKey { get; set; }

    /// <summary>
    /// Gets the tuning name of the mod manifest <see cref="ResourceType.SnippetTuning"/> of the mod file if it's a package
    /// </summary>
    public string? ManifestTuningName { get; set; }

    /// <summary>
    /// Gets/sets the relative path from the root of the Mods folder to the mod file
    /// </summary>
    public string ModsFolderPath { get; set; } = string.Empty;

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
            if (isList)
            {
                using var childReader = child.CreateReader();
                childReader.MoveToContent();
                if (tunableName == "hashes")
                    Hashes.AddRangeImmediately(childReader.ReadTunableList().Select(hex => hex.ToByteSequence().ToImmutableArray()));
            }
            else
            {
                var tunableValue = child.Value;
                if (tunableName == "manifest_key")
                    ManifestKey = ResourceKey.Parse(tunableValue);
                else if (tunableName == "manifest_tuning_name")
                    ManifestTuningName = tunableValue;
                else if (tunableName == "mods_folder_path")
                    ModsFolderPath = tunableValue;
            }
        }
    }

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("U");
        writer.WriteTunable("mods_folder_path", ModsFolderPath);
        writer.WriteTunable("manifest_key", ManifestKey);
        writer.WriteTunable("manifest_tuning_name", ManifestTuningName);
        writer.WriteTunableList("hashes", Hashes);
        writer.WriteEndElement();
    }
}
