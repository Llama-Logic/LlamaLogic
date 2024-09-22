namespace LlamaLogic.Packages.Models.ModFileManifest;

/// <summary>
/// Represents a mod file manifest <see cref="ResourceType.SnippetTuning"/> resource (🔓)
/// </summary>
/// <remarks>
/// This model allows callers to easily create, read, and update a mod file manifest.
/// These manifests are a format sponsored by the Llama Logic team to permit creators to specify the dependency requirements of their mods.
/// 
/// ### Use in Mod Package (`.package`) files
/// Place one and only one mod file manifest <see cref="ResourceType.SnippetTuning"/> resource in the package.
/// If an external application encounters multiple <see cref="ResourceType.SnippetTuning"/> resources in a package, it should use the first one (ordered by <see cref="ResourceKey.Group"/>, then by <see cref="ResourceKey.FullInstance"/>) and ignore the rest.
///
/// Avoid using <see cref="ResourceKey.Group"/> `0x00000000` as this is informally reserved for Maxis.
/// It would be a bizarre miracle if they started incorporating dependency information using a community format.
/// Regardless, let's not make them feel unwelcome and hold their spot for them.
///
/// ### Use in Script Mod Archive (`.ts4script`) files
/// Because these files are just ZIP archives with a different extension and their contents are not flatly merged by the game as the contents of package files are, there is no risk of collision.
/// Also, since they need not contend with game resource management, they need not be in an arcane format like XML, so they are in YAML instead.
/// The static parsing methods of this type expect YAML representations of manifests and the <see cref="ToString"/> method produces them.
/// Name your mod file manifest resource `manifest.yml` and put it in the root of your `.ts4script` archive.
/// </remarks>
public sealed class ModFileManifestModel :
    Model,
    IModel<ModFileManifestModel>,
    IXmlSerializable
#if IS_NET_7_0_OR_GREATER
    , IParsable<ModFileManifestModel>
