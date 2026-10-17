namespace LlamaLogic.Packages.Formats;

/// <summary>
/// Represents a record bundle: a set of records, each a <see cref="ResourceKey"/> and its content, in the <c>LDNB</c> container format (🔓)
/// </summary>
/// <remarks>
/// The game stores lot architecture in this container, as the content of a <see cref="ResourceType.WorldLotArchitecture"/> resource in world files, of the architecture data of a lot in a save game, and of the architecture of a tray blueprint.
/// Each lot architecture section is one record, keyed by the section's type, group zero and a fixed instance.
/// <see cref="Encode"/> lays the bundle out the way Maxis does: records grouped by type and group in ascending order, keeping their order within a group, so decoding and encoding a bundle Maxis wrote reproduces it byte for byte.
/// </remarks>
public sealed class RecordBundle
{
    const int headerSize = 40;
    const uint littleEndianFileIdentifier = 0x424e444c;
    const uint bigEndianFileIdentifier = 0x4c444e42;
    const uint supportedVersion = 3;
    const int maximumDataLength = 0xffffff;

    /// <summary>
    /// Decodes a record bundle
    /// </summary>
    /// <exception cref="InvalidDataException">The data is not a well-formed little-endian version 3 record bundle, or two of its records have the same key</exception>
    public static RecordBundle Decode(ReadOnlyMemory<byte> data)
    {
        var span = data.Span;
        if (span.Length < headerSize)
            throw new InvalidDataException($"{span.Length} bytes are too few for a record bundle");
        var fileIdentifier = BinaryPrimitives.ReadUInt32LittleEndian(span);
        if (fileIdentifier == bigEndianFileIdentifier)
            throw new InvalidDataException("Big-endian record bundles are not supported");
        if (fileIdentifier != littleEndianFileIdentifier)
            throw new InvalidDataException($"File identifier 0x{fileIdentifier:x8} is invalid");
        var version = BinaryPrimitives.ReadUInt32LittleEndian(span[4..]);
        if (version != supportedVersion)
            throw new InvalidDataException($"Version {version} is not supported, expected {supportedVersion}");
        var reserved1 = BinaryPrimitives.ReadUInt32LittleEndian(span[8..]);
        var reserved2 = BinaryPrimitives.ReadUInt32LittleEndian(span[12..]);
        var recordCount = BinaryPrimitives.ReadInt32LittleEndian(span[16..]);
        var instancesOffset = BinaryPrimitives.ReadUInt32LittleEndian(span[20..]);
        var dataOffsetsOffset = BinaryPrimitives.ReadUInt32LittleEndian(span[24..]);
        var dataOffset = BinaryPrimitives.ReadUInt32LittleEndian(span[28..]);
        var groupCount = BinaryPrimitives.ReadInt32LittleEndian(span[32..]);
        var groupsOffset = BinaryPrimitives.ReadUInt32LittleEndian(span[36..]);
        var length = (long)span.Length;
        if (recordCount < 0 || groupCount < 0)
            throw new InvalidDataException("The record or group count is negative");
        if ((instancesOffset & 7) != 0 || instancesOffset + 8L * recordCount > length)
            throw new InvalidDataException("The instances do not fit in the data or are not 8-byte aligned");
        if ((groupsOffset & 3) != 0 || groupsOffset + 12L * (groupCount + 1L) > length)
            throw new InvalidDataException("The groups do not fit in the data or are not 4-byte aligned");
        if (dataOffsetsOffset + 3L * (recordCount + 1L) > length)
            throw new InvalidDataException("The data offsets do not fit in the data");
        if (dataOffset > length)
            throw new InvalidDataException("The data does not fit in the bundle");
        var end = Math.Max(Math.Max(instancesOffset + 8L * recordCount, groupsOffset + 12L * (groupCount + 1L)), dataOffsetsOffset + 3L * (recordCount + 1L));
        if (end != length)
            throw new InvalidDataException($"{length - end} bytes follow the last table");
        var dataOffsets = new long[recordCount + 1];
        for (var i = 0; i <= recordCount; ++i)
        {
            var o = (int)(dataOffsetsOffset + 3L * i);
            dataOffsets[i] = dataOffset + ((long)span[o] << 16 | (long)span[o + 1] << 8 | span[o + 2]);
            if (dataOffsets[i] > length || i > 0 && dataOffsets[i] < dataOffsets[i - 1])
                throw new InvalidDataException($"The data offset of record {i} is out of order or beyond the data");
        }
        var bundle = new RecordBundle
        {
            Reserved1 = reserved1,
            Reserved2 = reserved2
        };
        var keys = new HashSet<ResourceKey>();
        var expectedStart = 0U;
        for (var g = 0; g < groupCount; ++g)
        {
            var groupSpan = span[(int)(groupsOffset + 12L * g)..];
            var type = (ResourceType)BinaryPrimitives.ReadUInt32LittleEndian(groupSpan);
            var group = BinaryPrimitives.ReadUInt32LittleEndian(groupSpan[4..]);
            var start = BinaryPrimitives.ReadUInt32LittleEndian(groupSpan[8..]);
            var next = BinaryPrimitives.ReadUInt32LittleEndian(groupSpan[20..]);
            if (start != expectedStart || next < start || next > recordCount)
                throw new InvalidDataException($"Group {g} of {groupCount} has an invalid range of records");
            expectedStart = next;
            for (var i = start; i < next; ++i)
            {
                var instance = BinaryPrimitives.ReadUInt64LittleEndian(span[(int)(instancesOffset + 8L * i)..]);
                var key = new ResourceKey(type, group, instance);
                if (!keys.Add(key))
                    throw new InvalidDataException($"Record {i} has the same key as an earlier record, {key}");
                bundle.records.Add(new RecordBundleRecord(key, data[(int)dataOffsets[i]..(int)dataOffsets[i + 1]]));
            }
        }
        if (BinaryPrimitives.ReadUInt32LittleEndian(span[(int)(groupsOffset + 12L * groupCount + 8)..]) != recordCount || bundle.records.Count != recordCount)
            throw new InvalidDataException("The groups do not cover every record exactly once");
        return bundle;
    }

