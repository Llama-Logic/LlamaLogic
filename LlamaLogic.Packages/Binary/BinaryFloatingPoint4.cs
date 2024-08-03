namespace LlamaLogic.Packages.Binary;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct BinaryFloatingPoint4 :
    IBinaryStruct
{
    public static DataResourceFloatingPoint4Value Read(in ReadOnlySpan<byte> data, ref uint position)
    {
        data.ReadBinaryStructAndAdvancePosition<BinaryFloatingPoint4>(out var binaryFloatingPoint4, ref position);
        return new DataResourceFloatingPoint4Value(binaryFloatingPoint4.X, binaryFloatingPoint4.Y, binaryFloatingPoint4.Z, binaryFloatingPoint4.W);
    }

    public static void Write(DataResourceFloatingPoint4Value? floatingPoint4, ArrayBufferWriter<byte> writer)
    {
        var binaryFloatingPoint4 = new BinaryFloatingPoint4
        {
            X = floatingPoint4?.X ?? 0,
            Y = floatingPoint4?.Y ?? 0,
            Z = floatingPoint4?.Z ?? 0,
            W = floatingPoint4?.W ?? 0
        };
        writer.WriteBinaryStruct(in binaryFloatingPoint4);
    }

    public float X;
    public float Y;
    public float Z;
    public float W;
}
