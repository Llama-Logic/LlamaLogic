namespace LlamaLogic.Packages;

/// <summary>
/// Represents a package
/// </summary>
public class Package :
    IDisposable
{
    static readonly byte[] preamble = "DBPF"u8.ToArray();

    static (int indexCount, int indexSize, int indexPosition) ParseHeader(Span<byte> header)
    {
        if (header.Length != 96)
            throw new InvalidOperationException("header length is not 96");
        if (!header[..4].SequenceEqual(preamble))
            throw new InvalidOperationException($"header preamble is not {Encoding.UTF8.GetString(preamble)}");
        if (MemoryMarshal.Read<int>(header[4..8]) != 2)
            throw new InvalidOperationException("major version is not 2");
        if (MemoryMarshal.Read<int>(header[8..12]) != 1)
            throw new InvalidOperationException("minor version is not 1");
        var indexCount = MemoryMarshal.Read<int>(header[36..40]);
        var indexSize = MemoryMarshal.Read<int>(header[44..48]);
        var indexPosition = MemoryMarshal.Read<int>(header[64..68]);
        return (indexCount, indexSize, indexPosition);
    }

    /// <summary>
    /// Instantiates a new package from a specified <paramref name="stream"/>
    /// </summary>
    /// <exception cref="ArgumentNullException">The specified <paramref name="stream"/> is <c>null</c></exception>
    /// <exception cref="ArgumentException">The specified <paramref name="stream"/> cannot seek or cannot be read from</exception>
    public Package(Stream stream)
    {
#if IS_NET_6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(stream);
#else
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));
#endif
        if (!stream.CanSeek)
            throw new ArgumentException("The provided stream must be seekable");
        if (!stream.CanRead)
            throw new ArgumentException("The provided stream must be readable");
        this.stream = stream;
    }

    /// <summary>
    /// Finalizes the package
    /// </summary>
    ~Package() =>
        Dispose(false);

    bool isHeaderRead;
    int indexCount;
    int indexPosition;
    int indexSize;
    readonly Stream stream;

    /// <summary>
    /// Gets whether the package cannot be altered
    /// </summary>
    public bool IsReadOnly =>
        !stream.CanWrite;

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
            stream.Dispose();
    }

    void ReadHeader()
    {
        if (!isHeaderRead)
        {
            Memory<byte> header = new byte[96];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(header.Span);
            (indexCount, indexSize, indexPosition) = ParseHeader(header.Span);
            isHeaderRead = true;
        }
    }

    async ValueTask ReadHeaderAsync()
    {
        if (!isHeaderRead)
        {
            Memory<byte> header = new byte[96];
            stream.Seek(0, SeekOrigin.Begin);
            await stream.ReadAsync(header).ConfigureAwait(false);
            (indexCount, indexSize, indexPosition) = ParseHeader(header.Span);
            isHeaderRead = true;
        }
    }

    void RequireCanWrite()
    {
        if (!stream.CanWrite)
            throw new InvalidOperationException("The package was opened with a read-only stream");
    }

    void WriteHeader()
    {
        RequireCanWrite();
        Span<byte> int32Bytes = stackalloc byte[sizeof(int)];
#pragma warning disable CS9191 // using the "in" keyword here is no legal in early versions of .NET
        stream.Seek(36, SeekOrigin.Begin);
        MemoryMarshal.Write(int32Bytes, ref indexCount);
        stream.Write(int32Bytes);
        var zero = 0;
        stream.Seek(40, SeekOrigin.Begin);
        MemoryMarshal.Write(int32Bytes, ref zero);
        stream.Write(int32Bytes);
        stream.Seek(44, SeekOrigin.Begin);
        MemoryMarshal.Write(int32Bytes, ref indexSize);
        stream.Write(int32Bytes);
        var indexVersion = 3;
        stream.Seek(60, SeekOrigin.Begin);
        MemoryMarshal.Write(int32Bytes, ref indexVersion);
        stream.Write(int32Bytes);
        stream.Seek(64, SeekOrigin.Begin);
        MemoryMarshal.Write(int32Bytes, ref indexSize);
        stream.Write(int32Bytes);
        var unused4 = 3;
        stream.Seek(74, SeekOrigin.Begin);
        MemoryMarshal.Write(int32Bytes, ref unused4);
        stream.Write(int32Bytes);
#pragma warning restore CS9191 // using the "in" keyword here is no legal in early versions of .NET
    }

    async ValueTask WriteHeaderAsync()
    {
        RequireCanWrite();
        Memory<byte> int32Bytes = new byte[sizeof(int)];
#pragma warning disable CS9191 // using the "in" keyword here is no legal in early versions of .NET
        stream.Seek(36, SeekOrigin.Begin);
        MemoryMarshal.Write(int32Bytes.Span, ref indexCount);
        await stream.WriteAsync(int32Bytes).ConfigureAwait(false);
        var zero = 0;
        stream.Seek(40, SeekOrigin.Begin);
        MemoryMarshal.Write(int32Bytes.Span, ref zero);
        await stream.WriteAsync(int32Bytes).ConfigureAwait(false);
        stream.Seek(44, SeekOrigin.Begin);
        MemoryMarshal.Write(int32Bytes.Span, ref indexSize);
        await stream.WriteAsync(int32Bytes).ConfigureAwait(false);
        var indexVersion = 3;
        stream.Seek(60, SeekOrigin.Begin);
        MemoryMarshal.Write(int32Bytes.Span, ref indexVersion);
        await stream.WriteAsync(int32Bytes).ConfigureAwait(false);
        stream.Seek(64, SeekOrigin.Begin);
        MemoryMarshal.Write(int32Bytes.Span, ref indexSize);
        await stream.WriteAsync(int32Bytes).ConfigureAwait(false);
        var unused4 = 3;
        stream.Seek(74, SeekOrigin.Begin);
        MemoryMarshal.Write(int32Bytes.Span, ref unused4);
        await stream.WriteAsync(int32Bytes).ConfigureAwait(false);
#pragma warning restore CS9191 // using the "in" keyword here is no legal in early versions of .NET
    }
}
