namespace LlamaLogic.Packages.Models.SimData;

/// <summary>
/// Represents a <see cref="ResourceType.SimData"/> resource (🔓)
/// </summary>
/// <remarks>
/// A SimData resource is a set of named <see cref="Instances"/>, each an object described by a <see cref="SimDataSchema"/> whose values may refer to further objects, strings, vectors and variants.
/// <see cref="Decode(ReadOnlyMemory{byte})"/> reads any well-formed layout, including those written by community tools, and reads a reference whose offset is zero (which could only refer to itself, and which some community tools write) as <see langword="null"/>.
/// It rejects a resource in which a row is referred to more than once, which Maxis never writes and which the model, being a tree, cannot represent.
/// <see cref="Encode"/> always lays the resource out the way Maxis does, so decoding and encoding a resource Maxis wrote reproduces it byte for byte.
/// </remarks>
public sealed class SimDataModel :
    Model,
    IModel<SimDataModel>
{
    const int nullOffset = BinaryUtilities.NullOffset;
    static readonly ImmutableHashSet<ResourceType> supportedTypes =
    [
        ResourceType.SimData
    ];

    sealed class DecodingSchema(SimDataSchema schema, uint[] offsets)
    {
        public uint[] Offsets { get; } = offsets;
        public SimDataSchema Schema { get; } = schema;
    }

    readonly record struct DecodingTable(string? Name, DecodingSchema? Schema, SimDataType Type, int RowSize, int RowPosition, int RowCount)
    {
        public long End =>
            RowPosition + (long)RowSize * RowCount;
    }

    sealed class EncodingTable(SimDataSchema? schema, SimDataType type)
    {
        public bool IsCharacterTable =>
            Schema is null && Type is SimDataType.CHAR8;

        public string? Name { get; set; }
        public int Position { get; set; }
        public List<EncodingReference[]> References { get; } = [];
        public List<object?> Rows { get; } = [];

        public uint RowSize =>
            Schema?.Size ?? Type.GetSize();

        public SimDataSchema? Schema { get; } = schema;
        public SimDataType Type { get; } = type;
    }

    readonly record struct EncodingReference(EncodingTable Table, int Row);

    /// <inheritdoc/>
    public static new ISet<ResourceType> SupportedTypes =>
        supportedTypes;

    /// <inheritdoc/>
    /// <exception cref="InvalidDataException">The data is not a well-formed SimData resource</exception>
    public static SimDataModel Decode(ReadOnlyMemory<byte> data)
    {
        var span = data.Span;
        if (!span.StartsWith("DATA"u8))
            throw new InvalidDataException("The data is not a SimData resource");
        var version = ReadUInt32(span, 4);
        if (version is not ((uint)SimDataVersion.SmartSimOriginalEdition or (uint)SimDataVersion.VariantSupport))
            throw new InvalidDataException($"Version {version:x} is not supported");
        var model = new SimDataModel
        {
            Version = (SimDataVersion)version,
            Unused = version >= (uint)SimDataVersion.VariantSupport ? ReadUInt32(span, 24) : 0
        };
        var tableCount = ReadCount(span, 12, 28);
        var tableHeaderPosition = tableCount > 0 ? ReadRequiredOffset(span, 8) : 0;
        var schemaCount = ReadCount(span, 20, 24);
        var schemaPosition = schemaCount > 0 ? ReadRequiredOffset(span, 16) : 0;
        var schemas = new Dictionary<int, DecodingSchema>();
        for (var s = 0; s < schemaCount; ++s)
        {
            var position = checked(schemaPosition + s * 24);
            var name = ReadOptionalString(span, position) ?? throw new InvalidDataException($"Schema {s} has no name");
            var hash = ReadUInt32(span, position + 8);
            var columnCount = ReadCount(span, position + 20, 20);
            var columnPosition = columnCount > 0 ? ReadRequiredOffset(span, position + 16) : 0;
            var columns = new List<(SimDataColumn column, uint offset)>(columnCount);
            for (var c = 0; c < columnCount; ++c)
            {
                var columnInfoPosition = checked(columnPosition + c * 20);
                var columnName = ReadOptionalString(span, columnInfoPosition) ?? throw new InvalidDataException($"Column {c} of schema {name} has no name");
                var type = (SimDataType)ReadUInt16(span, columnInfoPosition + 8);
                if (!type.IsDefined())
                    throw new InvalidDataException($"Column {columnName} of schema {name} has undefined type {(ushort)type}");
                if (type is SimDataType.VARIANT && model.Version is SimDataVersion.SmartSimOriginalEdition)
                    throw new InvalidDataException($"Column {columnName} of schema {name} is a {SimDataType.VARIANT}, which version {model.Version} does not support");
                columns.Add((new SimDataColumn(columnName, type, ReadUInt16(span, columnInfoPosition + 10)), ReadUInt32(span, columnInfoPosition + 12)));
            }
            var layout = columns.OrderBy(t => t.offset).ToList();
            try
            {
                schemas.Add(position, new(new SimDataSchema(name, hash, layout.Select(t => t.column)), [.. layout.Select(t => t.offset)]));
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException($"Schema {name} is not valid", ex);
            }
        }
        var tables = new List<DecodingTable>(tableCount);
        for (var t = 0; t < tableCount; ++t)
        {
            var position = checked(tableHeaderPosition + t * 28);
            var schemaReference = ReadOffset(span, position + 8);
            DecodingSchema? schema = null;
            if (schemaReference >= 0 && !schemas.TryGetValue(schemaReference, out schema))
                throw new InvalidDataException($"Table {t} refers to a schema that does not exist");
            var type = (SimDataType)ReadUInt32(span, position + 12);
            if (!type.IsDefined())
                throw new InvalidDataException($"Table {t} has undefined type {(uint)type}");
            if (type is SimDataType.VARIANT && model.Version is SimDataVersion.SmartSimOriginalEdition)
                throw new InvalidDataException($"Table {t} is of type {SimDataType.VARIANT}, which version {model.Version} does not support");
            var rowSize = (int)Math.Min(ReadUInt32(span, position + 16), int.MaxValue);
            var rowCount = (int)Math.Min(ReadUInt32(span, position + 24), int.MaxValue);
            var rowPosition = rowCount > 0 ? ReadRequiredOffset(span, position + 20) : 0;
            if (schema is not null && (rowSize < schema.Schema.Size || schema.Offsets.Select((offset, c) => offset + schema.Schema.Columns[c].Type.GetSize()).Any(end => end > rowSize)))
                throw new InvalidDataException($"Table {t}'s rows are too small for its schema");
            if (schema is null && rowSize != type.GetSize())
                throw new InvalidDataException($"Table {t}'s row size does not match its type");
            var table = new DecodingTable(ReadOptionalString(span, position), schema, schema is null ? type : SimDataType.OBJECT, rowSize, rowPosition, rowCount);
            if (table.End > span.Length)
                throw new InvalidDataException($"Table {t} does not fit in the data");
            tables.Add(table);
        }
        var tablesByPosition = tables
            .Select((table, index) => (table, index))
            .Where(t => t.table.RowCount > 0)
            .OrderBy(t => t.table.RowPosition)
            .ToList();
        (int table, int row) locate(int position, int count = 1)
        {
            var low = 0;
            var high = tablesByPosition.Count - 1;
            while (low <= high)
            {
                var middle = low + (high - low) / 2;
                var (table, index) = tablesByPosition[middle];
                if (position < table.RowPosition)
                    high = middle - 1;
                else if (position >= table.End)
                    low = middle + 1;
                else
                {
                    var (row, remainder) = Math.DivRem(position - table.RowPosition, table.RowSize);
                    if (remainder != 0 || (long)row + count > table.RowCount)
                        throw new InvalidDataException($"Position {position} is not the start of {count} row(s) of a table");
                    return (index, row);
                }
            }
            throw new InvalidDataException($"Position {position} is not in a table");
        }
        var referencedRows = new HashSet<(int table, int row)>();
        void noteReferences(ReadOnlySpan<byte> span, SimDataType type, int position)
        {
            if (type is not (SimDataType.OBJECT or SimDataType.VECTOR or SimDataType.VARIANT))
                return;
            var target = ReadReference(span, position);
            if (target < 0)
                return;
            var count = type is SimDataType.VECTOR ? (int)Math.Min(ReadUInt32(span, position + 4), int.MaxValue) : 1;
            if (count == 0)
                return;
            var (table, row) = locate(target, count);
            for (var i = 0; i < count; ++i)
                if (!referencedRows.Add((table, row + i)))
                    throw new InvalidDataException($"Row {row + i} of table {table} is referred to more than once");
        }
        for (var t = 0; t < tables.Count; ++t)
        {
            var table = tables[t];
            for (var r = 0; r < table.RowCount; ++r)
            {
                var rowPosition = table.RowPosition + r * table.RowSize;
                if (table.Schema is { } schema)
                    for (var c = 0; c < schema.Offsets.Length; ++c)
                        noteReferences(span, schema.Schema.Columns[c].Type, rowPosition + (int)schema.Offsets[c]);
                else
                    noteReferences(span, table.Type, rowPosition);
            }
        }
        object? readRow(ReadOnlySpan<byte> span, int tableIndex, int row)
        {
            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
            }
            catch (InsufficientExecutionStackException ex)
            {
                throw new InvalidDataException("The data is nested too deeply", ex);
            }
            var table = tables[tableIndex];
            var position = table.RowPosition + row * table.RowSize;
            if (table.Schema is not { } schema)
                return readValue(span, table.Type, position);
            var @object = new SimDataObject(schema.Schema);
            for (var c = 0; c < schema.Offsets.Length; ++c)
                @object[c] = readValue(span, schema.Schema.Columns[c].Type, position + (int)schema.Offsets[c]);
            return @object;
        }
        object? readValue(ReadOnlySpan<byte> span, SimDataType type, int position)
        {
            switch (type)
            {
                case SimDataType.BOOL:
                    return ReadByte(span, position) is not 0;
                case SimDataType.CHAR8 or SimDataType.UINT8:
                    return ReadByte(span, position);
                case SimDataType.INT8:
                    return (sbyte)ReadByte(span, position);
                case SimDataType.INT16:
                    return (short)ReadUInt16(span, position);
                case SimDataType.UINT16:
                    return ReadUInt16(span, position);
                case SimDataType.INT32:
                    return (int)ReadUInt32(span, position);
                case SimDataType.UINT32 or SimDataType.LOCKEY:
                    return ReadUInt32(span, position);
                case SimDataType.INT64:
                    return (long)ReadUInt64(span, position);
                case SimDataType.UINT64 or SimDataType.TABLESETREFERENCE:
                    return ReadUInt64(span, position);
                case SimDataType.FLOAT:
                    return ReadSingle(span, position);
                case SimDataType.FLOAT2:
                    return new Vector2(ReadSingle(span, position), ReadSingle(span, position + 4));
                case SimDataType.FLOAT3:
                    return new Vector3(ReadSingle(span, position), ReadSingle(span, position + 4), ReadSingle(span, position + 8));
                case SimDataType.FLOAT4:
                    return new Vector4(ReadSingle(span, position), ReadSingle(span, position + 4), ReadSingle(span, position + 8), ReadSingle(span, position + 12));
                case SimDataType.RESOURCEKEY:
                    return new ResourceKey((ResourceType)ReadUInt32(span, position + 8), ReadUInt32(span, position + 12), ReadUInt64(span, position));
                case SimDataType.STRING8 or SimDataType.HASHEDSTRING8:
                    return ReadReference(span, position) is var stringPosition and >= 0 ? ReadString(span, stringPosition) : null;
                case SimDataType.OBJECT:
                {
                    var target = ReadReference(span, position);
                    if (target < 0)
                        return null;
                    var (table, row) = locate(target);
                    if (tables[table].Schema is null)
                        throw new InvalidDataException($"The object at position {position} refers to a table that has no schema");
                    return readRow(span, table, row);
                }
                case SimDataType.VECTOR:
                {
                    var target = ReadReference(span, position);
                    var count = (int)Math.Min(ReadUInt32(span, position + 4), int.MaxValue);
                    if (target < 0)
                        return null;
                    if (count == 0)
                        return new SimDataVector(SimDataType.OBJECT);
                    var (table, row) = locate(target, count);
                    var vector = new SimDataVector(tables[table].Type);
                    try
                    {
                        for (var i = 0; i < count; ++i)
                            vector.Add(readRow(span, table, row + i));
                    }
                    catch (ArgumentException ex)
                    {
                        throw new InvalidDataException($"The vector at position {position} is not valid", ex);
                    }
                    return vector;
                }
                case SimDataType.VARIANT:
                {
                    var target = ReadReference(span, position);
                    var typeHash = ReadUInt32(span, position + 4);
                    if (target < 0)
                        return new SimDataVariant(typeHash);
                    var (table, row) = locate(target);
                    try
                    {
                        return new SimDataVariant(typeHash, tables[table].Type, readRow(span, table, row));
                    }
                    catch (ArgumentException ex)
                    {
                        throw new InvalidDataException($"The variant at position {position} is not valid", ex);
                    }
                }
                default:
                    throw new InvalidDataException($"Type {type} is not supported");
            }
        }
        for (var t = 0; t < tables.Count; ++t)
        {
            var table = tables[t];
            if (table.Name is not { } name || table.Schema is null)
                continue;
            for (var r = 0; r < table.RowCount; ++r)
                if (!referencedRows.Contains((t, r)))
                    model.Instances.Add(new SimDataInstance(name, (SimDataObject)readRow(span, t, r)!));
        }
        return model;
    }

    static string? FormatXmlPrimitive(SimDataType type, object? value) =>
        type switch
        {
            SimDataType.BOOL => (bool)value! ? "1" : "0",
            SimDataType.CHAR8 => ((char)(byte)value!).ToString(),
            SimDataType.FLOAT2 => FormatXmlSingles([((Vector2)value!).X, ((Vector2)value!).Y]),
            SimDataType.FLOAT3 => FormatXmlSingles([((Vector3)value!).X, ((Vector3)value!).Y, ((Vector3)value!).Z]),
            SimDataType.FLOAT4 => FormatXmlSingles([((Vector4)value!).X, ((Vector4)value!).Y, ((Vector4)value!).Z, ((Vector4)value!).W]),
            SimDataType.LOCKEY => string.Create(CultureInfo.InvariantCulture, $"0x{(uint)value!:X8}"),
            SimDataType.RESOURCEKEY => string.Create(CultureInfo.InvariantCulture, $"{(uint)((ResourceKey)value!).Type:X8}-{((ResourceKey)value!).Group:X8}-{((ResourceKey)value!).FullInstance:X16}"),
            SimDataType.STRING8 or SimDataType.HASHEDSTRING8 => value is string text ? RequireXmlCharacters(text) : null,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)!
        };

    static string FormatXmlSingles(float[] values) =>
        string.Join(",", values.Select(value => value.ToString(CultureInfo.InvariantCulture)));

    /// <summary>
    /// Creates a <see cref="SimDataModel"/> from the XML representation of a <see cref="ResourceType.SimData"/> resource that Sims 4 Studio exports and imports
    /// </summary>
    /// <remarks>
    /// Fields are matched to columns by exact name, and every column of an object must have exactly one field.
    /// A string field written as an empty element (<c>&lt;T name="..." /&gt;</c>) is read as <see langword="null"/>.
    /// Type names written by Sims4Toolkit (<c>Int8</c> and <c>UInt8</c>) are accepted along with Sims 4 Studio's.
    /// A schema without a <c>schema_hash</c> attribute gets its <see cref="SimDataSchema.ComputedHash"/>.
    /// </remarks>
    /// <exception cref="InvalidDataException"><paramref name="document"/> is not a valid representation of a <see cref="ResourceType.SimData"/> resource</exception>
    public static SimDataModel FromXml(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        try
        {
            return ReadXml(document);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or InsufficientExecutionStackException or OverflowException)
        {
            throw new InvalidDataException("The document is not a valid representation of a SimData resource", ex);
        }
    }

    static XElement GetXmlElement(SimDataType type, object? value, string? name, bool includeType)
    {
        var element = new XElement(type switch
        {
            SimDataType.OBJECT => "U",
            SimDataType.VECTOR => "L",
            SimDataType.VARIANT => "V",
            _ => "T"
        });
        if (includeType)
            element.Add(new XAttribute("type", type.GetStudioName()));
        if (name is not null)
            element.Add(new XAttribute("name", RequireXmlCharacters(name)));
        switch (value)
        {
            case SimDataObject @object:
                element.Add(new XAttribute("schema", @object.Schema.Name), GetXmlFields(@object));
                break;
            case SimDataVector vector:
                element.Add(vector.Select(vectorElement => GetXmlElement(vector.ElementType, vectorElement, null, true)));
                break;
            case SimDataVariant variant:
                element.Add(new XAttribute("variant", string.Create(CultureInfo.InvariantCulture, $"0x{variant.TypeHash:X8}")));
                if (variant.Value is SimDataObject variantObject)
                    element.Add(new XAttribute("schema", variantObject.Schema.Name), GetXmlFields(variantObject));
                else if (variant.Type is { } variantType)
                    element.Add(GetXmlElement(variantType, variant.Value, null, true));
                break;
            default:
                if (type is not (SimDataType.OBJECT or SimDataType.VECTOR or SimDataType.VARIANT) && FormatXmlPrimitive(type, value) is { } text)
                    element.Value = text;
                break;
        }
        return element;
    }

    static SimDataType GetXmlElementType(XElement element) =>
        element.Attribute("type") is { } typeAttribute
        ? SimDataUtilities.TryParseStudioName(typeAttribute.Value, out var type) ? type : throw new InvalidDataException($"{typeAttribute.Value} is not a SimData type")
        : element.Attribute("schema") is not null
        ? SimDataType.OBJECT
        : throw new InvalidDataException($"A {element.Name.LocalName} element has neither a type nor a schema");

    static IEnumerable<XElement> GetXmlFields(SimDataObject @object) =>
        @object.Schema.Columns.Select((column, index) => GetXmlElement(column.Type, @object[index], column.Name, false));

    /// <inheritdoc/>
    public static new string? GetName(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[28];
        if (DataBasePackedFile.FillBuffer(stream, buffer[..16]) != 16 || !buffer.StartsWith("DATA"u8))
            return null;
        var tableHeaderOffset = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]);
        var tableCount = BinaryPrimitives.ReadInt32LittleEndian(buffer[12..]);
        if (tableHeaderOffset is nullOffset)
            return null;
        for (var t = 0; t < tableCount; ++t)
        {
            var tableInfoPosition = 8L + tableHeaderOffset + t * 28L;
            stream.Seek(tableInfoPosition, SeekOrigin.Begin);
            if (DataBasePackedFile.FillBuffer(stream, buffer) != 28)
                return null;
            var nameOffset = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            if (nameOffset is nullOffset)
                continue;
            stream.Seek(tableInfoPosition + nameOffset, SeekOrigin.Begin);
            var nameBytes = new ArrayBufferWriter<byte>();
            Span<byte> nameByte = stackalloc byte[1];
            while (stream.Read(nameByte) is 1 && nameByte[0] is not 0)
                nameBytes.Write(nameByte);
            return Encoding.UTF8.GetString(nameBytes.WrittenSpan);
        }
        return null;
    }

    /// <inheritdoc/>
    public static new async Task<string?> GetNameAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var buffer = new byte[28];
        if (await DataBasePackedFile.FillBufferAsync(stream, buffer.AsMemory(0, 16), cancellationToken).ConfigureAwait(false) != 16 || !buffer.AsSpan().StartsWith("DATA"u8))
            return null;
        var tableHeaderOffset = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(8));
        var tableCount = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(12));
        if (tableHeaderOffset is nullOffset)
            return null;
        for (var t = 0; t < tableCount; ++t)
        {
            var tableInfoPosition = 8L + tableHeaderOffset + t * 28L;
            stream.Seek(tableInfoPosition, SeekOrigin.Begin);
            if (await DataBasePackedFile.FillBufferAsync(stream, buffer, cancellationToken).ConfigureAwait(false) != 28)
                return null;
            var nameOffset = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            if (nameOffset is nullOffset)
                continue;
            stream.Seek(tableInfoPosition + nameOffset, SeekOrigin.Begin);
            var nameBytes = new ArrayBufferWriter<byte>();
            var nameByte = new byte[1];
            while (await stream.ReadAsync(nameByte, cancellationToken).ConfigureAwait(false) is 1 && nameByte[0] is not 0)
                nameBytes.Write(nameByte);
            return Encoding.UTF8.GetString(nameBytes.WrittenSpan);
        }
        return null;
    }

    static uint ParseXmlHex(string text)
    {
        var digits = text.Trim();
        if (digits.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            digits = digits[2..];
        return uint.Parse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
    }

    static object? ParseXmlPrimitive(SimDataType type, string text)
    {
        float[] parseSingles(int count)
        {
            var values = text.Split(',').Select(value => float.Parse(value, CultureInfo.InvariantCulture)).ToArray();
            return values.Length == count ? values : throw new FormatException($"{type} values have {count} components separated by commas");
        }
        switch (type)
        {
            case SimDataType.BOOL:
                return byte.Parse(text, CultureInfo.InvariantCulture) is not 0;
            case SimDataType.CHAR8:
                return text is [var character] && character <= byte.MaxValue ? (byte)character : throw new FormatException("A CHAR8 value is one character");
            case SimDataType.INT8:
                return sbyte.Parse(text, CultureInfo.InvariantCulture);
            case SimDataType.UINT8:
                return byte.Parse(text, CultureInfo.InvariantCulture);
            case SimDataType.INT16:
                return short.Parse(text, CultureInfo.InvariantCulture);
            case SimDataType.UINT16:
                return ushort.Parse(text, CultureInfo.InvariantCulture);
            case SimDataType.INT32:
                return int.Parse(text, CultureInfo.InvariantCulture);
            case SimDataType.UINT32:
                return uint.Parse(text, CultureInfo.InvariantCulture);
            case SimDataType.INT64:
                return long.Parse(text, CultureInfo.InvariantCulture);
            case SimDataType.UINT64 or SimDataType.TABLESETREFERENCE:
                return ulong.Parse(text, CultureInfo.InvariantCulture);
            case SimDataType.FLOAT:
                return float.Parse(text, CultureInfo.InvariantCulture);
            case SimDataType.FLOAT2:
                var float2 = parseSingles(2);
                return new Vector2(float2[0], float2[1]);
            case SimDataType.FLOAT3:
                var float3 = parseSingles(3);
                return new Vector3(float3[0], float3[1], float3[2]);
            case SimDataType.FLOAT4:
                var float4 = parseSingles(4);
                return new Vector4(float4[0], float4[1], float4[2], float4[3]);
            case SimDataType.LOCKEY:
                return ParseXmlHex(text);
            case SimDataType.RESOURCEKEY:
                var parts = text.Split('-');
                if (parts.Length != 3)
                    throw new FormatException("A RESOURCEKEY value is a type, group and instance in hexadecimal separated by hyphens");
                return new ResourceKey((ResourceType)ParseXmlHex(parts[0]), ParseXmlHex(parts[1]), ulong.Parse(parts[2].Trim(), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
            default:
                throw new InvalidDataException($"Type {type} is not a primitive");
        }
    }

    static byte ReadByte(ReadOnlySpan<byte> data, int position) =>
        position >= 0 && position < data.Length
        ? data[position]
        : throw new InvalidDataException($"Position {position} is outside the data");

    static int ReadCount(ReadOnlySpan<byte> data, int position, int entrySize)
    {
        var count = ReadUInt32(data, position);
        if (count > (uint)(data.Length / entrySize))
            throw new InvalidDataException($"The count at position {position} exceeds the data");
        return (int)count;
    }

    static int ReadOffset(ReadOnlySpan<byte> data, int position)
    {
        var offset = (int)ReadUInt32(data, position);
        if (offset is nullOffset)
            return -1;
        var target = (long)position + offset;
        if (target < 0 || target >= data.Length)
            throw new InvalidDataException($"The offset at position {position} points outside the data");
        return (int)target;
    }

    static string? ReadOptionalString(ReadOnlySpan<byte> data, int position) =>
        ReadOffset(data, position) is var stringPosition and >= 0
        ? ReadString(data, stringPosition)
        : null;

    static int ReadReference(ReadOnlySpan<byte> data, int position) =>
        ReadUInt32(data, position) is 0
        ? -1
        : ReadOffset(data, position);

    static string ReadString(ReadOnlySpan<byte> data, int position)
    {
        var length = data[position..].IndexOf((byte)0);
        if (length < 0)
            throw new InvalidDataException($"The string at position {position} is not terminated");
        return Encoding.UTF8.GetString(data.Slice(position, length));
    }

    static int ReadRequiredOffset(ReadOnlySpan<byte> data, int position) =>
        ReadOffset(data, position) is var target and >= 0
        ? target
        : throw new InvalidDataException($"The offset at position {position} is null");

    static float ReadSingle(ReadOnlySpan<byte> data, int position) =>
        BitConverter.Int32BitsToSingle((int)ReadUInt32(data, position));

    static ushort ReadUInt16(ReadOnlySpan<byte> data, int position) =>
        position >= 0 && position <= data.Length - sizeof(ushort)
        ? BinaryPrimitives.ReadUInt16LittleEndian(data[position..])
        : throw new InvalidDataException($"Position {position} is outside the data");

    static uint ReadUInt32(ReadOnlySpan<byte> data, int position) =>
        position >= 0 && position <= data.Length - sizeof(uint)
        ? BinaryPrimitives.ReadUInt32LittleEndian(data[position..])
        : throw new InvalidDataException($"Position {position} is outside the data");

    static ulong ReadUInt64(ReadOnlySpan<byte> data, int position) =>
        position >= 0 && position <= data.Length - sizeof(ulong)
        ? BinaryPrimitives.ReadUInt64LittleEndian(data[position..])
        : throw new InvalidDataException($"Position {position} is outside the data");

    static SimDataModel ReadXml(XDocument document)
    {
        if (document.Root is not { Name.LocalName: "SimData" } root)
            throw new InvalidDataException("The document's root element is not SimData");
        var version = root.Attribute("version") is { } versionAttribute ? ParseXmlHex(versionAttribute.Value) : (uint)SimDataVersion.VariantSupport;
        if (version is not ((uint)SimDataVersion.SmartSimOriginalEdition or (uint)SimDataVersion.VariantSupport))
            throw new InvalidDataException($"Version {version:x} is not supported");
        var model = new SimDataModel
        {
            Version = (SimDataVersion)version,
            Unused = root.Attribute("u") is { } unusedAttribute ? ParseXmlHex(unusedAttribute.Value) : 0
        };
        var schemas = new Dictionary<string, SimDataSchema>();
        foreach (var schemaElement in root.Elements("Schemas").Elements("Schema"))
        {
            var name = RequireXmlAttribute(schemaElement, "name");
            var columns = schemaElement.Elements("Columns").Elements("Column").Select(columnElement =>
            {
                var columnName = RequireXmlAttribute(columnElement, "name");
                if (!SimDataUtilities.TryParseStudioName(columnElement.Attribute("type")?.Value, out var type))
                    throw new InvalidDataException($"Column {columnName} of schema {name} does not have a SimData type");
                if (type is SimDataType.VARIANT && model.Version is SimDataVersion.SmartSimOriginalEdition)
                    throw new InvalidDataException($"Column {columnName} of schema {name} is a {SimDataType.VARIANT}, which version {model.Version} does not support");
                return new SimDataColumn(columnName, type, columnElement.Attribute("flags") is { } flagsAttribute ? checked((ushort)ParseXmlHex(flagsAttribute.Value)) : (ushort)0);
            }).ToList();
            if (!schemas.TryAdd(name, schemaElement.Attribute("schema_hash") is { } hashAttribute ? new SimDataSchema(name, ParseXmlHex(hashAttribute.Value), columns) : new SimDataSchema(name, columns)))
                throw new InvalidDataException($"There are two schemas named {name}");
        }
        foreach (var instanceElement in root.Elements("Instances").Elements("I"))
            model.Instances.Add(new SimDataInstance(RequireXmlAttribute(instanceElement, "name"), ReadXmlObject(instanceElement, schemas)));
        try
        {
            model.RequireEncodableLayout();
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidDataException($"The document describes a resource that cannot be encoded: {ex.Message}", ex);
        }
        return model;
    }

    static SimDataObject ReadXmlObject(XElement element, Dictionary<string, SimDataSchema> schemas)
    {
        var schemaName = RequireXmlAttribute(element, "schema");
        if (!schemas.TryGetValue(schemaName, out var schema))
            throw new InvalidDataException($"Schema {schemaName} is not defined");
        var @object = new SimDataObject(schema);
        var columnsRead = new HashSet<int>();
        foreach (var fieldElement in element.Elements())
        {
            var fieldName = RequireXmlAttribute(fieldElement, "name");
            if (schema.GetColumnIndex(fieldName) is not { } columnIndex)
                throw new InvalidDataException($"Schema {schemaName} has no column named {fieldName}");
            if (!columnsRead.Add(columnIndex))
                throw new InvalidDataException($"An object of schema {schemaName} has two fields named {fieldName}");
            @object[columnIndex] = ReadXmlValue(schema.Columns[columnIndex].Type, fieldElement, schemas);
        }
        if (columnsRead.Count != schema.Columns.Count)
            throw new InvalidDataException($"An object of schema {schemaName} has no field named {schema.Columns.Where((_, index) => !columnsRead.Contains(index)).First().Name}");
        return @object;
    }

    static object? ReadXmlValue(SimDataType type, XElement element, Dictionary<string, SimDataSchema> schemas)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        switch (type)
        {
            case SimDataType.OBJECT:
                return element.HasElements ? ReadXmlObject(element, schemas) : null;
            case SimDataType.VECTOR:
                if (!element.HasElements)
                    return null;
                var elementType = GetXmlElementType(element.Elements().First());
                var vector = new SimDataVector(elementType);
                foreach (var vectorElement in element.Elements())
                {
                    if (GetXmlElementType(vectorElement) != elementType)
                        throw new InvalidDataException($"A vector of {elementType} has an element of another type");
                    vector.Add(ReadXmlValue(elementType, vectorElement, schemas));
                }
                return vector;
            case SimDataType.VARIANT:
                var typeHash = element.Attribute("variant") is { } variantAttribute ? ParseXmlHex(variantAttribute.Value) : 0;
                if (element.Attribute("schema") is not null)
                    return new SimDataVariant(typeHash, SimDataType.OBJECT, ReadXmlObject(element, schemas));
                var variantElements = element.Elements().ToList();
                if (variantElements.Count is 0)
                    return new SimDataVariant(typeHash);
                if (variantElements.Count is not 1)
                    throw new InvalidDataException("A variant has more than one value");
                var variantType = GetXmlElementType(variantElements[0]);
                return new SimDataVariant(typeHash, variantType, ReadXmlValue(variantType, variantElements[0], schemas));
            case SimDataType.STRING8 or SimDataType.HASHEDSTRING8:
                return element.IsEmpty ? null : element.Value;
            default:
                return ParseXmlPrimitive(type, element.Value);
        }
    }

    static string RequireXmlAttribute(XElement element, string name) =>
        element.Attribute(name)?.Value ?? throw new InvalidDataException($"A {element.Name.LocalName} element has no {name} attribute");

    static string RequireXmlCharacters(string text)
    {
        try
        {
            return XmlConvert.VerifyXmlChars(text);
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException($"\"{text}\" contains characters XML cannot represent", ex);
        }
    }

    /// <summary>
    /// Gets the named, top-level objects of the resource
    /// </summary>
    public Collection<SimDataInstance> Instances { get; } = [];

    /// <inheritdoc/>
    public override string? ResourceName =>
        Instances.Count > 0 ? Instances[0].Name : null;

    /// <summary>
    /// Gets the schemas of the objects in the resource, in the order <see cref="Encode"/> writes them
    /// </summary>
    /// <exception cref="InvalidOperationException">Two different schemas have the same hash, or an object contains itself</exception>
    public IReadOnlyList<SimDataSchema> Schemas =>
        [.. Layout().tables.Where(table => table.Schema is not null).Select(table => table.Schema!)];

    /// <summary>
    /// Gets/sets the value of the <c>mUnused</c> field of the resource's header (Maxis always writes <c>0</c>; ignored for <see cref="SimDataVersion.SmartSimOriginalEdition"/>)
    /// </summary>
    public uint Unused { get; set; }

    /// <summary>
    /// Gets/sets the version of the format of the resource
    /// </summary>
    public SimDataVersion Version { get; set; } = SimDataVersion.VariantSupport;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">The resource cannot be encoded: <see cref="Version"/> is not supported or does not support a value used, two different schemas have the same hash, two instances of the same schema have different names, or an object or vector contains itself</exception>
    public override ReadOnlyMemory<byte> Encode()
    {
        var (tables, characters) = RequireEncodableLayout();
        var schemas = tables.Where(table => table.Schema is not null).Select(table => table.Schema!).ToList();
        static int align(int position, int mask) =>
            position + (-position & mask);
        int getRowCount(EncodingTable table) =>
            table.IsCharacterTable ? characters.WrittenCount : table.Rows.Count;
        var position = align(Version is SimDataVersion.SmartSimOriginalEdition ? 24 : 28, 15);
        var tableHeaderPosition = position;
        position += tables.Count * 28;
        foreach (var table in tables)
        {
            position = align(align(position, 15), (int)table.RowSize - 1);
            table.Position = position;
            position = checked(position + (int)table.RowSize * getRowCount(table));
        }
        var schemaPosition = align(position, 15);
        var columnPosition = schemaPosition + schemas.Count * 24;
        var namePosition = columnPosition + schemas.Sum(schema => schema.Columns.Count) * 20;
        var namePositions = new Dictionary<string, int>();
        var names = new ArrayBufferWriter<byte>();
        int getNamePosition(string name)
        {
            if (!namePositions.TryGetValue(name, out var position))
            {
                position = namePosition + names.WrittenCount;
                namePositions.Add(name, position);
                names.Write(Encoding.UTF8.GetBytes(name));
                names.Write([(byte)0]);
            }
            return position;
        }
        foreach (var schema in schemas)
        {
            foreach (var column in schema.Columns)
                getNamePosition(column.Name);
            getNamePosition(schema.Name);
        }
        foreach (var table in tables)
            if (table.Name is { } tableName)
                getNamePosition(tableName);
        var data = new byte[namePosition + names.WrittenCount];
        var span = data.AsSpan();
        static void writeOffset(Span<byte> span, int position, int target) =>
            BinaryPrimitives.WriteInt32LittleEndian(span[position..], target < 0 ? nullOffset : target - position);
        static void writeSingle(Span<byte> span, int position, float value) =>
            BinaryPrimitives.WriteInt32LittleEndian(span[position..], BitConverter.SingleToInt32Bits(value));
        "DATA"u8.CopyTo(span);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], (uint)Version);
        writeOffset(span, 8, tableHeaderPosition);
        BinaryPrimitives.WriteInt32LittleEndian(span[12..], tables.Count);
        writeOffset(span, 16, schemaPosition);
        BinaryPrimitives.WriteInt32LittleEndian(span[20..], schemas.Count);
        if (Version is SimDataVersion.VariantSupport)
            BinaryPrimitives.WriteUInt32LittleEndian(span[24..], Unused);
        for (var t = 0; t < tables.Count; ++t)
        {
            var table = tables[t];
            var tablePosition = tableHeaderPosition + t * 28;
            writeOffset(span, tablePosition, table.Name is { } tableName ? getNamePosition(tableName) : -1);
            BinaryPrimitives.WriteUInt32LittleEndian(span[(tablePosition + 4)..], SimDataUtilities.GetNameHash(table.Name));
            writeOffset(span, tablePosition + 8, table.Schema is { } schema ? schemaPosition + schemas.IndexOf(schema) * 24 : -1);
            BinaryPrimitives.WriteUInt32LittleEndian(span[(tablePosition + 12)..], (uint)table.Type);
            BinaryPrimitives.WriteUInt32LittleEndian(span[(tablePosition + 16)..], table.RowSize);
            writeOffset(span, tablePosition + 20, table.Position);
            BinaryPrimitives.WriteInt32LittleEndian(span[(tablePosition + 24)..], getRowCount(table));
            if (table.IsCharacterTable)
                characters.WrittenSpan.CopyTo(span[table.Position..]);
        }
        static int getTarget(EncodingReference reference) =>
            reference.Table is null
            ? -1
            : reference.Table.Position + reference.Row * (int)reference.Table.RowSize;
        static void writeValue(Span<byte> span, int position, SimDataType type, object? value, EncodingReference reference)
        {
            switch (type)
            {
                case SimDataType.BOOL:
                    span[position] = (bool)value! ? (byte)1 : (byte)0;
                    break;
                case SimDataType.CHAR8 or SimDataType.UINT8:
                    span[position] = (byte)value!;
                    break;
                case SimDataType.INT8:
                    span[position] = (byte)(sbyte)value!;
                    break;
                case SimDataType.INT16:
                    BinaryPrimitives.WriteInt16LittleEndian(span[position..], (short)value!);
                    break;
                case SimDataType.UINT16:
                    BinaryPrimitives.WriteUInt16LittleEndian(span[position..], (ushort)value!);
                    break;
                case SimDataType.INT32:
                    BinaryPrimitives.WriteInt32LittleEndian(span[position..], (int)value!);
                    break;
                case SimDataType.UINT32 or SimDataType.LOCKEY:
                    BinaryPrimitives.WriteUInt32LittleEndian(span[position..], (uint)value!);
                    break;
                case SimDataType.INT64:
                    BinaryPrimitives.WriteInt64LittleEndian(span[position..], (long)value!);
                    break;
                case SimDataType.UINT64 or SimDataType.TABLESETREFERENCE:
                    BinaryPrimitives.WriteUInt64LittleEndian(span[position..], (ulong)value!);
                    break;
                case SimDataType.FLOAT:
                    writeSingle(span, position, (float)value!);
                    break;
                case SimDataType.FLOAT2:
                    var float2 = (Vector2)value!;
                    writeSingle(span, position, float2.X);
                    writeSingle(span, position + 4, float2.Y);
                    break;
                case SimDataType.FLOAT3:
                    var float3 = (Vector3)value!;
                    writeSingle(span, position, float3.X);
                    writeSingle(span, position + 4, float3.Y);
                    writeSingle(span, position + 8, float3.Z);
                    break;
                case SimDataType.FLOAT4:
                    var float4 = (Vector4)value!;
                    writeSingle(span, position, float4.X);
                    writeSingle(span, position + 4, float4.Y);
                    writeSingle(span, position + 8, float4.Z);
                    writeSingle(span, position + 12, float4.W);
                    break;
                case SimDataType.RESOURCEKEY:
                    var key = (ResourceKey)value!;
                    BinaryPrimitives.WriteUInt64LittleEndian(span[position..], key.FullInstance);
                    BinaryPrimitives.WriteUInt32LittleEndian(span[(position + 8)..], (uint)key.Type);
                    BinaryPrimitives.WriteUInt32LittleEndian(span[(position + 12)..], key.Group);
                    break;
                case SimDataType.STRING8 or SimDataType.OBJECT:
                    writeOffset(span, position, getTarget(reference));
                    break;
                case SimDataType.HASHEDSTRING8:
                    writeOffset(span, position, getTarget(reference));
                    BinaryPrimitives.WriteUInt32LittleEndian(span[(position + 4)..], value is string hashedString ? SimDataUtilities.GetNameHash(hashedString) : SimDataUtilities.EmptyNameHash);
                    break;
                case SimDataType.VECTOR:
                    writeOffset(span, position, getTarget(reference));
                    BinaryPrimitives.WriteInt32LittleEndian(span[(position + 4)..], reference.Table is null ? 0 : ((SimDataVector)value!).Count);
                    break;
                case SimDataType.VARIANT:
                    writeOffset(span, position, getTarget(reference));
                    BinaryPrimitives.WriteUInt32LittleEndian(span[(position + 4)..], ((SimDataVariant?)value)?.TypeHash ?? 0);
                    break;
            }
        }
        foreach (var table in tables)
        {
            if (table.IsCharacterTable)
                continue;
            for (var r = 0; r < table.Rows.Count; ++r)
            {
                var rowPosition = table.Position + r * (int)table.RowSize;
                var references = table.References[r];
                if (table.Schema is { } schema)
                {
                    var @object = (SimDataObject)table.Rows[r]!;
                    for (var c = 0; c < schema.Columns.Count; ++c)
                        writeValue(span, rowPosition + (int)schema.Offsets[c], schema.Columns[c].Type, @object[c], references[c]);
                }
                else
                    writeValue(span, rowPosition, table.Type, table.Rows[r], references[0]);
            }
        }
        for (var s = 0; s < schemas.Count; ++s)
        {
            var schema = schemas[s];
            var schemaInfoPosition = schemaPosition + s * 24;
            writeOffset(span, schemaInfoPosition, getNamePosition(schema.Name));
            BinaryPrimitives.WriteUInt32LittleEndian(span[(schemaInfoPosition + 4)..], SimDataUtilities.GetNameHash(schema.Name));
            BinaryPrimitives.WriteUInt32LittleEndian(span[(schemaInfoPosition + 8)..], schema.Hash);
            BinaryPrimitives.WriteUInt32LittleEndian(span[(schemaInfoPosition + 12)..], schema.Size);
            writeOffset(span, schemaInfoPosition + 16, columnPosition);
            BinaryPrimitives.WriteInt32LittleEndian(span[(schemaInfoPosition + 20)..], schema.Columns.Count);
            foreach (var c in Enumerable.Range(0, schema.Columns.Count).OrderBy(c => SimDataUtilities.GetNameHash(schema.Columns[c].Name)))
            {
                var column = schema.Columns[c];
                writeOffset(span, columnPosition, getNamePosition(column.Name));
                BinaryPrimitives.WriteUInt32LittleEndian(span[(columnPosition + 4)..], SimDataUtilities.GetNameHash(column.Name));
                BinaryPrimitives.WriteUInt16LittleEndian(span[(columnPosition + 8)..], (ushort)column.Type);
                BinaryPrimitives.WriteUInt16LittleEndian(span[(columnPosition + 10)..], column.Flags);
                BinaryPrimitives.WriteUInt32LittleEndian(span[(columnPosition + 12)..], schema.Offsets[c]);
                BinaryPrimitives.WriteInt32LittleEndian(span[(columnPosition + 16)..], nullOffset);
                columnPosition += 20;
            }
        }
        names.WrittenSpan.CopyTo(span[namePosition..]);
        return data;
    }

    (List<EncodingTable> tables, ArrayBufferWriter<byte> characters) Layout()
    {
        var schemaTables = new List<EncodingTable>();
        var rawTables = new List<EncodingTable>();
        var tablesBySchemaHash = new Dictionary<uint, EncodingTable>();
        var tablesByType = new Dictionary<SimDataType, EncodingTable>();
        var characters = new ArrayBufferWriter<byte>();
        var valuesBeingVisited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        EncodingTable getSchemaTable(SimDataSchema schema)
        {
            if (tablesBySchemaHash.TryGetValue(schema.Hash, out var table))
            {
                if (!table.Schema!.DefinitionEquals(schema))
                    throw new InvalidOperationException($"Two different schemas ({table.Schema.Name} and {schema.Name}) have the same hash {schema.Hash:x8}");
                return table;
            }
            table = new EncodingTable(schema, SimDataType.OBJECT);
            tablesBySchemaHash.Add(schema.Hash, table);
            schemaTables.Add(table);
            return table;
        }
        EncodingTable getRawTable(SimDataType type)
        {
            if (!tablesByType.TryGetValue(type, out var table))
            {
                table = new EncodingTable(null, type);
                tablesByType.Add(type, table);
                rawTables.Add(table);
            }
            return table;
        }
        EncodingTable getTable(SimDataType type, object? value) =>
            type is SimDataType.OBJECT
            ? getSchemaTable(((SimDataObject)value!).Schema)
            : getRawTable(type);
        int allocate(EncodingTable table, IEnumerable<object?> rows)
        {
            if (table.IsCharacterTable)
            {
                var position = characters.WrittenCount;
                foreach (var row in rows)
                    characters.Write([(byte)row!]);
                return position;
            }
            var start = table.Rows.Count;
            foreach (var row in rows)
            {
                table.Rows.Add(row);
                table.References.Add(new EncodingReference[table.Schema?.Columns.Count ?? 1]);
            }
            return start;
        }
        void enter(object value)
        {
            if (!valuesBeingVisited.Add(value))
                throw new InvalidOperationException("An object or vector contains itself");
            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
            }
            catch (InsufficientExecutionStackException ex)
            {
                throw new InvalidOperationException("The resource is nested too deeply", ex);
            }
        }
        void visitRow(EncodingTable table, int row)
        {
            if (table.IsCharacterTable)
                return;
            var references = table.References[row];
            if (table.Schema is { } schema)
            {
                var @object = (SimDataObject)table.Rows[row]!;
                enter(@object);
                for (var c = 0; c < schema.Columns.Count; ++c)
                    references[c] = visitValue(schema.Columns[c].Type, @object[c]);
                valuesBeingVisited.Remove(@object);
            }
            else
                references[0] = visitValue(table.Type, table.Rows[row]);
        }
        EncodingReference visitValue(SimDataType type, object? value)
        {
            switch (type, value)
            {
                case (SimDataType.STRING8 or SimDataType.HASHEDSTRING8, string @string):
                {
                    var characterTable = getRawTable(SimDataType.CHAR8);
                    var position = characters.WrittenCount;
                    characters.Write(Encoding.UTF8.GetBytes(@string));
                    characters.Write([(byte)0]);
                    return new(characterTable, position);
                }
                case (SimDataType.OBJECT, SimDataObject @object):
                {
                    var table = getSchemaTable(@object.Schema);
                    var row = allocate(table, [@object]);
                    visitRow(table, row);
                    return new(table, row);
                }
                case (SimDataType.VECTOR, SimDataVector { Count: > 0 } vector):
                {
                    enter(vector);
                    var table = getTable(vector.ElementType, vector[0]);
                    var start = allocate(table, vector);
                    for (var i = 0; i < vector.Count; ++i)
                        visitRow(table, start + i);
                    valuesBeingVisited.Remove(vector);
                    return new(table, start);
                }
                case (SimDataType.VARIANT, SimDataVariant { Type: { } variantType, Value: var variantValue }):
                {
                    var table = getTable(variantType, variantValue);
                    var row = allocate(table, [variantValue]);
                    visitRow(table, row);
                    return new(table, row);
                }
                default:
                    return default;
            }
        }
        foreach (var instance in Instances)
        {
            var table = getSchemaTable(instance.Object.Schema);
            if (table.Name is null)
                table.Name = instance.Name;
            else if (table.Name != instance.Name)
                throw new InvalidOperationException($"Instances {table.Name} and {instance.Name} have the same schema ({instance.Object.Schema.Name}) but different names, and all of a schema's objects are written to one table, which has one name");
            visitRow(table, allocate(table, [instance.Object]));
        }
        return ([.. schemaTables, .. rawTables], characters);
    }

    (List<EncodingTable> tables, ArrayBufferWriter<byte> characters) RequireEncodableLayout()
    {
        if (Version is not (SimDataVersion.SmartSimOriginalEdition or SimDataVersion.VariantSupport))
            throw new InvalidOperationException($"Version {Version} is not supported");
        var (tables, characters) = Layout();
        if (Version is SimDataVersion.SmartSimOriginalEdition && tables.Any(table => table.Type is SimDataType.VARIANT || table.Schema is { } schema && schema.Columns.Any(column => column.Type is SimDataType.VARIANT)))
            throw new InvalidOperationException($"Version {Version} does not support {SimDataType.VARIANT} values");
        return (tables, characters);
    }

    /// <summary>
    /// Gets the XML representation of the resource that Sims 4 Studio exports and imports
    /// </summary>
    /// <remarks>
    /// A <see langword="null"/> string is written as an empty element (<c>&lt;T name="..." /&gt;</c>) and an empty string as an element with empty content (<c>&lt;T name="..."&gt;&lt;/T&gt;</c>), which <see cref="FromXml(XDocument)"/> tells apart (Sims 4 Studio reads both as empty strings).
    /// An empty vector is written the same way as a <see langword="null"/> one, which <see cref="Encode"/> also writes the same way.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Two different schemas have the same hash or the same name, a name or string contains characters XML cannot represent, or an object or vector contains itself</exception>
    public XDocument ToXml()
    {
        var schemas = Schemas;
        if (schemas.GroupBy(schema => schema.Name).FirstOrDefault(group => group.Count() > 1) is { } duplicateName)
            throw new InvalidOperationException($"Two different schemas are named {duplicateName.Key}");
        return new XDocument
        (
            new XDeclaration("1.0", "utf-8", null),
            new XElement
            (
                "SimData",
                new XAttribute("version", string.Create(CultureInfo.InvariantCulture, $"0x{(uint)Version:X8}")),
                new XAttribute("u", string.Create(CultureInfo.InvariantCulture, $"0x{Unused:X8}")),
                new XElement("Instances", Instances.Select(instance => new XElement
                (
                    "I",
                    new XAttribute("name", RequireXmlCharacters(instance.Name)),
                    new XAttribute("schema", instance.Object.Schema.Name),
                    new XAttribute("type", SimDataType.OBJECT.GetStudioName()),
                    GetXmlFields(instance.Object)
                ))),
                new XElement("Schemas", schemas.Select(schema => new XElement
                (
                    "Schema",
                    new XAttribute("name", RequireXmlCharacters(schema.Name)),
                    new XAttribute("schema_hash", string.Create(CultureInfo.InvariantCulture, $"0x{schema.Hash:X8}")),
                    new XElement("Columns", schema.Columns.Select(column => new XElement
                    (
                        "Column",
                        new XAttribute("name", RequireXmlCharacters(column.Name)),
                        new XAttribute("type", column.Type.GetStudioName()),
                        new XAttribute("flags", string.Create(CultureInfo.InvariantCulture, $"0x{column.Flags:X8}"))
                    )))
                )))
            )
        );
    }
}
