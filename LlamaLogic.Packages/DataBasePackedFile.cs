using LlamaLogic.Packages.Formats;

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
    record IndexEntry(uint mnPosition, CompressionTypeMethodBitmask mnSize, uint mnSizeDecompressed, CompressionTypeMethodNumber mnCompressionType, ushort mnCommitted);

    record LoadedResource(ReadOnlyMemory<byte> Memory, CompressionTypeMethodNumber CompressionType, int SizeDecompressed);

    const int createdFileStreamBufferSize = 4096;
    const FileOptions createDestinationFileStreamOptions = FileOptions.None;
    const FileOptions createPackageFileStreamOptions = FileOptions.RandomAccess;
    static readonly Version defaultFileVersion = new(2, 1);
    const ushort defaultMnCommitted = 1;
    static readonly ReadOnlyMemory<byte> defaultUnused1 = Enumerable.Range(0, 4).Select(_ => (byte)0x00).ToArray();
    static readonly ReadOnlyMemory<byte> defaultUnused2 = Enumerable.Range(0, 4).Select(_ => (byte)0x00).ToArray();
    static readonly ReadOnlyMemory<byte> defaultUnused3 = Enumerable.Range(0, 12).Select(_ => (byte)0x00).ToArray();
    static readonly ReadOnlyMemory<byte> defaultUnused4 = new byte[] { 0x03, 0x00, 0x00, 0x00 };
    static readonly ReadOnlyMemory<byte> defaultUnused5 = Enumerable.Range(0, 24).Select(_ => (byte)0x00).ToArray();
    static readonly Version defaultUserVersion = new(0, 0);
    static readonly Memory<byte> expectedFileIdentifier = "DBPF"u8.ToArray();
    static readonly ImmutableDictionary<ResourceType, ResourceFileType?> resourceFileTypeByResourceType = Enum
        .GetValues<ResourceType>()
        .ToImmutableDictionary(resourceType => resourceType, resourceType => typeof(ResourceType).GetMember(resourceType.ToString()).FirstOrDefault()?.GetCustomAttribute<ResourceFileTypeAttribute>()?.ResourceFileType);
    static readonly ImmutableDictionary<ResourceType, CompressionTypeMethodNumber?> standardCompressionByResourceType = Enum
        .GetValues<ResourceType>()
        .ToImmutableDictionary(resourceType => resourceType, resourceType => typeof(ResourceType).GetMember(resourceType.ToString()).FirstOrDefault()?.GetCustomAttribute<StandardCompressionAttribute>()?.MethodNumber);
    const int unloadedResourceCopyBufferSize = 4096;
    static readonly ResourceKey version1DirectoryKey = new((ResourceType)0xE86B1EEF, 0xE86B1EEF, 0x286B1F03);

    static void AddIndexedResourceName(Dictionary<string, HashSet<ResourceKey>> resourceKeysByName, Dictionary<ResourceKey, string> resourceNameByKey, ResourceKey key, string resourceName)
    {
        resourceNameByKey.TryAdd(key, resourceName);
        ref var keys = ref CollectionsMarshal.GetValueRefOrAddDefault(resourceKeysByName, resourceName, out var keysExisted);
        if (!keysExisted)
            keys = [key];
        else
            keys!.Add(key);
    }

    static ReadOnlyMemory<byte> FetchMemory(LoadedResource loadedResource, bool force, bool isVersion1)
    {
        var (memory, compressionType, sizeDecompressed) = loadedResource;
        if (compressionType is CompressionTypeMethodNumber.Uncompressed)
            return memory;
        if (compressionType is CompressionTypeMethodNumber.Deleted_record)
        {
            if (!force)
                throw new FileNotFoundException($"Resource is marked as deleted (you can provide a value of {true} for the {nameof(force)} parameter of the retrieval method you used to try to recover the content anyway, but this is not guaranteed to work)");
            return memory;
        }
        if (compressionType is CompressionTypeMethodNumber.Internal_compression or CompressionTypeMethodNumber.Streamable_compression)
            return InternalDecompressStored(memory, isVersion1);
        if (compressionType is CompressionTypeMethodNumber.ZLIB)
            return ZLibDecompress(memory, sizeDecompressed);
        throw new NotSupportedException($"Compression type {compressionType} not supported");
    }

    static Task<ReadOnlyMemory<byte>> FetchMemoryAsync(LoadedResource loadedResource, bool force, bool isVersion1, CancellationToken cancellationToken)
    {
        var (memory, compressionType, sizeDecompressed) = loadedResource;
        if (compressionType is CompressionTypeMethodNumber.Uncompressed)
            return Task.FromResult(memory);
        if (compressionType is CompressionTypeMethodNumber.Deleted_record)
        {
            if (!force)
                throw new FileNotFoundException($"Resource is marked as deleted (you can provide a value of {true} for the {nameof(force)} parameter of the retrieval method you used to try to recover the content anyway, but this is not guaranteed to work)");
            return Task.FromResult(memory);
        }
        if (compressionType is CompressionTypeMethodNumber.Internal_compression or CompressionTypeMethodNumber.Streamable_compression)
            return Task.FromResult(InternalDecompressStored(memory, isVersion1));
        if (compressionType is CompressionTypeMethodNumber.ZLIB)
            return ZLibDecompressAsync(memory, sizeDecompressed, cancellationToken);
        throw new NotSupportedException($"Compression type {compressionType} not supported");
    }

    static ReadOnlyMemory<byte> FetchRawMemory(LoadedResource loadedResource, bool force)
    {
        var (memory, compressionType, _) = loadedResource;
        if (compressionType is CompressionTypeMethodNumber.Deleted_record && !force)
            throw new FileNotFoundException($"Resource is marked as deleted (you can provide a value of {true} for the {nameof(force)} parameter of the retrieval method you used to try to recover the content anyway, but this is not guaranteed to work)");
        return memory;
    }

    internal static int FillBuffer(Stream stream, Span<byte> buffer)
    {
        var totalBytesRead = 0;
        int bytesRead;
        while (totalBytesRead < buffer.Length && (bytesRead = stream.Read(buffer[totalBytesRead..])) > 0)
            totalBytesRead += bytesRead;
        return totalBytesRead;
    }

    internal static async ValueTask<int> FillBufferAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var totalBytesRead = 0;
        int bytesRead;
        while (totalBytesRead < buffer.Length && (bytesRead = await stream.ReadAsync(buffer[totalBytesRead..], cancellationToken).ConfigureAwait(false)) > 0)
            totalBytesRead += bytesRead;
        return totalBytesRead;
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

    static ResourceFileType? GetResourceFileType(ResourceType resourceType) =>
        resourceFileTypeByResourceType.TryGetValue(resourceType, out var resourceFileType) ? resourceFileType : null;

    static CompressionTypeMethodNumber? GetStandardCompression(ResourceType resourceType) =>
        standardCompressionByResourceType.TryGetValue(resourceType, out var standardCompression) ? standardCompression : null;

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

    /// <summary>
    /// Compresses the specified <paramref name="memory"/> in the manner of Maxis' internal algorithm, returning the result
    /// </summary>
    [SuppressMessage("Design", "CA1021: Avoid out parameters")]
    public static ReadOnlyMemory<byte> InternalCompress(ReadOnlyMemory<byte> memory, out bool wasCompressed)
    {
        if (Gibbed.RefPack.Compression.Compress(memory.ToArray(), out var output)
            && output is not null)
        {
            wasCompressed = true;
            return output;
        }
        wasCompressed = false;
        return memory;
    }

    /// <summary>
    /// Decompresses the specified <paramref name="memory"/> in the manner of Maxis' internal algorithm, returning the result
    /// </summary>
    public static ReadOnlyMemory<byte> InternalDecompress(ReadOnlyMemory<byte> memory)
    {
        using var compressedStream = new ReadOnlyMemoryOfByteStream(memory);
        return Gibbed.RefPack.Decompression.Decompress(compressedStream);
    }

    static ReadOnlyMemory<byte> InternalDecompressStored(ReadOnlyMemory<byte> stored, bool isVersion1)
    {
        if (!isVersion1)
            return InternalDecompress(stored);
        if (stored.Length < 4)
            throw new InvalidDataException("The compressed resource is too short to hold its size prefix");
        return InternalDecompress(stored[4..]);
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
        using var compressionStream = new DeflaterOutputStream(compressedStream, new Deflater(Deflater.BEST_COMPRESSION));
        compressionStream.Write(memory.Span);
        compressionStream.Finish();
        compressionStream.Flush();
        return compressedStream.WrittenMemory;
    }

    /// <summary>
    /// Compresses the specified <paramref name="memory"/> using ZLib asynchronously, returning the result
    /// </summary>
    public static async Task<ReadOnlyMemory<byte>> ZLibCompressAsync(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default)
    {
        using var compressedStream = new ArrayBufferWriterOfByteStream();
        using var compressionStream = new DeflaterOutputStream(compressedStream, new Deflater(Deflater.BEST_COMPRESSION));
        await compressionStream.WriteAsync(memory, cancellationToken).ConfigureAwait(false);
        await compressionStream.FinishAsync(cancellationToken).ConfigureAwait(false);
        await compressionStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        return compressedStream.WrittenMemory;
    }

    /// <summary>
    /// Decompresses the specified <paramref name="memory"/> using ZLib, returning the result at the specified <paramref name="sizeDecompressed"/>
    /// </summary>
    public static ReadOnlyMemory<byte> ZLibDecompress(ReadOnlyMemory<byte> memory, int sizeDecompressed)
    {
        using var compressedStream = new ReadOnlyMemoryOfByteStream(memory);
        using var decompressionStream = new InflaterInputStream(compressedStream);
        var decompressed = new byte[sizeDecompressed];
        if (FillBuffer(decompressionStream, decompressed) < decompressed.Length)
            throw new EndOfStreamException($"The data decompressed to fewer than {sizeDecompressed} bytes");
        return decompressed;
    }

    /// <summary>
    /// Decompresses the specified <paramref name="memory"/> using ZLib asynchronously, returning the result at the specified <paramref name="sizeDecompressed"/>
    /// </summary>
    public static async Task<ReadOnlyMemory<byte>> ZLibDecompressAsync(ReadOnlyMemory<byte> memory, int sizeDecompressed, CancellationToken cancellationToken = default)
    {
        using var compressedStream = new ReadOnlyMemoryOfByteStream(memory);
        using var decompressionStream = new InflaterInputStream(compressedStream);
        Memory<byte> decompressed = new byte[sizeDecompressed];
        if (await FillBufferAsync(decompressionStream, decompressed, cancellationToken).ConfigureAwait(false) < decompressed.Length)
            throw new EndOfStreamException($"The data decompressed to fewer than {sizeDecompressed} bytes");
        return decompressed;
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
        unused1 = defaultUnused1;
        unused2 = defaultUnused2;
        unused3 = defaultUnused3;
        unused4 = defaultUnused4;
        unused5 = defaultUnused5;
        CreationTime = DateTimeOffset.UtcNow;
        fileVersion = defaultFileVersion;
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

    Version fileVersion;
    bool isDisposed;
    readonly OrderedHashSet<ResourceKey> keysInIndexOrder;
    readonly Dictionary<ResourceKey, LoadedResource> loadedResources;
    Dictionary<string, HashSet<ResourceKey>>? resourceKeysByName;
    readonly AsyncLock resourceLock;
    Dictionary<ResourceKey, string>? resourceNameByKey;
    readonly Stream? stream;
    readonly Dictionary<ResourceKey, IndexEntry> unloadedResources;
    ReadOnlyMemory<byte> unused1;
    ReadOnlyMemory<byte> unused2;
    ReadOnlyMemory<byte> unused3;
    ReadOnlyMemory<byte> unused4;
    ReadOnlyMemory<byte> unused5;

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
    /// Versions <c>1.x</c> (used by The Sims 2) are written with the <c>1.x</c> header and index layouts, compress only with Maxis' internal algorithm, and record which resources are compressed in a directory resource (type, group, and instance <c>E86B1EEF</c>, <c>E86B1EEF</c>, <c>286B1F03</c>) which this class brings up to date whenever it writes the package, removing it when no resources are compressed.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The new value would move the package between version <c>1.x</c> and any other version while it contains a compressed or deleted resource (set those resources with <see cref="CompressionMode.ForceOff"/> first)</exception>
    public Version FileVersion
    {
        get => fileVersion;
        set
        {
            if (value is not null && fileVersion is not null && (value.Major is 1) != (fileVersion.Major is 1))
            {
                using var heldResourceLock = resourceLock.Lock();
                if (unloadedResources.Values.Any(indexEntry => indexEntry.mnCompressionType is not CompressionTypeMethodNumber.Uncompressed)
                    || loadedResources.Values.Any(loadedResource => loadedResource.CompressionType is not CompressionTypeMethodNumber.Uncompressed))
                    throw new InvalidOperationException($"The file version cannot be changed from {fileVersion} to {value} while the package contains compressed or deleted resources, because their stored forms differ between DBPF 1.x and later versions (set them with {nameof(CompressionMode)}.{nameof(CompressionMode.ForceOff)} first)");
            }
            fileVersion = value!;
        }
    }

    /// <summary>
    /// Gets a list of keys for all the resources in the package
    /// </summary>
    public IReadOnlyList<ResourceKey> Keys =>
        GetKeys();

    /// <summary>
    /// Gets/sets the four bytes in the package header starting at index 14h
    /// </summary>
    public ReadOnlyMemory<byte> Unused1
    {
        get => unused1;
        set
        {
            if (value.Length is not 4)
                throw new InvalidOperationException($"{nameof(Unused1)} must be four bytes in length");
            unused1 = value;
        }
    }

    /// <summary>
    /// Gets/sets the four bytes in the package header starting at index 20h
    /// </summary>
    public ReadOnlyMemory<byte> Unused2
    {
        get => unused2;
        set
        {
            if (value.Length is not 4)
                throw new InvalidOperationException($"{nameof(Unused2)} must be four bytes in length");
            unused2 = value;
        }
    }

    /// <summary>
    /// Gets/sets the twelve bytes in the package header starting at index 30h
    /// </summary>
    public ReadOnlyMemory<byte> Unused3
    {
        get => unused3;
        set
        {
            if (value.Length is not 12)
                throw new InvalidOperationException($"{nameof(Unused3)} must be twelve bytes in length");
            unused3 = value;
        }
    }

    /// <summary>
    /// Gets/sets the four bytes in the package header starting at index 3Ch
    /// </summary>
    public ReadOnlyMemory<byte> Unused4
    {
        get => unused4;
        set
        {
            if (value.Length is not 4)
                throw new InvalidOperationException($"{nameof(Unused4)} must be four bytes in length");
            unused4 = value;
        }
    }

    /// <summary>
    /// Gets/sets the twenty-four bytes in the package header starting at index 48h
    /// </summary>
    public ReadOnlyMemory<byte> Unused5
    {
        get => unused5;
        set
        {
            if (value.Length is not 24)
                throw new InvalidOperationException($"{nameof(Unused5)} must be four bytes in length");
            unused5 = value;
        }
    }

    /// <summary>
    /// Gets/sets when the package was last updated
    /// </summary>
    public DateTimeOffset UpdatedTime { get; set; }

    /// <summary>
    /// Gets/sets the version of the user (presumably the user agent, actually? -- is Maxis versioning human beings?)
    /// </summary>
    public Version UserVersion { get; set; }

    /// <summary>
    /// Gets/sets the content of a resource with the specified <paramref name="key"/>
    /// </summary>
    public object? this[ResourceKey key]
    {
        get
        {
            if (!ContainsKey(key))
                return null;
            var fileType = GetResourceFileType(key.Type);
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
                var fileType = GetResourceFileType(key.Type);
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

    void ApplyVersion1Directory(ReadOnlySpan<byte> directory)
    {
        var recordSize = MemoryMarshal.Read<uint>(unused4.Span) >= 2 ? 20 : 16;
        for (var readPosition = 0; directory.Length - readPosition >= recordSize;)
        {
            var mType = directory.ReadAndAdvancePosition<ResourceType>(ref readPosition);
            var mGroup = directory.ReadAndAdvancePosition<uint>(ref readPosition);
            var mInstance = directory.ReadAndAdvancePosition<uint>(ref readPosition);
            var mInstanceEx = recordSize is 20 ? directory.ReadAndAdvancePosition<uint>(ref readPosition) : 0U;
            var mnSizeDecompressed = directory.ReadAndAdvancePosition<uint>(ref readPosition);
            var key = new ResourceKey(mType, mGroup, ((ulong)mInstanceEx << 32) | mInstance);
            if (key == version1DirectoryKey)
                continue;
            ref var indexEntry = ref CollectionsMarshal.GetValueRefOrNullRef(unloadedResources, key);
            if (!Unsafe.IsNullRef(ref indexEntry))
                indexEntry = indexEntry with
                {
                    mnCompressionType = CompressionTypeMethodNumber.Internal_compression,
                    mnSizeDecompressed = mnSizeDecompressed
                };
        }
    }

    [MemberNotNullWhen(true, nameof(resourceKeysByName), nameof(resourceNameByKey))]
    bool AreNamesIndexed() =>
        resourceKeysByName is not null && resourceNameByKey is not null;

    bool BeginIndex(ResourceKeyOrder resourceKeyOrder, uint? version1IndexMinorVersion, [NotNullWhen(true)] out ArrayBufferWriter<byte>? index, out ImmutableArray<ResourceKey> keys, out IndexFlags indexFlags, out int indexSize)
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
        if (version1IndexMinorVersion is { } nonNullVersion1IndexMinorVersion)
        {
            indexFlags = default;
            indexSize = keys.Length * (nonNullVersion1IndexMinorVersion >= 2 ? 24 : 20);
            index = new ArrayBufferWriter<byte>(indexSize);
            return true;
        }
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
                + sizeof(CompressionTypeMethodBitmask) // mnSize
                + sizeof(uint) // mnSizeDecompressed
            )
            + keys.Count(key => !unloadedResources.TryGetValue(key, out var indexEntry) || indexEntry.mnSize.HasFlag(CompressionTypeMethodBitmask.mbExtendedCompressionType))
            *
            (
                  sizeof(CompressionTypeMethodNumber) // mnCompressionType
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
        return content[..FillBuffer(contentStream, content.Span)];
    }

    async Task<ReadOnlyMemory<byte>> FetchMemoryAsync(IndexEntry indexEntry, bool force, CancellationToken cancellationToken)
    {
        using var contentStream = FetchStream(indexEntry, force);
        Memory<byte> content = new byte[indexEntry.mnSizeDecompressed];
        return content[..await FillBufferAsync(contentStream, content, cancellationToken).ConfigureAwait(false)];
    }

    ReadOnlyMemory<byte> FetchRawMemory(IndexEntry indexEntry, bool force)
    {
        using var contentStream = FetchRawStream(indexEntry, force);
        var content = new byte[contentStream.Length];
        if (FillBuffer(contentStream, content) < content.Length)
            throw new EndOfStreamException("The resource could not be fully read");
        return content;
    }

    async Task<ReadOnlyMemory<byte>> FetchRawMemoryAsync(IndexEntry indexEntry, bool force, CancellationToken cancellationToken)
    {
        using var contentStream = FetchRawStream(indexEntry, force);
        Memory<byte> content = new byte[contentStream.Length];
        if (await FillBufferAsync(contentStream, content, cancellationToken).ConfigureAwait(false) < content.Length)
            throw new EndOfStreamException("The resource could not be fully read");
        return content;
    }

    ReadOnlySubStream FetchRawStream(IndexEntry indexEntry, bool force)
    {
        if (indexEntry.mnCompressionType is CompressionTypeMethodNumber.Deleted_record && !force)
            throw new FileNotFoundException($"Resource is marked as deleted (you can provide a value of {true} for the {nameof(force)} parameter of the retrieval method you used to try to recover the content anyway, but this is not guaranteed to work)");
        RequireStream();
        return new ReadOnlySubStream(stream, indexEntry.mnPosition, (uint)(indexEntry.mnSize & CompressionTypeMethodBitmask.mnSizeMask));
    }

    Stream FetchStream(IndexEntry indexEntry, bool force)
    {
        Stream stream = FetchRawStream(indexEntry, force);
        if (indexEntry.mnCompressionType is CompressionTypeMethodNumber.ZLIB)
            stream = new InflaterInputStream(stream);
        else if (indexEntry.mnCompressionType is CompressionTypeMethodNumber.Internal_compression or CompressionTypeMethodNumber.Streamable_compression)
        {
            Memory<byte> compressed = new byte[stream.Length];
            if (FillBuffer(stream, compressed.Span) < compressed.Length)
                throw new EndOfStreamException("The resource could not be fully read");
            stream = new ReadOnlyMemoryOfByteStream(InternalDecompressStored(compressed, FileVersion.Major is 1));
        }
        else if (indexEntry.mnCompressionType is CompressionTypeMethodNumber.Deleted_record)
        {
            if (!force)
                throw new FileNotFoundException($"Compression type {indexEntry.mnCompressionType} cannot be retrieved unless forced");
        }
        else if (indexEntry.mnCompressionType is not CompressionTypeMethodNumber.Uncompressed)
            throw new NotSupportedException($"Compression type {indexEntry.mnCompressionType} not supported");
        return stream;
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
            return FetchMemory(loadedResource, force, FileVersion.Major is 1);
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
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="CombinedTuningModel"/>
    /// </summary>
    public CombinedTuningModel GetCombinedTuning(ResourceKey key, bool force = false) =>
        GetModel<CombinedTuningModel>(key, force);

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="CombinedTuningModel"/> asynchronously
    /// </summary>
    public Task<CombinedTuningModel> GetCombinedTuningAsync(ResourceKey key, bool force = false, CancellationToken cancellationToken = default) =>
        GetModelAsync<CombinedTuningModel>(key, force, cancellationToken);

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
    /// Gets the content of a resource that is a DirectDraw Surface with the specified <paramref name="key"/>, encoded as a PNG
    /// </summary>
    public ReadOnlyMemory<byte> GetDdsAsPng(ResourceKey key, bool force = false)
    {
        if (resourceFileTypeByResourceType.TryGetValue(key.Type, out var resourceFileType)
            && resourceFileType is ResourceFileType.DirectDrawSurfaceAsPortableNetworkGraphic)
            key = new(ResourceType.DSTImage, key.Group, key.FullInstance);
        return DirectDrawSurface.GetPngDataFromDdsData(Get(key, force));
    }

    /// <summary>
    /// Gets the content of a resource that is a DirectDraw Surface with the specified <paramref name="key"/>, encoded as a PNG, asynchronously
    /// </summary>
    public async Task<ReadOnlyMemory<byte>> GetDdsAsPngAsync(ResourceKey key, bool force = false, CancellationToken cancellationToken = default)
    {
        if (resourceFileTypeByResourceType.TryGetValue(key.Type, out var resourceFileType)
            && resourceFileType is ResourceFileType.DirectDrawSurfaceAsPortableNetworkGraphic)
            key = new(ResourceType.DSTImage, key.Group, key.FullInstance);
        return await DirectDrawSurface.GetPngDataFromDdsDataAsync(await GetAsync(key, force, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the <see cref="CompressionMode"/> value to pass to Set methods to ensure they will compress the content of the resource specified by <paramref name="key"/> in the same way it is currently compressed in the package
    /// </summary>
    /// <remarks>
    /// For a resource compressed using Maxis' streamable method, which this library cannot produce, this returns <see cref="CompressionMode.CallerSuppliedStreamable"/>, which must be passed with the resource's raw content to <see cref="SetRaw(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int)"/> or <see cref="SetRawAsync(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int, CancellationToken)"/> rather than to Set methods.
    /// </remarks>
    public CompressionMode GetExplicitCompressionMode(ResourceKey key)
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        static CompressionMode getMode(CompressionTypeMethodNumber compressionType) =>
            compressionType switch
            {
                CompressionTypeMethodNumber.Deleted_record => CompressionMode.SetDeletedFlag,
                CompressionTypeMethodNumber.Uncompressed => CompressionMode.ForceOff,
                CompressionTypeMethodNumber.Internal_compression => CompressionMode.ForceInternal,
                CompressionTypeMethodNumber.Streamable_compression => CompressionMode.CallerSuppliedStreamable,
                CompressionTypeMethodNumber.ZLIB => CompressionMode.ForceZLib,
                _ => throw new NotSupportedException("compression method not supported")
            };
        ref var indexEntry = ref CollectionsMarshal.GetValueRefOrNullRef(unloadedResources, key);
        if (!Unsafe.IsNullRef(ref indexEntry))
            return getMode(indexEntry.mnCompressionType);
        ref var loadedResource = ref CollectionsMarshal.GetValueRefOrNullRef(loadedResources, key);
        if (!Unsafe.IsNullRef(ref loadedResource))
            return getMode(loadedResource.CompressionType);
        throw new KeyNotFoundException($"Key {key} not found");
    }

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
    /// Gets the content of a resource with the specified <paramref name="key"/> as the model <see cref="Model.GetModelType(ResourceType)"/> gives for its type
    /// </summary>
    /// <exception cref="ArgumentException">No model decodes resources of the type specified by <paramref name="key"/></exception>
    public Model GetModel(ResourceKey key, bool force = false) =>
        Model.GetModelType(key.Type) is not null
        ? Model.Decode(key.Type, Get(key, force))
        : throw new ArgumentException($"Resource type {key.Type} is not supported by any model", nameof(key));

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as the model <see cref="Model.GetModelType(ResourceType)"/> gives for its type asynchronously
    /// </summary>
    /// <exception cref="ArgumentException">No model decodes resources of the type specified by <paramref name="key"/></exception>
    public async Task<Model> GetModelAsync(ResourceKey key, bool force = false, CancellationToken cancellationToken = default)
    {
        if (Model.GetModelType(key.Type) is null)
            throw new ArgumentException($"Resource type {key.Type} is not supported by any model", nameof(key));
        return await Task.Run(async () => Model.Decode(key.Type, await GetAsync(key, force, cancellationToken).ConfigureAwait(false))).ConfigureAwait(false);
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
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="SimDataModel"/>
    /// </summary>
    public SimDataModel GetSimData(ResourceKey key, bool force = false) =>
        GetModel<SimDataModel>(key, force);

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="SimDataModel"/> asynchronously
    /// </summary>
    public Task<SimDataModel> GetSimDataAsync(ResourceKey key, bool force = false, CancellationToken cancellationToken = default) =>
        GetModelAsync<SimDataModel>(key, force, cancellationToken);

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
    /// Gets the content of a resource that is a TS4 proprietary translucent JPEG based with the specified <paramref name="key"/>, reintegrated into RGBA and encoded as a PNG
    /// </summary>
    public ReadOnlyMemory<byte> GetTranslucentJpegAsPng(ResourceKey key, bool force = false) =>
        Ts4TranslucentJointPhotographicExpertsGroupImage.ConvertTranslucentJpegToPng(Get(key, force));

    /// <summary>
    /// Gets the content of a resource that is a TS4 proprietary translucent JPEG based with the specified <paramref name="key"/>, reintegrated into RGBA and encoded as a PNG, asynchronously
    /// </summary>
    public async Task<ReadOnlyMemory<byte>> GetTranslucentJpegAsPngAsync(ResourceKey key, bool force = false, CancellationToken cancellationToken = default) =>
        await Ts4TranslucentJointPhotographicExpertsGroupImage.ConvertTranslucentJpegToPngAsync(await GetAsync(key, force, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="WorldDataModel"/>
    /// </summary>
    public WorldDataModel GetWorldData(ResourceKey key, bool force = false) =>
        GetModel<WorldDataModel>(key, force);

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="WorldDataModel"/> asynchronously
    /// </summary>
    public Task<WorldDataModel> GetWorldDataAsync(ResourceKey key, bool force = false, CancellationToken cancellationToken = default) =>
        GetModelAsync<WorldDataModel>(key, force, cancellationToken);

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="WorldLotArchitectureModel"/>
    /// </summary>
    public WorldLotArchitectureModel GetWorldLotArchitecture(ResourceKey key, bool force = false) =>
        GetModel<WorldLotArchitectureModel>(key, force);

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="WorldLotArchitectureModel"/> asynchronously
    /// </summary>
    public Task<WorldLotArchitectureModel> GetWorldLotArchitectureAsync(ResourceKey key, bool force = false, CancellationToken cancellationToken = default) =>
        GetModelAsync<WorldLotArchitectureModel>(key, force, cancellationToken);

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="WorldObjectDataModel"/>
    /// </summary>
    public WorldObjectDataModel GetWorldObjectData(ResourceKey key, bool force = false) =>
        GetModel<WorldObjectDataModel>(key, force);

    /// <summary>
    /// Gets the content of a resource with the specified <paramref name="key"/> as a <see cref="WorldObjectDataModel"/> asynchronously
    /// </summary>
    public Task<WorldObjectDataModel> GetWorldObjectDataAsync(ResourceKey key, bool force = false, CancellationToken cancellationToken = default) =>
        GetModelAsync<WorldObjectDataModel>(key, force, cancellationToken);

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
                    resourceFileType: GetResourceFileType(kv.Key.Type)
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
                    resourceFileType: GetResourceFileType(kv.Key.Type)
                )
            ))
        {
            try
            {
                string? resourceName = null;
                if (resourceFileType is ResourceFileType.TuningMarkup)
                {
                    using var tuningMarkupStream = new ReadOnlyMemoryOfByteStream(FetchMemory(loadedResource, false, FileVersion.Major is 1));
                    resourceName = GetTuningMarkupResourceName(tuningMarkupStream);
                }
                else if (Model.SupportedTypes.Contains(resourceType))
                {
                    using var binaryStream = new ReadOnlyMemoryOfByteStream(FetchMemory(loadedResource, false, FileVersion.Major is 1));
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
                    resourceFileType: GetResourceFileType(kv.Key.Type)
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
                    resourceFileType: GetResourceFileType(kv.Key.Type)
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
                    using var tuningMarkupStream = new ReadOnlyMemoryOfByteStream(await FetchMemoryAsync(loadedResource, false, FileVersion.Major is 1, cancellationToken).ConfigureAwait(false));
                    resourceName = await GetTuningMarkupResourceNameAsync(tuningMarkupStream).ConfigureAwait(false);
#pragma warning restore CS8774 // The way this method is awaited, they won't be null by the time the caller could possibly read them
                }
                else if (Model.SupportedTypes.Contains(resourceType))
                {
#pragma warning disable CS8774 // The way this method is awaited, they won't be null by the time the caller could possibly read them
                    using var binaryStream = new ReadOnlyMemoryOfByteStream(await FetchMemoryAsync(loadedResource, false, FileVersion.Major is 1, cancellationToken).ConfigureAwait(false));
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

    uint GetVersion1IndexMinorVersion()
    {
        if (keysInIndexOrder.Any(key => key.InstanceEx is not 0))
            return 2;
        var indexMinorVersion = MemoryMarshal.Read<uint>(unused4.Span);
        return indexMinorVersion <= 2 ? indexMinorVersion : 1;
    }

    void InitializeFromStream()
    {
        stream!.Seek(0, SeekOrigin.Begin);
        Span<byte> header = stackalloc byte[96];
        var headerBytesRead = FillBuffer(stream, header);
        ParseHeader(header[..headerBytesRead], out var indexEntryCount, out var indexSize, out var indexPosition);
        if (indexSize > 0)
        {
            stream.Seek(indexPosition, SeekOrigin.Begin);
            var indexPoolArray = ArrayPool<byte>.Shared.Rent(indexSize);
            try
            {
                var index = indexPoolArray.AsSpan()[..indexSize];
                var indexBytesRead = FillBuffer(stream, index);
                if (indexBytesRead != indexSize)
                    throw new InvalidDataException($"Index at position {indexPosition} of size {indexSize} could not be fully read");
                ParseIndex(index, indexEntryCount);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(indexPoolArray);
            }
        }
        if (FileVersion.Major is 1 && unloadedResources.TryGetValue(version1DirectoryKey, out var directoryIndexEntry))
            ApplyVersion1Directory(FetchRawMemory(directoryIndexEntry, true).Span);
    }

    async ValueTask InitializeFromStreamAsync(CancellationToken cancellationToken)
    {
        stream!.Seek(0, SeekOrigin.Begin);
        var headerPoolArray = ArrayPool<byte>.Shared.Rent(96);
        try
        {
            var header = headerPoolArray.AsMemory()[..96];
            var headerBytesRead = await FillBufferAsync(stream, header, cancellationToken).ConfigureAwait(false);
            ParseHeader(header[..headerBytesRead].Span, out var indexEntryCount, out var indexSize, out var indexPosition);
            if (indexSize > 0)
            {
                stream.Seek(indexPosition, SeekOrigin.Begin);
                var indexPoolArray = ArrayPool<byte>.Shared.Rent(indexSize);
                try
                {
                    var index = indexPoolArray.AsMemory()[..indexSize];
                    var indexBytesRead = await FillBufferAsync(stream, index, cancellationToken).ConfigureAwait(false);
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
        if (FileVersion.Major is 1 && unloadedResources.TryGetValue(version1DirectoryKey, out var directoryIndexEntry))
            ApplyVersion1Directory((await FetchRawMemoryAsync(directoryIndexEntry, true, cancellationToken).ConfigureAwait(false)).Span);
    }

    void InternalCopyTo(Stream destination, ResourceKeyOrder resourceKeyOrder)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
            throw new ArgumentException("Stream must be writable", nameof(destination));
        uint? version1IndexMinorVersion = null;
        if (FileVersion.Major is 1)
        {
            version1IndexMinorVersion = GetVersion1IndexMinorVersion();
            ReadOnlyMemory<byte>? directory = null;
            if (unloadedResources.TryGetValue(version1DirectoryKey, out var directoryIndexEntry))
                directory = FetchRawMemory(directoryIndexEntry, true);
            else if (loadedResources.TryGetValue(version1DirectoryKey, out var directoryLoadedResource))
                directory = directoryLoadedResource.Memory;
            UpdateVersion1Directory(version1IndexMinorVersion.Value, directory);
        }
        var hasIndex = BeginIndex(resourceKeyOrder, version1IndexMinorVersion, out var index, out var keys, out var indexFlags, out var indexSize);
        RequireResourcePositionsFit(keys);
        Span<byte> header = stackalloc byte[96];
        WriteHeader(header, hasIndex, indexSize, version1IndexMinorVersion);
        destination.Write(header);
        var position = 96L;
        var bufferPoolArray = ArrayPool<byte>.Shared.Rent(unloadedResourceCopyBufferSize);
        try
        {
            var buffer = bufferPoolArray.AsSpan()[..unloadedResourceCopyBufferSize];
            foreach (var key in keys)
            {
                if (unloadedResources.TryGetValue(key, out var indexEntry))
                {
                    WriteIndexEntry(index!, indexFlags, version1IndexMinorVersion, position, key, indexEntry);
                    stream!.Seek(indexEntry.mnPosition, SeekOrigin.Begin);
                    var resourceSize = (int)(indexEntry.mnSize & CompressionTypeMethodBitmask.mnSizeMask);
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
                    WriteIndexEntry(index!, indexFlags, version1IndexMinorVersion, position, key, loadedResource);
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
        uint? version1IndexMinorVersion = null;
        if (FileVersion.Major is 1)
        {
            version1IndexMinorVersion = GetVersion1IndexMinorVersion();
            ReadOnlyMemory<byte>? directory = null;
            if (unloadedResources.TryGetValue(version1DirectoryKey, out var directoryIndexEntry))
                directory = await FetchRawMemoryAsync(directoryIndexEntry, true, cancellationToken).ConfigureAwait(false);
            else if (loadedResources.TryGetValue(version1DirectoryKey, out var directoryLoadedResource))
                directory = directoryLoadedResource.Memory;
            UpdateVersion1Directory(version1IndexMinorVersion.Value, directory);
        }
        var hasIndex = BeginIndex(resourceKeyOrder, version1IndexMinorVersion, out var index, out var keys, out var indexFlags, out var indexSize);
        RequireResourcePositionsFit(keys);
        var headerPoolArray = ArrayPool<byte>.Shared.Rent(96);
        try
        {
            var header = headerPoolArray.AsMemory()[..96];
            header.Span.Clear();
            WriteHeader(header.Span, hasIndex, indexSize, version1IndexMinorVersion);
            await destination.WriteAsync(header, cancellationToken).ConfigureAwait(false);
            var position = 96L;
            var bufferPoolArray = ArrayPool<byte>.Shared.Rent(unloadedResourceCopyBufferSize);
            try
            {
                var buffer = bufferPoolArray.AsMemory()[..unloadedResourceCopyBufferSize];
                foreach (var key in keys)
                {
                    if (unloadedResources.TryGetValue(key, out var indexEntry))
                    {
                        WriteIndexEntry(index!, indexFlags, version1IndexMinorVersion, position, key, indexEntry);
                        stream!.Seek(indexEntry.mnPosition, SeekOrigin.Begin);
                        var resourceSize = (int)(indexEntry.mnSize & CompressionTypeMethodBitmask.mnSizeMask);
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
                        WriteIndexEntry(index!, indexFlags, version1IndexMinorVersion, position, key, loadedResource);
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
            return FetchMemoryAsync(loadedResource, force, FileVersion.Major is 1, cancellationToken);
        throw new KeyNotFoundException($"Key {key} not found");
    }

    ImmutableArray<ResourceKey> InternalGetKeys(ResourceKeyOrder resourceKeyOrder) =>
        [..resourceKeyOrder switch
        {
            ResourceKeyOrder.InstanceTypeGroup => keysInIndexOrder.OrderBy(key => key.FullInstance).ThenBy(key => key.Type).ThenBy(key => key.Group),
            ResourceKeyOrder.TypeGroupInstance => keysInIndexOrder.OrderBy(key => key.Type).ThenBy(key => key.Group).ThenBy(key => key.FullInstance),
            _ => (IEnumerable<ResourceKey>)keysInIndexOrder
        }];

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

    void InternalLoadAll(bool force)
    {
        foreach (var (key, indexEntry) in unloadedResources)
            Store(key, FetchRawMemory(indexEntry, force), indexEntry.mnCompressionType, (int)indexEntry.mnSizeDecompressed);
    }

    async Task InternalLoadAllAsync(bool force, CancellationToken cancellationToken)
    {
        foreach (var (key, indexEntry) in unloadedResources)
            Store(key, await FetchRawMemoryAsync(indexEntry, force, cancellationToken).ConfigureAwait(false), indexEntry.mnCompressionType, (int)indexEntry.mnSizeDecompressed);
    }

    bool InternalSet(ResourceKey key, ReadOnlyMemory<byte> memory, CompressionMode compressionMode)
    {
        if (compressionMode is CompressionMode.CallerSuppliedInternal or CompressionMode.CallerSuppliedStreamable or CompressionMode.CallerSuppliedZLib)
            throw new ArgumentException($"Content set with a caller-supplied compression mode must come with its decompressed size; use {nameof(SetRaw)} or {nameof(SetRawAsync)} instead", nameof(compressionMode));
        CompressionTypeMethodNumber compressionMethod;
        ReadOnlyMemory<byte> memoryToStore;
        if (FileVersion.Major is 1)
            memoryToStore = Version1Compress(memory, compressionMode, out compressionMethod);
        else
        {
            compressionMethod = compressionMode switch
            {
                CompressionMode.Auto => GetStandardCompression(key.Type) ?? CompressionTypeMethodNumber.ZLIB,
                CompressionMode.ForceOff => CompressionTypeMethodNumber.Uncompressed,
                CompressionMode.SetDeletedFlag => CompressionTypeMethodNumber.Deleted_record,
                CompressionMode.ForceInternal => CompressionTypeMethodNumber.Internal_compression,
                CompressionMode.ForceZLib => CompressionTypeMethodNumber.ZLIB,
                _ => throw new NotSupportedException($"the compression mode {compressionMode} is not supported")
            };
            if (compressionMethod is CompressionTypeMethodNumber.Uncompressed
                || compressionMode is CompressionMode.ForceOff
                or CompressionMode.SetDeletedFlag)
                memoryToStore = memory;
            else if (compressionMethod is CompressionTypeMethodNumber.Internal_compression)
            {
                memoryToStore = InternalCompress(memory, out var wasInternallyCompressed);
                if (!wasInternallyCompressed)
                    compressionMethod = CompressionTypeMethodNumber.Uncompressed;
            }
            else if (compressionMethod is CompressionTypeMethodNumber.ZLIB)
            {
                var zlibCompressed = ZLibCompress(memory);
                if (zlibCompressed.Length >= memory.Length)
                {
                    compressionMethod = CompressionTypeMethodNumber.Uncompressed;
                    memoryToStore = memory;
                }
                else
                    memoryToStore = zlibCompressed;
            }
            else
                throw new NotSupportedException($"the compression method {compressionMethod} is not supported");
        }
        if (AreNamesIndexed())
        {
            if (compressionMode is not CompressionMode.SetDeletedFlag)
            {
                resourceNameByKey.TryGetValue(key, out var previousName);
                string? newName = null;
                if (GetResourceFileType(key.Type) is ResourceFileType.TuningMarkup)
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
        Store(key, memoryToStore, compressionMethod, memory.Length);
        return
            compressionMode is not (CompressionMode.ForceOff
            or CompressionMode.SetDeletedFlag)
            && compressionMethod is CompressionTypeMethodNumber.Internal_compression
            or CompressionTypeMethodNumber.ZLIB;
    }

    async Task<bool> InternalSetAsync(ResourceKey key, ReadOnlyMemory<byte> memory, CompressionMode compressionMode, CancellationToken cancellationToken)
    {
        if (compressionMode is CompressionMode.CallerSuppliedInternal or CompressionMode.CallerSuppliedStreamable or CompressionMode.CallerSuppliedZLib)
            throw new ArgumentException($"Content set with a caller-supplied compression mode must come with its decompressed size; use {nameof(SetRaw)} or {nameof(SetRawAsync)} instead", nameof(compressionMode));
        CompressionTypeMethodNumber compressionMethod;
        ReadOnlyMemory<byte> memoryToStore;
        if (FileVersion.Major is 1)
            memoryToStore = Version1Compress(memory, compressionMode, out compressionMethod);
        else
        {
            compressionMethod = compressionMode switch
            {
                CompressionMode.Auto => GetStandardCompression(key.Type) ?? CompressionTypeMethodNumber.ZLIB,
                CompressionMode.ForceOff => CompressionTypeMethodNumber.Uncompressed,
                CompressionMode.SetDeletedFlag => CompressionTypeMethodNumber.Deleted_record,
                CompressionMode.ForceInternal => CompressionTypeMethodNumber.Internal_compression,
                CompressionMode.ForceZLib => CompressionTypeMethodNumber.ZLIB,
                _ => throw new NotSupportedException($"the compression mode {compressionMode} is not supported")
            };
            if (compressionMethod is CompressionTypeMethodNumber.Uncompressed
                || compressionMode is CompressionMode.ForceOff
                or CompressionMode.SetDeletedFlag)
                memoryToStore = memory;
            else if (compressionMethod is CompressionTypeMethodNumber.Internal_compression)
            {
                memoryToStore = InternalCompress(memory, out var wasInternallyCompressed);
                if (!wasInternallyCompressed)
                    compressionMethod = CompressionTypeMethodNumber.Uncompressed;
            }
            else if (compressionMethod is CompressionTypeMethodNumber.ZLIB)
            {
                var zlibCompressed = await ZLibCompressAsync(memory).ConfigureAwait(false);
                if (zlibCompressed.Length >= memory.Length)
                {
                    compressionMethod = CompressionTypeMethodNumber.Uncompressed;
                    memoryToStore = memory;
                }
                else
                    memoryToStore = zlibCompressed;
            }
            else
                throw new NotSupportedException($"the compression method {compressionMethod} is not supported");
        }
        if (AreNamesIndexed())
        {
            if (compressionMode is not CompressionMode.SetDeletedFlag)
            {
                resourceNameByKey.TryGetValue(key, out var previousName);
                string? newName = null;
                if (GetResourceFileType(key.Type) is ResourceFileType.TuningMarkup)
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
        Store(key, memoryToStore, compressionMethod, memory.Length);
        return
            compressionMode is not (CompressionMode.ForceOff
            or CompressionMode.SetDeletedFlag)
            && compressionMethod is CompressionTypeMethodNumber.Internal_compression
            or CompressionTypeMethodNumber.ZLIB;
    }

    void InternalSetRaw(ResourceKey key, ReadOnlyMemory<byte> rawContent, CompressionMode compressionMode, int sizeDecompressed)
    {
        var compressionMethod = compressionMode switch
        {
            CompressionMode.CallerSuppliedInternal => CompressionTypeMethodNumber.Internal_compression,
            CompressionMode.CallerSuppliedStreamable => CompressionTypeMethodNumber.Streamable_compression,
            CompressionMode.CallerSuppliedZLib => CompressionTypeMethodNumber.ZLIB,
            _ => throw new ArgumentException($"Only caller-supplied compression modes may be used to set raw content; use {nameof(Set)} or {nameof(SetAsync)} for {compressionMode}", nameof(compressionMode))
        };
        if (sizeDecompressed < 0)
            throw new ArgumentOutOfRangeException(nameof(sizeDecompressed), sizeDecompressed, "The decompressed size cannot be negative");
        if (FileVersion.Major is 1 && compressionMethod is not CompressionTypeMethodNumber.Internal_compression)
            throw new NotSupportedException($"DBPF 1.x packages support only the {nameof(CompressionMode.CallerSuppliedInternal)} caller-supplied compression mode, not {compressionMode}");
        if (AreNamesIndexed())
        {
            resourceKeysByName = null;
            resourceNameByKey = null;
        }
        Store(key, rawContent, compressionMethod, sizeDecompressed);
    }

    /// <summary>
    /// Loads all resources in the package into memory
    /// </summary>
    /// <param name="force"><see langword="true"/> to get the content of the resources even if they has been marked as deleted; otheriwse, <see langword="false"/> (default)</param>
    /// <param name="compressionMode">This parameter is now ignored</param>
    public void LoadAll(bool force = false, CompressionMode compressionMode = CompressionMode.Auto)
    {
        RequireNotDisposed();
        using var heldResourceLock = resourceLock.Lock();
        InternalLoadAll(force);
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
    /// <param name="compressionMode">This parameter is now ignored</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public async Task LoadAllAsync(bool force = false, CompressionMode compressionMode = CompressionMode.Auto, CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        await InternalLoadAllAsync(force, cancellationToken).ConfigureAwait(false);
    }

    void ParseHeader(ReadOnlySpan<byte> header, out int indexEntryCount, out int indexSize, out long indexPosition)
    {
        if (header.Length is not 96)
            throw new NotSupportedException("Header length is not supported");
        var mnFileIdentifier = header[0..4];
        if (!mnFileIdentifier.SequenceEqual(expectedFileIdentifier.Span))
            throw new NotSupportedException($"File identifier \"{Encoding.ASCII.GetString(mnFileIdentifier)}\" is invalid");
        var mnFileVersion_major = MemoryMarshal.Read<uint>(header[4..8]);
        var mnFileVersion_minor = MemoryMarshal.Read<uint>(header[8..12]);
        fileVersion = new Version((int)mnFileVersion_major, (int)mnFileVersion_minor);
        var mnUserVersion_major = MemoryMarshal.Read<uint>(header[12..16]);
        var mnUserVersion_minor = MemoryMarshal.Read<uint>(header[16..20]);
        UserVersion = new Version((int)mnUserVersion_major, (int)mnUserVersion_minor);
        unused1 = header[20..24].ToArray();
        var mnCreationTime = MemoryMarshal.Read<int>(header[24..28]);
        CreationTime = DateTimeOffset.FromUnixTimeSeconds(mnCreationTime);
        var mnUpdatedTime = MemoryMarshal.Read<int>(header[28..32]);
        UpdatedTime = DateTimeOffset.FromUnixTimeSeconds(mnUpdatedTime);
        unused2 = header[32..36].ToArray();
        var mnIndexRecordEntryCount = MemoryMarshal.Read<uint>(header[36..40]);
        indexEntryCount = (int)mnIndexRecordEntryCount;
        var mnIndexRecordPositionLow = MemoryMarshal.Read<uint>(header[40..44]);
        var mnIndexRecordSize = MemoryMarshal.Read<uint>(header[44..48]);
        indexSize = (int)mnIndexRecordSize;
        unused3 = header[48..60].ToArray();
        unused4 = header[60..64].ToArray();
        var mnIndexRecordPosition = MemoryMarshal.Read<ulong>(header[64..72]);
        indexPosition = mnIndexRecordPosition != 0 ? (long)mnIndexRecordPosition : mnIndexRecordPositionLow;
        unused5 = header[72..96].ToArray();
    }

    void ParseIndex(ReadOnlySpan<byte> index, int indexEntryCount)
    {
        if (FileVersion.Major is 1)
        {
            ParseVersion1Index(index, indexEntryCount);
            return;
        }
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
                    index.ReadAndAdvancePosition<CompressionTypeMethodBitmask>(ref readPosition),
                    index.ReadAndAdvancePosition<uint>(ref readPosition),
                    default,
                    default
                );
                if (entry.mnSize.HasFlag(CompressionTypeMethodBitmask.mbExtendedCompressionType))
                    entry = entry with
                    {
                        mnCompressionType = index.ReadAndAdvancePosition<CompressionTypeMethodNumber>(ref readPosition),
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

    void ParseVersion1Index(ReadOnlySpan<byte> index, int indexEntryCount)
    {
        var hasInstanceEx = MemoryMarshal.Read<uint>(unused4.Span) >= 2;
        var readPosition = 0;
        try
        {
            for (var i = 0; i < indexEntryCount; ++i)
            {
                var mType = index.ReadAndAdvancePosition<ResourceType>(ref readPosition);
                var mGroup = index.ReadAndAdvancePosition<uint>(ref readPosition);
                var mInstance = index.ReadAndAdvancePosition<uint>(ref readPosition);
                var mInstanceEx = hasInstanceEx ? index.ReadAndAdvancePosition<uint>(ref readPosition) : 0U;
                var key = new ResourceKey(mType, mGroup, ((ulong)mInstanceEx << 32) | mInstance);
                var mnPosition = index.ReadAndAdvancePosition<uint>(ref readPosition);
                var mnSize = index.ReadAndAdvancePosition<uint>(ref readPosition);
                keysInIndexOrder.Add(key);
                ref var entryRef = ref CollectionsMarshal.GetValueRefOrAddDefault(unloadedResources, key, out var exists);
                if (!exists)
                    entryRef = new IndexEntry(mnPosition, (CompressionTypeMethodBitmask)mnSize, mnSize, CompressionTypeMethodNumber.Uncompressed, default);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Index corruption found at position {readPosition} (DBPF {FileVersion})", ex);
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

    void RequireResourcePositionsFit(ImmutableArray<ResourceKey> keys)
    {
        var position = 96L;
        foreach (var key in keys)
        {
            if (position > uint.MaxValue)
                throw new InvalidOperationException($"Resource {key} would begin beyond 4 GiB, which an index entry cannot record");
            position += unloadedResources.TryGetValue(key, out var indexEntry)
                ? (uint)(indexEntry.mnSize & CompressionTypeMethodBitmask.mnSizeMask)
                : loadedResources[key].Memory.Length;
        }
    }

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
    /// <remarks>
    /// This method overwrites the stream in place.
    /// If it fails partway through writing (for example, because the disk is full, another process has the file open, or the process is terminated), the stream will hold neither the original package nor the new one, and the original cannot be recovered.
    /// If losing the original would be costly, load the package as read-only, use <see cref="SaveAs(string, ResourceKeyOrder)"/> to write a temporary file in the same directory, dispose of the package, and then replace the original with the temporary file (e.g. with <see cref="File.Replace(string, string, string)"/>).
    /// <see cref="SaveAs(string, ResourceKeyOrder)"/> cannot write to the path from which the package is currently open.
    /// </remarks>
    public void Save(bool unloadFromMemory = false, ResourceKeyOrder resourceKeyOrder = ResourceKeyOrder.Preserve)
    {
        RequireWritableStream();
        using var heldResourceLock = resourceLock.Lock();
        InternalLoadAll(true);
        stream.Seek(0, SeekOrigin.Begin);
        InternalCopyTo(stream, resourceKeyOrder);
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
    /// <remarks>
    /// This method overwrites the stream in place.
    /// If it fails partway through writing (for example, because the disk is full, another process has the file open, or the process is terminated), the stream will hold neither the original package nor the new one, and the original cannot be recovered.
    /// If losing the original would be costly, load the package as read-only, use <see cref="SaveAsAsync(string, ResourceKeyOrder, CancellationToken)"/> to write a temporary file in the same directory, dispose of the package, and then replace the original with the temporary file (e.g. with <see cref="File.Replace(string, string, string)"/>).
    /// <see cref="SaveAsAsync(string, ResourceKeyOrder, CancellationToken)"/> cannot write to the path from which the package is currently open.
    /// </remarks>
    public async Task SaveAsync(bool unloadFromMemory = false, ResourceKeyOrder resourceKeyOrder = ResourceKeyOrder.Preserve, CancellationToken cancellationToken = default)
    {
        RequireWritableStream();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        await InternalLoadAllAsync(true, cancellationToken).ConfigureAwait(false);
        stream.Seek(0, SeekOrigin.Begin);
        await InternalCopyToAsync(stream, resourceKeyOrder, cancellationToken).ConfigureAwait(false);
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
    /// <exception cref="ArgumentException"><paramref name="compressionMode"/> is a caller-supplied compression mode (use <see cref="SetRaw(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int)"/> or <see cref="SetRawAsync(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int, CancellationToken)"/> instead)</exception>
    public bool Set(ResourceKey key, ReadOnlyMemory<byte> content, CompressionMode compressionMode = CompressionMode.Auto)
    {
        RequireNotDisposed();
        Memory<byte> memory = new byte[content.Length];
        content.CopyTo(memory);
        using var heldResourceLock = resourceLock.Lock();
        return InternalSet(key, memory, compressionMode);
    }

    /// <summary>
    /// Sets the <paramref name="content"/> of a resource with the specified <paramref name="key"/>, returning <see langword="true"/> if the resource was compressed; otherwise, <see langword="false"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="content">The content of the resource</param>
    /// <param name="compressionMode">The compression mode to use for the resource</param>
    /// <exception cref="ArgumentException"><paramref name="compressionMode"/> is a caller-supplied compression mode (use <see cref="SetRaw(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int)"/> or <see cref="SetRawAsync(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int, CancellationToken)"/> instead)</exception>
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
    /// <exception cref="ArgumentException"><paramref name="compressionMode"/> is a caller-supplied compression mode (use <see cref="SetRaw(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int)"/> or <see cref="SetRawAsync(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int, CancellationToken)"/> instead)</exception>
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
    /// <exception cref="ArgumentException"><paramref name="compressionMode"/> is a caller-supplied compression mode (use <see cref="SetRaw(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int)"/> or <see cref="SetRawAsync(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int, CancellationToken)"/> instead)</exception>
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
    /// <exception cref="ArgumentException"><paramref name="compressionMode"/> is a caller-supplied compression mode (use <see cref="SetRaw(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int)"/> or <see cref="SetRawAsync(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int, CancellationToken)"/> instead)</exception>
    public async Task<bool> SetAsync(ResourceKey key, ReadOnlyMemory<byte> content, CompressionMode compressionMode = CompressionMode.Auto, CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        Memory<byte> memory = new byte[content.Length];
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
    /// <exception cref="ArgumentException"><paramref name="compressionMode"/> is a caller-supplied compression mode (use <see cref="SetRaw(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int)"/> or <see cref="SetRawAsync(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int, CancellationToken)"/> instead)</exception>
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
    /// <exception cref="ArgumentException"><paramref name="compressionMode"/> is a caller-supplied compression mode (use <see cref="SetRaw(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int)"/> or <see cref="SetRawAsync(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int, CancellationToken)"/> instead)</exception>
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
    /// <exception cref="ArgumentException"><paramref name="compressionMode"/> is a caller-supplied compression mode (use <see cref="SetRaw(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int)"/> or <see cref="SetRawAsync(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int, CancellationToken)"/> instead)</exception>
    public async Task<bool> SetAsync(ResourceKey key, XDocument content, CompressionMode compressionMode = CompressionMode.Auto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        RequireNotDisposed();
        using var newMarkupStream = new ArrayBufferWriterOfByteStream();
        using var xmlWriter = XmlWriter.Create(newMarkupStream, TuningUtilities.XmlWriterSettings);
        content.Save(xmlWriter);
        xmlWriter.Flush();
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        return await InternalSetAsync(key, newMarkupStream.WrittenMemory, compressionMode, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the content of a resource specified by <paramref name="key"/> to a shuffled DirectDraw Surface based on <paramref name="pngData"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="pngData">The encoded PNG data</param>
    public void SetPngAsDds(ResourceKey key, ReadOnlyMemory<byte> pngData) =>
        Set(key, DirectDrawSurface.GetDstDataFromPngData(pngData), CompressionMode.ForceZLib);

    /// <summary>
    /// Sets the content of a resource specified by <paramref name="key"/> to a shuffled DirectDraw Surface based on <paramref name="pngData"/> asycnhronously
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="pngData">The encoded PNG data</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public async Task SetPngAsDdsAsync(ResourceKey key, ReadOnlyMemory<byte> pngData, CancellationToken cancellationToken = default) =>
        await SetAsync(key, await DirectDrawSurface.GetDstDataFromPngDataAsync(pngData, cancellationToken).ConfigureAwait(false), CompressionMode.ForceZLib, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Sets the content of a resource specified by <paramref name="key"/> to a TS4 proprietary translucent JPEG based on <paramref name="pngData"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="pngData">The encoded PNG data</param>
    public void SetPngAsTranslucentJpeg(ResourceKey key, ReadOnlyMemory<byte> pngData) =>
        Set(key, Ts4TranslucentJointPhotographicExpertsGroupImage.ConvertPngToTranslucentJpeg(pngData), CompressionMode.ForceOff);

    /// <summary>
    /// Sets the content of a resource specified by <paramref name="key"/> to a TS4 proprietary translucent JPEG based on <paramref name="pngData"/> asycnhronously
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="pngData">The encoded PNG data</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    public async Task SetPngAsTranslucentJpegAsync(ResourceKey key, ReadOnlyMemory<byte> pngData, CancellationToken cancellationToken = default) =>
        await SetAsync(key, await Ts4TranslucentJointPhotographicExpertsGroupImage.ConvertPngToTranslucentJpegAsync(pngData, cancellationToken).ConfigureAwait(false), CompressionMode.ForceOff, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Sets the raw content of a resource with the specified <paramref name="key"/>, meaning that the library stores <paramref name="rawContent"/> as it is, marked as compressed in the manner of <paramref name="compressionMode"/>, with the decompressed size the caller states
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="rawContent">The content of the resource, already compressed by the caller (for DBPF 1.x packages, including the four-byte size prefix that format requires)</param>
    /// <param name="compressionMode">The manner in which <paramref name="rawContent"/> is compressed: <see cref="CompressionMode.CallerSuppliedInternal"/>, <see cref="CompressionMode.CallerSuppliedStreamable"/>, or <see cref="CompressionMode.CallerSuppliedZLib"/></param>
    /// <param name="sizeDecompressed">The size of the content of the resource once decompressed</param>
    /// <exception cref="ArgumentException"><paramref name="compressionMode"/> is not a caller-supplied compression mode</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizeDecompressed"/> is negative</exception>
    /// <exception cref="NotSupportedException">The package is DBPF 1.x and <paramref name="compressionMode"/> is not <see cref="CompressionMode.CallerSuppliedInternal"/></exception>
    public void SetRaw(ResourceKey key, ReadOnlyMemory<byte> rawContent, CompressionMode compressionMode, int sizeDecompressed)
    {
        RequireNotDisposed();
        Memory<byte> memory = new byte[rawContent.Length];
        rawContent.CopyTo(memory);
        using var heldResourceLock = resourceLock.Lock();
        InternalSetRaw(key, memory, compressionMode, sizeDecompressed);
    }

    /// <summary>
    /// Sets the raw content of a resource with the specified <paramref name="key"/> asynchronously, meaning that the library stores <paramref name="rawContent"/> as it is, marked as compressed in the manner of <paramref name="compressionMode"/>, with the decompressed size the caller states
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="rawContent">The content of the resource, already compressed by the caller (for DBPF 1.x packages, including the four-byte size prefix that format requires)</param>
    /// <param name="compressionMode">The manner in which <paramref name="rawContent"/> is compressed: <see cref="CompressionMode.CallerSuppliedInternal"/>, <see cref="CompressionMode.CallerSuppliedStreamable"/>, or <see cref="CompressionMode.CallerSuppliedZLib"/></param>
    /// <param name="sizeDecompressed">The size of the content of the resource once decompressed</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    /// <exception cref="ArgumentException"><paramref name="compressionMode"/> is not a caller-supplied compression mode</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizeDecompressed"/> is negative</exception>
    /// <exception cref="NotSupportedException">The package is DBPF 1.x and <paramref name="compressionMode"/> is not <see cref="CompressionMode.CallerSuppliedInternal"/></exception>
    public async Task SetRawAsync(ResourceKey key, ReadOnlyMemory<byte> rawContent, CompressionMode compressionMode, int sizeDecompressed, CancellationToken cancellationToken = default)
    {
        RequireNotDisposed();
        Memory<byte> memory = new byte[rawContent.Length];
        rawContent.CopyTo(memory);
        using var heldResourceLock = await resourceLock.LockAsync(cancellationToken).ConfigureAwait(false);
        InternalSetRaw(key, memory, compressionMode, sizeDecompressed);
    }

    /// <summary>
    /// Sets the <paramref name="xmlContent"/> of a resource with the specified <paramref name="key"/>, returning <see langword="true"/> if the resource was compressed; otherwise, <see langword="false"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="xmlContent">The XML content of the resource</param>
    /// <param name="compressionMode">The compression mode to use for the resource</param>
    /// <exception cref="ArgumentException"><paramref name="compressionMode"/> is a caller-supplied compression mode (use <see cref="SetRaw(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int)"/> or <see cref="SetRawAsync(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int, CancellationToken)"/> instead)</exception>
    public bool SetXml(ResourceKey key, string xmlContent, CompressionMode compressionMode = CompressionMode.Auto) =>
        Set(key, XDocument.Parse(xmlContent, TuningUtilities.XDocumentLoadOptions), compressionMode);

    /// <summary>
    /// Sets the <paramref name="xmlContent"/> of a resource with the specified <paramref name="key"/>, returning <see langword="true"/> if the resource was compressed; otherwise, <see langword="false"/>
    /// </summary>
    /// <param name="key">The key of the resource</param>
    /// <param name="xmlContent">The XML content of the resource</param>
    /// <param name="compressionMode">The compression mode to use for the resource</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests</param>
    /// <exception cref="ArgumentException"><paramref name="compressionMode"/> is a caller-supplied compression mode (use <see cref="SetRaw(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int)"/> or <see cref="SetRawAsync(ResourceKey, ReadOnlyMemory{byte}, CompressionMode, int, CancellationToken)"/> instead)</exception>
    public Task<bool> SetXmlAsync(ResourceKey key, string xmlContent, CompressionMode compressionMode = CompressionMode.Auto, CancellationToken cancellationToken = default) =>
        SetAsync(key, XDocument.Parse(xmlContent, TuningUtilities.XDocumentLoadOptions), compressionMode, cancellationToken);

    void Store(ResourceKey key, ReadOnlyMemory<byte> content, CompressionTypeMethodNumber compressionType, int sizeDecompressed)
    {
        keysInIndexOrder.Add(key);
        var newLoadedResource = new LoadedResource(content, compressionType, sizeDecompressed);
        unloadedResources.Remove(key);
        ref var loadedResource = ref CollectionsMarshal.GetValueRefOrAddDefault(loadedResources, key, out _);
        loadedResource = newLoadedResource;
    }

    void UpdateVersion1Directory(uint indexMinorVersion, ReadOnlyMemory<byte>? directory)
    {
        var recordSize = indexMinorVersion >= 2 ? 20 : 16;
        var compressedKeys = new List<ResourceKey>();
        var sizesDecompressed = new Dictionary<ResourceKey, uint>();
        foreach (var key in keysInIndexOrder)
        {
            if (key == version1DirectoryKey)
                continue;
            if (unloadedResources.TryGetValue(key, out var indexEntry))
            {
                if (indexEntry.mnCompressionType is CompressionTypeMethodNumber.Internal_compression)
                {
                    compressedKeys.Add(key);
                    sizesDecompressed.Add(key, indexEntry.mnSizeDecompressed);
                }
            }
            else if (loadedResources.TryGetValue(key, out var loadedResource) && loadedResource.CompressionType is CompressionTypeMethodNumber.Internal_compression)
            {
                compressedKeys.Add(key);
                sizesDecompressed.Add(key, (uint)loadedResource.SizeDecompressed);
            }
        }
        if (sizesDecompressed.Count is 0)
        {
            if (directory is not null)
            {
                unloadedResources.Remove(version1DirectoryKey);
                loadedResources.Remove(version1DirectoryKey);
                keysInIndexOrder.Remove(version1DirectoryKey);
                if (AreNamesIndexed())
                    RemoveIndexedResourceName(resourceKeysByName, resourceNameByKey, version1DirectoryKey);
            }
            return;
        }
        if (directory is { } existingDirectory && existingDirectory.Length == sizesDecompressed.Count * recordSize)
        {
            var existingDirectorySpan = existingDirectory.Span;
            var listedKeys = new HashSet<ResourceKey>();
            for (var readPosition = 0; readPosition < existingDirectorySpan.Length;)
            {
                var mType = existingDirectorySpan.ReadAndAdvancePosition<ResourceType>(ref readPosition);
                var mGroup = existingDirectorySpan.ReadAndAdvancePosition<uint>(ref readPosition);
                var mInstance = existingDirectorySpan.ReadAndAdvancePosition<uint>(ref readPosition);
                var mInstanceEx = recordSize is 20 ? existingDirectorySpan.ReadAndAdvancePosition<uint>(ref readPosition) : 0U;
                var mnSizeDecompressed = existingDirectorySpan.ReadAndAdvancePosition<uint>(ref readPosition);
                var key = new ResourceKey(mType, mGroup, ((ulong)mInstanceEx << 32) | mInstance);
                if (!sizesDecompressed.TryGetValue(key, out var sizeDecompressed) || sizeDecompressed != mnSizeDecompressed || !listedKeys.Add(key))
                    break;
            }
            if (listedKeys.Count == sizesDecompressed.Count)
                return;
        }
        var newDirectory = new ArrayBufferWriter<byte>(sizesDecompressed.Count * recordSize);
        foreach (var key in compressedKeys)
        {
            var mType = key.Type;
            newDirectory.Write(ref mType);
            var mGroup = key.Group;
            newDirectory.Write(ref mGroup);
            var mInstance = key.Instance;
            newDirectory.Write(ref mInstance);
            if (recordSize is 20)
            {
                var mInstanceEx = key.InstanceEx;
                newDirectory.Write(ref mInstanceEx);
            }
            var mnSizeDecompressed = sizesDecompressed[key];
            newDirectory.Write(ref mnSizeDecompressed);
        }
        Store(version1DirectoryKey, newDirectory.WrittenMemory, CompressionTypeMethodNumber.Uncompressed, newDirectory.WrittenCount);
    }

    static ReadOnlyMemory<byte> Version1Compress(ReadOnlyMemory<byte> memory, CompressionMode compressionMode, out CompressionTypeMethodNumber compressionMethod)
    {
        if (compressionMode is not (CompressionMode.Auto or CompressionMode.ForceInternal or CompressionMode.ForceOff))
            throw new NotSupportedException($"DBPF 1.x packages support only the {nameof(CompressionMode.Auto)}, {nameof(CompressionMode.ForceInternal)}, and {nameof(CompressionMode.ForceOff)} compression modes, not {compressionMode}");
        compressionMethod = CompressionTypeMethodNumber.Uncompressed;
        if (compressionMode is CompressionMode.ForceOff || memory.Length > 0xFFFFFF)
            return memory;
        var compressed = InternalCompress(memory, out var wasCompressed);
        if (!wasCompressed || compressed.Length + 4 >= memory.Length)
            return memory;
        var stored = new byte[compressed.Length + 4];
        BinaryPrimitives.WriteUInt32LittleEndian(stored, (uint)stored.Length);
        compressed.Span.CopyTo(stored.AsSpan(4));
        compressionMethod = CompressionTypeMethodNumber.Internal_compression;
        return stored;
    }

    void WriteHeader(Span<byte> header, bool hasIndex, int indexSize, uint? version1IndexMinorVersion)
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
        unused1.Span.CopyTo(header[20..24]);
        var mnCreationTime = (int)CreationTime.ToUnixTimeSeconds();
        MemoryMarshal.Write(header[24..28], ref mnCreationTime);
        var mnUpdatedTime = (int)UpdatedTime.ToUnixTimeSeconds();
        MemoryMarshal.Write(header[28..32], ref mnUpdatedTime);
        unused2.Span.CopyTo(header[32..36]);
        var mnIndexRecordEntryCount = (uint)unloadedResources.Count + (uint)loadedResources.Count;
        MemoryMarshal.Write(header[36..40], ref mnIndexRecordEntryCount);
        var mnIndexRecordSize = (uint)indexSize;
        MemoryMarshal.Write(header[44..48], ref mnIndexRecordSize);
        var mnIndexRecordPosition = hasIndex
            ? 96UL
                + (ulong)unloadedResources.Values.Sum(entry => (uint)(entry.mnSize & CompressionTypeMethodBitmask.mnSizeMask))
                + (ulong)loadedResources.Values.Sum(loadedResource => (uint)loadedResource.Memory.Length)
            : 0UL;
        if (version1IndexMinorVersion is { } nonNullVersion1IndexMinorVersion)
        {
            var mnIndexMajorVersion = 7U;
            MemoryMarshal.Write(header[32..36], ref mnIndexMajorVersion);
            if (mnIndexRecordPosition > uint.MaxValue)
                throw new InvalidOperationException("DBPF 1.x packages cannot exceed 4 GiB");
            var mnIndexRecordPositionLow = (uint)mnIndexRecordPosition;
            MemoryMarshal.Write(header[40..44], ref mnIndexRecordPositionLow);
            header[48..60].Clear();
            var mnIndexMinorVersion = nonNullVersion1IndexMinorVersion;
            MemoryMarshal.Write(header[60..64], ref mnIndexMinorVersion);
            header[64..72].Clear();
        }
        else
        {
            unused3.Span.CopyTo(header[48..60]);
            unused4.Span.CopyTo(header[60..64]);
            if (hasIndex)
                MemoryMarshal.Write(header[64..72], ref mnIndexRecordPosition);
        }
        unused5.Span.CopyTo(header[72..96]);
    }

    static void WriteIndexEntry(ArrayBufferWriter<byte> index, IndexFlags indexFlags, long position, ResourceKey key, CompressionTypeMethodBitmask mnSize, uint mnSizeDecompressed, CompressionTypeMethodNumber mnCompressionType, ushort mnCommitted)
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
        if (mnSize.HasFlag(CompressionTypeMethodBitmask.mbExtendedCompressionType))
        {
            index.Write(ref mnCompressionType);
            index.Write(ref mnCommitted);
        }
    }

    static void WriteIndexEntry(ArrayBufferWriter<byte> index, IndexFlags indexFlags, uint? version1IndexMinorVersion, long position, ResourceKey key, IndexEntry indexEntry)
    {
        if (version1IndexMinorVersion is { } nonNullVersion1IndexMinorVersion)
            WriteVersion1IndexEntry(index, nonNullVersion1IndexMinorVersion, position, key, (uint)(indexEntry.mnSize & CompressionTypeMethodBitmask.mnSizeMask));
        else
            WriteIndexEntry(index, indexFlags, position, key, indexEntry.mnSize, indexEntry.mnSizeDecompressed, indexEntry.mnCompressionType, indexEntry.mnCommitted);
    }

    static void WriteIndexEntry(ArrayBufferWriter<byte> index, IndexFlags indexFlags, uint? version1IndexMinorVersion, long position, ResourceKey key, LoadedResource loadedResource)
    {
        if (version1IndexMinorVersion is { } nonNullVersion1IndexMinorVersion)
            WriteVersion1IndexEntry(index, nonNullVersion1IndexMinorVersion, position, key, (uint)loadedResource.Memory.Length);
        else
            WriteIndexEntry
            (
                index,
                indexFlags,
                position,
                key,
                (CompressionTypeMethodBitmask)loadedResource.Memory.Length | CompressionTypeMethodBitmask.mbExtendedCompressionType,
                (uint)loadedResource.SizeDecompressed,
                loadedResource.CompressionType,
                defaultMnCommitted
            );
    }

    static void WriteVersion1IndexEntry(ArrayBufferWriter<byte> index, uint indexMinorVersion, long position, ResourceKey key, uint mnSize)
    {
        var type = key.Type;
        index.Write(ref type);
        var group = key.Group;
        index.Write(ref group);
        var instance = key.Instance;
        index.Write(ref instance);
        if (indexMinorVersion >= 2)
        {
            var instanceEx = key.InstanceEx;
            index.Write(ref instanceEx);
        }
        var mnPosition = (uint)position;
        index.Write(ref mnPosition);
        index.Write(ref mnSize);
    }
}
