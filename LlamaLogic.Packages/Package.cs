namespace LlamaLogic.Packages;

/// <summary>
/// Represents a package
/// </summary>
public class Package :
    IAsyncDisposable,
    IDisposable
{
    const int unloadedResourceStreamCopyBufferSize = 81_920; // 80KB

    static readonly JsonWriterOptions jsonWriterOptions = new()
    {
        Indented = true
    };

    static readonly XmlWriterSettings markupWriterSettings = new()
    {
        Indent = true,
        OmitXmlDeclaration = false,
        Encoding = Encoding.UTF8
    };

    static readonly ImmutableHashSet<PackageResourceType> tuningMarkupTypes = Enum
#if IS_NET_6_0_OR_GREATER
        .GetValues<PackageResourceType>()
#else
        .GetValues(typeof(PackageResourceType)).Cast<PackageResourceType>()
#endif
        .Where(resourceType => typeof(PackageResourceType).GetMember(resourceType.ToString()).FirstOrDefault()?.GetCustomAttribute<PackageResourceFileTypeAttribute>()?.PackageResourceFileType is PackageResourceFileType.TuningMarkup).ToImmutableHashSet();

    /// <summary>
    /// Instantiates a <see cref="Package"/> by reading from the specified <paramref name="stream"/>
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c></exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> cannot seek or cannot read</exception>
    /// <exception cref="EndOfStreamException">encountered unexpected end of stream while reading package header or index from <paramref name="stream"/></exception>
    public static Package FromStream(Stream stream)
    {
#if IS_NET_6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(stream);
#else
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));
#endif
        if (!stream.CanSeek)
            throw new ArgumentException("stream cannot seek", nameof(stream));
        if (!stream.CanRead)
            throw new ArgumentException("stream cannot read", nameof(stream));
        var package = new Package(stream);
        Span<byte> header = stackalloc byte[96];
        stream.Seek(0, SeekOrigin.Begin);
        if (stream.Read(header) != 96)
            throw new EndOfStreamException("encountered unexpected end of stream while reading header");
        var (indexCount, indexSize, indexPosition) = ParseHeader(header);
        stream.Seek((long)indexPosition, SeekOrigin.Begin);
        Span<byte> index = new byte[(int)indexSize];
        if (stream.Read(index) != indexSize)
            throw new EndOfStreamException("encountered unexpected end of stream while reading index");
        package.LoadIndex(index, indexCount);
        return package;
    }

    /// <summary>
    /// Instantiates a <see cref="Package"/> by reading asynchronously from the specified <paramref name="stream"/>
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c></exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> cannot seek or cannot read</exception>
    /// <exception cref="EndOfStreamException">encountered unexpected end of stream while reading package header or index from <paramref name="stream"/></exception>
    public static async Task<Package> FromStreamAsync(Stream stream)
    {
#if IS_NET_6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(stream);
#else
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));
#endif
        if (!stream.CanSeek)
            throw new ArgumentException("stream cannot seek", nameof(stream));
        if (!stream.CanRead)
            throw new ArgumentException("stream cannot read", nameof(stream));
        var package = new Package(stream);
        Memory<byte> header = new byte[96];
        stream.Seek(0, SeekOrigin.Begin);
        if (await stream.ReadAsync(header).ConfigureAwait(false) != 96)
            throw new EndOfStreamException("encountered unexpected end of stream while reading package header");
        var (indexCount, indexSize, indexPosition) = ParseHeader(header.Span);
        stream.Seek((long)indexPosition, SeekOrigin.Begin);
        Memory<byte> index = new byte[indexSize];
        if (await stream.ReadAsync(index).ConfigureAwait(false) != indexSize)
            throw new EndOfStreamException("encountered unexpected end of stream while reading index");
        package.LoadIndex(index.Span, indexCount);
        return package;
    }

    static string? GetTuningMarkupResourceName(Stream tuningMarkupStream)
    {
        try
        {
            using var tuningMarkupReader = XmlReader.Create(tuningMarkupStream);
            var continuedReading = false;
            while ((continuedReading = tuningMarkupReader.Read()) && tuningMarkupReader.NodeType is not XmlNodeType.Element)
            {
            }
            if (continuedReading && tuningMarkupReader.GetAttribute("n") is { } resourceName)
                return resourceName;
        }
        catch
        {
        }
        return null;
    }

    static async ValueTask<string?> GetTuningMarkupResourceNameAsync(Stream tuningMarkupStream)
    {
        try
        {
            using var tuningMarkupReader = XmlReader.Create(tuningMarkupStream, new XmlReaderSettings { Async = true });
            var continuedReading = false;
            while ((continuedReading = await tuningMarkupReader.ReadAsync().ConfigureAwait(false)) && tuningMarkupReader.NodeType is not XmlNodeType.Element)
            {
            }
            if (continuedReading && tuningMarkupReader.GetAttribute("n") is { } resourceName)
                return resourceName;
        }
        catch
        {
        }
        return null;
    }

    static (uint indexCount, uint indexSize, ulong indexPosition) ParseHeader(ReadOnlySpan<byte> header)
    {
        header.ReadBinaryStruct(out BinaryPackageHeader packageHeader);
        packageHeader.Check();
        return (packageHeader.IndexCount, packageHeader.IndexSize, packageHeader.LongIndexPosition);
    }

    /// <summary>
    /// Instantiates a new package
    /// </summary>
    public Package()
    {
    }

    Package(Stream stream) =>
        this.stream = stream;

    /// <summary>
    /// Finalizes the package
    /// </summary>
    ~Package() =>
        Dispose(false);

    readonly Dictionary<PackageResourceKey, ReadOnlyMemory<byte>> loadedResources = [];
    Dictionary<string, HashSet<PackageResourceKey>>? resourceKeysByName;
    Dictionary<PackageResourceKey, string>? resourceNameByKey;
    readonly Stream? stream;
    readonly Dictionary<PackageResourceKey, PackageIndexEntry> unloadedResources = [];

    void AddIndexedResourceName(PackageResourceKey key, string resourceName)
    {
        resourceNameByKey ??= [];
        resourceNameByKey.TryAdd(key, resourceName);
        resourceKeysByName ??= [];
#if IS_NET_6_0_OR_GREATER
        ref var keys = ref CollectionsMarshal.GetValueRefOrAddDefault(resourceKeysByName, resourceName, out var keysExisted);
        if (!keysExisted)
            keys = [key];
        else
            keys!.Add(key);
#else
        if (resourceKeysByName.TryGetValue(resourceName, out var keys))
            keys.Add(key);
        else
            resourceKeysByName.Add(resourceName, [key]);
#endif
    }

    ReadOnlyCollection<PackageResourceKey> CopyResourceKeysByName(string name)
    {
        IEnumerable<PackageResourceKey> enumerable = [];
        if (resourceKeysByName?.TryGetValue(name, out var keys) ?? false)
            enumerable = keys;
        return enumerable.ToList().AsReadOnly();
    }

    /// <summary>
    /// Deletes the resource with the specified <paramref name="key"/> and returns <c>true</c> if it was found; otherwise, <c>false</c>
    /// </summary>
    public bool DeleteResource(PackageResourceKey key)
    {
        var removed = unloadedResources.Remove(key) || loadedResources.Remove(key);
        if (removed)
            RemoveIndexedResourceName(key);
        return removed;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases all resources of the package
    /// </summary>
    /// <param name="disposing"><c>true</c> if <see cref="Dispose()"/> was called; otherwise, <c>false</c></param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            stream?.Dispose();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases all resources of the package
    /// </summary>
    /// <param name="disposing"><c>true</c> if <see cref="DisposeAsync()"/> was called; otherwise, <c>false</c></param>
    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            if (stream is not null)
                await stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the content for the resource with the specified <paramref name="key"/>
    /// </summary>
    public ReadOnlyMemory<byte> GetResourceContent(PackageResourceKey key)
    {
#if IS_NET_6_0_OR_GREATER
        ref var indexEntry = ref CollectionsMarshal.GetValueRefOrNullRef(unloadedResources, key);
        if (!Unsafe.IsNullRef(ref indexEntry))
            return LoadResourceContentFromStream(indexEntry);
        ref var loadedResource = ref CollectionsMarshal.GetValueRefOrNullRef(loadedResources, key);
        if (!Unsafe.IsNullRef(ref loadedResource))
            return loadedResource;
#else
        if (unloadedResources.TryGetValue(key, out var indexEntry))
            return LoadResourceContentFromStream(indexEntry);
        if (loadedResources.TryGetValue(key, out var alteredResource))
            return alteredResource;
#endif
        return default;
    }

    /// <summary>
    /// Gets the content for the resource with the specified <paramref name="key"/> asynchronously
    /// </summary>
    public Task<ReadOnlyMemory<byte>> GetResourceContentAsync(PackageResourceKey key)
    {
#if IS_NET_6_0_OR_GREATER
        ref var indexEntry = ref CollectionsMarshal.GetValueRefOrNullRef(unloadedResources, key);
        if (!Unsafe.IsNullRef(ref indexEntry))
            return LoadResourceContentFromStreamAsync(indexEntry);
        ref var loadedResource = ref CollectionsMarshal.GetValueRefOrNullRef(loadedResources, key);
        if (!Unsafe.IsNullRef(ref loadedResource))
            return Task.FromResult(loadedResource);
#else
        if (unloadedResources.TryGetValue(key, out var indexEntry))
            return LoadResourceContentFromStreamAsync(indexEntry);
        if (loadedResources.TryGetValue(key, out var alteredResource))
            return Task.FromResult(alteredResource);
#endif
        return Task.FromResult(default(ReadOnlyMemory<byte>));
    }

    /// <summary>
    /// Gets the content for the resource with the specified <paramref name="key"/> as a <see cref="DataResource"/>
    /// </summary>
    public DataResource GetResourceContentAsData(PackageResourceKey key) =>
        new(GetResourceContent(key));

    /// <summary>
    /// Gets the content for the resource with the specified <paramref name="key"/> as a <see cref="DataResource"/> asynchronously
    /// </summary>
    public async Task<DataResource> GetResourceContentAsDataAsync(PackageResourceKey key) =>
        new(await GetResourceContentAsync(key).ConfigureAwait(false));

    /// <summary>
    /// Gets the content for the resource with the specified <paramref name="key"/> as a <see cref="JsonDocument"/>
    /// </summary>
    public JsonDocument GetResourceContentAsJsonDocument(PackageResourceKey key) =>
        JsonDocument.Parse(GetResourceContentAsText(key));

    /// <summary>
    /// Gets the content for the resource with the specified <paramref name="key"/> as a <see cref="JsonDocument"/> asynchronously
    /// </summary>
    public async Task<JsonDocument> GetResourceContentAsJsonDocumentAsync(PackageResourceKey key) =>
        JsonDocument.Parse(await GetResourceContentAsTextAsync(key).ConfigureAwait(false));

    /// <summary>
    /// Gets the content for the resource with the specified <paramref name="key"/> as a <see cref="string"/>
    /// </summary>
    public string GetResourceContentAsText(PackageResourceKey key) =>
        Encoding.UTF8.GetString(GetResourceContent(key).Span);

    /// <summary>
    /// Gets the content for the resource with the specified <paramref name="key"/> as a <see cref="string"/> asynchronously
    /// </summary>
    public async Task<string> GetResourceContentAsTextAsync(PackageResourceKey key) =>
        Encoding.UTF8.GetString((await GetResourceContentAsync(key).ConfigureAwait(false)).Span);

    /// <summary>
    /// Gets the content for the resource with the specified <paramref name="key"/> as an <see cref="XDocument"/>
    /// </summary>
    public XDocument GetResourceContentAsXDocument(PackageResourceKey key) =>
        XDocument.Parse(GetResourceContentAsText(key));

    /// <summary>
    /// Gets the content for the resource with the specified <paramref name="key"/> as an <see cref="XDocument"/> asynchronously
    /// </summary>
    public async Task<XDocument> GetResourceContentAsXDocumentAsync(PackageResourceKey key) =>
        XDocument.Parse(await GetResourceContentAsTextAsync(key).ConfigureAwait(false));

    /// <summary>
    /// Gets the size (in memory) of the content of the resource with the specified <paramref name="key"/>, or <c>null</c> if it is not in the package
    /// </summary>
    public uint? GetResourceContentSize(PackageResourceKey key)
    {
#if IS_NET_6_0_OR_GREATER
        ref var indexEntry = ref CollectionsMarshal.GetValueRefOrNullRef(unloadedResources, key);
        if (!Unsafe.IsNullRef(ref indexEntry))
            return indexEntry.Size;
        ref var alteredResource = ref CollectionsMarshal.GetValueRefOrNullRef(loadedResources, key);
        if (!Unsafe.IsNullRef(ref alteredResource))
            return (uint)alteredResource.Length;
#else
        if (unloadedResources.TryGetValue(key, out var indexEntry))
            return indexEntry.Size;
        if (loadedResources.TryGetValue(key, out var alteredResource))
            return (uint)alteredResource.Length;
#endif
        return default;
    }

    Stream GetResourceContentStream(PackageIndexEntry entry)
    {
        if (stream is null)
            throw new InvalidOperationException("package was not loaded from stream");
        stream.Seek(entry.Position, SeekOrigin.Begin);
        var contentStream = new ReadOnlySubStream(stream, new Range(Index.FromStart((int)entry.Position), Index.FromStart((int)entry.Position + (int)entry.SizeCompressed)));
        if (entry.IsCompressed)
            return new InflaterInputStream(contentStream);
        return contentStream;
    }

    /// <summary>
    /// Gets a list of keys for resources with the specified <paramref name="name"/>
    /// </summary>
    public IReadOnlyList<PackageResourceKey> GetResourceKeysByName(string name)
    {
        LoadResourceNames();
        return CopyResourceKeysByName(name);
    }

    /// <summary>
    /// Gets a list of keys for resources with the specified <paramref name="name"/> asynchronously
    /// </summary>
    public async Task<IReadOnlyList<PackageResourceKey>> GetResourceKeysByNameAsync(string name)
    {
        await LoadResourceNamesAsync().ConfigureAwait(false);
        return CopyResourceKeysByName(name);
    }

    /// <summary>
    /// Gets the name for the resource with the specified <paramref name="key"/>
    /// </summary>
    public string? GetResourceNameByKey(PackageResourceKey key)
    {
        LoadResourceNames();
        return (resourceNameByKey?.TryGetValue(key, out var resourceName) ?? false) ? resourceName : null;
    }

    /// <summary>
    /// Gets the name for the resource with the specified <paramref name="key"/> asynchronously
    /// </summary>
    public async Task<string?> GetResourceNameByKeyAsync(PackageResourceKey key)
    {
        await LoadResourceNamesAsync().ConfigureAwait(false);
        return (resourceNameByKey?.TryGetValue(key, out var resourceName) ?? false) ? resourceName : null;
    }

    /// <summary>
    /// Gets the names for all the resources in the package
    /// </summary>
    public IReadOnlyList<string> GetResourceNames()
    {
        LoadResourceNames();
        return (resourceKeysByName?.Keys ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets the names for all the resources in the package asynchronously
    /// </summary>
    public async Task<IReadOnlyList<string>> GetResourceNamesAsync()
    {
        await LoadResourceNamesAsync().ConfigureAwait(false);
        return (resourceKeysByName?.Keys ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets all the keys for all the resources in the package
    /// </summary>
    public IReadOnlyList<PackageResourceKey> GetResourceKeys() =>
        unloadedResources.Keys.Concat(loadedResources.Keys).ToList().AsReadOnly();

    (ArrayBufferWriter<byte> index, bool writeTypes, bool writeGroups, bool writeHighOrderInstances) InitializeIndex()
    {
        var distinctTypes = new HashSet<PackageResourceType>();
        var distinctGroups = new HashSet<uint>();
        var distinctHighOrderInstances = new HashSet<uint>();
        foreach (var key in unloadedResources.Keys.Concat(loadedResources.Keys))
        {
            distinctTypes.Add(key.Type);
            distinctGroups.Add(key.Group);
            distinctHighOrderInstances.Add(key.HighOrderInstance);
        }
        var writeTypes = distinctTypes.Count > 1;
        var writeGroups = distinctGroups.Count > 1;
        var writeHighOrderInstances = distinctHighOrderInstances.Count > 1;
        var totalEntries = unloadedResources.Count + loadedResources.Count;
        var index = new ArrayBufferWriter<byte>
        (
              sizeof(PackageIndexType)
            + (writeTypes ? 0 : sizeof(PackageResourceType))
            + (writeGroups ? 0 : sizeof(uint))
            + (writeHighOrderInstances ? 0 : sizeof(uint))
            +
            (
                  (writeTypes ? sizeof(PackageResourceType) : 0)
                + (writeGroups ? sizeof(uint) : 0)
                + (writeHighOrderInstances ? sizeof(uint) : 0)
                + sizeof(uint) // low order instance
                + sizeof(uint) // position
                + sizeof(uint) // file size
                + sizeof(uint) // memory size
                + sizeof(ushort) // compression
                + sizeof(ushort) // ?
            )
            * totalEntries
        );
        var indexType =
            (writeTypes ? PackageIndexType.Default : PackageIndexType.NoMoreThanOneType) |
            (writeGroups ? PackageIndexType.Default : PackageIndexType.NoMoreThanOneGroup) |
            (writeHighOrderInstances ? PackageIndexType.Default : PackageIndexType.NoMoreThanOneHighOrderInstance);
        index.Write(ref indexType);
        if (distinctTypes.Count == 1)
        {
            var distinctType = distinctTypes.First();
            index.Write(ref distinctType);
        }
        if (distinctGroups.Count == 1)
        {
            var distinctGroup = distinctGroups.First();
            index.Write(ref distinctGroup);
        }
        if (distinctHighOrderInstances.Count == 1)
        {
            var distinctHighOrderInstance = distinctHighOrderInstances.First();
            index.Write(ref distinctHighOrderInstance);
        }
        return (index, writeTypes, writeGroups, writeHighOrderInstances);
    }

    void LoadIndex(ReadOnlySpan<byte> index, uint indexCount)
    {
        var readPosition = 0U;
        var indexType = index.ReadAndAdvancePosition<PackageIndexType>(ref readPosition);
        PackageResourceType? onlyType = null;
        if ((indexType & PackageIndexType.NoMoreThanOneType) > 0)
            onlyType = index.ReadAndAdvancePosition<PackageResourceType>(ref readPosition);
        uint? onlyGroup = null;
        if ((indexType & PackageIndexType.NoMoreThanOneGroup) > 0)
            onlyGroup = index.ReadAndAdvancePosition<uint>(ref readPosition);
        uint? onlyHighOrderInstance = null;
        if ((indexType & PackageIndexType.NoMoreThanOneHighOrderInstance) > 0)
            onlyHighOrderInstance = index.ReadAndAdvancePosition<uint>(ref readPosition);
        for (var i = 0; i < indexCount; ++i)
        {
            var type = onlyType ?? index.ReadAndAdvancePosition<PackageResourceType>(ref readPosition);
            var group = onlyGroup ?? index.ReadAndAdvancePosition<uint>(ref readPosition);
            var highOrderInstance = onlyHighOrderInstance ?? index.ReadAndAdvancePosition<uint>(ref readPosition);
            index.ReadBinaryStructAndAdvancePosition(out BinaryIndexEntry binaryEntry, ref readPosition);
            binaryEntry.Read(type, group, highOrderInstance, out var key, out var entry);
            unloadedResources.Add(key, entry);
        }
    }

    ReadOnlyMemory<byte> LoadResourceContentFromStream(PackageIndexEntry entry)
    {
        Memory<byte> resourceContent = new byte[entry.Size];
        using var resourceContentStream = GetResourceContentStream(entry);
        resourceContentStream.Read(resourceContent.Span);
        return resourceContent;
    }

    async Task<ReadOnlyMemory<byte>> LoadResourceContentFromStreamAsync(PackageIndexEntry entry)
    {
        Memory<byte> resourceContent = new byte[entry.Size];
        using var resourceContentStream = GetResourceContentStream(entry);
        await resourceContentStream.ReadAsync(resourceContent).ConfigureAwait(false);
        return resourceContent;
    }

    void LoadResourceNames()
    {
        foreach (var kv in unloadedResources.Where(keyAndEntry => tuningMarkupTypes.Contains(keyAndEntry.Key.Type)))
        {
            var key = kv.Key;
#pragma warning disable CA2000 // Dispose objects before losing scope
            using var resourceContentStream = GetResourceContentStream(kv.Value);
#pragma warning restore CA2000 // Dispose objects before losing scope
            if (GetTuningMarkupResourceName(resourceContentStream) is { } resourceName)
                AddIndexedResourceName(key, resourceName);
        }
        foreach (var kv in loadedResources.Where(keyAndContent => tuningMarkupTypes.Contains(keyAndContent.Key.Type)))
        {
            var key = kv.Key;
#pragma warning disable CA2000 // Dispose objects before losing scope
            using var resourceContentStream = new ReadOnlyMemoryOfBytesStream(kv.Value);
#pragma warning restore CA2000 // Dispose objects before losing scope
            if (GetTuningMarkupResourceName(resourceContentStream) is { } resourceName)
                AddIndexedResourceName(key, resourceName);
        }
    }

    async ValueTask LoadResourceNamesAsync()
    {
        foreach (var kv in unloadedResources.Where(keyAndEntry => tuningMarkupTypes.Contains(keyAndEntry.Key.Type)))
        {
            var key = kv.Key;
#pragma warning disable CA2000 // Dispose objects before losing scope
            using var resourceContentStream = GetResourceContentStream(kv.Value);
#pragma warning restore CA2000 // Dispose objects before losing scope
            if (await GetTuningMarkupResourceNameAsync(resourceContentStream).ConfigureAwait(false) is { } resourceName)
                AddIndexedResourceName(key, resourceName);
        }
        foreach (var kv in loadedResources.Where(keyAndContent => tuningMarkupTypes.Contains(keyAndContent.Key.Type)))
        {
            var key = kv.Key;
#pragma warning disable CA2000 // Dispose objects before losing scope
            using var resourceContentStream = new ReadOnlyMemoryOfBytesStream(kv.Value);
#pragma warning restore CA2000 // Dispose objects before losing scope
            if (await GetTuningMarkupResourceNameAsync(resourceContentStream).ConfigureAwait(false) is { } resourceName)
                AddIndexedResourceName(key, resourceName);
        }
    }

    void RemoveIndexedResourceName(PackageResourceKey key)
    {
        if ((resourceNameByKey?.Remove(key, out var resourceName) ?? false) && (resourceKeysByName?.TryGetValue(resourceName, out var keys) ?? false))
        {
            keys.Remove(key);
            if (keys.Count == 0)
                resourceKeysByName.Remove(resourceName);
        }
    }

    /// <summary>
    /// Saves the package to the specified <paramref name="stream"/>
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c></exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> cannot seek or cannot write or is the same stream that was used to open the package</exception>
    /// <exception cref="InvalidOperationException">an internal error has occurred described by the exception message</exception>
    /// <exception cref="EndOfStreamException">encountered unexpected end of stream while reading original package</exception>
    public void SaveTo(Stream stream)
    {
#if IS_NET_6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(stream);
#else
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));
#endif
        if (ReferenceEquals(this.stream, stream))
            throw new ArgumentException("cannot save to the stream from which the package was opened -- save to a temporary location, dispose this object, and then overwrite the original package file with the new one");
        if (!stream.CanSeek)
            throw new ArgumentException("stream cannot seek", nameof(stream));
        if (!stream.CanWrite)
            throw new ArgumentException("stream cannot write", nameof(stream));
        Span<byte> header = stackalloc byte[96];
        stream.Write(header);
        var (index, writeTypes, writeGroups, writeHighOrderInstances) = InitializeIndex();
        if (unloadedResources.Count > 0)
        {
            if (this.stream is not { } originalStream)
                throw new InvalidOperationException("unloaded resources are cataloged without a stream reference");
            Span<byte> resourceBuffer = new byte[unloadedResourceStreamCopyBufferSize];
            foreach (var key in unloadedResources.Keys)
            {
                var binaryIndexEntry = new BinaryIndexEntry();
                key.WriteIndexComponent(index, writeTypes, writeGroups, writeHighOrderInstances, ref binaryIndexEntry);
                stream.WriteIndexComponent(ref binaryIndexEntry);
                var entry = unloadedResources[key];
                entry.WriteIndexComponent(ref binaryIndexEntry);
                index.WriteBinaryStruct(ref binaryIndexEntry);
                originalStream.Seek(entry.Position, SeekOrigin.Begin);
                var totalResourceRead = 0;
                while (totalResourceRead < entry.SizeCompressed)
                {
                    var resourceBufferConsumption = originalStream.Read(resourceBuffer[0..(Index)Math.Min(unloadedResourceStreamCopyBufferSize, entry.SizeCompressed - totalResourceRead)]);
                    totalResourceRead += resourceBufferConsumption;
                    stream.Write(resourceBuffer[0..resourceBufferConsumption]);
                }
            }
        }
        foreach (var key in loadedResources.Keys)
        {
            var binaryIndexEntry = new BinaryIndexEntry();
            key.WriteIndexComponent(index, writeTypes, writeGroups, writeHighOrderInstances, ref binaryIndexEntry);
            stream.WriteIndexComponent(ref binaryIndexEntry);
            var decompressedResource = loadedResources[key];
            using var compressedStream = new MemoryStream();
            using var compressionStream = new DeflaterOutputStream(compressedStream) { IsStreamOwner = false };
            compressionStream.Write(decompressedResource.Span);
            compressionStream.Flush();
            var compress = compressedStream.Length < decompressedResource.Length;
            var entry = compress
                ? new PackageIndexEntry(0, (uint)compressedStream.Length, (uint)decompressedResource.Length, true)
                : new PackageIndexEntry(0, (uint)decompressedResource.Length, (uint)decompressedResource.Length, false);
            entry.WriteIndexComponent(ref binaryIndexEntry);
            index.WriteBinaryStruct(ref binaryIndexEntry);
            if (compress)
            {
                compressedStream.Seek(0, SeekOrigin.Begin);
                compressedStream.CopyTo(stream);
            }
            else
                stream.Write(decompressedResource.Span);
        }
        WriteHeader(header, index.WrittenCount, stream);
        stream.Write(index.WrittenSpan);
        stream.Flush();
        stream.Seek(0, SeekOrigin.Begin);
        stream.Write(header);
        stream.Flush();
    }

    /// <summary>
    /// Saves the package to the specified <paramref name="stream"/> asynchronously
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c></exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> cannot seek or cannot write or is the same stream that was used to open the package</exception>
    /// <exception cref="InvalidOperationException">an internal error has occurred described by the exception message</exception>
    /// <exception cref="EndOfStreamException">encountered unexpected end of stream while reading original package</exception>
    public async Task SaveToAsync(Stream stream)
    {
#if IS_NET_6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(stream);
#else
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));
#endif
        if (ReferenceEquals(this.stream, stream))
            throw new ArgumentException("cannot save to the stream from which the package was opened -- save to a temporary location, dispose this object, and then overwrite the original package file with the new one");
        if (!stream.CanSeek)
            throw new ArgumentException("stream cannot seek", nameof(stream));
        if (!stream.CanWrite)
            throw new ArgumentException("stream cannot write", nameof(stream));
        Memory<byte> header = new byte[96];
        await stream.WriteAsync(header).ConfigureAwait(false);
        var (index, writeTypes, writeGroups, writeHighOrderInstances) = InitializeIndex();
        if (unloadedResources.Count > 0)
        {
            if (this.stream is not { } originalStream)
                throw new InvalidOperationException("unloaded resources are cataloged without a stream reference");
            Memory<byte> resourceBuffer = new byte[unloadedResourceStreamCopyBufferSize];
            foreach (var key in unloadedResources.Keys)
            {
                var binaryIndexEntry = new BinaryIndexEntry();
                key.WriteIndexComponent(index, writeTypes, writeGroups, writeHighOrderInstances, ref binaryIndexEntry);
                stream.WriteIndexComponent(ref binaryIndexEntry);
                var entry = unloadedResources[key];
                entry.WriteIndexComponent(ref binaryIndexEntry);
                index.WriteBinaryStruct(ref binaryIndexEntry);
                originalStream.Seek(entry.Position, SeekOrigin.Begin);
                var totalResourceRead = 0;
                while (totalResourceRead < entry.SizeCompressed)
                {
                    var resourceBufferConsumption = await originalStream.ReadAsync(resourceBuffer[0..(Index)Math.Min(unloadedResourceStreamCopyBufferSize, entry.SizeCompressed - totalResourceRead)]).ConfigureAwait(false);
                    totalResourceRead += resourceBufferConsumption;
                    await stream.WriteAsync(resourceBuffer[0..resourceBufferConsumption]).ConfigureAwait(false);
                }
            }
        }
        foreach (var key in loadedResources.Keys)
        {
            var binaryIndexEntry = new BinaryIndexEntry();
            key.WriteIndexComponent(index, writeTypes, writeGroups, writeHighOrderInstances, ref binaryIndexEntry);
            stream.WriteIndexComponent(ref binaryIndexEntry);
            var decompressedResource = loadedResources[key];
            using var compressedStream = new MemoryStream();
            using var compressionStream = new DeflaterOutputStream(compressedStream) { IsStreamOwner = false };
            await compressionStream.WriteAsync(decompressedResource).ConfigureAwait(false);
            await compressionStream.FlushAsync().ConfigureAwait(false);
            var compress = compressedStream.Length < decompressedResource.Length;
            var entry = compress
                ? new PackageIndexEntry(0, (uint)compressedStream.Length, (uint)decompressedResource.Length, true)
                : new PackageIndexEntry(0, (uint)decompressedResource.Length, (uint)decompressedResource.Length, false);
            entry.WriteIndexComponent(ref binaryIndexEntry);
            index.WriteBinaryStruct(ref binaryIndexEntry);
            if (compress)
            {
                compressedStream.Seek(0, SeekOrigin.Begin);
                await compressedStream.CopyToAsync(stream).ConfigureAwait(false);
            }
            else
                await stream.WriteAsync(decompressedResource).ConfigureAwait(false);
        }
        WriteHeader(header.Span, index.WrittenCount, stream);
        await stream.WriteAsync(index.WrittenMemory).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);
        stream.Seek(0, SeekOrigin.Begin);
        await stream.WriteAsync(header).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Adds or updates the specified <paramref name="content"/> in the package using the specified <paramref name="key"/>
    /// </summary>
    public void SetResourceContent(PackageResourceKey key, ReadOnlySpan<byte> content)
    {
        Memory<byte> safeCopy = new byte[content.Length];
        content.CopyTo(safeCopy.Span);
        StoreResourceContent(key, safeCopy);
    }

    /// <summary>
    /// Adds or updates the specified <paramref name="content"/> in the package using the specified <paramref name="key"/>
    /// </summary>
    public void SetResourceContent(PackageResourceKey key, DataResource content)
    {
#if IS_NET_6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(content);
#else
        if (content is null)
            throw new ArgumentNullException(nameof(content));
#endif
        StoreResourceContent(key, content.Encode());
    }

    /// <summary>
    /// Adds or updates the specified <paramref name="content"/> in the package using the specified <paramref name="key"/>
    /// </summary>
    public void SetResourceContent(PackageResourceKey key, JsonDocument content)
    {
#if IS_NET_6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(content);
#else
        if (content is null)
            throw new ArgumentNullException(nameof(content));
#endif
        using var newDocumentStream = new MemoryStream();
        using var documentWriter = new Utf8JsonWriter(newDocumentStream, jsonWriterOptions);
        content.WriteTo(documentWriter);
        documentWriter.Flush();
        StoreResourceContent(key, newDocumentStream.ToArray());
    }

    /// <summary>
    /// Adds or updates the specified <paramref name="content"/> in the package using the specified <paramref name="key"/>
    /// </summary>
    public void SetResourceContent(PackageResourceKey key, string content)
    {
#if IS_NET_6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(content);
#else
        if (content is null)
            throw new ArgumentNullException(nameof(content));
#endif
        StoreResourceContent(key, Encoding.UTF8.GetBytes(content));
    }

    /// <summary>
    /// Adds or updates the specified <paramref name="content"/> in the package using the specified <paramref name="key"/>
    /// </summary>
    public void SetResourceContent(PackageResourceKey key, XDocument content)
    {
#if IS_NET_6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(content);
#else
        if (content is null)
            throw new ArgumentNullException(nameof(content));
#endif
        using var newMarkupStream = new MemoryStream();
        using var markupWriter = XmlWriter.Create(newMarkupStream, markupWriterSettings);
        content.Save(markupWriter);
        markupWriter.Flush();
        StoreResourceContent(key, newMarkupStream.ToArray());
    }

    void StoreResourceContent(PackageResourceKey key, ReadOnlyMemory<byte> content)
    {
        unloadedResources.Remove(key);
#if IS_NET_6_0_OR_GREATER
        ref var loadedResource = ref CollectionsMarshal.GetValueRefOrAddDefault(loadedResources, key, out _);
        loadedResource = content;
#else
        if (!loadedResources.TryAdd(key, content))
            loadedResources[key] = content;
#endif
        if (tuningMarkupTypes.Contains(key.Type) && resourceNameByKey is not null && resourceKeysByName is not null)
        {
            resourceNameByKey.TryGetValue(key, out var previousName);
            using var resourceContentStream = new ReadOnlyMemoryOfBytesStream(content);
            var newName = GetTuningMarkupResourceName(resourceContentStream);
            if (newName != previousName)
            {
                RemoveIndexedResourceName(key);
                if (newName is not null)
                    AddIndexedResourceName(key, newName);
            }
        }
    }

    void WriteHeader(Span<byte> header, int indexSize, Stream stream)
    {
        BinaryPackageHeader.CreateForSerialization(stream, out var packageHeader);
        packageHeader.IndexCount = (uint)(unloadedResources.Count + loadedResources.Count);
        packageHeader.IndexSize = (uint)indexSize;
        header.WriteBinaryStruct(in packageHeader);
    }
}
