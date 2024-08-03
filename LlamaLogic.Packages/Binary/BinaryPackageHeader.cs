namespace LlamaLogic.Packages.Binary;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct BinaryPackageHeader :
    IBinaryStruct
{
    const uint supportedIndexMinorVersion = 3;
    const uint supportedMajorVersion = 2;
    const uint supportedMinorVersion = 1;
    static readonly Memory<byte> supportedPreamble = "DBPF"u8.ToArray();

    public static void CreateForSerialization(Stream stream, out BinaryPackageHeader packageHeader) =>
        packageHeader = new()
        {
            Preamble = supportedPreamble.ToArray(),
            MajorVersion = supportedMajorVersion,
            MinorVersion = supportedMinorVersion,
            IndexPosition = 0,
            IndexMinorVersion = supportedIndexMinorVersion,
            LongIndexPosition = (ulong)stream.Position
        };

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Preamble;
    public uint MajorVersion;
    public uint MinorVersion;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
    public byte[] Unused1;
    public uint IndexCount;
    public uint IndexPosition;
    public uint IndexSize;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
    public byte[] Unused2;
    public uint IndexMinorVersion;
    public ulong LongIndexPosition;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
    public byte[] Reserved;

    public void Check()
    {
        if (!supportedPreamble.Span.SequenceEqual(Preamble))
            throw new NotSupportedException($"unsupported preamble {string.Join(" ", Preamble.Select(b => b.ToString("x2")))}");
        if (MajorVersion != supportedMajorVersion)
            throw new NotSupportedException($"unsupported major version {MajorVersion}");
        if (MinorVersion != supportedMinorVersion)
            throw new NotSupportedException($"unsupported minor version {MinorVersion}");
        if (LongIndexPosition is 0)
            LongIndexPosition = IndexPosition;
    }
}
