namespace LlamaLogic.Packages.Binary;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct BinaryFloatingPoint2 :
    IBinaryStruct
{
    public static DataResourceFloatingPoint2Value Read(in ReadOnlySpan<byte> data, ref uint position)
    {
        data.ReadBinaryStructAndAdvancePosition<BinaryFloatingPoint2>(out var binaryFloatingPoint2, ref position);
        return new DataResourceFloatingPoint2Value(binaryFloatingPoint2.X, binaryFloatingPoint2.Y);
    }

    public static void Write(DataResourceFloatingPoint2Value? floatingPoint2, ArrayBufferWriter<byte> writer)
    {
        var binaryFloatingPoint2 = new BinaryFloatingPoint2
        {
            X = floatingPoint2?.X ?? 0,
            Y = floatingPoint2?.Y ?? 0
        };
        writer.WriteBinaryStruct(in binaryFloatingPoint2);
    }

    public float X;
    public float Y;
}