    readonly List<RecordBundleRecord> records = [];

    /// <summary>
    /// Gets the number of records in the bundle
    /// </summary>
    public int Count =>
        records.Count;

    /// <summary>
    /// Gets the records in the bundle, in the order <see cref="Encode"/> would write them
    /// </summary>
    public IReadOnlyList<RecordBundleRecord> Records =>
        [..records.OrderBy(record => (uint)record.Key.Type).ThenBy(record => record.Key.Group)];

    /// <summary>
    /// Gets/sets the first reserved header value, which the game does not read (zero in every bundle observed)
    /// </summary>
    public uint Reserved1 { get; set; }

    /// <summary>
    /// Gets/sets the second reserved header value, which the game does not read (zero in every bundle observed)
    /// </summary>
    public uint Reserved2 { get; set; }

    /// <summary>
    /// Encodes the record bundle
    /// </summary>
    /// <exception cref="InvalidOperationException">The content of the records totals more than 16,777,215 bytes, the most the format can address</exception>
    public ReadOnlyMemory<byte> Encode()
    {
        var ordered = Records;
        var dataLength = 0L;
        foreach (var record in ordered)
            dataLength += record.Data.Length;
        if (dataLength > maximumDataLength)
            throw new InvalidOperationException($"The records total {dataLength} bytes, more than the {maximumDataLength} a record bundle can address");
        var groups = new List<(ResourceKey key, int start)>();
        for (var i = 0; i < ordered.Count; ++i)
        {
            var key = ordered[i].Key;
            if (groups.Count == 0 || groups[^1].key.Type != key.Type || groups[^1].key.Group != key.Group)
                groups.Add((key, i));
        }
        var instancesOffset = (int)(headerSize + dataLength + (-(headerSize + dataLength) & 7));
        var groupsOffset = instancesOffset + 8 * ordered.Count;
        var dataOffsetsOffset = groupsOffset + 12 * (groups.Count + 1);
        var result = new byte[dataOffsetsOffset + 3 * (ordered.Count + 1)];
        var span = result.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(span, littleEndianFileIdentifier);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], supportedVersion);
        BinaryPrimitives.WriteUInt32LittleEndian(span[8..], Reserved1);
        BinaryPrimitives.WriteUInt32LittleEndian(span[12..], Reserved2);
        BinaryPrimitives.WriteInt32LittleEndian(span[16..], ordered.Count);
        BinaryPrimitives.WriteInt32LittleEndian(span[20..], instancesOffset);
        BinaryPrimitives.WriteInt32LittleEndian(span[24..], dataOffsetsOffset);
        BinaryPrimitives.WriteInt32LittleEndian(span[28..], headerSize);
        BinaryPrimitives.WriteInt32LittleEndian(span[32..], groups.Count);
        BinaryPrimitives.WriteInt32LittleEndian(span[36..], groupsOffset);
        var position = headerSize;
        for (var i = 0; i <= ordered.Count; ++i)
        {
            var relative = position - headerSize;
            var o = dataOffsetsOffset + 3 * i;
            span[o] = (byte)(relative >> 16);
            span[o + 1] = (byte)(relative >> 8);
            span[o + 2] = (byte)relative;
            if (i == ordered.Count)
                break;
            ordered[i].Data.Span.CopyTo(span[position..]);
            position += ordered[i].Data.Length;
            BinaryPrimitives.WriteUInt64LittleEndian(span[(instancesOffset + 8 * i)..], ordered[i].Key.FullInstance);
        }
        for (var g = 0; g <= groups.Count; ++g)
        {
            var groupSpan = span[(groupsOffset + 12 * g)..];
            if (g < groups.Count)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(groupSpan, (uint)groups[g].key.Type);
                BinaryPrimitives.WriteUInt32LittleEndian(groupSpan[4..], groups[g].key.Group);
            }
            BinaryPrimitives.WriteInt32LittleEndian(groupSpan[8..], g < groups.Count ? groups[g].start : ordered.Count);
        }
        return result;
    }

    /// <summary>
    /// Gets the content of the record with the specified <paramref name="key"/>
    /// </summary>
    /// <exception cref="KeyNotFoundException">The bundle has no record with the specified <paramref name="key"/></exception>
    public ReadOnlyMemory<byte> Get(ResourceKey key) =>
        TryGet(key, out var data)
            ? data
            : throw new KeyNotFoundException($"The bundle has no record {key}");

    /// <summary>
    /// Removes the record with the specified <paramref name="key"/>, returning whether there was one
    /// </summary>
    public bool Remove(ResourceKey key) =>
        records.RemoveAll(record => record.Key == key) > 0;

    /// <summary>
    /// Sets the content of the record with the specified <paramref name="key"/>, adding the record if the bundle has none with that key
    /// </summary>
    public void Set(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        var index = records.FindIndex(record => record.Key == key);
        if (index >= 0)
            records[index] = new RecordBundleRecord(key, data);
        else
            records.Add(new RecordBundleRecord(key, data));
    }

    /// <summary>
    /// Gets the content of the record with the specified <paramref name="key"/>, returning whether there was one
    /// </summary>
    public bool TryGet(ResourceKey key, out ReadOnlyMemory<byte> data)
    {
        foreach (var record in records)
            if (record.Key == key)
            {
                data = record.Data;
                return true;
            }
        data = default;
        return false;
    }
}
