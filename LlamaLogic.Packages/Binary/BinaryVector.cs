using System;
using System.Collections.Generic;
using System.Text;

namespace LlamaLogic.Packages.Binary;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct BinaryVector :
    IBinaryStruct
{
    public static DataResourceVectorValue Read(in ReadOnlySpan<byte> data, ref uint position)
    {
        data.ReadBinaryStructAndAdvancePosition<BinaryVector>(out var binaryVector, ref position);
        return new DataResourceVectorValue(binaryVector.Offset, binaryVector.Count);
    }

    public static void Write(DataResourceVectorValue? vector, ArrayBufferWriter<byte> writer)
    {
        var binaryVector = new BinaryVector
        {
            Offset = vector?.Offset ?? BinaryUtilities.NullOffset,
            Count = vector?.Count ?? 0
        };
        writer.WriteBinaryStruct(in binaryVector);
    }

    public uint Offset;
    public uint Count;
}
