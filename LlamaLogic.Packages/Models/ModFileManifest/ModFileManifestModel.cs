namespace LlamaLogic.Packages.Models.ModFileManifest;

/// <summary>
/// Represents a mod file manifest <see cref="ResourceType.SnippetTuning"/> resource (🔓)
/// </summary>
/// <remarks>
/// This model allows callers to easily create, read, and update a mod file manifest.
/// These manifests are a format sponsored by the Llama Logic team to permit creators to specify the dependency requirements of their mods.
/// 
/// ### Use in Mod Package (`.package`) files
/// When originally creating packages, place one and only one mod file manifest <see cref="ResourceType.SnippetTuning"/> resource in the package and use sufficiently unique values for its <see cref="ResourceKey.FullInstance"/> and <see cref="TuningName"/>.
/// If an external application encounters multiple <see cref="ResourceType.SnippetTuning"/> manifest resources in a package, it must assume that a manifest un-aware tool has merged multiple manifested packages and process each manifest accordingly.
///
/// Avoid using <see cref="ResourceKey.Group"/> `0x00000000` as this is informally reserved for Maxis.
/// It would be a bizarre miracle if they started incorporating dependency information using a community format.
/// Regardless, let's not make them feel unwelcome and hold their spot for them.
///
/// ### Use in Script Mod Archive (`.ts4script`) files
/// Because these files are just ZIP archives with a different extension and their contents are not flatly merged by the game as the contents of package files are, there is no risk of collision.
/// Also, since they need not contend with game resource management, they need not be in an arcane format like XML, so they are in YAML instead.
/// The static parsing methods of this type expect YAML representations of manifests and the <see cref="ToString"/> method produces them.
/// Name your mod file manifest resource `llamalogic.modfilemanifest.yml` and put it in the root of your `.ts4script` archive.
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
    const string tuningClass = "ModFileManifest";
    const string tuningInstance = "snippet";
    const string tuningModule = "llamalogic.snippets.modfilemanifest";
    const string zipArchiveManifestName = "llamalogic.modfilemanifest.yml";

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
    /// Deletes all mod file manifest resources in the specified <paramref name="scriptMod"/>
    /// </summary>
    [Obsolete($"This method will be removed in a future version. Call {nameof(DeleteModFileManifests)} instead.")]
    public static void DeleteModFileManifest(ZipArchive scriptMod) =>
        DeleteModFileManifests(scriptMod);

    /// <summary>
    /// Deletes all mod file manifest resources in the specified <paramref name="package"/>
    /// </summary>
    public static void DeleteModFileManifests(DataBasePackedFile package)
    {
        ArgumentNullException.ThrowIfNull(package);
        foreach (var manifestResourceKey in GetModFileManifests(package).Keys)
            package.Delete(manifestResourceKey);
    }

    /// <summary>
    /// Deletes all mod file manifest resources in the specified <paramref name="scriptMod"/>
    /// </summary>
    public static void DeleteModFileManifests(ZipArchive scriptMod)
    {
        ArgumentNullException.ThrowIfNull(scriptMod);
        while (scriptMod.Entries.FirstOrDefault(entry => entry.Name.Equals(zipArchiveManifestName, StringComparison.OrdinalIgnoreCase)) is { } entry)
            entry.Delete();
    }

    /// <summary>
    /// Deletes all mod file manifest resources in the specified <paramref name="package"/>, asynchronously
    /// </summary>
    public static async Task DeleteModFileManifestsAsync(DataBasePackedFile package)
    {
        ArgumentNullException.ThrowIfNull(package);
        foreach (var manifestResourceKey in (await GetModFileManifestsAsync(package).ConfigureAwait(false)).Keys)
            package.Delete(manifestResourceKey);
    }

    /// <summary>
    /// Gets the SHA 256 hash of the content of the file located at <paramref name="filePath"/>
    /// </summary>
    public static ImmutableArray<byte> GetFileSha256Hash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = sha256.ComputeHash(fileStream);
        fileStream.Close();
        return [..hash];
    }

    /// <summary>
    /// Gets the SHA 256 hash of the content of the file located at <paramref name="filePath"/> asynchronously
    /// </summary>
    public static async Task<ImmutableArray<byte>> GetFileSha256HashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var hash = await sha256.ComputeHashAsync(fileStream, cancellationToken).ConfigureAwait(false);
        fileStream.Close();
        return [..hash];
    }

    /// <summary>
    /// Gets the hash for the specified <paramref name="package"/> using the specified <paramref name="hashResourceKeys"/>
    /// </summary>
    public static ImmutableArray<byte> GetModFileHash(DataBasePackedFile package, HashSet<ResourceKey> hashResourceKeys)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(hashResourceKeys);
        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        package.ForEach
        (
            ResourceKeyOrder.TypeGroupInstance,
            hashResourceKeys.Contains,
            (key, content) =>
            {
                Span<byte> keySpan = new byte[16];
                var fullInstance = key.FullInstance;
                MemoryMarshal.Write(keySpan[0..8], ref fullInstance);
                var type = key.Type;
                MemoryMarshal.Write(keySpan[8..12], ref type);
                var group = key.Group;
                MemoryMarshal.Write(keySpan[12..16], ref group);
                sha256.AppendData(keySpan);
                sha256.AppendData(content.Span);
            }
        );
        return [..sha256.GetCurrentHash()];
    }

    /// <summary>
    /// Gets the hash for the specified <paramref name="scriptMod"/>
    /// </summary>
    public static ImmutableArray<byte> GetModFileHash(ZipArchive scriptMod)
    {
        ArgumentNullException.ThrowIfNull(scriptMod);
        Span<byte> crc32Span = stackalloc byte[4];
        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var entry in scriptMod.Entries
            .Where(entry => !entry.Name.Equals(zipArchiveManifestName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.Ordinal))
        {
            sha256.AppendData(Encoding.UTF8.GetBytes(entry.FullName));
            var crc32 = entry.Crc32;
            MemoryMarshal.Write(crc32Span, ref crc32);
            sha256.AppendData(crc32Span);
        }
        return [..sha256.GetCurrentHash()];
    }

    /// <summary>
    /// Gets the hash for the specified <paramref name="package"/> using the specified <paramref name="hashResourceKeys"/>, asynchronously
    /// </summary>
    public static async Task<ImmutableArray<byte>> GetModFileHashAsync(DataBasePackedFile package, HashSet<ResourceKey> hashResourceKeys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(hashResourceKeys);
        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await package.ForEachAsync
        (
            ResourceKeyOrder.TypeGroupInstance,
            hashResourceKeys.Contains,
            (key, content) =>
            {
                Span<byte> keySpan = new byte[16];
                var fullInstance = key.FullInstance;
                MemoryMarshal.Write(keySpan[0..8], ref fullInstance);
                var type = key.Type;
                MemoryMarshal.Write(keySpan[8..12], ref type);
                var group = key.Group;
                MemoryMarshal.Write(keySpan[12..16], ref group);
                sha256.AppendData(keySpan);
                sha256.AppendData(content.Span);
                return Task.CompletedTask;
            },
            cancellationToken
        ).ConfigureAwait(false);
        return [..sha256.GetCurrentHash()];
    }

    /// <summary>
    /// Gets the mod file manifest for the specified <paramref name="package"/>, if it has one
    /// </summary>
    public static ModFileManifestModel? GetModFileManifest(DataBasePackedFile package)
    {
        ArgumentNullException.ThrowIfNull(package);
        ModFileManifestModel? modFileManifest = null;
        try
        {
            using var cts = new CancellationTokenSource();
            package.ForEach(ResourceKeyOrder.TypeGroupInstance, key => key.Type is ResourceType.SnippetTuning, (key, content) =>
            {
                try
                {
                    modFileManifest = Decode(content);
                    cts.Cancel();
                }
                catch (XmlException)
                {
                }
            }, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        return modFileManifest;
    }

    /// <summary>
    /// Gets the mod file manifest for the specified <paramref name="scriptMod"/>, if it has one
    /// </summary>
    public static ModFileManifestModel? GetModFileManifest(ZipArchive scriptMod)
    {
        ArgumentNullException.ThrowIfNull(scriptMod);
        if (scriptMod.Entries.OrderBy(entry => entry.Name == entry.FullName ? 0 : 1).ThenBy(entry => entry.FullName, StringComparer.Ordinal).FirstOrDefault(entry => entry.Name.Equals(zipArchiveManifestName, StringComparison.OrdinalIgnoreCase)) is { } manifestEntry)
        {
            using var manifestStream = manifestEntry.Open();
            using var manifestReader = new StreamReader(manifestStream);
            if (TryParse(manifestReader.ReadToEnd(), out var modFileManifest))
                return modFileManifest;
        }
        return null;
    }

    /// <summary>
    /// Gets the mod file manifest for the specified <paramref name="package"/> and its <see cref="ResourceKey"/>, if the <paramref name="package"/> has one
    /// </summary>
    public static (ResourceKey, ModFileManifestModel?) GetModFileManifestAndKey(DataBasePackedFile package)
    {
        ArgumentNullException.ThrowIfNull(package);
        ResourceKey foundKey = default;
        ModFileManifestModel? modFileManifest = null;
        try
        {
            using var cts = new CancellationTokenSource();
            package.ForEach(ResourceKeyOrder.TypeGroupInstance, key => key.Type is ResourceType.SnippetTuning, (key, content) =>
            {
                try
                {
                    modFileManifest = Decode(content);
                    foundKey = key;
                    cts.Cancel();
                }
                catch (XmlException)
                {
                }
            }, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        return (foundKey, modFileManifest);
    }

    /// <summary>
    /// Gets the mod file manifest for the specified <paramref name="package"/>, asynchronously, if the <paramref name="package"/> has one
    /// </summary>
    public static async Task<(ResourceKey, ModFileManifestModel?)> GetModFileManifestAndKeyAsync(DataBasePackedFile package)
    {
        ArgumentNullException.ThrowIfNull(package);
        ResourceKey foundKey = default;
        ModFileManifestModel? modFileManifest = null;
        try
        {
            using var cts = new CancellationTokenSource();
            await package.ForEachAsync(ResourceKeyOrder.TypeGroupInstance, key => key.Type is ResourceType.SnippetTuning, (key, content) =>
            {
                try
                {
                    modFileManifest = Decode(content);
                    foundKey = key;
                    cts.Cancel();
                }
                catch (XmlException)
                {
                }
                return Task.CompletedTask;
            }, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        return (foundKey, modFileManifest);
    }

    /// <summary>
    /// Gets the mod file manifest for the specified <paramref name="package"/>, asynchronously, if it has one
    /// </summary>
    public static async Task<ModFileManifestModel?> GetModFileManifestAsync(DataBasePackedFile package)
    {
        ArgumentNullException.ThrowIfNull(package);
        ModFileManifestModel? modFileManifest = null;
        try
        {
            using var cts = new CancellationTokenSource();
            await package.ForEachAsync(ResourceKeyOrder.TypeGroupInstance, key => key.Type is ResourceType.SnippetTuning, (key, content) =>
            {
                try
                {
                    modFileManifest = Decode(content);
                    cts.Cancel();
                }
                catch (XmlException)
                {
                }
                return Task.CompletedTask;
            }, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        return modFileManifest;
    }

    /// <summary>
    /// Gets the mod file manifest for the specified <paramref name="scriptMod"/>, asynchronously, if it has one
    /// </summary>
    public static async Task<ModFileManifestModel?> GetModFileManifestAsync(ZipArchive scriptMod)
    {
        ArgumentNullException.ThrowIfNull(scriptMod);
        if (scriptMod.Entries.OrderBy(entry => entry.Name == entry.FullName ? 0 : 1).ThenBy(entry => entry.FullName, StringComparer.Ordinal).FirstOrDefault(entry => entry.Name.Equals(zipArchiveManifestName, StringComparison.OrdinalIgnoreCase)) is { } manifestEntry)
        {
            using var manifestStream = manifestEntry.Open();
            using var manifestReader = new StreamReader(manifestStream);
            if (TryParse(await manifestReader.ReadToEndAsync().ConfigureAwait(false), out var modFileManifest))
                return modFileManifest;
        }
        return null;
    }

    /// <summary>
    /// Gets the mod file manifests for the specified <paramref name="package"/> (for use by editors checking for packages which may have been merged by manifest unaware tooling)
    /// </summary>
    public static IReadOnlyDictionary<ResourceKey, ModFileManifestModel> GetModFileManifests(DataBasePackedFile package)
    {
        ArgumentNullException.ThrowIfNull(package);
        var result = new Dictionary<ResourceKey, ModFileManifestModel>();
        package.ForEach(ResourceKeyOrder.TypeGroupInstance, key => key.Type is ResourceType.SnippetTuning, (key, content) =>
        {
            try
            {
                result.Add(key, Decode(content));
            }
            catch (XmlException)
            {
            }
        });
        return result;
    }

    /// <summary>
    /// Gets the mod file manifests for the specified <paramref name="package"/>, asynchronously (for use by editors checking for packages which may have been merged by manifest unaware tooling)
    /// </summary>
    public static async Task<IReadOnlyDictionary<ResourceKey, ModFileManifestModel>> GetModFileManifestsAsync(DataBasePackedFile package)
    {
        ArgumentNullException.ThrowIfNull(package);
        var result = new Dictionary<ResourceKey, ModFileManifestModel>();
        await package.ForEachAsync(ResourceKeyOrder.TypeGroupInstance, key => key.Type is ResourceType.SnippetTuning, (key, content) =>
        {
            try
            {
                result.Add(key, Decode(content));
            }
            catch (XmlException)
            {
            }
            return Task.CompletedTask;
        }).ConfigureAwait(false);
        return result;
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
    /// Set the specified <paramref name="manifest"/> for the specified <paramref name="package"/>
    /// </summary>
    public static void SetModFileManifest(DataBasePackedFile package, ModFileManifestModel manifest)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(manifest);
        package.Set(new ResourceKey(ResourceType.SnippetTuning, 0x80000000, manifest.TuningFullInstance), manifest);
    }

    /// <summary>
    /// Set the specified <paramref name="manifest"/> for the specified <paramref name="scriptMod"/>
    /// </summary>
    public static void SetModFileManifest(ZipArchive scriptMod, ModFileManifestModel manifest)
    {
        ArgumentNullException.ThrowIfNull(scriptMod);
        ArgumentNullException.ThrowIfNull(manifest);
        var manifestEntryFullName = zipArchiveManifestName;
        var pathSplitEntries = scriptMod.Entries.Select(entry => entry.FullName.Split('/')).ToImmutableArray();
        if (pathSplitEntries.All(pathSplitEntry => pathSplitEntry.Length is > 1)
            && pathSplitEntries.Select(pathSplitEntry => pathSplitEntry[0]).Distinct(StringComparer.OrdinalIgnoreCase).Count() is 1)
            manifestEntryFullName = $"{pathSplitEntries[0][0]}/{manifestEntryFullName}";
        var manifestEntry = scriptMod.CreateEntry(manifestEntryFullName);
        using var manifestEntryStream = manifestEntry.Open();
        using var manifestEntryStreamWriter = new StreamWriter(manifestEntryStream);
        manifestEntryStreamWriter.WriteLine(manifest.ToString());
        manifestEntryStreamWriter.Flush();
    }

    /// <summary>
    /// Set the specified <paramref name="manifest"/> for the specified <paramref name="package"/>, asynchronously
    /// </summary>
    public static Task SetModFileManifestAsync(DataBasePackedFile package, ModFileManifestModel manifest)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(manifest);
        return package.SetAsync(new ResourceKey(ResourceType.SnippetTuning, 0x80000000, manifest.TuningFullInstance), manifest);
    }

    /// <summary>
    /// Set the specified <paramref name="manifest"/> for the specified <paramref name="scriptMod"/>, asynchronously
    /// </summary>
    public static async Task SetModFileManifestAsync(ZipArchive scriptMod, ModFileManifestModel manifest)
    {
        ArgumentNullException.ThrowIfNull(scriptMod);
        ArgumentNullException.ThrowIfNull(manifest);
        var manifestEntryFullName = zipArchiveManifestName;
        var pathSplitEntries = scriptMod.Entries.Select(entry => entry.FullName.Split('/')).ToImmutableArray();
        if (pathSplitEntries.All(pathSplitEntry => pathSplitEntry.Length is > 1)
            && pathSplitEntries.Select(pathSplitEntry => pathSplitEntry[0]).Distinct(StringComparer.Ordinal).Count() is 1)
            manifestEntryFullName = $"{pathSplitEntries[0][0]}/{manifestEntryFullName}";
        var manifestEntry = scriptMod.CreateEntry(manifestEntryFullName);
        using var manifestEntryStream = manifestEntry.Open();
        using var manifestEntryStreamWriter = new StreamWriter(manifestEntryStream);
        await manifestEntryStreamWriter.WriteLineAsync(manifest.ToString()).ConfigureAwait(false);
        await manifestEntryStreamWriter.FlushAsync().ConfigureAwait(false);
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
    /// Gets/sets the email address which can be used to contact this mod's creator
    /// </summary>
    [YamlMember(Order = 5, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets/sets the URL which can be used to contact this mod's creator
    /// </summary>
    [YamlMember(Order = 6, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public Uri? ContactUrl { get; set; }

    /// <summary>
    /// Gets the names of the creators of the mod
    /// </summary>
    [YamlMember(Order = 2, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<string> Creators { get; private set; } = [];

    /// <summary>
    /// Gets the globally unique names of the exclusivities of this mod, causing it to be incompatible with other mods which share one or more of them
    /// </summary>
    [YamlMember(Order = 15, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<string> Exclusivities { get; private set; } = [];

    /// <summary>
    /// Gets the names of the features unique to this mod which it offers to other mods as a dependency
    /// </summary>
    [YamlMember(Order = 14, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<string> Features { get; private set; } = [];

    /// <summary>
    /// Gets/sets the hash of the mod file
    /// </summary>
    [YamlMember(Order = 11, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public ImmutableArray<byte> Hash { get; set; }

    /// <summary>
    /// Gets the resource keys for the resources included in the <see cref="Hash"/>
    /// </summary>
    [YamlMember(Order = 12, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public HashSet<ResourceKey> HashResourceKeys { get; private set; } = [];

    /// <summary>
    /// Gets the list of pack codes identifying the packs incompatible with this mod (e.g. "EP01" for Get to Work)
    /// </summary>
    [YamlMember(Order = 18, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<string> IncompatiblePacks { get; private set; } = [];

    /// <summary>
    /// Gets/sets the message to translators by this mod's creator
    /// </summary>
    [YamlMember(Order = 8, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? MessageToTranslators { get; set; }

    /// <summary>
    /// Gets/sets the name of the mod
    /// </summary>
    [YamlMember(Order = 1, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public required string Name { get; set; }

    /// <summary>
    /// Gets the list of languages repurposed by this mod
    /// </summary>
    [YamlMember(Order = 10, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<ModFileManifestModelRepurposedLanguage> RepurposedLanguages { get; private set; } = [];

    /// <summary>
    /// Gets the list of mods required by this mod
    /// </summary>
    [YamlMember(Order = 19, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<ModFileManifestModelRequiredMod> RequiredMods { get; private set; } = [];

    /// <summary>
    /// Gets the list of pack codes identifying the packs required by this mod (e.g. "EP01" for Get to Work)
    /// </summary>
    [YamlMember(Order = 16, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<string> RequiredPacks { get; private set; } = [];

    /// <summary>
    /// Gets/sets the promo code it is suggested the player use during check out in the EA Store if purchasing a pack for use with this mod
    /// </summary>
    [YamlMember(Order = 17, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? ElectronicArtsPromoCode { get; set; }

    /// <inheritdoc/>
    [YamlIgnore]
    public override string? ResourceName =>
        TuningName;

    /// <summary>
    /// Gets the hashes of previous versions of this mod in for which I stand even though my hash is different
    /// </summary>
    [YamlMember(Order = 13, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public HashSet<ImmutableArray<byte>> SubsumedHashes { get; private set; } = [];

    /// <summary>
    /// Gets/sets the URL at which translators may submit translations to this mod's creator
    /// </summary>
    [YamlMember(Order = 9, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public Uri? TranslationSubmissionUrl { get; set; }

    /// <summary>
    /// Gets the list of mods required by this mod
    /// </summary>
    [YamlMember(Order = 7, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<ModFileManifestModelTranslator> Translators { get; private set; } = [];

    /// <summary>
    /// Gets the <see cref="ResourceType.SnippetTuning"/> name for the mod file manifest
    /// </summary>
    [YamlMember(Order = -1, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? TuningName { get; set; }

    /// <summary>
    /// Gets the <see cref="ResourceType.SnippetTuning"/> decimal conversion of the <see cref="ResourceKey.FullInstance"/> for the mod file manifest
    /// </summary>
    [YamlMember(Order = -2, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public ulong TuningFullInstance { get; set; }

    /// <summary>
    /// Gets/sets the URL to which players can go to find more information about this mod
    /// </summary>
    [YamlMember(Order = 4, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public Uri? Url { get; set; }

    /// <summary>
    /// Gets/sets the version of this mod
    /// </summary>
    [YamlMember(Order = 3, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public string? Version { get; set; }

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
        if (reader.GetAttribute("i") != tuningInstance)
            throw new XmlException($"Expected the \"{tuningInstance}\" instance");
        if (reader.GetAttribute("m") != tuningModule)
            throw new XmlException($"Expected the \"{tuningModule}\" module");
        if (reader.GetAttribute("c") != tuningClass)
            throw new XmlException($"Expected the \"{tuningClass}\" class");
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
                else if (tunableName == "exclusivities")
                    Exclusivities.AddRange(reader.ReadTunableList());
                else if (tunableName == "features")
                    Features.AddRange(reader.ReadTunableList());
                else if (tunableName == "hash_resource_keys")
                    HashResourceKeys.AddRangeImmediately(reader.ReadTunableList().Select(ResourceKey.Parse));
                else if (tunableName == "incompatible_packs")
                    IncompatiblePacks.AddRange(reader.ReadTunableList());
                else if (tunableName == "repurposed_languages")
                    RepurposedLanguages.AddRange(reader.ReadTunableTupleList<ModFileManifestModelRepurposedLanguage>());
                else if (tunableName == "required_mods")
                    RequiredMods.AddRange(reader.ReadTunableTupleList<ModFileManifestModelRequiredMod>());
                else if (tunableName == "required_packs")
                    RequiredPacks.AddRange(reader.ReadTunableList());
                else if (tunableName == "subsumed_hashes")
                    SubsumedHashes.AddRangeImmediately(reader.ReadTunableList().Select(hex => hex.ToByteSequence().ToImmutableArray()));
                else if (tunableName == "translators")
                    Translators.AddRange(reader.ReadTunableTupleList<ModFileManifestModelTranslator>());
            }
            else
            {
                var tunableValue = reader.ReadElementContentAsString();
                if (tunableName == "contact_email")
                    ContactEmail = tunableValue;
                else if (tunableName == "contact_url")
                    ContactUrl = new Uri(tunableValue, UriKind.Absolute);
                else if (tunableName == "ea_promo_code")
                    ElectronicArtsPromoCode = tunableValue;
                else if (tunableName == "message_to_translators")
                    MessageToTranslators = tunableValue;
                else if (tunableName == "name")
                    Name = tunableValue;
                else if (tunableName == "hash")
                    Hash = tunableValue.ToByteSequence().ToImmutableArray();
                else if (tunableName == "translation_submission_url")
                    TranslationSubmissionUrl = new Uri(tunableValue, UriKind.Absolute);
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
        writer.WriteStartElement("I");
        writer.WriteAttributeString("c", tuningClass);
        writer.WriteAttributeString("i", tuningInstance);
        writer.WriteAttributeString("m", tuningModule);
        writer.WriteAttributeString("n", TuningName);
        writer.WriteAttributeString("s", TuningFullInstance.ToString());
        writer.WriteTunable("name", Name);
        writer.WriteTunableList("creators", Creators);
        writer.WriteTunable("version", Version);
        writer.WriteTunable("url", Url);
        writer.WriteTunable("contact_email", ContactEmail);
        writer.WriteTunable("contact_url", ContactUrl);
        writer.WriteTunableList("translators", Translators);
        writer.WriteTunable("message_to_translators", MessageToTranslators);
        writer.WriteTunable("translation_submission_url", TranslationSubmissionUrl);
        writer.WriteTunableList("repurposed_languages", RepurposedLanguages);
        writer.WriteTunable("hash", Hash);
        writer.WriteTunableList("hash_resource_keys", HashResourceKeys);
        writer.WriteTunableList("subsumed_hashes", SubsumedHashes);
        writer.WriteTunableList("features", Features);
        writer.WriteTunableList("exclusivities", Exclusivities);
        writer.WriteTunableList("required_packs", RequiredPacks);
        writer.WriteTunable("ea_promo_code", ElectronicArtsPromoCode);
        writer.WriteTunableList("incompatible_packs", IncompatiblePacks);
        writer.WriteTunableList("required_mods", RequiredMods);
        writer.WriteEndElement();
    }

    #endregion IXmlSerializable
}
