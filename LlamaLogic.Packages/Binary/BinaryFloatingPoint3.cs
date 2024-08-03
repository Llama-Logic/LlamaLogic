namespace LlamaLogic.Packages.Binary;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct BinaryFloatingPoint3 :
    IBinaryStruct
{
    public static DataResourceFloatingPoint3Value Read(in ReadOnlySpan<byte> data, ref uint position)
    {
        data.ReadBinaryStructAndAdvancePosition<BinaryFloatingPoint3>(out var binaryFloatingPoint3, ref position);
        return new DataResourceFloatingPoint3Value(binaryFloatingPoint3.X, binaryFloatingPoint3.Y, binaryFloatingPoint3.Z);
    }

    public static void Write(DataResourceFloatingPoint3Value? floatingPoint3, ArrayBufferWriter<byte> writer)
    {
        var binaryFloatingPoint3 = new BinaryFloatingPoint3
        {
            X = floatingPoint3?.X ?? 0,
            Y = floatingPoint3?.Y ?? 0,
            Z = floatingPoint3?.Z ?? 0
        };
        writer.WriteBinaryStruct(in binaryFloatingPoint3);
    }

    public float X;
    public float Y;
    public float Z;
}