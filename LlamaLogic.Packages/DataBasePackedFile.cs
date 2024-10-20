namespace LlamaLogic.Packages;

/// <summary>
/// Represents a Maxis DBPF format package file (🔒)
/// </summary>
/// <remarks>
/// ### Streams and Disposal
/// When a package is opened by this class from a <see cref="Stream"/>, the class assumes ownership of the <see cref="Stream"/>.
/// Manipulating the <see cref="Stream"/> afterward may result in undefined behavior.
/// The class will dispose of the <see cref="Stream"/> when it is, itself, disposed.
///
/// To use the <see cref="Save(bool, ResourceKeyOrder)"/> or <see cref="SaveAsync(bool, ResourceKeyOrder, CancellationToken)"/> methods, the package must have been opened from a <see cref="Stream"/> which is writeable.
/// This can also be determined by the caller in advance of attempting to use them by checking the value of the <see cref="CanSaveInPlace"/> property.
/// However, for ease of use in web frameworks like ASP.NET Core, the <see cref="CopyTo(Stream, ResourceKeyOrder)"/> and <see cref="CopyToAsync(Stream, ResourceKeyOrder, CancellationToken)"/> methods do *not* require that the <see cref="Stream"/> which they are passed is seekable.
/// 
/// ### Lazy Loading
/// When a package is first opened using this class, the package index is read but the content of the package resources is not.
/// All of the retrieval methods of this class have async counterparts for callers who prefer not to be blocked by I/O operations which may be required when retrieving resource content.
///
/// Because resource names (when applicable) are determined by their content, this class will not index resource names until they are first referenced, either explicitly by calling <see cref="GetNames"/> or <see cref="GetNamesAsync(CancellationToken)"/>, or implicitly by calling any of the other methods which deal with resource names.
/// Afterward, the names are cached for the lifetime of the package instance and updated as the caller sets the content of resources or deletes them.
///
/// ### Thread Safety
/// All properties and methods of this class, including async methods, are kept thread-safe with internally managed locks.
/// Async methods which accept cancellation tokens will honor signals so long as they have not progressed to mutating the state of the package.
/// 
/// ### Compression
/// Decompression of resource content is handled automatically by this class.
/// 
/// When retrieving resource content, this class will automatically decompress it using either proprietary Maxis internal or streaming compression, or ZLib compression.
/// When saving resources, this class will always use ZLib compression.
/// For more details, see the remarks for the <see cref="CompressionMode"/> enumeration.
///
/// Resource content is compressed in memory when it is set to minimize the amount of memory used while operating on a package.
///
/// ### Deleted Entries
/// This class honors the deleted entry flag in the package index under normal circumstances.
/// If a caller attempts to retrieve the content of a resource flagged as deleted, the class will throw a <see cref="FileNotFoundException"/> by default.
/// All retrieval methods have an optional `force` <see cref="bool"/> parameter which, when <see langword="true"/>, will cause the class to attempt to retrieve the content of a resource flagged as deleted (this is not guaranteed to work).
/// </remarks>
public sealed class DataBasePackedFile :
    IAsyncDisposable,
    IDisposable
{
    [SuppressMessage("Style", "IDE1006: Naming Styles", Justification = "Original Maxis format naming.")]
    enum mnCompressionType :
        ushort
    {
        Uncompressed =              0x0000,
        Streamable_compression =    0xfffe,
        Internal_compression =      0xffff,
        Deleted_record =            0xffe0,
        ZLIB =                      0x5a42
    }

    [Flags]
    [SuppressMessage("Style", "IDE1006: Naming Styles", Justification = "Original Maxis format naming.")]
    enum mbCompressionType :
        uint
    {
        mnSizeMask =                0b_01111111_11111111_11111111_11111111,
        mbExtendedCompressionType = 0b_10000000_00000000_00000000_00000000
    }

    [SuppressMessage("Style", "IDE1006: Naming Styles", Justification = "Original Maxis format naming.")]
    record IndexEntry(uint mnPosition, mbCompressionType mnSize, uint mnSizeDecompressed, mnCompressionType mnCompressionType, ushort mnCommitted);

    [Flags]
    enum IndexFlags :
        uint
    {
        constantType =          0b_00000000_00000000_00000000_00000001,
        constantGroup =         0b_00000000_00000000_00000000_00000010,
        constantInstanceEx =    0b_00000000_00000000_00000000_00000100
    }

    record LoadedResource(ReadOnlyMemory<byte> Memory, mnCompressionType CompressionType, int SizeDecompressed);

    const int createdFileStreamBufferSize = 4096;
    const FileOptions createDestinationFileStreamOptions = FileOptions.None;
    const FileOptions createPackageFileStreamOptions = FileOptions.RandomAccess;
    static readonly Version defaultFileVersion = new(2, 1);
    const ushort defaultMnCommitted = 1;
    const uint defaultUnused4 = 3U;
    static readonly Version defaultUserVersion = new(0, 0);
    static readonly Memory<byte> expectedFileIdentifier = "DBPF"u8.ToArray();
    static readonly ImmutableDictionary<ResourceType, ResourceFileType?> resourceFileTypeByResourceType = Enum
        .GetValues<ResourceType>()
        .ToImmutableDictionary(resourceType => resourceType, resourceType => typeof(ResourceType).GetMember(resourceType.ToString()).FirstOrDefault()?.GetCustomAttribute<ResourceFileTypeAttribute>()?.ResourceFileType);
    const int unloadedResourceCopyBufferSize = 4096;

    static void AddIndexedResourceName(Dictionary<string, HashSet<ResourceKey>> resourceKeysByName, Dictionary<ResourceKey, string> resourceNameByKey, ResourceKey key, string resourceName)
    {
        resourceNameByKey.TryAdd(key, resourceName);
        ref var keys = ref CollectionsMarshal.GetValueRefOrAddDefault(resourceKeysByName, resourceName, out var keysExisted);
        if (!keysExisted)
            keys = [key];
        else
            keys!.Add(key);
    }

    static ReadOnlyMemory<byte> FetchMemory(LoadedResource loadedResource, bool force)
    {
        var (memory, compressionType, sizeDecompressed) = loadedResource;
        if (compressionType is mnCompressionType.Uncompressed)
            return memory;
        if (compressionType is mnCompressionType.Deleted_record)
        {
            if (!force)
                throw new FileNotFoundException($"Resource is marked as deleted (you can provide a value of {true} for the {nameof(force)} parameter of the retrieval method you used to try to recover the content anyway, but this is not guaranteed to work)");
            return memory;
        }
        if (compressionType is mnCompressionType.Internal_compression or mnCompressionType.Streamable_compression)
        {
            using var compressedStream = new ReadOnlyMemoryOfByteStream(memory);
            using var legacyDecompressionStream = new LegacyDecompressionStream(compressedStream);
            Memory<byte> decompressed = new byte[sizeDecompressed];
            legacyDecompressionStream.Read(decompressed.Span);
            return decompressed;
        }
        if (compressionType is mnCompressionType.ZLIB)
        {
            using var compressedStream = new ReadOnlyMemoryOfByteStream(memory);
            using var inflaterStream = new InflaterInputStream(compressedStream);
            Memory<byte> decompressed = new byte[sizeDecompressed];
            inflaterStream.Read(decompressed.Span);
            return decompressed;
        }
        throw new NotSupportedException($"Compression type {compressionType} not supported");
    }

    static async Task<ReadOnlyMemory<byte>> FetchMemoryAsync(LoadedResource loadedResource, bool force, CancellationToken cancellationToken)
    {
        var (memory, compressionType, sizeDecompressed) = loadedResource;
        if (compressionType is mnCompressionType.Uncompressed)
            return memory;
        if (compressionType is mnCompressionType.Deleted_record)
        {
            if (!force)
                throw new FileNotFoundException($"Resource is marked as deleted (you can provide a value of {true} for the {nameof(force)} parameter of the retrieval method you used to try to recover the content anyway, but this is not guaranteed to work)");
            return memory;
        }
        if (compressionType is mnCompressionType.Internal_compression or mnCompressionType.Streamable_compression)
        {
            using var compressedStream = new ReadOnlyMemoryOfByteStream(memory);
            using var legacyDecompressionStream = new LegacyDecompressionStream(compressedStream);
            Memory<byte> decompressed = new byte[sizeDecompressed];
            legacyDecompressionStream.Read(decompressed.Span);
            return decompressed;
        }
        if (compressionType is mnCompressionType.ZLIB)
        {
            using var compressedStream = new ReadOnlyMemoryOfByteStream(memory);
            using var inflaterStream = new InflaterInputStream(compressedStream);
            Memory<byte> decompressed = new byte[sizeDecompressed];
            await inflaterStream.ReadAsync(decompressed, cancellationToken).ConfigureAwait(false);
            return decompressed;
        }
        throw new NotSupportedException($"Compression type {compressionType} not supported");
    }

    static ReadOnlyMemory<byte> FetchRawMemory(LoadedResource loadedResource, bool force)
    {
        var (memory, compressionType, _) = loadedResource;
        if (compressionType is mnCompressionType.Deleted_record && !force)
            throw new FileNotFoundException($"Resource is marked as deleted (you can provide a value of {true} for the {nameof(force)} parameter of the retrieval method you used to try to recover the content anyway, but this is not guaranteed to work)");
        return memory;
    }

    /// <summary>
    /// Initializes a <see cref="DataBasePackedFile"/> asynchronously from the specified <paramref name="path"/> (🗑️💤)
    /// </summary>
    public static async Task<DataBasePackedFile> FromPathAsync(string path, bool forReadOnly = true)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var fileStream = new FileStream
        (
            path,
            FileMode.Open,
            GetFileAccess(path, forReadOnly, out var fileShare),
            fileShare,
            createdFileStreamBufferSize,
            createPackageFileStreamOptions | FileOptions.Asynchronous
        );
#pragma warning restore CA2000 // Dispose objects before losing scope
        try
        {
            return await FromStreamAsync(fileStream).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            fileStream.Close();
            await fileStream.DisposeAsync().ConfigureAwait(false);
            throw new Exception($"{nameof(FromStreamAsync)} threw an exception", ex);
        }
    }

    /// <summary>
    /// Initializes a <see cref="DataBasePackedFile"/> asynchronously from the specified <paramref name="stream"/> (🗑️💤)
    /// </summary>
    public static async Task<DataBasePackedFile> FromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var instance = new DataBasePackedFile(stream, true, false);
        await instance.InitializeFromStreamAsync(cancellationToken).ConfigureAwait(false);
        return instance;
    }

    static FileAccess GetFileAccess(string path, bool forReadOnly, out FileShare fileShare)
    {
        if (forReadOnly)
        {
            fileShare = FileShare.Read;
            return FileAccess.Read;
        }
        if (new FileInfo(path).IsReadOnly)
            throw new UnauthorizedAccessException($"The file at \"{path}\" is marked as read-only");
        fileShare = FileShare.None;
        return FileAccess.ReadWrite;
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

    static void RemoveIndexedResourceName(Dictionary<string, HashSet<ResourceKey>> resourceKeysByName, Dictionary<ResourceKey, string> resourceNameByKey, ResourceKey key)
    {
        if (resourceNameByKey.Remove(key, out var resourceName) && resourceKeysByName.TryGetValue(resourceName, out var keys))
        {
            keys.Remove(key);
            if (keys.Count == 0)
                resourceKeysByName.Remove(resourceName);
        }
    }

    /// <summary>
    /// Compresses the specified <paramref name="memory"/> using ZLib, returning the result
    /// </summary>
    public static ReadOnlyMemory<byte> ZLibCompress(ReadOnlyMemory<byte> memory)
    {
        using var compressedStream = new ArrayBufferWriterOfByteStream();
        using var deflaterStream = new DeflaterOutputStream(compressedStream);
        deflaterStream.Write(memory.Span);
        deflaterStream.Flush();
        return compressedStream.WrittenMemory;
    }

    /// <summary>
    /// Compresses the specified <paramref name="memory"/> using ZLib asynchronously, returning the result
    /// </summary>
    public static async Task<ReadOnlyMemory<byte>> ZLibCompressAsync(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default)
    {
        using var compressedStream = new ArrayBufferWriterOfByteStream();
        using var deflaterStream = new DeflaterOutputStream(compressedStream);
        await deflaterStream.WriteAsync(memory, cancellationToken).ConfigureAwait(false);
        await deflaterStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        return compressedStream.WrittenMemory;
    }

    /// <summary>
    /// Initializes a new, blank <see cref="DataBasePackedFile"/>
    /// </summary>
    public DataBasePackedFile()
    {
        keysInIndexOrder = [];
        loadedResources = [];
        resourceLock = new();
        unloadedResources = [];
        CreationTime = DateTimeOffset.UtcNow;
        FileVersion = defaultFileVersion;
        UpdatedTime = DateTimeOffset.UtcNow;
        UserVersion = defaultUserVersion;
    }

    /// <summary>
    /// Initializes a <see cref="DataBasePackedFile"/> from the specified <paramref name="path"/> (🗑️💤)
    /// </summary>
    public DataBasePackedFile(string path, bool forReadOnly = true) :
        this
        (
            new FileStream
            (
                path,
                FileMode.Open,
                GetFileAccess(path, forReadOnly, out var fileShare),
                fileShare,
                createdFileStreamBufferSize,
                createPackageFileStreamOptions
            ),
            false,
            true
        )
    {
    }

    /// <summary>
    /// Initializes a <see cref="DataBasePackedFile"/> from the specified <paramref name="stream"/> (🗑️💤)
    /// </summary>
    public DataBasePackedFile(Stream stream) :
        this(stream, false, false)
    {
    }

#pragma warning disable CS8618 // Don't worry, sweet compiler, these fields will have non-null values by the time the caller has the instance
    DataBasePackedFile(Stream stream, bool returnAfterSettingFields, bool disposeStreamOnInitThrow)
#pragma warning restore CS8618 // Don't worry, sweet compiler, these fields will have non-null values by the time the caller has the instance
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek)
            throw new ArgumentException("Stream must be seekable", nameof(stream));
        if (!stream.CanRead)
            throw new ArgumentException("Stream must be readable", nameof(stream));
        keysInIndexOrder = [];
        loadedResources = [];
        resourceLock = new();
        this.stream = stream;
        unloadedResources = [];
        if (returnAfterSettingFields)
            return;
        try
        {
            InitializeFromStream();
        }
        catch (Exception ex)
        {
            if (disposeStreamOnInitThrow)
            {
                this.stream.Close();
                this.stream.Dispose();
            }
            throw new Exception($"{nameof(InitializeFromStream)} threw an exception", ex);
        }
    }

    /// <summary>
    /// Called by the finalizer
    /// </summary>
    ~DataBasePackedFile() =>
        Dispose(false);

    bool isDisposed;
    readonly OrderedHashSet<ResourceKey> keysInIndexOrder;
    readonly Dictionary<ResourceKey, LoadedResource> loadedResources;
    Dictionary<string, HashSet<ResourceKey>>? resourceKeysByName;
    readonly AsyncLock resourceLock;
    Dictionary<ResourceKey, string>? resourceNameByKey;
    readonly Stream? stream;
    readonly Dictionary<ResourceKey, IndexEntry> unloadedResources;

    /// <summary>
    /// Gets whether the package can be saved in place
    /// </summary>
    public bool CanSaveInPlace =>
        stream is not null && stream.CanWrite;

    /// <summary>
    /// Gets the number of resources
    /// </summary>
    public int Count =>
        GetCount();

    /// <summary>
    /// Gets/sets when the package was created
    /// </summary>
    public DateTimeOffset CreationTime { get; set; }

    /// <summary>
    /// Gets/sets the version of the format of the file
    /// </summary>
    /// <remarks>
    /// This class is intended to work with versions of the Maxis DBPF format up to version <c>2.1</c> and going back as far as possible.
    /// While it will permit the caller to specify any version they like, it does not guard against potential negative consequences which may result from such packages actually being used.
    /// The default version for newly created packages is currently <c>2.1</c>.
    /// </remarks>
    public Version FileVersion { get; set; }

    /// <summary>
    /// Gets a list of keys for all the resources in the package
    /// </summary>
    public IReadOnlyList<ResourceKey> Keys =>
        GetKeys();

    /// <summary>
    /// Gets/sets the version of the user (presumably the user agent, actually? -- is Maxis versioning human beings?)
    /// </summary>
    public Version UserVersion { get; set; }

    /// <summary>
    /// Gets/sets when the package was last updated
    /// </summary>
    public DateTimeOffset UpdatedTime { get; set; }

    /// <summary>
    /// Gets/sets the content of a resource with the specified <paramref name="key"/>
    /// </summary>
    public object? this[ResourceKey key]
    {
        get
        {
            if (!ContainsKey(key))
                return null;
            var fileType = resourceFileTypeByResourceType[key.Type];
            if (fileType is ResourceFileType.JavaScriptObjectNotation
                or ResourceFileType.TuningMarkup)
                return GetText(key);
            return Get(key);
        }
        set
        {
            if (value is null)
                Delete(key);
            else if (value is string str)
            {
                var fileType = resourceFileTypeByResourceType[key.Type];
                if (fileType is ResourceFileType.TuningMarkup)
                    SetXml(key, str);
                else
                    Set(key, str);
            }
            else if (value is ReadOnlyMemory<byte> memory)
                Set(key, memory);
            else
                throw new NotSupportedException("Value type not supported");
        }
    }

    [MemberNotNullWhen(true, nameof(resourceKeysByName), nameof(resourceNameByKey))]
    bool AreNamesIndexed() =>
        resourceKeysByName is not null && resourceNameByKey is not null;

    bool BeginIndex(ResourceKeyOrder resourceKeyOrder, [NotNullWhen(true)] out ArrayBufferWriter<byte>? index, out ImmutableArray<ResourceKey> keys, out IndexFlags indexFlags, out int indexSize)
    {
        if (unloadedResources.Count == 0 && loadedResources.Count == 0)
        {
            index = null;
            keys = [];
            indexFlags = default;
            indexSize = 0;
            return false;
        }
        keys = InternalGetKeys(resourceKeyOrder);
        var distinctTypes = keys.Select(key => key.Type).Distinct().ToImmutableArray();
        var constantType = distinctTypes.Length == 1 ? (ResourceType?)distinctTypes[0] : null;
        var distinctGroups = keys.Select(key => key.Group).Distinct().ToImmutableArray();
        var constantGroup = distinctGroups.Length == 1 ? (uint?)distinctGroups[0] : null;
        var distinctInstanceExs = keys.Select(key => key.InstanceEx).Distinct().ToImmutableArray();
        var constantInstanceEx = distinctInstanceExs.Length == 1 ? (uint?)distinctInstanceExs[0] : null;
        indexFlags =
            (constantType is not null ? IndexFlags.constantType : 0) |
            (constantGroup is not null ? IndexFlags.constantGroup : 0) |
            (constantInstanceEx is not null ? IndexFlags.constantInstanceEx : 0);
        indexSize =
              sizeof(IndexFlags)
            + (indexFlags.HasFlag(IndexFlags.constantType) ? sizeof(ResourceType) : 0)
            + (indexFlags.HasFlag(IndexFlags.constantGroup) ? sizeof(uint) : 0)
            + (indexFlags.HasFlag(IndexFlags.constantInstanceEx) ? sizeof(uint) : 0)
            + keys.Length
            *
            (
                  (constantType is null ? sizeof(ResourceType) : 0)
                + (constantGroup is null ? sizeof(uint) : 0)
                + (constantInstanceEx is null ? sizeof(uint) : 0)
                + sizeof(uint) // mInstance
                + sizeof(uint) // mnPosition
                + sizeof(mbCompressionType) // mnSize
                + sizeof(uint) // mnSizeDecompressed
                + sizeof(mnCompressionType) // mnCompressionType
                + sizeof(ushort) // mnCommitted
            );
        index = new ArrayBufferWriter<byte>(indexSize);
        index.Write(ref indexFlags);
        if (constantType is { } nonNullConstantType)
            index.Write(ref nonNullConstantType);
        if (constantGroup is { } nonNullConstantGroup)
            index.Write(ref nonNullConstantGroup);
        if (constantInstanceEx is { } nonNullConstantInstanceEx)
            index.Write(ref nonNullConstantInstanceEx);
        return true;
    }

    /// <summary>
    /// Gets whether the package contains a resource with the specified <paramref name="key"/>
    /// </summary>
    public bool ContainsKey(ResourceKey key)
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        return unloadedResources.ContainsKey(key) || loadedResources.ContainsKey(key);
    }

    /// <summary>
    /// Gets whether the package contains a resource with the specified <paramref name="key"/> asynchronously
    /// </summary>
    public async Task<bool> ContainsKeyAsync(ResourceKey key, CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        return unloadedResources.ContainsKey(key) || loadedResources.ContainsKey(key);
    }

    /// <summary>
    /// Copies the package in binary format to the specified <paramref name="destination"/> (🔄️🏃)
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="destination"/> is not writeable</exception>
    public void CopyTo(Stream destination, ResourceKeyOrder resourceKeyOrder = ResourceKeyOrder.Preserve)
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        InternalCopyTo(destination, resourceKeyOrder);
    }

    /// <summary>
    /// Copies the package in binary format to the specified <paramref name="destination"/> asynchronously (🔄️🏃)
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="destination"/> is not writeable</exception>
    public async Task CopyToAsync(Stream destination, ResourceKeyOrder resourceKeyOrder = ResourceKeyOrder.Preserve, CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        await InternalCopyToAsync(destination, resourceKeyOrder, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes the resource with the specified <paramref name="key"/> and returns <see langword="true"/> if it was found; otherwise, <see langword="false"/>
    /// </summary>
    public bool Delete(ResourceKey key)
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        var removed = unloadedResources.Remove(key) || loadedResources.Remove(key);
        if (removed)
        {
            keysInIndexOrder.Remove(key);
            if (AreNamesIndexed())
                RemoveIndexedResourceName(resourceKeysByName, resourceNameByKey, key);
        }
        return removed;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (isDisposed)
            return;
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            isDisposed = true;
            if (stream is not null)
            {
                stream.Close();
                stream.Dispose();
            }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (isDisposed)
            return;
        await DisposeAsync(true).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            isDisposed = true;
            if (stream is not null)
            {
                stream.Close();
                await stream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    ReadOnlyMemory<byte> FetchMemory(IndexEntry indexEntry, bool force)
    {
        using var contentStream = FetchStream(indexEntry, force);
        Memory<byte> content = new byte[indexEntry.mnSizeDecompressed];
        contentStream.Read(content.Span);
        return content;
    }

    async Task<ReadOnlyMemory<byte>> FetchMemoryAsync(IndexEntry indexEntry, bool force, CancellationToken cancellationToken)
    {
        using var contentStream = FetchStream(indexEntry, force);
        Memory<byte> content = new byte[indexEntry.mnSizeDecompressed];
        await contentStream.ReadAsync(content, cancellationToken).ConfigureAwait(false);
        return content;
    }

    ReadOnlyMemory<byte> FetchRawMemory(IndexEntry indexEntry, bool force)
    {
        using var contentStream = FetchRawStream(indexEntry, force);
        Memory<byte> content = new byte[contentStream.Length];
        contentStream.Read(content.Span);
        return content;
    }

    async Task<ReadOnlyMemory<byte>> FetchRawMemoryAsync(IndexEntry indexEntry, bool force, CancellationToken cancellationToken)
    {
        using var contentStream = FetchRawStream(indexEntry, force);
        Memory<byte> content = new byte[contentStream.Length];
        await contentStream.ReadAsync(content, cancellationToken).ConfigureAwait(false);
        return content;
    }

    ReadOnlySubStream FetchRawStream(IndexEntry indexEntry, bool force)
    {
        if (indexEntry.mnCompressionType is mnCompressionType.Deleted_record && !force)
            throw new FileNotFoundException($"Resource is marked as deleted (you can provide a value of {true} for the {nameof(force)} parameter of the retrieval method you used to try to recover the content anyway, but this is not guaranteed to work)");
        RequireStream();
        var position = (int)indexEntry.mnPosition;
        var compressedSize = (int)(indexEntry.mnSize & mbCompressionType.mnSizeMask);
        return new ReadOnlySubStream(stream, new Range(Index.FromStart(position), Index.FromStart(position + compressedSize)));
    }

    Stream FetchStream(IndexEntry indexEntry, bool force)
    {
        Stream rawStream = FetchRawStream(indexEntry, force);
        if (indexEntry.mnCompressionType is mnCompressionType.ZLIB)
            rawStream = new InflaterInputStream(rawStream);
        else if (indexEntry.mnCompressionType is mnCompressionType.Internal_compression or mnCompressionType.Streamable_compression)
            rawStream = new LegacyDecompressionStream(rawStream);
        else if (indexEntry.mnCompressionType is not mnCompressionType.Uncompressed)
            throw new NotSupportedException($"Compression type {indexEntry.mnCompressionType} not supported");
        return rawStream;
    }

    /// <summary>
    /// Processes undeleted resources in the specified <paramref name="keyOrder"/> if they satisfy the specified <paramref name="keyPredicate"/> using the specified <paramref name="processResourceAction"/>
    /// </summary>
    public void ForEach(ResourceKeyOrder keyOrder, Predicate<ResourceKey> keyPredicate, Action<ResourceKey, ReadOnlyMemory<byte>> processResourceAction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyPredicate);
        ArgumentNullException.ThrowIfNull(processResourceAction);
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock(cancellationToken);
        foreach (var key in InternalGetKeys(keyOrder))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (keyPredicate(key))
            {
                ReadOnlyMemory<byte> content;
                try
                {
                    content = InternalGet(key, false);
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
                processResourceAction(key, content);
            }
        }
    }

    /// <summary>
    /// Processes undeleted resources in the specified <paramref name="keyOrder"/> if they satisfy the specified <paramref name="keyPredicate"/> using the specified <paramref name="processResourceAsyncAction"/>
    /// </summary>
    public async Task ForEachAsync(ResourceKeyOrder keyOrder, Predicate<ResourceKey> keyPredicate, Func<ResourceKey, ReadOnlyMemory<byte>, Task> processResourceAsyncAction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyPredicate);
        ArgumentNullException.ThrowIfNull(processResourceAsyncAction);
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        foreach (var key in InternalGetKeys(keyOrder))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (keyPredicate(key))
            {
                ReadOnlyMemory<byte> content;
                try
                {
                    content = await InternalGetAsync(key, false, cancellationToken).ConfigureAwait(false);
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
                await processResourceAsyncAction(key, content).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Processes undeleted raw resources (meaning that the library will not decompress a resource for you if it is compressed) in the specified <paramref name="keyOrder"/> if they satisfy the specified <paramref name="keyPredicate"/> using the specified <paramref name="processResourceAction"/>
    /// </summary>
    public void ForEachRaw(ResourceKeyOrder keyOrder, Predicate<ResourceKey> keyPredicate, Action<ResourceKey, ReadOnlyMemory<byte>> processResourceAction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyPredicate);
        ArgumentNullException.ThrowIfNull(processResourceAction);
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock(cancellationToken);
        foreach (var key in InternalGetKeys(keyOrder))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (keyPredicate(key))
            {
                ReadOnlyMemory<byte> content;
                try
                {
                    content = InternalGetRaw(key, false);
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
                processResourceAction(key, content);
            }
        }
    }

    /// <summary>
    /// Processes undeleted raw resources (meaning that the library will not decompress a resource for you if it is compressed) in the specified <paramref name="keyOrder"/> if they satisfy the specified <paramref name="keyPredicate"/> using the specified <paramref name="processResourceAsyncAction"/>
    /// </summary>
    public async Task ForEachRawAsync(ResourceKeyOrder keyOrder, Predicate<ResourceKey> keyPredicate, Func<ResourceKey, ReadOnlyMemory<byte>, Task> processResourceAsyncAction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyPredicate);
        ArgumentNullException.ThrowIfNull(processResourceAsyncAction);
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        foreach (var key in InternalGetKeys(keyOrder))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (keyPredicate(key))
            {
                ReadOnlyMemory<byte> content;
                try
                {
                    content = await InternalGetRawAsync(key, false, cancellationToken).ConfigureAwait(false);
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
                await processResourceAsyncAction(key, content).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="force"><see langword="true"/> to get the content of the resource even if it has been marked as deleted; otheriwse, <see langword="false"/> (default)</param>
    public ReadOnlyMemory<byte> Get(ResourceKey key, bool force = false)
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        return InternalGet(key, force);
    }

    ReadOnlyMemory<byte> InternalGet(ResourceKey key, bool force)
    {
        ref var indexEntry = ref CollectionsMarshal.GetValueRefOrNullRef(unloadedResources, key);
        if (!Unsafe.IsNullRef(ref indexEntry))
            return FetchMemory(indexEntry, force);
        ref var loadedResource = ref CollectionsMarshal.GetValueRefOrNullRef(loadedResources, key);
        if (!Unsafe.IsNullRef(ref loadedResource))
            return FetchMemory(loadedResource, force);
        throw new KeyNotFoundException($"Key {key} not found");
    }

    /// <summary>
    /// Gets the size of the content of each resource in the package indexed by key
    /// </summary>
    /// <param name="predicate">An optional predicate to filter the keys</param>
    public IReadOnlyDictionary<ResourceKey, int> GetAllSizes(Predicate<ResourceKey>? predicate = null)
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        if (predicate is null)
            return unloadedResources.ToDictionary(kv => kv.Key, kv => (int)kv.Value.mnSizeDecompressed)
                .Concat(loadedResources.ToDictionary(kv => kv.Key, kv => kv.Value.SizeDecompressed))
                .ToImmutableDictionary(kv => kv.Key, kv => kv.Value);
        return unloadedResources.Where(kv => predicate(kv.Key)).ToDictionary(kv => kv.Key, kv => (int)kv.Value.mnSizeDecompressed)
            .Concat(loadedResources.Where(kv => predicate(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value.SizeDecompressed))
            .ToImmutableDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// Gets the size of the content of each resource in the package indexed by key, asynchronously
    /// </summary>
    /// <param name="predicate">An optional predicate to filter the keys</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public async Task<IReadOnlyDictionary<ResourceKey, int>> GetAllSizesAsync(Predicate<ResourceKey>? predicate = null, CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        if (predicate is null)
            return unloadedResources.ToDictionary(kv => kv.Key, kv => (int)kv.Value.mnSizeDecompressed)
                .Concat(loadedResources.ToDictionary(kv => kv.Key, kv => kv.Value.SizeDecompressed))
                .ToImmutableDictionary(kv => kv.Key, kv => kv.Value);
        return unloadedResources.Where(kv => predicate(kv.Key)).ToDictionary(kv => kv.Key, kv => (int)kv.Value.mnSizeDecompressed)
            .Concat(loadedResources.Where(kv => predicate(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value.SizeDecompressed))
            .ToImmutableDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> asynchronously
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="force"><see langword="true"/> to get the content of the resource even if it has been marked as deleted; otheriwse, <see langword="false"/> (default)</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public async Task<ReadOnlyMemory<byte>> GetAsync(ResourceKey key, bool force = false, CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        return await InternalGetAsync(key, force, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the number of resources
    /// </summary>
    public int GetCount()
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        return unloadedResources.Count + loadedResources.Count;
    }

    /// <summary>
    /// Gets the number of resources asychronously
    /// </summary>
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        return unloadedResources.Count + loadedResources.Count;
    }

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="DataModel"/>
    /// </summary>
    public DataModel GetData(ResourceKey key, bool force = false) =>
        GetModel<DataModel>(key, force);

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="DataModel"/> asynchronously
    /// </summary>
    public Task<DataModel> GetDataAsync(ResourceKey key, bool force = false, CancellationToken cancellationToken = default) =>
        GetModelAsync<DataModel>(key, force, cancellationToken);

    /// <summary>
    /// Gets a list of keys for all the resources in the package
    /// </summary>
    public IReadOnlyList<ResourceKey> GetKeys(ResourceKeyOrder resourceKeyOrder = ResourceKeyOrder.Preserve)
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        return InternalGetKeys(resourceKeyOrder);
    }

    /// <summary>
    /// Gets a list of keys for all the resources in the package asynchronously
    /// </summary>
    public async Task<IReadOnlyList<ResourceKey>> GetKeysAsync(ResourceKeyOrder resourceKeyOrder = ResourceKeyOrder.Preserve, CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        return InternalGetKeys(resourceKeyOrder);
    }

    /// <summary>
    /// Gets a list of keys for resources with the specified <paramref name="name"/>
    /// </summary>
    public IReadOnlyList<ResourceKey> GetKeysByName(string name)
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        IndexNames();
        return resourceKeysByName.TryGetValue(name, out var keys) ? [..keys] : [];
    }

    /// <summary>
    /// Gets a list of keys for resources with the specified <paramref name="name"/> asynchronously
    /// </summary>
    public async Task<IReadOnlyList<ResourceKey>> GetKeysByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        await IndexNamesAsync(cancellationToken).ConfigureAwait(false);
        return resourceKeysByName.TryGetValue(name, out var keys) ? [..keys] : [];
    }

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <typeparamref name="TModel"/>
    /// </summary>
    /// <typeparam name="TModel">A model used to browse and modify certain types of resources</typeparam>
    /// <exception cref="ArgumentException"><typeparamref name="TModel"/> cannot deal with resources of the type specified by <paramref name="key"/></exception>
    public TModel GetModel<TModel>(ResourceKey key, bool force = false)
        where TModel : IModel<TModel>
    {
        if (!TModel.SupportedTypes.Contains(key.Type))
            throw new ArgumentException($"Resource type {key.Type} not supported by model {typeof(TModel)}", nameof(key));
        return TModel.Decode(Get(key, force));
    }

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <typeparamref name="TModel"/> asynchronously
    /// </summary>
    /// <typeparam name="TModel">A model used to browse and modify certain types of resources</typeparam>
    /// <exception cref="ArgumentException"><typeparamref name="TModel"/> cannot deal with resources of the type specified by <paramref name="key"/></exception>
    public async Task<TModel> GetModelAsync<TModel>(ResourceKey key, bool force = false, CancellationToken cancellationToken = default)
        where TModel : IModel<TModel>
    {
        if (!TModel.SupportedTypes.Contains(key.Type))
            throw new ArgumentException($"Resource type {key.Type} not supported by model {typeof(TModel)}", nameof(key));
        return await Task.Run(async () => TModel.Decode(await GetAsync(key, force, cancellationToken).ConfigureAwait(false))).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="ModFileManifestModel"/>
    /// </summary>
    public ModFileManifestModel GetModFileManifest(ResourceKey key, bool force = false) =>
        GetModel<ModFileManifestModel>(key, force);

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="ModFileManifestModel"/> asynchronously
    /// </summary>
    public Task<ModFileManifestModel> GetModFileManifestAsync(ResourceKey key, bool force = false, CancellationToken cancellationToken = default) =>
        GetModelAsync<ModFileManifestModel>(key, force, cancellationToken);

    /// <summary>
    /// Gets the name for the resource with the specified <paramref name="key"/>
    /// </summary>
    public string? GetNameByKey(ResourceKey key)
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        IndexNames();
        return (resourceNameByKey?.TryGetValue(key, out var resourceName) ?? false) ? resourceName : null;
    }

    /// <summary>
    /// Gets the name for the resource with the specified <paramref name="key"/> asynchronously
    /// </summary>
    public async Task<string?> GetNameByKeyAsync(ResourceKey key, CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        await IndexNamesAsync(cancellationToken).ConfigureAwait(false);
        return (resourceNameByKey?.TryGetValue(key, out var resourceName) ?? false) ? resourceName : null;
    }

    /// <summary>
    /// Gets the names for all the resources in the package
    /// </summary>
    public IReadOnlyList<string> GetNames()
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        IndexNames();
        return [..resourceKeysByName.Keys];
    }

    /// <summary>
    /// Gets the names for all the resources in the package asynchronously
    /// </summary>
    public async Task<IReadOnlyList<string>> GetNamesAsync(CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        await IndexNamesAsync(cancellationToken).ConfigureAwait(false);
        return [.. resourceKeysByName.Keys];
    }

    /// <summary>
    /// Gets the raw content of a resource with the specified <paramref name="key"/>, meaning that the library will not decompress it for you if it is compressed
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="force"><see langword="true"/> to get the content of the resource even if it has been marked as deleted; otheriwse, <see langword="false"/> (default)</param>
    public ReadOnlyMemory<byte> GetRaw(ResourceKey key, bool force = false)
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        return InternalGetRaw(key, force);
    }

    /// <summary>
    /// Gets the raw content of a resource with the specified <paramref name="key"/> asynchronously, meaning that the library will not decompress it for you if it is compressed
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="force"><see langword="true"/> to get the content of the resource even if it has been marked as deleted; otheriwse, <see langword="false"/> (default)</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public async Task<ReadOnlyMemory<byte>> GetRawAsync(ResourceKey key, bool force = false, CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        return await InternalGetRawAsync(key, force, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the size of the content of the resource with the specified <paramref name="key"/>
    /// </summary>
    public int GetSize(ResourceKey key)
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        if (unloadedResources.TryGetValue(key, out var indexEntry))
            return (int)indexEntry.mnSizeDecompressed;
        if (loadedResources.TryGetValue(key, out var loadedResource))
            return loadedResource.SizeDecompressed;
        throw new KeyNotFoundException($"Key {key} not found");
    }

    /// <summary>
    /// Gets the size of the content of the resource with the specified <paramref name="key"/> asynchronously
    /// </summary>
    public async Task<int> GetSizeAsync(ResourceKey key, CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        if (unloadedResources.TryGetValue(key, out var indexEntry))
            return (int)indexEntry.mnSizeDecompressed;
        if (loadedResources.TryGetValue(key, out var loadedResource))
            return loadedResource.SizeDecompressed;
        throw new KeyNotFoundException($"Key {key} not found");
    }

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="StringTableModel"/>
    /// </summary>
    public StringTableModel GetStringTable(ResourceKey key, bool force = false) =>
        GetModel<StringTableModel>(key, force);

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="StringTableModel"/> asynchronously
    /// </summary>
    public Task<StringTableModel> GetStringTableAsync(ResourceKey key, bool force = false, CancellationToken cancellationToken = default) =>
        GetModelAsync<StringTableModel>(key, force, cancellationToken);

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="string"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="force"><see langword="true"/> to get the content of the resource even if it has been marked as deleted; otheriwse, <see langword="false"/> (default)</param>
    public string GetText(ResourceKey key, bool force = false)
    {
        using var contentStreamReader = new StreamReader(new ReadOnlyMemoryOfByteStream(Get(key, force)));
        return contentStreamReader.ReadToEnd();
    }

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="string"/> asynchronously
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="force"><see langword="true"/> to get the content of the resource even if it has been marked as deleted; otheriwse, <see langword="false"/> (default)</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public async Task<string> GetTextAsync(ResourceKey key, bool force = false, CancellationToken cancellationToken = default)
    {
        using var contentStreamReader = new StreamReader(new ReadOnlyMemoryOfByteStream(await GetAsync(key, force, cancellationToken).ConfigureAwait(false)));
        return await contentStreamReader.ReadToEndAsync
        (
#if IS_NET_7_0_OR_GREATER
            cancellationToken
#endif
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as an <see cref="XDocument"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="force"><see langword="true"/> to get the content of the resource even if it has been marked as deleted; otheriwse, <see langword="false"/> (default)</param>
    public XDocument GetXml(ResourceKey key, bool force = false)
    {
        using var contentStream = new ReadOnlyMemoryOfByteStream(Get(key, force));
        return XDocument.Load(contentStream, TuningUtilities.XDocumentLoadOptions);
    }

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as an <see cref="XDocument"/> asynchronously
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="force"><see langword="true"/> to get the content of the resource even if it has been marked as deleted; otheriwse, <see langword="false"/> (default)</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public async Task<XDocument> GetXmlAsync(ResourceKey key, bool force = false, CancellationToken cancellationToken = default)
    {
        using var contentStream = new ReadOnlyMemoryOfByteStream(await GetAsync(key, force, cancellationToken).ConfigureAwait(false));
        return await XDocument.LoadAsync(contentStream, TuningUtilities.XDocumentLoadOptions, cancellationToken).ConfigureAwait(false);
    }

    [MemberNotNull(nameof(resourceKeysByName), nameof(resourceNameByKey))]
    void IndexNames()
    {
        if (AreNamesIndexed())
            return;
        resourceKeysByName = [];
        resourceNameByKey = [];
        foreach (var (key, indexEntry, resourceType, resourceFileType) in unloadedResources
            .Select
            (
                kv =>
                (
                    key: kv.Key,
                    indexEntry: kv.Value,
                    resourceType: kv.Key.Type,
                    resourceFileType: resourceFileTypeByResourceType[kv.Key.Type]
                )
            ))
        {
            try
            {
                string? resourceName = null;
                if (resourceFileType is ResourceFileType.TuningMarkup)
                {
                    using var tuningMarkupStream = FetchStream(indexEntry, false);
                    resourceName = GetTuningMarkupResourceName(tuningMarkupStream);
                }
                else if (Model.SupportedTypes.Contains(resourceType))
                {
                    var binaryData = FetchMemory(indexEntry, false);
                    using var binaryStream = new ReadOnlyMemoryOfByteStream(binaryData);
                    resourceName = Model.GetName(resourceType, binaryStream);
                }
                if (resourceName is not null)
                    AddIndexedResourceName(resourceKeysByName, resourceNameByKey, key, resourceName);
            }
            catch (FileNotFoundException)
            {
                continue;
            }
        }
        foreach (var (key, loadedResource, resourceType, resourceFileType) in loadedResources
            .Select
            (
                kv =>
                (
                    key: kv.Key,
                    loadedResource: kv.Value,
                    resourceType: kv.Key.Type,
                    resourceFileType: resourceFileTypeByResourceType[kv.Key.Type]
                )
            ))
        {
            try
            {
                string? resourceName = null;
                if (resourceFileType is ResourceFileType.TuningMarkup)
                {
                    using var tuningMarkupStream = new ReadOnlyMemoryOfByteStream(FetchMemory(loadedResource, false));
                    resourceName = GetTuningMarkupResourceName(tuningMarkupStream);
                }
                else if (Model.SupportedTypes.Contains(resourceType))
                {
                    using var binaryStream = new ReadOnlyMemoryOfByteStream(FetchMemory(loadedResource, false));
                    resourceName = Model.GetName(resourceType, binaryStream);
                }
                if (resourceName is not null)
                    AddIndexedResourceName(resourceKeysByName, resourceNameByKey, key, resourceName);
            }
            catch (FileNotFoundException)
            {
                continue;
            }
        }
    }

    [MemberNotNull(nameof(resourceKeysByName), nameof(resourceNameByKey))]
    async ValueTask IndexNamesAsync(CancellationToken cancellationToken)
    {
        if (AreNamesIndexed())
            return;
        var resourceKeysByName = new Dictionary<string, HashSet<ResourceKey>>();
        var resourceNameByKey = new Dictionary<ResourceKey, string>();
        foreach (var (key, indexEntry, resourceType, resourceFileType) in unloadedResources
            .Select
            (
                kv =>
                (
                    key: kv.Key,
                    indexEntry: kv.Value,
                    resourceType: kv.Key.Type,
                    resourceFileType: resourceFileTypeByResourceType[kv.Key.Type]
                )
            ))
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                string? resourceName = null;
                if (resourceFileType is ResourceFileType.TuningMarkup)
                {
                    using var tuningMarkupStream = FetchStream(indexEntry, false);
#pragma warning disable CS8774 // The way this method is awaited, they won't be null by the time the caller could possibly read them
                    resourceName = await GetTuningMarkupResourceNameAsync(tuningMarkupStream).ConfigureAwait(false);
#pragma warning restore CS8774 // The way this method is awaited, they won't be null by the time the caller could possibly read them
                }
                else if (Model.SupportedTypes.Contains(resourceType))
                {
#pragma warning disable CS8774 // The way this method is awaited, they won't be null by the time the caller could possibly read them
                    var binaryData = await FetchMemoryAsync(indexEntry, false, cancellationToken).ConfigureAwait(false);
#pragma warning restore CS8774 // The way this method is awaited, they won't be null by the time the caller could possibly read them
                    using var binaryStream = new ReadOnlyMemoryOfByteStream(binaryData);
#pragma warning disable CS8774 // The way this method is awaited, they won't be null by the time the caller could possibly read them
                    resourceName = await Model.GetNameAsync(resourceType, binaryStream, cancellationToken).ConfigureAwait(false);
#pragma warning restore CS8774 // The way this method is awaited, they won't be null by the time the caller could possibly read them
                }
                if (resourceName is not null)
                    AddIndexedResourceName(resourceKeysByName, resourceNameByKey, key, resourceName);
            }
            catch (FileNotFoundException)
            {
                continue;
            }
        }
        foreach (var (key, loadedResource, resourceType, resourceFileType) in loadedResources
            .Select
            (
                kv =>
                (
                    key: kv.Key,
                    loadedResource: kv.Value,
                    resourceType: kv.Key.Type,
                    resourceFileType: resourceFileTypeByResourceType[kv.Key.Type]
                )
            ))
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                string? resourceName = null;
                if (resourceFileType is ResourceFileType.TuningMarkup)
                {
#pragma warning disable CS8774 // The way this method is awaited, they won't be null by the time the caller could possibly read them
                    using var tuningMarkupStream = new ReadOnlyMemoryOfByteStream(await FetchMemoryAsync(loadedResource, false, cancellationToken).ConfigureAwait(false));
                    resourceName = await GetTuningMarkupResourceNameAsync(tuningMarkupStream).ConfigureAwait(false);
#pragma warning restore CS8774 // The way this method is awaited, they won't be null by the time the caller could possibly read them
                }
                else if (Model.SupportedTypes.Contains(resourceType))
                {
#pragma warning disable CS8774 // The way this method is awaited, they won't be null by the time the caller could possibly read them
                    using var binaryStream = new ReadOnlyMemoryOfByteStream(await FetchMemoryAsync(loadedResource, false, cancellationToken).ConfigureAwait(false));
                    resourceName = await Model.GetNameAsync(resourceType, binaryStream, cancellationToken).ConfigureAwait(false);
#pragma warning restore CS8774 // The way this method is awaited, they won't be null by the time the caller could possibly read them
                }
                if (resourceName is not null)
                    AddIndexedResourceName(resourceKeysByName, resourceNameByKey, key, resourceName);
            }
            catch (FileNotFoundException)
            {
                continue;
            }
        }
        this.resourceKeysByName = resourceKeysByName;
        this.resourceNameByKey = resourceNameByKey;
    }

    void InitializeFromStream()
    {
        stream!.Seek(0, SeekOrigin.Begin);
        Span<byte> header = stackalloc byte[96];
        var headerBytesRead = stream.Read(header);
        ParseHeader(header[..headerBytesRead], out var indexEntryCount, out var indexSize, out var indexPosition);
        if (indexSize > 0)
        {
            stream.Seek(indexPosition, SeekOrigin.Begin);
            var indexPoolArray = ArrayPool<byte>.Shared.Rent(indexSize);
            try
            {
                var index = indexPoolArray.AsSpan()[..indexSize];
                var indexBytesRead = stream.Read(index);
                if (indexBytesRead != indexSize)
                    throw new InvalidDataException($"Index at position {indexPosition} of size {indexSize} could not be fully read");
                ParseIndex(index, indexEntryCount);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(indexPoolArray);
            }
        }
    }

    async ValueTask InitializeFromStreamAsync(CancellationToken cancellationToken)
    {
        stream!.Seek(0, SeekOrigin.Begin);
        var headerPoolArray = ArrayPool<byte>.Shared.Rent(96);
        try
        {
            var header = headerPoolArray.AsMemory()[..96];
            var headerBytesRead = await stream.ReadAsync(header, cancellationToken).ConfigureAwait(false);
            ParseHeader(header[..headerBytesRead].Span, out var indexEntryCount, out var indexSize, out var indexPosition);
            if (indexSize > 0)
            {
                stream.Seek(indexPosition, SeekOrigin.Begin);
                var indexPoolArray = ArrayPool<byte>.Shared.Rent(indexSize);
                try
                {
                    var index = indexPoolArray.AsMemory()[..indexSize];
                    var indexBytesRead = await stream.ReadAsync(index, cancellationToken).ConfigureAwait(false);
                    if (indexBytesRead != indexSize)
                        throw new InvalidDataException($"Index at position {indexPosition} of size {indexSize} could not be fully read");
                    ParseIndex(index.Span, indexEntryCount);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(indexPoolArray);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(headerPoolArray);
        }
    }

    void InternalCopyTo(Stream destination, ResourceKeyOrder resourceKeyOrder)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
            throw new ArgumentException("Stream must be writable", nameof(destination));
        var hasIndex = BeginIndex(resourceKeyOrder, out var index, out var keys, out var indexFlags, out var indexSize);
        Span<byte> header = stackalloc byte[96];
        WriteHeader(header, hasIndex, indexSize);
        destination.Write(header);
        var position = 96;
        var bufferPoolArray = ArrayPool<byte>.Shared.Rent(unloadedResourceCopyBufferSize);
        try
        {
            var buffer = bufferPoolArray.AsSpan()[..unloadedResourceCopyBufferSize];
            foreach (var key in keys)
            {
                if (unloadedResources.TryGetValue(key, out var indexEntry))
                {
                    WriteIndexEntry(index!, indexFlags, position, key, indexEntry);
                    stream!.Seek(indexEntry.mnPosition, SeekOrigin.Begin);
                    var resourceSize = (int)(indexEntry.mnSize & mbCompressionType.mnSizeMask);
                    for (int bytesRead, totalBytesRead = 0; resourceSize - totalBytesRead > 0;)
                    {
                        bytesRead = stream.Read(buffer[..Math.Min(buffer.Length, resourceSize - totalBytesRead)]);
                        if (bytesRead == 0)
                            break;
                        totalBytesRead += bytesRead;
                        destination.Write(buffer[..bytesRead]);
                    }
                    position += resourceSize;
                }
                else if (loadedResources.TryGetValue(key, out var loadedResource))
                {
                    WriteIndexEntry(index!, indexFlags, position, key, loadedResource);
                    destination.Write(loadedResource.Memory.Span);
                    position += loadedResource.Memory.Length;
                }
                else
                    throw new KeyNotFoundException($"Key {key} not found in either loaded or unloaded resources");
            }
            if (hasIndex)
                destination.Write(index!.WrittenSpan);
            destination.Flush();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bufferPoolArray);
        }
    }

    async Task InternalCopyToAsync(Stream destination, ResourceKeyOrder resourceKeyOrder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
            throw new ArgumentException("Stream must be writable", nameof(destination));
        var hasIndex = BeginIndex(resourceKeyOrder, out var index, out var keys, out var indexFlags, out var indexSize);
        var headerPoolArray = ArrayPool<byte>.Shared.Rent(96);
        try
        {
            var header = headerPoolArray.AsMemory()[..96];
            WriteHeader(header.Span, hasIndex, indexSize);
            await destination.WriteAsync(header, cancellationToken).ConfigureAwait(false);
            var position = 96;
            var bufferPoolArray = ArrayPool<byte>.Shared.Rent(unloadedResourceCopyBufferSize);
            try
            {
                var buffer = bufferPoolArray.AsMemory()[..unloadedResourceCopyBufferSize];
                foreach (var key in keys)
                {
                    if (unloadedResources.TryGetValue(key, out var indexEntry))
                    {
                        WriteIndexEntry(index!, indexFlags, position, key, indexEntry);
                        stream!.Seek(indexEntry.mnPosition, SeekOrigin.Begin);
                        var resourceSize = (int)(indexEntry.mnSize & mbCompressionType.mnSizeMask);
                        for (int bytesRead, totalBytesRead = 0; resourceSize - totalBytesRead > 0;)
                        {
                            bytesRead = await stream.ReadAsync(buffer[..Math.Min(buffer.Length, resourceSize - totalBytesRead)], cancellationToken).ConfigureAwait(false);
                            if (bytesRead == 0)
                                break;
                            totalBytesRead += bytesRead;
                            await destination.WriteAsync(buffer[..bytesRead], cancellationToken).ConfigureAwait(false);
                        }
                        position += resourceSize;
                    }
                    else if (loadedResources.TryGetValue(key, out var loadedResource))
                    {
                        WriteIndexEntry(index!, indexFlags, position, key, loadedResource);
                        await destination.WriteAsync(loadedResource.Memory, cancellationToken).ConfigureAwait(false);
                        position += loadedResource.Memory.Length;
                    }
                    else
                        throw new KeyNotFoundException($"Key {key} not found in either loaded or unloaded resources");
                }
                if (hasIndex)
                    await destination.WriteAsync(index!.WrittenMemory, cancellationToken).ConfigureAwait(false);
                await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bufferPoolArray);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(headerPoolArray);
        }
    }

    Task<ReadOnlyMemory<byte>> InternalGetAsync(ResourceKey key, bool force, CancellationToken cancellationToken)
    {
        ref var indexEntry = ref CollectionsMarshal.GetValueRefOrNullRef(unloadedResources, key);
        if (!Unsafe.IsNullRef(ref indexEntry))
            return FetchMemoryAsync(indexEntry, force, cancellationToken);
        ref var loadedResource = ref CollectionsMarshal.GetValueRefOrNullRef(loadedResources, key);
        if (!Unsafe.IsNullRef(ref loadedResource))
            return FetchMemoryAsync(loadedResource, force, cancellationToken);
        throw new KeyNotFoundException($"Key {key} not found");
    }

    ImmutableArray<ResourceKey> InternalGetKeys(ResourceKeyOrder resourceKeyOrder) =>
        [..(resourceKeyOrder switch
        {
            ResourceKeyOrder.InstanceTypeGroup => keysInIndexOrder.OrderBy(key => key.FullInstance).ThenBy(key => key.Type).ThenBy(key => key.Group),
            ResourceKeyOrder.TypeGroupInstance => keysInIndexOrder.OrderBy(key => key.Type).ThenBy(key => key.Group).ThenBy(key => key.FullInstance),
            _ => (IEnumerable<ResourceKey>)keysInIndexOrder
        })];

    ReadOnlyMemory<byte> InternalGetRaw(ResourceKey key, bool force)
    {
        ref var indexEntry = ref CollectionsMarshal.GetValueRefOrNullRef(unloadedResources, key);
        if (!Unsafe.IsNullRef(ref indexEntry))
            return FetchRawMemory(indexEntry, force);
        ref var loadedResource = ref CollectionsMarshal.GetValueRefOrNullRef(loadedResources, key);
        if (!Unsafe.IsNullRef(ref loadedResource))
            return FetchRawMemory(loadedResource, force);
        throw new KeyNotFoundException($"Key {key} not found");
    }

    Task<ReadOnlyMemory<byte>> InternalGetRawAsync(ResourceKey key, bool force, CancellationToken cancellationToken)
    {
        ref var indexEntry = ref CollectionsMarshal.GetValueRefOrNullRef(unloadedResources, key);
        if (!Unsafe.IsNullRef(ref indexEntry))
            return FetchRawMemoryAsync(indexEntry, force, cancellationToken);
        ref var loadedResource = ref CollectionsMarshal.GetValueRefOrNullRef(loadedResources, key);
        if (!Unsafe.IsNullRef(ref loadedResource))
        {
            try
            {
                return Task.FromResult(FetchRawMemory(loadedResource, force));
            }
            catch (Exception ex)
            {
                return Task.FromException<ReadOnlyMemory<byte>>(ex);
            }
        }
        return Task.FromException<ReadOnlyMemory<byte>>(new KeyNotFoundException($"Key {key} not found"));
    }

    void InternalLoadAll(bool force, CompressionMode compressionMode)
    {
        var unloadedKeys = unloadedResources.Keys.ToImmutableArray();
        foreach (var unloadedKey in unloadedKeys)
            InternalSet(unloadedKey, FetchMemory(unloadedResources[unloadedKey], force), compressionMode);
    }

    async Task InternalLoadAllAsync(bool force, CompressionMode compressionMode, CancellationToken cancellationToken)
    {
        var unloadedKeys = unloadedResources.Keys.ToImmutableArray();
        foreach (var unloadedKey in unloadedKeys)
            await InternalSetAsync(unloadedKey, await FetchMemoryAsync(unloadedResources[unloadedKey], force, cancellationToken).ConfigureAwait(false), compressionMode, cancellationToken).ConfigureAwait(false);
    }

    bool InternalSet(ResourceKey key, ReadOnlyMemory<byte> memory, CompressionMode compressionMode)
    {
        ReadOnlyMemory<byte> memoryToStore = default;
        var sizeDecompressed = memory.Length;
        var wasCompressed = compressionMode is not
            CompressionMode.ForceOff
            or CompressionMode.SetDeletedFlag
            or CompressionMode.CallerSuppliedInternal
            or CompressionMode.CallerSuppliedStreamable;
        if (wasCompressed)
        {
            var compressedMemory = ZLibCompress(memory);
            if (wasCompressed = compressionMode is CompressionMode.ForceOn || compressedMemory.Length < sizeDecompressed)
                memoryToStore = compressedMemory;
        }
        if (!wasCompressed)
            memoryToStore = memory;
        if (AreNamesIndexed())
        {
            if (compressionMode is not CompressionMode.SetDeletedFlag
                or CompressionMode.CallerSuppliedInternal
                or CompressionMode.CallerSuppliedStreamable)
            {
                resourceNameByKey.TryGetValue(key, out var previousName);
                string? newName = null;
                if (resourceFileTypeByResourceType[key.Type] is ResourceFileType.TuningMarkup)
                {
                    using var tuningMarkupStream = new ReadOnlyMemoryOfByteStream(memory);
                    newName = GetTuningMarkupResourceName(tuningMarkupStream);
                }
                else if (Model.SupportedTypes.Contains(key.Type))
                {
                    using var binaryStream = new ReadOnlyMemoryOfByteStream(memory);
                    newName = Model.GetName(key.Type, binaryStream);
                }
                if (newName != previousName)
                {
                    RemoveIndexedResourceName(resourceKeysByName, resourceNameByKey, key);
                    if (newName is not null)
                        AddIndexedResourceName(resourceKeysByName, resourceNameByKey, key, newName);
                }
            }
            else
            {
                // force rebuild of the name indexing
                resourceKeysByName = null;
                resourceNameByKey = null;
            }
        }
        Store(key, memoryToStore, wasCompressed ? mnCompressionType.ZLIB : compressionMode switch
        {
            CompressionMode.SetDeletedFlag => mnCompressionType.Deleted_record,
            CompressionMode.CallerSuppliedInternal => mnCompressionType.Internal_compression,
            CompressionMode.CallerSuppliedStreamable => mnCompressionType.Streamable_compression,
            _ => mnCompressionType.Uncompressed
        }, sizeDecompressed);
        return wasCompressed;
    }

    async Task<bool> InternalSetAsync(ResourceKey key, ReadOnlyMemory<byte> memory, CompressionMode compressionMode, CancellationToken cancellationToken)
    {
        ReadOnlyMemory<byte> memoryToStore = default;
        var sizeDecompressed = memory.Length;
        var wasCompressed = compressionMode is not
            CompressionMode.ForceOff
            or CompressionMode.SetDeletedFlag
            or CompressionMode.CallerSuppliedInternal
            or CompressionMode.CallerSuppliedStreamable;
        if (wasCompressed)
        {
            var compressedMemory = await ZLibCompressAsync(memory, cancellationToken).ConfigureAwait(false);
            if (wasCompressed = compressionMode is CompressionMode.ForceOn || compressedMemory.Length < sizeDecompressed)
                memoryToStore = compressedMemory;
        }
        if (!wasCompressed)
            memoryToStore = memory;
        if (AreNamesIndexed())
        {
            if (compressionMode is not CompressionMode.SetDeletedFlag
                or CompressionMode.CallerSuppliedInternal
                or CompressionMode.CallerSuppliedStreamable)
            {
                resourceNameByKey.TryGetValue(key, out var previousName);
                string? newName = null;
                if (resourceFileTypeByResourceType[key.Type] is ResourceFileType.TuningMarkup)
                {
                    using var tuningMarkupStream = new ReadOnlyMemoryOfByteStream(memory);
                    newName = await GetTuningMarkupResourceNameAsync(tuningMarkupStream).ConfigureAwait(false);
                }
                else if (Model.SupportedTypes.Contains(key.Type))
                {
                    using var binaryStream = new ReadOnlyMemoryOfByteStream(memory);
                    newName = await Model.GetNameAsync(key.Type, binaryStream, cancellationToken).ConfigureAwait(false);
                }
                if (newName != previousName)
                {
                    RemoveIndexedResourceName(resourceKeysByName, resourceNameByKey, key);
                    if (newName is not null)
                        AddIndexedResourceName(resourceKeysByName, resourceNameByKey, key, newName);
                }
            }
            else
            {
                // force rebuild of the name indexing
                resourceKeysByName = null;
                resourceNameByKey = null;
            }
        }
        Store(key, memoryToStore, wasCompressed ? mnCompressionType.ZLIB : compressionMode switch
        {
            CompressionMode.SetDeletedFlag => mnCompressionType.Deleted_record,
            CompressionMode.CallerSuppliedInternal => mnCompressionType.Internal_compression,
            CompressionMode.CallerSuppliedStreamable => mnCompressionType.Streamable_compression,
            _ => mnCompressionType.Uncompressed
        }, sizeDecompressed);
        return wasCompressed;
    }

    /// <summary>
    /// Loads all resources in the package into memory
    /// </summary>
    /// <param name="force"><see langword="true"/> to get the content of the resources even if they has been marked as deleted; otheriwse, <see langword="false"/> (default)</param>
    /// <param name="compressionMode">The compression mode to use for resources in memory</param>
    public void LoadAll(bool force = false, CompressionMode compressionMode = CompressionMode.Auto)
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        InternalLoadAll(force, compressionMode);
    }

    /// <summary>
    /// Loads the names of all the resources in the package (a no-op if the names are already loaded)
    /// </summary>
    public void LoadNames()
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        IndexNames();
    }

    /// <summary>
    /// Loads the names of all the resources in the package asynchronously (a no-op if the names are already loaded)
    /// </summary>
    public async Task LoadNamesAsync(CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        await IndexNamesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads all resources in the package into memory
    /// </summary>
    /// <param name="force"><see langword="true"/> to get the content of the resources even if they has been marked as deleted; otheriwse, <see langword="false"/> (default)</param>
    /// <param name="compressionMode">The compression mode to use for resources in memory</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public async Task LoadAllAsync(bool force = false, CompressionMode compressionMode = CompressionMode.Auto, CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        await InternalLoadAllAsync(force, compressionMode, cancellationToken).ConfigureAwait(false);
    }

    void ParseHeader(ReadOnlySpan<byte> header, out int indexEntryCount, out int indexSize, out long indexPosition)
    {
        if (header.Length != 96)
            throw new NotSupportedException("Header length is not supported");
        var mnFileIdentifier = header[0..4];
        if (!mnFileIdentifier.SequenceEqual(expectedFileIdentifier.Span))
            throw new NotSupportedException($"File identifier \"{Encoding.ASCII.GetString(mnFileIdentifier)}\" is invalid");
        var mnFileVersion_major = MemoryMarshal.Read<uint>(header[4..8]);
        var mnFileVersion_minor = MemoryMarshal.Read<uint>(header[8..12]);
        FileVersion = new Version((int)mnFileVersion_major, (int)mnFileVersion_minor);
        var mnUserVersion_major = MemoryMarshal.Read<uint>(header[12..16]);
        var mnUserVersion_minor = MemoryMarshal.Read<uint>(header[16..20]);
        UserVersion = new Version((int)mnUserVersion_major, (int)mnUserVersion_minor);
        var mnCreationTime = MemoryMarshal.Read<int>(header[24..28]);
        CreationTime = DateTimeOffset.FromUnixTimeSeconds(mnCreationTime);
        var mnUpdatedTime = MemoryMarshal.Read<int>(header[28..32]);
        UpdatedTime = DateTimeOffset.FromUnixTimeSeconds(mnUpdatedTime);
        var mnIndexRecordEntryCount = MemoryMarshal.Read<uint>(header[36..40]);
        indexEntryCount = (int)mnIndexRecordEntryCount;
        var mnIndexRecordPositionLow = MemoryMarshal.Read<uint>(header[40..44]);
        var mnIndexRecordSize = MemoryMarshal.Read<uint>(header[44..48]);
        indexSize = (int)mnIndexRecordSize;
        var mnIndexRecordPosition = MemoryMarshal.Read<ulong>(header[64..72]);
        indexPosition = mnIndexRecordPosition != 0 ? (long)mnIndexRecordPosition : mnIndexRecordPositionLow;
    }

    void ParseIndex(ReadOnlySpan<byte> index, int indexEntryCount)
    {
        var readPosition = 0;
        IndexFlags indexFlags = default;
        try
        {
            indexFlags = index.ReadAndAdvancePosition<IndexFlags>(ref readPosition);
            var constantType = indexFlags.HasFlag(IndexFlags.constantType)
                ? (ResourceType?)index.ReadAndAdvancePosition<ResourceType>(ref readPosition)
                : null;
            var constantGroup = indexFlags.HasFlag(IndexFlags.constantGroup)
                ? (uint?)index.ReadAndAdvancePosition<uint>(ref readPosition)
                : null;
            var constantInstanceEx = indexFlags.HasFlag(IndexFlags.constantInstanceEx)
                ? (uint?)index.ReadAndAdvancePosition<uint>(ref readPosition)
                : null;
            for (var i = 0; i < indexEntryCount; ++i)
            {
                var mType = constantType ?? index.ReadAndAdvancePosition<ResourceType>(ref readPosition);
                var mGroup = constantGroup ?? index.ReadAndAdvancePosition<uint>(ref readPosition);
                var mInstanceEx = constantInstanceEx ?? index.ReadAndAdvancePosition<uint>(ref readPosition);
                var mInstance = index.ReadAndAdvancePosition<uint>(ref readPosition);
                var key = new ResourceKey(mType, mGroup, ((ulong)mInstanceEx << 32) | mInstance);
                var entry = new IndexEntry
                (
                    index.ReadAndAdvancePosition<uint>(ref readPosition),
                    index.ReadAndAdvancePosition<mbCompressionType>(ref readPosition),
                    index.ReadAndAdvancePosition<uint>(ref readPosition),
                    default,
                    default
                );
                if (entry.mnSize.HasFlag(mbCompressionType.mbExtendedCompressionType))
                    entry = entry with
                    {
                        mnCompressionType = index.ReadAndAdvancePosition<mnCompressionType>(ref readPosition),
                        mnCommitted = index.ReadAndAdvancePosition<ushort>(ref readPosition)
                    };
                keysInIndexOrder.Add(key);
                ref var entryRef = ref CollectionsMarshal.GetValueRefOrAddDefault(unloadedResources, key, out var exists);
                if (!exists)
                    entryRef = entry;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Index corruption found at position {readPosition} (flags: {indexFlags})", ex);
        }
    }

#if IS_NET_7_0_OR_GREATER
    void RequireNotDisposed() =>
        ObjectDisposedException.ThrowIf(isDisposed, this);
#else
    void RequireNotDisposed()
    {
        if (isDisposed)
            throw new ObjectDisposedException(nameof(DataBasePackedFile));
    }
#endif

    [MemberNotNull(nameof(stream))]
    void RequireStream()
    {
        if (stream is null)
            throw new InvalidOperationException("Package was not loaded from a stream");
    }

    [MemberNotNull(nameof(stream))]
    void RequireWritableStream()
    {
        RequireStream();
        if (!stream.CanWrite)
            throw new InvalidOperationException("Stream must be writable");
    }

    /// <summary>
    /// Saves the package to the stream from which it was loaded
    /// </summary>
    /// <exception cref="InvalidOperationException">The package was not loaded from stream or the stream is not writeable</exception>
    public void Save(bool unloadFromMemory = false, ResourceKeyOrder resourceKeyOrder = ResourceKeyOrder.Preserve)
    {
        RequireWritableStream();
        using var heldResourceLock = resourceLock.Lock();
        InternalLoadAll(false, CompressionMode.Auto);
        stream.Seek(0, SeekOrigin.Begin);
        CopyTo(stream, resourceKeyOrder);
        stream.SetLength(stream.Position);
        if (unloadFromMemory)
        {
            unloadedResources.Clear();
            unloadedResources.TrimExcess();
            loadedResources.Clear();
            loadedResources.TrimExcess();
            InitializeFromStream();
        }
    }

    /// <summary>
    /// Saves the package to the specified <paramref name="path"/> (🔄️🏃)
    /// </summary>
    public void SaveAs(string path, ResourceKeyOrder resourceKeyOrder = ResourceKeyOrder.Preserve)
    {
        using var destination =
            new FileStream
            (
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                createdFileStreamBufferSize,
                createDestinationFileStreamOptions
            );
        CopyTo(destination, resourceKeyOrder);
        destination.SetLength(destination.Position);
        destination.Close();
    }

    /// <summary>
    /// Saves the package to the stream from which it was loaded asynchronously
    /// </summary>
    /// <exception cref="InvalidOperationException">The package was not loaded from stream or the stream is not writeable</exception>
    public async Task SaveAsync(bool unloadFromMemory = false, ResourceKeyOrder resourceKeyOrder = ResourceKeyOrder.Preserve, CancellationToken cancellationToken = default)
    {
        RequireWritableStream();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        await InternalLoadAllAsync(false, CompressionMode.Auto, cancellationToken).ConfigureAwait(false);
        stream.Seek(0, SeekOrigin.Begin);
        await CopyToAsync(stream, resourceKeyOrder, cancellationToken).ConfigureAwait(false);
        stream.SetLength(stream.Position);
        if (unloadFromMemory)
        {
            unloadedResources.Clear();
            unloadedResources.TrimExcess();
            loadedResources.Clear();
            loadedResources.TrimExcess();
            await InitializeFromStreamAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Saves the package to the specified <paramref name="path"/> asynchronously (🔄️🏃)
    /// </summary>
    public async Task SaveAsAsync(string path, ResourceKeyOrder resourceKeyOrder = ResourceKeyOrder.Preserve, CancellationToken cancellationToken = default)
    {
        using var destination =
            new FileStream
            (
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                createdFileStreamBufferSize,
                createDestinationFileStreamOptions | FileOptions.Asynchronous
            );
        await CopyToAsync(destination, resourceKeyOrder, cancellationToken).ConfigureAwait(false);
        destination.SetLength(destination.Position);
        destination.Close();
    }

    /// <summary>
    /// Sets the <paramref name="content"/> of a resource with the specified <paramref name="key"/>, returning <see langword="true"/> if the resource was compressed; otherwise, <see langword="false"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="content">The content of the resource</param>
    /// <param name="compressionMode">The compression mode to use for the resource</param>
    public bool Set(ResourceKey key, ReadOnlyMemory<byte> content, CompressionMode compressionMode = CompressionMode.Auto)
    {
        RequireNotDisposed();
        var memory = content.ToArray().AsMemory();
        using var heldResourceLock = resourceLock.Lock();
        return InternalSet(key, memory, compressionMode);
    }

    /// <summary>
    /// Sets the <paramref name="content"/> of a resource with the specified <paramref name="key"/>, returning <see langword="true"/> if the resource was compressed; otherwise, <see langword="false"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="content">The content of the resource</param>
    /// <param name="compressionMode">The compression mode to use for the resource</param>
    public bool Set(ResourceKey key, IModel content, CompressionMode compressionMode = CompressionMode.Auto)
    {
        ArgumentNullException.ThrowIfNull(content);
        return Set(key, content.Encode(), compressionMode);
    }

    /// <summary>
    /// Sets the <paramref name="content"/> of a resource with the specified <paramref name="key"/>, returning <see langword="true"/> if the resource was compressed; otherwise, <see langword="false"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="content">The content of the resource</param>
    /// <param name="compressionMode">The compression mode to use for the resource</param>
    public bool Set(ResourceKey key, string content, CompressionMode compressionMode = CompressionMode.Auto)
    {
        ArgumentNullException.ThrowIfNull(content);
        RequireNotDisposed();
        var memory = Encoding.UTF8.GetBytes(content).AsMemory();
        using var heldResourceLock = resourceLock.Lock();
        return InternalSet(key, memory, compressionMode);
    }

    /// <summary>
    /// Sets the <paramref name="content"/> of a resource with the specified <paramref name="key"/>, returning <see langword="true"/> if the resource was compressed; otherwise, <see langword="false"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="content">The content of the resource</param>
    /// <param name="compressionMode">The compression mode to use for the resource</param>
    public bool Set(ResourceKey key, XDocument content, CompressionMode compressionMode = CompressionMode.Auto)
    {
        ArgumentNullException.ThrowIfNull(content);
        RequireNotDisposed();
        using var newMarkupStream = new ArrayBufferWriterOfByteStream();
        using var xmlWriter = XmlWriter.Create(newMarkupStream, TuningUtilities.XmlWriterSettings);
        content.Save(xmlWriter);
        xmlWriter.Flush();
        using var heldResourceLock = resourceLock.Lock();
        return InternalSet(key, newMarkupStream.WrittenMemory, compressionMode);
    }

    /// <summary>
    /// Sets the <paramref name="content"/> of a resource with the specified <paramref name="key"/>, returning <see langword="true"/> if the resource was compressed; otherwise, <see langword="false"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="content">The content of the resource</param>
    /// <param name="compressionMode">The compression mode to use for the resource</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public async Task<bool> SetAsync(ResourceKey key, ReadOnlyMemory<byte> content, CompressionMode compressionMode = CompressionMode.Auto, CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        var memory = new Memory<byte>();
        content.CopyTo(memory);
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        return await InternalSetAsync(key, memory, compressionMode, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the <paramref name="content"/> of a resource with the specified <paramref name="key"/>, returning <see langword="true"/> if the resource was compressed; otherwise, <see langword="false"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="content">The content of the resource</param>
    /// <param name="compressionMode">The compression mode to use for the resource</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public async Task<bool> SetAsync(ResourceKey key, IModel content, CompressionMode compressionMode = CompressionMode.Auto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        return await SetAsync(key, await Task.Run(content.Encode).ConfigureAwait(false), compressionMode, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the <paramref name="content"/> of a resource with the specified <paramref name="key"/>, returning <see langword="true"/> if the resource was compressed; otherwise, <see langword="false"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="content">The content of the resource</param>
    /// <param name="compressionMode">The compression mode to use for the resource</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public async Task<bool> SetAsync(ResourceKey key, string content, CompressionMode compressionMode = CompressionMode.Auto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        RequireNotDisposed();
        var memory = Encoding.UTF8.GetBytes(content).AsMemory();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        return await InternalSetAsync(key, memory, compressionMode, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the <paramref name="content"/> of a resource with the specified <paramref name="key"/>, returning <see langword="true"/> if the resource was compressed; otherwise, <see langword="false"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="content">The content of the resource</param>
    /// <param name="compressionMode">The compression mode to use for the resource</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public async Task<bool> SetAsync(ResourceKey key, XDocument content, CompressionMode compressionMode = CompressionMode.Auto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        RequireNotDisposed();
        using var newMarkupStream = new ArrayBufferWriterOfByteStream();
        using var xmlWriter = XmlWriter.Create(newMarkupStream, TuningUtilities.XmlWriterSettings);
        content.Save(xmlWriter);
        await xmlWriter.FlushAsync().ConfigureAwait(false);
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        return await InternalSetAsync(key, newMarkupStream.WrittenMemory, compressionMode, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the <paramref name="xmlContent"/> of a resource with the specified <paramref name="key"/>, returning <see langword="true"/> if the resource was compressed; otherwise, <see langword="false"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="xmlContent">The XML content of the resource</param>
    /// <param name="compressionMode">The compression mode to use for the resource</param>
    public bool SetXml(ResourceKey key, string xmlContent, CompressionMode compressionMode = CompressionMode.Auto) =>
        Set(key, XDocument.Parse(xmlContent, TuningUtilities.XDocumentLoadOptions), compressionMode);

    /// <summary>
    /// Sets the <paramref name="xmlContent"/> of a resource with the specified <paramref name="key"/>, returning <see langword="true"/> if the resource was compressed; otherwise, <see langword="false"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="xmlContent">The XML content of the resource</param>
    /// <param name="compressionMode">The compression mode to use for the resource</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public Task<bool> SetXmlAsync(ResourceKey key, string xmlContent, CompressionMode compressionMode = CompressionMode.Auto, CancellationToken cancellationToken = default) =>
        SetAsync(key, XDocument.Parse(xmlContent, TuningUtilities.XDocumentLoadOptions), compressionMode, cancellationToken);

    void Store(ResourceKey key, ReadOnlyMemory<byte> content, mnCompressionType compressionType, int sizeDecompressed)
    {
        keysInIndexOrder.Add(key);
        var newLoadedResource = new LoadedResource(content, compressionType, sizeDecompressed);
        unloadedResources.Remove(key);
        ref var loadedResource = ref CollectionsMarshal.GetValueRefOrAddDefault(loadedResources, key, out _);
        loadedResource = newLoadedResource;
    }

    void WriteHeader(Span<byte> header, bool hasIndex, int indexSize)
    {
        expectedFileIdentifier.Span.CopyTo(header[0..4]);
        var mnFileVersion_major = (uint)FileVersion.Major;
        var mnFileVersion_minor = (uint)FileVersion.Minor;
        MemoryMarshal.Write(header[4..8], ref mnFileVersion_major);
        MemoryMarshal.Write(header[8..12], ref mnFileVersion_minor);
        var mnUserVersion_major = (uint)UserVersion.Major;
        var mnUserVersion_minor = (uint)UserVersion.Minor;
        MemoryMarshal.Write(header[12..16], ref mnUserVersion_major);
        MemoryMarshal.Write(header[16..20], ref mnUserVersion_minor);
        var mnCreationTime = (int)CreationTime.ToUnixTimeSeconds();
        MemoryMarshal.Write(header[24..28], ref mnCreationTime);
        var mnUpdatedTime = (int)UpdatedTime.ToUnixTimeSeconds();
        MemoryMarshal.Write(header[28..32], ref mnUpdatedTime);
        var mnIndexRecordEntryCount = (uint)unloadedResources.Count + (uint)loadedResources.Count;
        MemoryMarshal.Write(header[36..40], ref mnIndexRecordEntryCount);
        var mnIndexRecordSize = (uint)indexSize;
        MemoryMarshal.Write(header[44..48], ref mnIndexRecordSize);
        var unused4 = defaultUnused4;
        MemoryMarshal.Write(header[60..64], ref unused4);
        if (hasIndex)
        {
            var mnIndexRecordPosition = 96UL
                + (ulong)unloadedResources.Values.Sum(entry => (uint)(entry.mnSize & mbCompressionType.mnSizeMask))
                + (ulong)loadedResources.Values.Sum(loadedResource => (uint)loadedResource.Memory.Length);
            MemoryMarshal.Write(header[64..72], ref mnIndexRecordPosition);
        }
    }

    static void WriteIndexEntry(ArrayBufferWriter<byte> index, IndexFlags indexFlags, int position, ResourceKey key, mbCompressionType mnSize, uint mnSizeDecompressed, mnCompressionType mnCompressionType, ushort mnCommitted)
    {
        if (!indexFlags.HasFlag(IndexFlags.constantType))
        {
            var type = key.Type;
            index.Write(ref type);
        }
        if (!indexFlags.HasFlag(IndexFlags.constantGroup))
        {
            var group = key.Group;
            index.Write(ref group);
        }
        if (!indexFlags.HasFlag(IndexFlags.constantInstanceEx))
        {
            var instanceEx = key.InstanceEx;
            index.Write(ref instanceEx);
        }
        var instance = key.Instance;
        index.Write(ref instance);
        var mnPosition = (uint)position;
        index.Write(ref mnPosition);
        index.Write(ref mnSize);
        index.Write(ref mnSizeDecompressed);
        if (mnSize.HasFlag(mbCompressionType.mbExtendedCompressionType))
        {
            index.Write(ref mnCompressionType);
            index.Write(ref mnCommitted);
        }
    }

    static void WriteIndexEntry(ArrayBufferWriter<byte> index, IndexFlags indexFlags, int position, ResourceKey key, IndexEntry indexEntry) =>
        WriteIndexEntry(index, indexFlags, position, key, indexEntry.mnSize, indexEntry.mnSizeDecompressed, indexEntry.mnCompressionType, indexEntry.mnCommitted);

    static void WriteIndexEntry(ArrayBufferWriter<byte> index, IndexFlags indexFlags, int position, ResourceKey key, LoadedResource loadedResource) =>
        WriteIndexEntry
        (
            index,
            indexFlags,
            position,
            key,
            (mbCompressionType)loadedResource.Memory.Length | mbCompressionType.mbExtendedCompressionType,
            (uint)loadedResource.SizeDecompressed,
            loadedResource.CompressionType,
            defaultMnCommitted
        );
}
