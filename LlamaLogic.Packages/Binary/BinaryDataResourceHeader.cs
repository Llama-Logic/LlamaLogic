namespace LlamaLogic.Packages.Binary;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct BinaryDataResourceHeader :
    IBinaryStruct
{
    const uint maximumSupportedVersion = 0x101;
    const uint minimumSupportedVersion = 0x100;
    static readonly Memory<byte> supportedPreamble = "DATA"u8.ToArray();

    public static void CreateForSerialization(ArrayBufferWriter<byte> writer, out BinaryDataResourceHeader dataResourceHeader, out Span<byte> dataResourceHeaderSpan, out Range dataResourceHeaderSpanRange)
    {
        dataResourceHeader = new()
        {
            Preamble = supportedPreamble.ToArray(),
            Version = maximumSupportedVersion,
            TablesOffset = BinaryUtilities.NullOffset,
            SchemasOffset = BinaryUtilities.NullOffset
        };
        writer.ReserveSpanForBinaryStruct<BinaryDataResourceHeader>(out dataResourceHeaderSpan, out dataResourceHeaderSpanRange);
    }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Preamble;
    public uint Version;
    public uint TablesOffset;
    public uint TableCount;
    public uint SchemasOffset;
    public uint SchemaCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public byte[] Reserved;

    public readonly void Read(in uint positionAfterRead, out uint tablesPosition, out uint schemasPosition)
    {
        if (!supportedPreamble.Span.SequenceEqual(Preamble))
            throw new NotSupportedException($"unsupported preamble {string.Join(" ", Preamble.Select(b => b.ToString("x2")))}");
        if (Version is < minimumSupportedVersion or > maximumSupportedVersion)
            throw new NotSupportedException($"unsupported version {Version}");
        if (TablesOffset is BinaryUtilities.NullOffset)
            throw new InvalidOperationException("invalid tables offset");
        if (SchemasOffset is BinaryUtilities.NullOffset)
            throw new InvalidOperationException("invalid schemas offset");
        tablesPosition = positionAfterRead - (sizeof(uint) * 4 + 8) + TablesOffset;
        schemasPosition = positionAfterRead - (sizeof(uint) * 2 + 8) + SchemasOffset;
    }

    public void SetSchemasOffset(ArrayBufferWriter<byte> writer, in Range structRange) =>
        SchemasOffset = writer.GetOffsetValueFromCurrentPosition(in structRange, supportedPreamble.Length + sizeof(uint) * 3);

    public void SetTablesOffset(ArrayBufferWriter<byte> writer, in Range structRange) =>
        TablesOffset = writer.GetOffsetValueFromCurrentPosition(in structRange, supportedPreamble.Length + sizeof(uint));
}