#endif
{
    static readonly ImmutableHashSet<ResourceType> supportedTypes =
    [
        ResourceType.SnippetTuning
    ];

    /// <inheritdoc/>
    public static new ISet<ResourceType> SupportedTypes =>
        supportedTypes;

    static TBuilder ConfigureBuilder<TBuilder>(TBuilder builder)
        where TBuilder : BuilderSkeleton<TBuilder> => builder
            .WithTypeConverter(new YamlHashHexConverter())
            .WithTypeConverter(new YamlResourceKeyConverter())
            .WithTypeConverter(new YamlUriConverter())
            .WithTypeConverter(new YamlVersionConverter())
            .WithNamingConvention(UnderscoredNamingConvention.Instance);

    static IDeserializer CreateYamlDeserializer() =>
        ConfigureBuilder(new DeserializerBuilder()).Build();

    static ISerializer CreateYamlSerializer() =>
        ConfigureBuilder(new SerializerBuilder()).Build();

    /// <inheritdoc/>
    public static ModFileManifestModel Decode(ReadOnlyMemory<byte> data)
    {
        using var readOnlyMemoryOfByteStream = new ReadOnlyMemoryOfByteStream(data);
        using var xmlReader = XmlReader.Create(readOnlyMemoryOfByteStream);
        var result = new ModFileManifestModel { Name = string.Empty };
        ((IXmlSerializable)result).ReadXml(xmlReader);
        return result;
    }

    /// <summary>
    /// Gets the SHA 256 hash of the content of the file located at <paramref name="filePath"/>
    /// </summary>
    public static byte[] GetFileSha256Hash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = sha256.ComputeHash(fileStream);
        fileStream.Close();
        return hash;
    }

    /// <summary>
    /// Gets the SHA 256 hash of the content of the file located at <paramref name="filePath"/> asynchronously
    /// </summary>
    public static async Task<byte[]> GetFileSha256HashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var hash = await sha256.ComputeHashAsync(fileStream, cancellationToken).ConfigureAwait(false);
        fileStream.Close();
        return hash;
    }

    /// <inheritdoc/>
    public static new string? GetName(Stream stream)
    {
        using var streamReader = new StreamReader(stream);
        var modFileManifest = CreateYamlDeserializer().Deserialize<ModFileManifestModel>(streamReader);
        return modFileManifest.ResourceName;
    }

    /// <inheritdoc/>
    public static new Task<string?> GetNameAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var streamReader = new StreamReader(stream);
        var modFileManifest = CreateYamlDeserializer().Deserialize<ModFileManifestModel>(streamReader);
        return Task.FromResult(modFileManifest.ResourceName);
    }

    /// <summary>
    /// Parses a string into a <see cref="ModFileManifestModel"/>
    /// </summary>
    /// <param name="s">The string to parse</param>
    /// <exception cref="FormatException"><paramref name="s"/> is not in the correct format</exception>
    public static ModFileManifestModel Parse(string s) =>
        Parse(s, null);

    /// <summary>
    /// Parses a string into a <see cref="ModFileManifestModel"/>
    /// </summary>
    /// <param name="s">The string to parse</param>
    /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s"/></param>
    /// <exception cref="FormatException"><paramref name="s"/> is not in the correct format</exception>
    public static ModFileManifestModel Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var key))
            return key;
        throw new FormatException($"Unable to parse '{s}' as {nameof(ModFileManifestModel)}.");
    }

    /// <summary>
    /// Tries to parse a string into a <see cref="ModFileManifestModel"/>
    /// </summary>
    /// <param name="s">The string to parse</param>
    /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="s"/> or an undefined value on failure</param>
    public static bool TryParse(string? s, [MaybeNullWhen(false)] out ModFileManifestModel result) =>
        TryParse(s, null, out result);

    /// <summary>
    /// Tries to parse a string into a <see cref="ResourceKey"/>
    /// </summary>
    /// <param name="s">The string to parse</param>
    /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s"/></param>
    /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="s"/> or an undefined value on failure</param>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out ModFileManifestModel result)
    {
        if (s is not null && CreateYamlDeserializer().Deserialize<ModFileManifestModel>(s) is { } modFileManifest)
        {
            result = modFileManifest;
            return true;
        }
        result = default;
        return false;
    }

    /// <summary>
    /// Gets the names of the creators of the mod
    /// </summary>
    [YamlMember(Order = 2, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<string> Creators { get; private set; } = [];

    /// <summary>
    /// Gets the names of the features this mod offers to other mods as a dependency
    /// </summary>
    [YamlMember(Order = 5, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<string> Features { get; private set; } = [];

    /// <summary>
    /// Gets the list of resources the mod intends to override
    /// </summary>
    [YamlMember(Order = 9, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<ModFileManifestModelIntentionalOverride> IntentionalOverrides { get; private set; } = [];

    /// <summary>
    /// Gets/sets the name of the mod
    /// </summary>
    [YamlMember(Order = 1)]
    public required string Name { get; set; }

    /// <summary>
    /// Gets the list of mods required by this mod
    /// </summary>
    [YamlMember(Order = 8, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<ModFileManifestModelRequiredMod> RequiredMods { get; private set; } = [];

    /// <summary>
    /// Gets the list of pack codes identifying the packs required by this mod (e.g. "EP01" for Get to Work)
    /// </summary>
    [YamlMember(Order = 7, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<string> RequiredPacks { get; private set; } = [];

    /// <inheritdoc/>
    [YamlIgnore]
    public override string? ResourceName =>
        TuningName;

    /// <summary>
    /// Gets the hashes of files in for which I stand even though my hash is different
    /// </summary>
    [YamlMember(Order = 6, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections, Description = "hashes of files in for which I stand even though my hash is different")]
    public HashSet<byte[]> SubsumedFiles { get; private set; } = [];

    /// <summary>
    /// Gets the <see cref="ResourceType.SnippetTuning"/> name for the mod file manifest
    /// </summary>
    [YamlIgnore]
    public string? TuningName { get; set; }

    /// <summary>
    /// Gets the <see cref="ResourceType.SnippetTuning"/> decimal conversion of the <see cref="ResourceKey.FullInstance"/> for the mod file manifest
    /// </summary>
    [YamlIgnore]
    public ulong TuningFullInstance { get; set; }

    /// <summary>
    /// Gets/sets the URL to which players can go to find more information about this mod
    /// </summary>
    [YamlMember(Order = 4, DefaultValuesHandling = DefaultValuesHandling.OmitNull, ScalarStyle = YamlDotNet.Core.ScalarStyle.SingleQuoted)]
    public Uri? Url { get; set; }

    /// <summary>
    /// Gets/sets the version of this mod
    /// </summary>
    [YamlMember(Order = 3, DefaultValuesHandling = DefaultValuesHandling.OmitNull, ScalarStyle = YamlDotNet.Core.ScalarStyle.SingleQuoted)]
    public Version? Version { get; set; }

    /// <summary>
    /// Adds a subsumed file based on its <paramref name="path"/>, returning whether the file was added because it was not already present in the list
    /// </summary>
    public bool AddSubsumedFile(string path) =>
        SubsumedFiles.Add(GetFileSha256Hash(path));

    /// <summary>
    /// Adds a file based on its <paramref name="path"/> asynchronously, returning whether the file was added because it was not already present in the list
    /// </summary>
    public async Task<bool> AddSubsumedFileAsync(string path) =>
        SubsumedFiles.Add(await GetFileSha256HashAsync(path).ConfigureAwait(false));

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Encode()
    {
        using var arrayBufferWriterOfByteStream = new ArrayBufferWriterOfByteStream();
        using var xmlWriter = XmlWriter.Create(arrayBufferWriterOfByteStream, TuningUtilities.XmlWriterSettings);
        ((IXmlSerializable)this).WriteXml(xmlWriter);
        xmlWriter.Flush();
        return arrayBufferWriterOfByteStream.WrittenMemory;
    }

    /// <summary>
    /// Removes a subsumed file based on its <paramref name="path"/>, returning whether the file was removed because it was present in the list
    /// </summary>
    public bool RemoveSubsumedFile(string path) =>
        SubsumedFiles.Remove(GetFileSha256Hash(path));

    /// <summary>
    /// Removes a subsumed file based on its <paramref name="path"/> asynchronously, returning whether the file was removed because it was present in the list
    /// </summary>
    public async Task<bool> RemoveSubsumedFileAsync(string path) =>
        SubsumedFiles.Remove(await GetFileSha256HashAsync(path).ConfigureAwait(false));

    /// <summary>
    /// Generates the YAML representation of the <see cref="ModFileManifestModel"/>
    /// </summary>
    public override string ToString() =>
        CreateYamlSerializer().Serialize(this);

    #region IXmlSerializable

    XmlSchema? IXmlSerializable.GetSchema() =>
        null;

    void IXmlSerializable.ReadXml(XmlReader reader)
    {
        reader.MoveToContent();
        if (reader.NodeType is not XmlNodeType.Element || reader.Name != "I")
            throw new XmlException("Expected a tuning instance element");
        if (reader.GetAttribute("i") != "snippet")
            throw new XmlException("Expected the \"snippet\" instance");
        if (reader.GetAttribute("m") != "llamalogic.snippets.modfilemanifest")
            throw new XmlException("Expected the \"llamalogic.snippets.modfilemanifest\" module");
        if (reader.GetAttribute("c") != "ModFileManifest")
            throw new XmlException("Expected the \"ModFileManifest\" class");
        TuningName = reader.GetAttribute("n");
        if (ulong.TryParse(reader.GetAttribute("s"), out var parsedTuningFullInstance))
            TuningFullInstance = parsedTuningFullInstance;
        while (reader.Read() && reader.NodeType is not XmlNodeType.EndElement)
        {
            if (reader.NodeType is not XmlNodeType.Element)
                continue;
            var (tunableName, isList) = reader.ReadTunableDetails();
            if (isList)
            {
                if (tunableName == "creators")
                    Creators.AddRange(reader.ReadTunableList());
                else if (tunableName == "features")
                    Features.AddRange(reader.ReadTunableList());
                else if (tunableName == "intentional_overrides")
                    IntentionalOverrides.AddRange(reader.ReadTunableTupleList<ModFileManifestModelIntentionalOverride>());
                else if (tunableName == "required_mods")
                    RequiredMods.AddRange(reader.ReadTunableTupleList<ModFileManifestModelRequiredMod>());
                else if (tunableName == "required_packs")
                    RequiredPacks.AddRange(reader.ReadTunableList());
                else if (tunableName == "subsumed_files")
                    SubsumedFiles.AddRangeImmediately(reader.ReadTunableList().Select(hex => hex.ToByteArray()));
            }
            else
            {
                var tunableValue = reader.ReadElementContentAsString();
                if (tunableName == "name")
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
        writer.WriteStartElement("I");
        writer.WriteAttributeString("c", "ModFileManifest");
        writer.WriteAttributeString("i", "snippet");
        writer.WriteAttributeString("m", "llamalogic.snippets.modfilemanifest");
        writer.WriteAttributeString("n", TuningName);
        writer.WriteAttributeString("s", TuningFullInstance.ToString());
        writer.WriteTunable("name", Name);
        writer.WriteTunableList("creators", Creators);
        writer.WriteTunable("version", Version);
        writer.WriteTunable("url", Url);
        writer.WriteTunableList("features", Features);
        writer.WriteTunableList("subsumed_files", SubsumedFiles);
        writer.WriteTunableList("required_packs", RequiredPacks);
        writer.WriteTunableList("required_mods", RequiredMods);
        writer.WriteTunableList("intentional_overrides", IntentionalOverrides);
        writer.WriteEndElement();
    }

    #endregion IXmlSerializable
}
