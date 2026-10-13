namespace LlamaLogic.Packages.Models.SimData;

static class SimDataUtilities
{
    public const uint EmptyNameHash = 0x811c9dc5;

    public static uint GetAlignment(this SimDataType type) =>
        type switch
        {
            SimDataType.BOOL or SimDataType.CHAR8 or SimDataType.INT8 or SimDataType.UINT8 => 1,
            SimDataType.INT16 or SimDataType.UINT16 => 2,
            SimDataType.INT64 or SimDataType.UINT64 or SimDataType.TABLESETREFERENCE or SimDataType.RESOURCEKEY => 8,
            _ => 4
        };

    [SuppressMessage("Globalization", "CA1308: Normalize strings to uppercase", Justification = "Maxis hashes lowercased names.")]
    public static uint GetNameHash(string? name)
    {
        if (name is null)
            return EmptyNameHash;
        var hash = EmptyNameHash;
        foreach (var b in Encoding.UTF8.GetBytes(name.ToLowerInvariant()))
        {
            hash *= 0x01000193;
            hash ^= b;
        }
        return hash;
    }

    public static uint GetSize(this SimDataType type) =>
        type switch
        {
            SimDataType.BOOL or SimDataType.CHAR8 or SimDataType.INT8 or SimDataType.UINT8 => 1,
            SimDataType.INT16 or SimDataType.UINT16 => 2,
            SimDataType.INT32 or SimDataType.UINT32 or SimDataType.FLOAT or SimDataType.STRING8 or SimDataType.OBJECT or SimDataType.LOCKEY => 4,
            SimDataType.INT64 or SimDataType.UINT64 or SimDataType.HASHEDSTRING8 or SimDataType.VECTOR or SimDataType.FLOAT2 or SimDataType.TABLESETREFERENCE or SimDataType.VARIANT => 8,
            SimDataType.FLOAT3 => 12,
            SimDataType.FLOAT4 or SimDataType.RESOURCEKEY => 16,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Not a SimData type")
        };

    public static bool IsDefined(this SimDataType type) =>
        type is >= SimDataType.BOOL and <= SimDataType.VARIANT;

    public static object? GetDefaultValue(this SimDataType type) =>
        type switch
        {
            SimDataType.BOOL => false,
            SimDataType.CHAR8 or SimDataType.UINT8 => (byte)0,
            SimDataType.INT8 => (sbyte)0,
            SimDataType.INT16 => (short)0,
            SimDataType.UINT16 => (ushort)0,
            SimDataType.INT32 => 0,
            SimDataType.UINT32 or SimDataType.LOCKEY => 0U,
            SimDataType.INT64 => 0L,
            SimDataType.UINT64 or SimDataType.TABLESETREFERENCE => 0UL,
            SimDataType.FLOAT => 0F,
            SimDataType.FLOAT2 => Vector2.Zero,
            SimDataType.FLOAT3 => Vector3.Zero,
            SimDataType.FLOAT4 => Vector4.Zero,
            SimDataType.RESOURCEKEY => default(ResourceKey),
            _ => null
        };

    public static object? RequireValue(this SimDataType type, object? value, string paramName) =>
        (type, value) switch
        {
            (SimDataType.BOOL, bool) or
            (SimDataType.CHAR8 or SimDataType.UINT8, byte) or
            (SimDataType.INT8, sbyte) or
            (SimDataType.INT16, short) or
            (SimDataType.UINT16, ushort) or
            (SimDataType.INT32, int) or
            (SimDataType.UINT32 or SimDataType.LOCKEY, uint) or
            (SimDataType.INT64, long) or
            (SimDataType.UINT64 or SimDataType.TABLESETREFERENCE, ulong) or
            (SimDataType.FLOAT, float) or
            (SimDataType.FLOAT2, Vector2) or
            (SimDataType.FLOAT3, Vector3) or
            (SimDataType.FLOAT4, Vector4) or
            (SimDataType.RESOURCEKEY, ResourceKey) or
            (SimDataType.STRING8 or SimDataType.HASHEDSTRING8, null or string) or
            (SimDataType.OBJECT, null or SimDataObject) or
            (SimDataType.VECTOR, null or SimDataVector) or
            (SimDataType.VARIANT, null or SimDataVariant) => value,
            _ => throw new ArgumentException($"A {type} value must be {GetValueTypeDescription(type)}, not {value?.GetType().Name ?? "null"}", paramName)
        };

    static string GetValueTypeDescription(SimDataType type) =>
        type switch
        {
            SimDataType.BOOL => nameof(Boolean),
            SimDataType.CHAR8 or SimDataType.UINT8 => nameof(Byte),
            SimDataType.INT8 => nameof(SByte),
            SimDataType.INT16 => nameof(Int16),
            SimDataType.UINT16 => nameof(UInt16),
            SimDataType.INT32 => nameof(Int32),
            SimDataType.UINT32 or SimDataType.LOCKEY => nameof(UInt32),
            SimDataType.INT64 => nameof(Int64),
            SimDataType.UINT64 or SimDataType.TABLESETREFERENCE => nameof(UInt64),
            SimDataType.FLOAT => nameof(Single),
            SimDataType.FLOAT2 => nameof(Vector2),
            SimDataType.FLOAT3 => nameof(Vector3),
            SimDataType.FLOAT4 => nameof(Vector4),
            SimDataType.RESOURCEKEY => nameof(ResourceKey),
            SimDataType.STRING8 or SimDataType.HASHEDSTRING8 => $"{nameof(String)} or null",
            SimDataType.OBJECT => $"{nameof(SimDataObject)} or null",
            SimDataType.VECTOR => $"{nameof(SimDataVector)} or null",
            SimDataType.VARIANT => $"{nameof(SimDataVariant)} or null",
            _ => "nothing (the type is not defined)"
        };
}
