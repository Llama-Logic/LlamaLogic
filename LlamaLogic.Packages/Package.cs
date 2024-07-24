namespace LlamaLogic.Packages;

/// <summary>
/// Represents a package
/// </summary>
public class Package :
    IAsyncDisposable,
    IDisposable
{
    const uint supportedIndexMinorVersion = 3;
    const uint supportedMajorVersion = 2;
    const uint supportedMinorVersion = 1;
    static readonly ReadOnlyMemory<byte> supportedPreamble = "DBPF"u8.ToArray();
    const int unloadedResourceStreamCopyBufferSize = 81_920; // 80KB

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

    static (uint indexCount, uint indexSize, ulong indexPosition) ParseHeader(Span<byte> header)
    {
        if (!header[0..4].SequenceEqual(supportedPreamble.Span))
            throw new InvalidOperationException($"header preamble is not {Encoding.UTF8.GetString(supportedPreamble.Span)}");
        var majorVersion = MemoryMarshal.Read<uint>(header[4..8]);
        if (majorVersion != supportedMajorVersion)
            throw new InvalidOperationException($"major version {majorVersion} is not {supportedMajorVersion}");
        var minorVersion = MemoryMarshal.Read<uint>(header[8..12]);
        if (minorVersion != supportedMinorVersion)
            throw new InvalidOperationException($"minor version {minorVersion} is not {supportedMinorVersion}");
        var indexCount = MemoryMarshal.Read<uint>(header[36..40]);
        var indexPosition32 = MemoryMarshal.Read<uint>(header[40..44]);
        var indexSize = MemoryMarshal.Read<uint>(header[44..48]);
        var indexPosition = MemoryMarshal.Read<ulong>(header[64..72]);
        if (indexPosition is 0)
            indexPosition = indexPosition32;
        return (indexCount, indexSize, indexPosition);
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
    public ValueTask<ReadOnlyMemory<byte>> GetResourceContentAsync(PackageResourceKey key)
    {
#if IS_NET_6_0_OR_GREATER
        ref var indexEntry = ref CollectionsMarshal.GetValueRefOrNullRef(unloadedResources, key);
        if (!Unsafe.IsNullRef(ref indexEntry))
            return LoadResourceContentFromStreamAsync(indexEntry);
        ref var loadedResource = ref CollectionsMarshal.GetValueRefOrNullRef(loadedResources, key);
        if (!Unsafe.IsNullRef(ref loadedResource))
            return ValueTask.FromResult(loadedResource);
#else
        if (unloadedResources.TryGetValue(key, out var indexEntry))
            return LoadResourceContentFromStreamAsync(indexEntry);
        if (loadedResources.TryGetValue(key, out var alteredResource))
            return new ValueTask<ReadOnlyMemory<byte>>(alteredResource);
#endif
        return default;
    }

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
    public async ValueTask<IReadOnlyList<PackageResourceKey>> GetResourceKeysByNameAsync(string name)
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
    public async ValueTask<string?> GetResourceNameByKeyAsync(PackageResourceKey key)
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
    public async ValueTask<IReadOnlyList<string>> GetResourceNamesAsync()
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
        MemoryMarshal.Write(index.GetSpanAndAdvance(sizeof(PackageIndexType)), ref indexType);
        if (distinctTypes.Count == 1)
        {
            var distinctType = distinctTypes.First();
            MemoryMarshal.Write(index.GetSpanAndAdvance(sizeof(PackageResourceType)), ref distinctType);
        }
        if (distinctGroups.Count == 1)
        {
            var distinctGroup = distinctGroups.First();
            MemoryMarshal.Write(index.GetSpanAndAdvance(sizeof(uint)), ref distinctGroup);
        }
        if (distinctHighOrderInstances.Count == 1)
        {
            var distinctHighOrderInstance = distinctHighOrderInstances.First();
            MemoryMarshal.Write(index.GetSpanAndAdvance(sizeof(uint)), ref distinctHighOrderInstance);
        }
        return (index, writeTypes, writeGroups, writeHighOrderInstances);
    }

    void LoadIndex(Span<byte> index, uint indexCount)
    {
        var indexType = MemoryMarshal.Read<PackageIndexType>(index[0..4]);
        var readIndexPosition = 4;
        PackageResourceType? onlyType = null;
        if ((indexType & PackageIndexType.NoMoreThanOneType) > 0)
            onlyType = MemoryMarshal.Read<PackageResourceType>(index[readIndexPosition..(readIndexPosition += 4)]);
        uint? onlyGroup = null;
        if ((indexType & PackageIndexType.NoMoreThanOneGroup) > 0)
            onlyGroup = MemoryMarshal.Read<uint>(index[readIndexPosition..(readIndexPosition += 4)]);
        uint? onlyHighOrderInstance = null;
        if ((indexType & PackageIndexType.NoMoreThanOneHighOrderInstance) > 0)
            onlyHighOrderInstance = MemoryMarshal.Read<uint>(index[readIndexPosition..(readIndexPosition += 4)]);
        for (var i = 0; i < indexCount; ++i)
        {
            var type = onlyType ?? MemoryMarshal.Read<PackageResourceType>(index[readIndexPosition..(readIndexPosition += 4)]);
            var group = onlyGroup ?? MemoryMarshal.Read<uint>(index[readIndexPosition..(readIndexPosition += 4)]);
            var highOrderInstance = onlyHighOrderInstance ?? MemoryMarshal.Read<uint>(index[readIndexPosition..(readIndexPosition += 4)]);
            var lowOrderInstance = MemoryMarshal.Read<uint>(index[readIndexPosition..(readIndexPosition += 4)]);
            var position = MemoryMarshal.Read<uint>(index[readIndexPosition..(readIndexPosition += 4)]);
            var sizeCompressed = MemoryMarshal.Read<uint>(index[readIndexPosition..(readIndexPosition += 4)]) & 0x7fffffff;
            var size = MemoryMarshal.Read<uint>(index[readIndexPosition..(readIndexPosition += 4)]);
            var compression = MemoryMarshal.Read<PackageResourceCompressionType>(index[readIndexPosition..(readIndexPosition += 2)]);
            readIndexPosition += 2; // skip the next ushort (we don't read it)
            var key = new PackageResourceKey(type, group, ((ulong)highOrderInstance) << 32 | lowOrderInstance);
            var entry = new PackageIndexEntry(position, sizeCompressed, size, compression != PackageResourceCompressionType.None);
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

    async ValueTask<ReadOnlyMemory<byte>> LoadResourceContentFromStreamAsync(PackageIndexEntry entry)
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
                key.WriteIndexComponent(index, writeTypes, writeGroups, writeHighOrderInstances);
                stream.WriteIndexComponent(index);
                var entry = unloadedResources[key];
                entry.WriteIndexComponent(index);
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
            key.WriteIndexComponent(index, writeTypes, writeGroups, writeHighOrderInstances);
            stream.WriteIndexComponent(index);
            var decompressedResource = loadedResources[key];
            using var compressedStream = new MemoryStream();
            using var compressionStream = new DeflaterOutputStream(compressedStream) { IsStreamOwner = false };
            compressionStream.Write(decompressedResource.Span);
            compressionStream.Flush();
            var compress = compressedStream.Length < decompressedResource.Length;
            var entry = compress
                ? new PackageIndexEntry(0, (uint)compressedStream.Length, (uint)decompressedResource.Length, true)
                : new PackageIndexEntry(0, (uint)decompressedResource.Length, (uint)decompressedResource.Length, false);
            entry.WriteIndexComponent(index);
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
    public async ValueTask SaveToAsync(Stream stream)
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
                key.WriteIndexComponent(index, writeTypes, writeGroups, writeHighOrderInstances);
                stream.WriteIndexComponent(index);
                var entry = unloadedResources[key];
                entry.WriteIndexComponent(index);
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
            key.WriteIndexComponent(index, writeTypes, writeGroups, writeHighOrderInstances);
            stream.WriteIndexComponent(index);
            var decompressedResource = loadedResources[key];
            using var compressedStream = new MemoryStream();
            using var compressionStream = new DeflaterOutputStream(compressedStream) { IsStreamOwner = false };
            await compressionStream.WriteAsync(decompressedResource).ConfigureAwait(false);
            await compressionStream.FlushAsync().ConfigureAwait(false);
            var compress = compressedStream.Length < decompressedResource.Length;
            var entry = compress
                ? new PackageIndexEntry(0, (uint)compressedStream.Length, (uint)decompressedResource.Length, true)
                : new PackageIndexEntry(0, (uint)decompressedResource.Length, (uint)decompressedResource.Length, false);
            entry.WriteIndexComponent(index);
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
        ReadOnlyMemory<byte> readOnlySafeCopy = safeCopy;
        unloadedResources.Remove(key);
#if IS_NET_6_0_OR_GREATER
        ref var loadedResource = ref CollectionsMarshal.GetValueRefOrAddDefault(loadedResources, key, out _);
        loadedResource = readOnlySafeCopy;
#else
        if (!loadedResources.TryAdd(key, readOnlySafeCopy))
            loadedResources[key] = readOnlySafeCopy;
#endif
        if (tuningMarkupTypes.Contains(key.Type) && resourceNameByKey is not null && resourceKeysByName is not null)
        {
            resourceNameByKey.TryGetValue(key, out var previousName);
            using var resourceContentStream = new ReadOnlyMemoryOfBytesStream(readOnlySafeCopy);
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
        supportedPreamble.Span.CopyTo(header[0..4]);
        var majorVersion = supportedMajorVersion;
        MemoryMarshal.Write(header[4..8], ref majorVersion);
        var minorVersion = supportedMinorVersion;
        MemoryMarshal.Write(header[8..12], ref minorVersion);
        var indexCount = unloadedResources.Count + loadedResources.Count;
        MemoryMarshal.Write(header[36..40], ref indexCount);
        var indexPosition = (ulong)stream.Position;
        var indexPosition32 = (uint)(indexPosition & 0xffffffff);
        if (indexPosition32 == indexPosition)
            MemoryMarshal.Write(header[40..44], ref indexPosition32);
        MemoryMarshal.Write(header[44..48], ref indexSize);
        var indexMinorVersion = supportedIndexMinorVersion;
        MemoryMarshal.Write(header[60..64], ref indexMinorVersion);
        MemoryMarshal.Write(header[64..72], ref indexPosition);
    }
}
