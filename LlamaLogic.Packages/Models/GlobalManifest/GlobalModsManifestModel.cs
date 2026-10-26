namespace LlamaLogic.Packages.Models.GlobalManifest;

/// <summary>
/// Represents the global mods manifest <see cref="ResourceType.SnippetTuning"/> resource (🔓)
/// </summary>
/// <remarks>
/// This model allows agents to easily create, read, and update the global mods manifest, which itself is an index of the player's installed packs and manifested mod files easily acessible to script mods.
/// </remarks>
public sealed class GlobalModsManifestModel :
    Model,
    IModel<GlobalModsManifestModel>,
    IXmlSerializable
{
    static readonly ImmutableHashSet<ResourceType> supportedTypes =
    [
        ResourceType.SnippetTuning
    ];
    const string tuningClass = "GlobalModsManifest";
    const string tuningInstance = "snippet";
    const string tuningModule = "llamalogic.snippets.globalmodsmanifest";
    const string tuningName = "llamalogic:GlobalModsManifest";

    /// <summary>
    /// Gets the <see cref="Packages.ResourceKey"/> always used by the global mods manifest
    /// </summary>
    public static ResourceKey ResourceKey { get; } =
        new(ResourceType.SnippetTuning, 0x80000000, Fnv64.SetHighBit(Fnv64.GetHash(tuningName)));

    /// <inheritdoc/>
    public static new ISet<ResourceType> SupportedTypes =>
        supportedTypes;

    /// <inheritdoc/>
    public static GlobalModsManifestModel Decode(ReadOnlyMemory<byte> data)
    {
        using var readOnlyMemoryOfByteStream = new ReadOnlyMemoryOfByteStream(data);
        using var xmlReader = XmlReader.Create(readOnlyMemoryOfByteStream);
        var result = new GlobalModsManifestModel();
        ((IXmlSerializable)result).ReadXml(xmlReader);
        return result;
    }

    /// <summary>
    /// Gets the collection of pack codes that the launcher has specified as disabled
    /// </summary>
    public Collection<string> DisabledPacks { get; private set; } = [];

    /// <summary>
    /// Gets the collection of pack codes for this game installation's installed packs
    /// </summary>
    public Collection<string> InstalledPacks { get; private set; } = [];

    /// <summary>
    /// Gets the collection of manifested mod files in the player's Mods folder
    /// </summary>
    public Collection<GlobalModsManifestModelManifestedModFile> ManifestedModFiles { get; private set; } = [];

    /// <inheritdoc/>
    public override string? ResourceName =>
        tuningName;

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Encode()
    {
        using var arrayBufferWriterOfByteStream = new ArrayBufferWriterOfByteStream();
        using var xmlWriter = XmlWriter.Create(arrayBufferWriterOfByteStream, TuningUtilities.XmlWriterSettings);
        ((IXmlSerializable)this).WriteXml(xmlWriter);
        xmlWriter.Flush();
        return arrayBufferWriterOfByteStream.WrittenMemory;
    }

    #region IXmlSerializable

    XmlSchema? IXmlSerializable.GetSchema() =>
        null;

    void IXmlSerializable.ReadXml(XmlReader reader)
    {
        reader.MoveToContent();
        if (reader.NodeType is not XmlNodeType.Element || reader.Name != "I")
            throw new XmlException("Expected a tuning instance element");
        if (reader.GetAttribute("i") != tuningInstance)
            throw new XmlException($"Expected the \"{tuningInstance}\" instance");
        if (reader.GetAttribute("m") != tuningModule)
            throw new XmlException($"Expected the \"{tuningModule}\" module");
        if (reader.GetAttribute("c") != tuningClass)
            throw new XmlException($"Expected the \"{tuningClass}\" class");
        var element = XElement.Load(reader.ReadSubtree());
        foreach (var child in element.Elements())
        {
            var (tunableName, isList) = child.ReadTunableDetails();
            if (isList)
            {
                using var childReader = child.CreateReader();
                childReader.MoveToContent();
                if (tunableName == "disabled_packs")
                    DisabledPacks.AddRange(childReader.ReadTunableList());
                else if (tunableName == "installed_packs")
                    InstalledPacks.AddRange(childReader.ReadTunableList());
                else if (tunableName == "manifested_mod_files")
                    ManifestedModFiles.AddRange(childReader.ReadTunableTupleList<GlobalModsManifestModelManifestedModFile>());
            }
        }
    }

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("I");
        writer.WriteAttributeString("c", tuningClass);
        writer.WriteAttributeString("i", tuningInstance);
        writer.WriteAttributeString("m", tuningModule);
        writer.WriteAttributeString("n", tuningName);
        writer.WriteAttributeString("s", ResourceKey.FullInstance.ToString());
        writer.WriteTunableList("installed_packs", InstalledPacks);
        writer.WriteTunableList("disabled_packs", DisabledPacks);
        writer.WriteTunableList("manifested_mod_files", ManifestedModFiles);
        writer.WriteEndElement();
    }

    #endregion IXmlSerializable
}
