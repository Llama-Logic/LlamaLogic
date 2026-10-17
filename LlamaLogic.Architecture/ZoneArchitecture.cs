namespace LlamaLogic.Architecture;

/// <summary>
/// Provides access to the architecture of the lots of a zone in a save game: the <c>architecture_data</c> field of the <c>EA.Sims4.Persistence.ZoneObjectData</c> protobuf message, the content of a <see cref="ResourceType.ZoneObjectData"/> resource
/// </summary>
/// <remarks>
/// <para><c>architecture_data</c> is a repeated field parallel to the message's <c>objects</c> field, and <c>active_data_index</c> selects the entry in use.
/// Most zones have one entry, some have none, and a few have several.</para>
/// <para>These methods read and replace only that field's entries in the message's bytes and copy everything else verbatim, so they never re-serialize the rest of the message.</para>
/// </remarks>
public static class ZoneArchitecture
{
    const int architectureDataFieldNumber = 2;

    /// <summary>
    /// Gets the architecture of each entry of the <c>architecture_data</c> field of a <c>ZoneObjectData</c> message, or <see langword="null"/> for an empty entry
    /// </summary>
    /// <exception cref="InvalidDataException">The data is not a well-formed protobuf message, or an entry is not a well-formed lot architecture</exception>
    public static IReadOnlyList<LotArchitecture?> GetArchitecture(ReadOnlyMemory<byte> zoneObjectData) =>
        [.. GetArchitectureData(zoneObjectData).Select(data => data.IsEmpty ? null : LotArchitecture.Decode(data))];

    /// <summary>
    /// Gets the content of each entry of the <c>architecture_data</c> field of a <c>ZoneObjectData</c> message: a record bundle, or nothing
    /// </summary>
    /// <exception cref="InvalidDataException">The data is not a well-formed protobuf message</exception>
    public static IReadOnlyList<ReadOnlyMemory<byte>> GetArchitectureData(ReadOnlyMemory<byte> zoneObjectData) =>
        [.. ScanFields(zoneObjectData.Span).Where(field => field.Number == architectureDataFieldNumber).Select(field => zoneObjectData.Slice(field.ValueStart, field.ValueLength))];

    static ulong ReadVarint(ReadOnlySpan<byte> data, ref int position)
    {
        var result = 0UL;
        for (var shift = 0; shift < 64; shift += 7)
        {
            if (position >= data.Length)
                throw new InvalidDataException($"The message ends inside a varint at offset {position}");
            var value = data[position++];
            if (shift == 63 && value > 1)
                throw new InvalidDataException($"A varint ending at offset {position} is longer than 64 bits");
            result |= (ulong)(value & 0x7f) << shift;
            if ((value & 0x80) == 0)
                return result;
        }
        throw new InvalidDataException($"A varint ending at offset {position} is longer than 64 bits");
    }

    static List<(int Number, int Start, int ValueStart, int ValueLength, int End)> ScanFields(ReadOnlySpan<byte> data)
    {
        var fields = new List<(int Number, int Start, int ValueStart, int ValueLength, int End)>();
        var position = 0;
        while (position < data.Length)
        {
            var start = position;
            var tag = ReadVarint(data, ref position);
            var number = tag >> 3;
            if (number is 0 or > int.MaxValue)
                throw new InvalidDataException($"The field at offset {start} has number {number}");
            int valueStart, valueLength;
            switch (tag & 7)
            {
                case 0:
                    valueStart = position;
                    ReadVarint(data, ref position);
                    valueLength = position - valueStart;
                    break;
                case 1:
                    valueStart = position;
                    valueLength = 8;
                    break;
                case 2:
                    var length = ReadVarint(data, ref position);
                    if (length > (ulong)(data.Length - position))
                        throw new InvalidDataException($"The field at offset {start} is {length} bytes long, but only {data.Length - position} remain");
                    valueStart = position;
                    valueLength = (int)length;
                    break;
                case 5:
                    valueStart = position;
                    valueLength = 4;
                    break;
                default:
                    throw new InvalidDataException($"The field at offset {start} has wire type {tag & 7}, which is not supported");
            }
            if (valueLength > data.Length - valueStart)
                throw new InvalidDataException($"The field at offset {start} extends beyond the message");
            position = valueStart + valueLength;
            if (number == architectureDataFieldNumber && (tag & 7) != 2)
                throw new InvalidDataException($"The architecture_data field at offset {start} has wire type {tag & 7}, expected 2");
            fields.Add(((int)number, start, valueStart, valueLength, position));
        }
        return fields;
    }

    /// <summary>
    /// Gets a <c>ZoneObjectData</c> message with each entry of its <c>architecture_data</c> field replaced by the encoding of the corresponding <paramref name="architecture"/>, or emptied where it is <see langword="null"/>
    /// </summary>
    /// <exception cref="InvalidDataException">The data is not a well-formed protobuf message</exception>
    /// <exception cref="ArgumentException">The number of entries of <paramref name="architecture"/> differs from that of the message's field</exception>
    /// <exception cref="InvalidOperationException">An architecture cannot be encoded as it is</exception>
    public static ReadOnlyMemory<byte> SetArchitecture(ReadOnlyMemory<byte> zoneObjectData, IReadOnlyList<LotArchitecture?> architecture)
    {
        ArgumentNullException.ThrowIfNull(architecture);
        return SetArchitectureData(zoneObjectData, [.. architecture.Select(entry => entry?.Encode() ?? ReadOnlyMemory<byte>.Empty)]);
    }

    /// <summary>
    /// Gets a <c>ZoneObjectData</c> message with the content of each entry of its <c>architecture_data</c> field replaced by the corresponding <paramref name="architectureData"/>
    /// </summary>
    /// <exception cref="InvalidDataException">The data is not a well-formed protobuf message</exception>
    /// <exception cref="ArgumentException">The number of entries of <paramref name="architectureData"/> differs from that of the message's field</exception>
    public static ReadOnlyMemory<byte> SetArchitectureData(ReadOnlyMemory<byte> zoneObjectData, IReadOnlyList<ReadOnlyMemory<byte>> architectureData)
    {
        ArgumentNullException.ThrowIfNull(architectureData);
        var data = zoneObjectData.Span;
        var fields = ScanFields(data);
        var entryCount = fields.Count(field => field.Number == architectureDataFieldNumber);
        if (entryCount != architectureData.Count)
            throw new ArgumentException($"The message has {entryCount} architecture entries, but {architectureData.Count} were given", nameof(architectureData));
        var writer = new ArrayBufferWriter<byte>(data.Length);
        var entry = 0;
        foreach (var field in fields)
        {
            if (field.Number != architectureDataFieldNumber)
            {
                writer.Write(data[field.Start..field.End]);
                continue;
            }
            var replacement = architectureData[entry++].Span;
            WriteVarint(writer, (ulong)architectureDataFieldNumber << 3 | 2);
            WriteVarint(writer, (ulong)replacement.Length);
            writer.Write(replacement);
        }
        return writer.WrittenMemory;
    }

    static void WriteVarint(ArrayBufferWriter<byte> writer, ulong value)
    {
        while (value >= 0x80)
        {
            writer.GetSpan(1)[0] = (byte)(value | 0x80);
            writer.Advance(1);
            value >>= 7;
        }
        writer.GetSpan(1)[0] = (byte)value;
        writer.Advance(1);
    }
}
