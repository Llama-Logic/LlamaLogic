namespace LlamaLogic.Packages.Models.Data;

/// <summary>
/// Represents a <see cref="ResourceType.SimData"/> or <see cref="ResourceType.CombinedTuning"/> resource (🔓)
/// </summary>
/// <remarks>
/// ### Synchronous API Only
/// Due to performance considerations, this object model provides no asynchronous API.
/// Thus, the caller will be blocked until the entire <see cref="ResourceType.SimData"/> or <see cref="ResourceType.CombinedTuning"/> resource is decoded by <see cref="Decode(ReadOnlyMemory{byte})"/> or encoded by <see cref="Encode"/>.
/// Front-end developers are advised to wrap calls to either method with <see cref="Task.Run(Func{Task?})"/> to avoid blocking the UI thread.
/// The asynchronous API of <see cref="DataBasePackedFile"/> will do this automatically.
///
/// ### Eager Loading
/// When a <see cref="ResourceType.SimData"/> or <see cref="ResourceType.CombinedTuning"/> resource is passed to <see cref="Decode(ReadOnlyMemory{byte})"/>, the entire resource is decoded to compose a complete graph of all tables in the resource, including their metadata and records.
/// For some <see cref="ResourceType.CombinedTuning"/> resources shipped by Maxis, this may result in a large number of tables and records being loaded into memory and take a significant amount of time.
///
/// ### Thread Safety
/// For performance reasons, this class and its supporting classes in this namespace are not thread-safe.
/// If a caller has need for parallelized use of a <see cref="DataModel"/>, they are expected to manage synchronization on their own.
/// 
/// ### Exposed Reference Structure
/// To grant callers maximum control, the structure of references from the <see cref="ResourceType.SimData"/> or <see cref="ResourceType.CombinedTuning"/> resource is preserved and unabstracted.
/// 
/// While the classes which implement <see cref="DataModelReference"/> have properties to produce useful compositions (e.g. the <see cref="DataModelString.Value"/> property of <see cref="DataModelString"/> will convert the null-terminated sequence of ASCII characters it references in a <see cref="DataModelTable"/> of <see cref="DataModelType.CHAR8"/> values to a <see cref="string"/>), these conveniences are processed on-demand and only so long as the reference has remained valid.
/// The original data being referenced remains stored by instances of <see cref="DataModelTable"/> as it appeared in the resource at the time it was decoded.
/// All instances of <see cref="DataModelReference"/> monitor the row (or rows) of the <see cref="DataModelTable"/> they are referencing and will invalidate themselves if any constituent element is altered by the caller.
///
/// <see cref="Encode"/> reacts to invalid references by writing the `RELOFFSET_NULL` value in their place.
/// Callers can check to see if something they've done has invalidated a reference by checking its <see cref="DataModelReference.IsValid"/> property.
/// </remarks>
public sealed class DataModel :
    Model,
    IModel<DataModel>
{
    record PendingReferenceOffset(DataModelReference Reference, Memory<byte> OffsetMemory);

    record PendingStringOffset(string? Text, Memory<byte> OffsetMemory);

    [SuppressMessage("Style", "IDE1006: Naming Styles", Justification = "Original Maxis format naming.")]
    record SchemaColumn(string? Name, DataModelType mnDataType, ushort mnFlags, uint mnOffset, uint? nSchemaPos);

    [SuppressMessage("Style", "IDE1006: Naming Styles", Justification = "Original Maxis format naming.")]
    record SchemaInfo(uint nSchemaPos, string? Name, uint mnSchemaHash, uint mnSchemaSize, uint nColumnPos, uint mnNumColumns, IReadOnlyList<SchemaColumn> Columns);

    [SuppressMessage("Style", "IDE1006: Naming Styles", Justification = "Original Maxis format naming.")]
    record TableInfo(string? Name, uint? nSchemaPos, DataModelType mnDataType, uint mnRowSize, uint nRowPos, uint mnRowCount);

    static readonly Memory<byte> expectedFileIdentifier = "DATA"u8.ToArray();
    static readonly Memory<byte> markupFileIdentifier = "<com"u8.ToArray();
    static readonly ImmutableHashSet<ResourceType> supportedTypes =
    [
        ResourceType.SimData,
        ResourceType.CombinedTuning
    ];

    internal static object? CoerceValue(object? value, DataModelType type) =>
        type switch
        {
            DataModelType.BOOL when value is bool b => b,
            DataModelType.BOOL => Convert.ToBoolean(value),
            DataModelType.CHAR8 when value is char c => c,
            DataModelType.CHAR8 => Convert.ToChar(value),
            DataModelType.INT8 when value is sbyte sb => sb,
            DataModelType.INT8 => Convert.ToSByte(value),
            DataModelType.UINT8 when value is byte b => b,
            DataModelType.UINT8 => Convert.ToByte(value),
            DataModelType.INT16 when value is short s => s,
            DataModelType.INT16 => Convert.ToInt16(value),
            DataModelType.UINT16 when value is ushort us => us,
            DataModelType.UINT16 => Convert.ToUInt16(value),
            DataModelType.INT32 when value is int i => i,
            DataModelType.INT32 => Convert.ToInt32(value),
            DataModelType.UINT32 or DataModelType.LOCKEY when value is uint ui => ui,
            DataModelType.LOCKEY when value is string { Length: 8 } locKeyString && uint.TryParse(locKeyString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var locKey) => locKey,
            DataModelType.LOCKEY when value is string { Length: 10 } locKeyString && locKeyString.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && uint.TryParse(locKeyString[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var locKey) => locKey,
            DataModelType.UINT32 or DataModelType.LOCKEY => Convert.ToUInt32(value),
            DataModelType.INT64 => Convert.ToInt64(value),
            DataModelType.UINT64 or DataModelType.TABLESETREFERENCE => Convert.ToUInt64(value),
            DataModelType.FLOAT => Convert.ToSingle(value),
            DataModelType.STRING8 or DataModelType.HASHEDSTRING8 when value is DataModelString @string => @string,
            DataModelType.OBJECT when value is DataModelObject @object => @object,
            DataModelType.VECTOR when value is DataModelVector vector => vector,
            DataModelType.FLOAT2 when value is IEnumerable<float> floatEnumerable && floatEnumerable.Count() == 2 => floatEnumerable.ToImmutableArray(),
            DataModelType.FLOAT2 when value is null => Enumerable.Repeat<float>(0, 2).ToImmutableArray(),
            DataModelType.FLOAT3 when value is IEnumerable<float> floatEnumerable && floatEnumerable.Count() == 3 => floatEnumerable.ToImmutableArray(),
            DataModelType.FLOAT3 when value is null => Enumerable.Repeat<float>(0, 3).ToImmutableArray(),
            DataModelType.FLOAT4 when value is IEnumerable<float> floatEnumerable && floatEnumerable.Count() == 4 => floatEnumerable.ToImmutableArray(),
            DataModelType.FLOAT4 when value is null => Enumerable.Repeat<float>(0, 4).ToImmutableArray(),
            DataModelType.RESOURCEKEY when value is ResourceKey resourceKey => resourceKey,
            DataModelType.RESOURCEKEY when value is string resourceKeyString => ResourceKey.Parse(resourceKeyString),
            DataModelType.RESOURCEKEY when value is null => new ResourceKey(),
            DataModelType.VARIANT when value is DataModelVariant variant => variant,
            _ => throw new NotSupportedException($"Type {type} is not supported or value could not be coerced")
        };

    /// <inheritdoc/>
    /// <exception cref="UnexpectedFormatDecodingException">The data appears to be a <see cref="ResourceType.CombinedTuning"/> resource in XML format</exception>
    public static DataModel Decode(ReadOnlyMemory<byte> data)
    {
        var dataSpan = data.Span;
        var mnFileIdentifier = dataSpan[0..4];
        if (!mnFileIdentifier.SequenceEqual(expectedFileIdentifier.Span))
        {
            if (mnFileIdentifier.SequenceEqual(markupFileIdentifier.Span))
                throw new UnexpectedFormatDecodingException($"This appears to be a combined tuning resource in XML rather than binary format. Please use {nameof(DataBasePackedFile.GetText)}, {nameof(DataBasePackedFile.GetTextAsync)}, {nameof(DataBasePackedFile.GetXml)}, or {nameof(DataBasePackedFile.GetXmlAsync)} instead.");
            throw new NotSupportedException($"File identifier \"{Encoding.ASCII.GetString(mnFileIdentifier)}\" is invalid");
        }
        var readPosition = 4U;
        var mnVersion = dataSpan.ReadAndAdvancePosition<DataModelVersion>(ref readPosition);
        if (mnVersion is < DataModelVersion.SmartSimOriginalEdition or > DataModelVersion.VariantSupport)
            throw new NotSupportedException($"Version {mnVersion} is not supported");
        var nTableHeaderPos = dataSpan.ReadOffsetAndAdvancePosition(ref readPosition)
            ?? throw new FormatException("mnTableHeaderOffset is RELOFFSET_NULL");
        var mnNumTables = dataSpan.ReadAndAdvancePosition<int>(ref readPosition);
        var nSchemaPos = dataSpan.ReadOffsetAsLongPositionAndAdvancePosition(ref readPosition)
            ?? throw new FormatException("mnSchemaOffset is RELOFFSET_NULL");
        var mnNumSchemas = dataSpan.ReadAndAdvancePosition<int>(ref readPosition);
        var tableInfos = new List<TableInfo>();
        readPosition = nTableHeaderPos;
        for (var i = 0; i < mnNumTables; ++i)
            tableInfos.Add(new TableInfo
            (
                dataSpan.ReadOffsetHashedStringAndAdvancePosition(ref readPosition),
                dataSpan.ReadOffsetAndAdvancePosition(ref readPosition),
                (DataModelType)dataSpan.ReadAndAdvancePosition<uint>(ref readPosition),
                dataSpan.ReadAndAdvancePosition<uint>(ref readPosition),
                dataSpan.ReadOffsetAndAdvancePosition(ref readPosition)
                    ?? throw new FormatException($"Table at index {i} has mnRowOffset RELOFFSET_NULL"),
                dataSpan.ReadAndAdvancePosition<uint>(ref readPosition)
            ));
        var schemaInfos = new List<SchemaInfo>();
        var schemaInfosByPosition = new Dictionary<uint, SchemaInfo>();
        for (var i = 0; i < mnNumSchemas; ++i)
        {
            readPosition = (uint)(nSchemaPos + i *
            (
                  sizeof(int) // mnNameOffset
                + sizeof(uint) // mnNameHash
                + sizeof(uint) // mnSchemaHash
                + sizeof(uint) // mnSchemaSize
                + sizeof(int) // mnColumnOffset
                + sizeof(uint) // mnNumColumns
            ));
            var nThisSchemaPos = readPosition;
            var name = dataSpan.ReadOffsetHashedStringAndAdvancePosition(ref readPosition);
            var mnSchemaHash = dataSpan.ReadAndAdvancePosition<uint>(ref readPosition);
            var mnSchemaSize = dataSpan.ReadAndAdvancePosition<uint>(ref readPosition);
            var nColumnPos = dataSpan.ReadOffsetAndAdvancePosition(ref readPosition)
                ?? throw new FormatException($"Schema at index {i} has mnColumnOffset RELOFFSET_NULL");
            var mnNumColumns = dataSpan.ReadAndAdvancePosition<uint>(ref readPosition);
            readPosition = nColumnPos;
            var columns = new List<SchemaColumn>();
            for (var j = 0; j < mnNumColumns; ++j)
                columns.Add(new SchemaColumn
                (
                    dataSpan.ReadOffsetHashedStringAndAdvancePosition(ref readPosition),
                    (DataModelType)dataSpan.ReadAndAdvancePosition<ushort>(ref readPosition),
                    dataSpan.ReadAndAdvancePosition<ushort>(ref readPosition),
                    dataSpan.ReadAndAdvancePosition<uint>(ref readPosition),
                    dataSpan.ReadOffsetAndAdvancePosition(ref readPosition)
                ));
            var schemaInfo = new SchemaInfo(nThisSchemaPos, name, mnSchemaHash, mnSchemaSize, nColumnPos, mnNumColumns, [.. columns]);
            schemaInfos.Add(schemaInfo);
            schemaInfosByPosition.Add(nThisSchemaPos, schemaInfo);
        }
        SchemaInfo? getSchemaInfo(int tableIndex, uint? maybeNull_nSchemaPos)
        {
            if (maybeNull_nSchemaPos is { } mnSchemaPos)
            {
                if (!schemaInfosByPosition.TryGetValue(mnSchemaPos, out var schemaInfo))
                {
                    // the schema offset in the table header was not updated when the schema was moved because Maxis
                    // try to recover using appearance order
                    if (tableIndex >= schemaInfos.Count)
                        throw new FormatException($"Failed to find a schema at position {mnSchemaPos} ({mnSchemaPos:X}h)");
                    // the sims 4 has spaghetti code? —BlackCherry
                    schemaInfo = schemaInfos[tableIndex];
                }
                return schemaInfo;
            }
            return null;
        }
        var tableTypesByPositionRange = tableInfos
            .Select((tableInfo, index) =>
            (
                index,
                nRowPosStart: tableInfo.nRowPos,
                nRowPosEnd: tableInfo.nRowPos + tableInfo.mnRowCount * tableInfo.mnRowSize,
                tableInfo.nSchemaPos,
                tableInfo.mnDataType,
                tableInfo.mnRowSize
            ))
            .ToImmutableArray();
        (int tableIndex, DataModelType mnDataType, int rowIndex, SchemaColumn? schemaColumn, int columnIndex) getValueMetaDataByPosition(uint position)
        {
            int index;
            uint nRowPosStart, mnRowSize;
            uint? maybeNull_nSchemaPos;
            DataModelType mnDataType;
            SchemaColumn? schemaColumn = null;
            var columnIndex = 0;
            try
            {
                (index, nRowPosStart, _, maybeNull_nSchemaPos, mnDataType, mnRowSize) = tableTypesByPositionRange.First(t => t.nRowPosStart <= position && position < t.nRowPosEnd);
                if (mnDataType is DataModelType.OBJECT && getSchemaInfo(index, maybeNull_nSchemaPos) is { } schemaInfo)
                {
                    var mnOffset = (position - nRowPosStart) % mnRowSize;
                    columnIndex = schemaInfo.Columns.Select((column, i) => (column, i)).First(t => t.column.mnOffset == mnOffset).i;
                    schemaColumn = schemaInfo.Columns[columnIndex];
                }
            }
            catch (Exception ex)
            {
                throw new FormatException($"Failed to find meta data for the value at position {position} ({position:X}h)", ex);
            }
            return (index, mnDataType, (int)((position - nRowPosStart) / mnRowSize), schemaColumn, columnIndex);
        }
        var model = new DataModel { Version = mnVersion };
        var references = new List<DataModelReference>();
        for (var tableIndex = 0; tableIndex < mnNumTables; ++tableIndex)
        {
            var (tableName, maybeNull_nSchemaPos, mnDataType, mnRowSize, nRowPos, mnRowCount) = tableInfos[tableIndex];
            string? schemaName = null;
            uint? mnSchemaHash = null, mnSchemaSize = null;
            IReadOnlyList<SchemaColumn>? columns = null;
            if (getSchemaInfo(tableIndex, maybeNull_nSchemaPos) is { } schemaInfo)
            {
                (_, schemaName, _, _, _, _, columns) = schemaInfo;
                mnSchemaHash = schemaInfo.mnSchemaHash;
                mnSchemaSize = schemaInfo.mnSchemaSize;
            }
            var table = new DataModelTable(tableName, schemaName, mnSchemaHash);
            if (columns is not null)
                foreach (var (columnName, columnType, columnFlags, _, _) in columns)
                    table.AddColumn(columnName, columnType, columnFlags);
            if (mnSchemaSize is { } nSchemaSize && columns is not null)
            {
                var rowSize = nSchemaSize;
                for (uint i = 0; i < mnRowCount; ++i)
                {
                    readPosition = nRowPos + i * rowSize;
                    table.AddRow(columns.Select(column =>
                    {
                        try
                        {
                            return GetValue(data, column.mnDataType, readPosition + column.mnOffset, getValueMetaDataByPosition, references);
                        }
                        catch (Exception ex)
                        {
                            throw new FormatException($"Failed to read {column} from row at position {readPosition} ({readPosition:X}h)", ex);
                        }
                    }));
                }
            }
            else
            {
                var rawValues = new List<object?>();
                var rowSize = mnDataType.GetSize();
                for (uint i = 0; i < mnRowCount; ++i)
                {
                    readPosition = nRowPos + i * rowSize;
                    rawValues.Add(GetValue(data, mnDataType, readPosition, getValueMetaDataByPosition, references));
                }
                table.AddColumn(null, mnDataType, 0, rawValues);
            }
            model.Tables.Add(table);
        }
        foreach (var reference in references)
            reference.Bind(model);
        return model;
    }

    /// <inheritdoc/>
    public static new string? GetName(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> buffer = stackalloc byte[28];
        if (stream.Read(buffer) != 28)
            return null;
        ReadOnlySpan<byte> headerSpan = buffer;
        var mnFileIdentifier = headerSpan[0..4];
        if (!mnFileIdentifier.SequenceEqual(expectedFileIdentifier.Span))
            return null;
        var readPosition = 4U;
        var mnVersion = headerSpan.ReadAndAdvancePosition<DataModelVersion>(ref readPosition);
        if (mnVersion is < DataModelVersion.SmartSimOriginalEdition or > DataModelVersion.VariantSupport)
            return null;
        var maybeNull_nTableHeaderPos = headerSpan.ReadOffsetAndAdvancePosition(ref readPosition);
        if (maybeNull_nTableHeaderPos is not { } nTableHeaderPos)
            return null;
        var mnNumTables = headerSpan.ReadAndAdvancePosition<int>(ref readPosition);
        stream.Seek(nTableHeaderPos, SeekOrigin.Begin);
        Span<byte> stringByte = stackalloc byte[1];
        for (var i = 0; i < mnNumTables; ++i)
        {
            if (stream.Read(buffer) != 28)
                return null;
            ReadOnlySpan<byte> tableInfoSpan = buffer;
            if (tableInfoSpan.ReadOffset(0) is { } nNamePos)
            {
                stream.Seek(nNamePos, SeekOrigin.Begin);
                var stringBuffer = new ArrayBufferWriter<byte>();
                var amountRead = stream.Read(stringByte);
                while (amountRead > 0 && stringByte[0] != 0)
                {
                    stringBuffer.Write(stringByte);
                    amountRead = stream.Read(stringByte);
                }
                return Encoding.ASCII.GetString(stringBuffer.WrittenSpan);
            }
        }
        return null;
    }

    /// <inheritdoc/>
    public static new async Task<string?> GetNameAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var bufferArray = ArrayPool<byte>.Shared.Rent(28);
        try
        {
            var buffer = bufferArray.AsMemory()[..28];
            if (await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false) != 28)
                return null;
            ReadOnlyMemory<byte> headerMemory = buffer;
            var mnFileIdentifier = headerMemory[0..4];
            if (!mnFileIdentifier.Span.SequenceEqual(expectedFileIdentifier.Span))
                return null;
            var readPosition = 4U;
            var mnVersion = headerMemory.ReadAndAdvancePosition<DataModelVersion>(ref readPosition);
            if (mnVersion is < DataModelVersion.SmartSimOriginalEdition or > DataModelVersion.VariantSupport)
                return null;
            var maybeNull_nTableHeaderPos = headerMemory.ReadOffsetAndAdvancePosition(ref readPosition);
            if (maybeNull_nTableHeaderPos is not { } nTableHeaderPos)
                return null;
            var mnNumTables = headerMemory.ReadAndAdvancePosition<int>(ref readPosition);
            stream.Seek(nTableHeaderPos, SeekOrigin.Begin);
            var stringByteArray = ArrayPool<byte>.Shared.Rent(1);
            try
            {
                var stringByte = stringByteArray.AsMemory()[..1];
                for (var i = 0; i < mnNumTables; ++i)
                {
                    if (await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false) != 28)
                        return null;
                    ReadOnlyMemory<byte> tableInfoSpan = buffer;
                    if (tableInfoSpan.ReadOffset(0) is { } nNamePos)
                    {
                        stream.Seek(nNamePos, SeekOrigin.Begin);
                        var stringBuffer = new ArrayBufferWriter<byte>();
                        var amountRead = await stream.ReadAsync(stringByte, cancellationToken).ConfigureAwait(false);
                        while (amountRead > 0 && stringByte.Span[0] != 0)
                        {
                            stringBuffer.Write(stringByte.Span);
                            amountRead = await stream.ReadAsync(stringByte, cancellationToken).ConfigureAwait(false);
                        }
                        return Encoding.ASCII.GetString(stringBuffer.WrittenSpan);
                    }
                }
                return null;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(stringByteArray);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bufferArray);
        }
    }

    static object? GetValue
    (
        ReadOnlyMemory<byte> data,
        DataModelType type,
        uint valuePos,
        Func<uint, (int tableIndex, DataModelType mnDataType, int rowIndex, SchemaColumn? schemaColumn, int columnIndex)> getValueMetaDataByPosition,
        List<DataModelReference> references
    )
    {
        var dataSpan = data.Span;
        switch (type)
        {
            case DataModelType.BOOL:
                return dataSpan.Read<byte>(valuePos) != BinaryUtilities.FalseByte;
            case DataModelType.CHAR8:
                return (char)dataSpan.Read<byte>(valuePos); // watch out, CLR chars are UTF16, so decode CHAR8 as one byte, not two!
            case DataModelType.INT8:
                return dataSpan.Read<sbyte>(valuePos);
            case DataModelType.UINT8:
                return dataSpan.Read<byte>(valuePos);
            case DataModelType.INT16:
                return dataSpan.Read<short>(valuePos);
            case DataModelType.UINT16:
                return dataSpan.Read<ushort>(valuePos);
            case DataModelType.INT32:
                return dataSpan.Read<int>(valuePos);
            case DataModelType.UINT32 or DataModelType.LOCKEY:
                return dataSpan.Read<uint>(valuePos);
            case DataModelType.INT64:
                return dataSpan.Read<long>(valuePos);
            case DataModelType.UINT64 or DataModelType.TABLESETREFERENCE:
                return dataSpan.Read<ulong>(valuePos);
            case DataModelType.FLOAT:
                return dataSpan.Read<float>(valuePos);
            case DataModelType.STRING8 or DataModelType.HASHEDSTRING8:
                if (dataSpan.ReadOffset(valuePos) is { } mStringPos)
                {
                    var (stringTableIndex, _, stringTableRowIndex, _, _) = getValueMetaDataByPosition(mStringPos);
                    var @string = new DataModelString(stringTableIndex, stringTableRowIndex);
                    references.Add(@string);
                    return @string;
                }
                return DataModelString.NullReference;
            case DataModelType.OBJECT:
                if (dataSpan.ReadOffset(valuePos) is { } mObjectPos)
                {
                    var (objectTableIndex, mnDataType, objectTableRowIndex, schemaColumn, objectTableColumnIndex) = getValueMetaDataByPosition(mObjectPos);
                    var @object = new DataModelObject(objectTableIndex, objectTableRowIndex, objectTableColumnIndex, schemaColumn?.mnDataType ?? mnDataType);
                    references.Add(@object);
                    return @object;
                }
                return DataModelObject.NullReference;
            case DataModelType.VECTOR:
                var mCount = (int)dataSpan.Read<uint>(valuePos + sizeof(int));
                if (dataSpan.ReadOffset(valuePos) is { } mVectorPos)
                {
                    var (vectorTableIndex, mnDataType, vectorTableRowIndex, schemaColumn, vectorTableColumnIndex) = getValueMetaDataByPosition(mVectorPos);
                    if (vectorTableColumnIndex != 0)
                        throw new FormatException($"Vector at position {valuePos} ({valuePos:X}h) references a value in a table with non-zero column index {vectorTableColumnIndex}");
                    var vector = new DataModelVector(vectorTableIndex, vectorTableRowIndex, schemaColumn?.mnDataType ?? mnDataType, mCount);
                    references.Add(vector);
                    return vector;
                }
                return DataModelVector.CreateNullReference(mCount);
            case DataModelType.FLOAT2:
                return new float[]
                {
                    dataSpan.Read<float>(valuePos),
                    dataSpan.Read<float>(valuePos + sizeof(float))
                };
            case DataModelType.FLOAT3:
                return new float[]
                {
                    dataSpan.Read<float>(valuePos),
                    dataSpan.Read<float>(valuePos + sizeof(float)),
                    dataSpan.Read<float>(valuePos + sizeof(float) * 2)
                };
            case DataModelType.FLOAT4:
                return new float[]
                {
                    dataSpan.Read<float>(valuePos),
                    dataSpan.Read<float>(valuePos + sizeof(float)),
                    dataSpan.Read<float>(valuePos + sizeof(float) * 2),
                    dataSpan.Read<float>(valuePos + sizeof(float) * 3)
                };
            case DataModelType.RESOURCEKEY:
                var instance = dataSpan.Read<ulong>(valuePos);
                return new ResourceKey
                (
                    dataSpan.Read<ResourceType>(valuePos + sizeof(ulong)),
                    dataSpan.Read<uint>(valuePos + sizeof(ulong) + sizeof(ResourceType)),
                    instance
                );
            case DataModelType.VARIANT:
                var mTypeHash = dataSpan.Read<uint>(valuePos + sizeof(int));
                if (dataSpan.ReadOffset(valuePos) is { } mVariantPos)
                {
                    var (variantTableIndex, mnDataType, variantTableRowIndex, schemaColumn, variantTableColumnIndex) = getValueMetaDataByPosition(mVariantPos);
                    var variant = new DataModelVariant(variantTableIndex, variantTableRowIndex, variantTableColumnIndex, schemaColumn?.mnDataType ?? mnDataType, mTypeHash);
                    references.Add(variant);
                    return variant;
                }
                return DataModelVariant.CreateNullReference(mTypeHash);
            default:
                throw new NotSupportedException($"Type {type} is not supported");
        }
    }

    /// <inheritdoc/>
    public static new ISet<ResourceType> SupportedTypes =>
        supportedTypes;

    /// <inheritdoc/>
    public override string? ResourceName
    {
        get
        {
            foreach (var table in Tables)
                if (table.Name is not null)
                    return table.Name;
            return null;
        }
    }

    /// <summary>
    /// Gets the tables in this resource
    /// </summary>
    public Collection<DataModelTable> Tables { get; } = [];

    /// <summary>
    /// Gets/sets the <see cref="DataModelVersion"/> of this resource
    /// </summary>
    public DataModelVersion Version { get; set; }

    /// <summary>
    /// Gets the table at the specified <paramref name="index"/>
    /// </summary>
    public DataModelTable this[int index] =>
        Tables[index];

    /// <summary>
    /// Gets the table with the specified <paramref name="name"/>
    /// </summary>
    public DataModelTable this[string name] =>
        GetTable(name);

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Encode()
    {
        var version = Version;
        if (version is < DataModelVersion.SmartSimOriginalEdition or > DataModelVersion.VariantSupport)
            throw new InvalidOperationException($"Cannot encode a data resource of version {version}");
        var supportVariants = version >= DataModelVersion.VariantSupport;
        if (!supportVariants && Tables.Any(table => Enumerable.Range(0, table.ColumnCount).Any(c => table.GetColumnType(c) is DataModelType.VARIANT)))
            throw new InvalidOperationException($"Cannot encode a data resource of version {version} with {DataModelType.VARIANT} columns");
        var writer = new ArrayBufferWriter<byte>
        (
            (
                  4 // mnFileIdentifier
                + sizeof(uint) // mnVersion
                + sizeof(int) // mnTableHeaderOffset
                + sizeof(uint) // mnNumTables
                + sizeof(int) // mnSchemaOffset
                + sizeof(uint) // mnNumSchemas
                +
                (
                      supportVariants
                    ? sizeof(uint) // mUnused
                    : 0
                )
            ).AlignForNextBlock()
            +
            (
                (
                      sizeof(int) // mnNameOffset
                    + sizeof(uint) // mnNameHash
                    + sizeof(int) // mnSchemaOffset
                    + sizeof(uint) // mnDataType
                    + sizeof(uint) // mnRowSize
                    + sizeof(int) // mnRowOffset
                    + sizeof(uint) // mnRowCount
                )
                * Tables.Count
            ).AlignForNextBlock()
            +
            Tables.Sum(table => ((int)(table.RowSize * table.RowCount)).AlignForNextBlock())
            +
            Tables.Sum
            (
                table =>
                  table.SchemaHash is not null
                ? sizeof(int) // mnNameOffset
                + sizeof(uint) // mnNameHash
                + sizeof(uint) // mnSchemaHash
                + sizeof(uint) // mnSchemaSize
                + sizeof(int) // mnColumnOffset
                + sizeof(uint) // mnNumColumns
                + table.ColumnCount
                *
                (
                      sizeof(int) // mnNameOffset
                    + sizeof(uint) // mnNameHash
                    + sizeof(ushort) // mnDataType
                    + sizeof(ushort) // mnFlags
                    + sizeof(uint) // mnOffset
                    + sizeof(int) // mnSchemaOffset
                )
                : 0
            )
            +
            Tables.Sum
            (
                table =>
                  (table.Name is { } tableName ? tableName.Length + 1 : 0)
                +
                (
                      table.SchemaHash is not null
                    ? (table.SchemaName?.Length ?? 0) + 1
                    + table.ColumnNamesStringTableEntriesLength
                    : 0
                )
            )
        );
        var nullOffset = BinaryUtilities.NullOffset;
        var trueByte = BinaryUtilities.TrueByte;
        var falseByte = BinaryUtilities.FalseByte;
        var nullShort = (short)0;
        var nullUShort = (ushort)0;
        var nullInt = 0;
        var nullUInt = 0U;
        var nullLong = 0L;
        var nullULong = 0UL;
        var nullFloat = 0F;
        var nullStringHash = Fnv32.GetHash(null);
        ResourceType nullResourceType = default;
        var pendingReferenceOffsets = new List<PendingReferenceOffset>();
        var pendingRowOffsets = new Queue<Memory<byte>>();
        var pendingSchemaOffsets = new Queue<Memory<byte>>();
        var pendingStringOffsets = new Queue<PendingStringOffset>();
        var valuePositionByIndicies = new Dictionary<(int tableIndex, int rowIndex, int columnIndex), int>();
        static void writeStringOffsetToMemory(Memory<byte> offsetMemory, Queue<PendingStringOffset> pendingStringOffsets, string? text) =>
            pendingStringOffsets.Enqueue(new PendingStringOffset(text, offsetMemory));
        static void writeStringOffsetToWriter(ArrayBufferWriter<byte> writer, Queue<PendingStringOffset> pendingStringOffsets, string? text) =>
            writeStringOffsetToMemory(writer.GetOffsetMemoryAndAdvance(), pendingStringOffsets, text);
        static void writeHashedStringOffsetToWriter(ArrayBufferWriter<byte> writer, Queue<PendingStringOffset> pendingStringOffsets, string? text)
        {
            writeStringOffsetToWriter(writer, pendingStringOffsets, text);
            var mnHash = Fnv32.GetHash(text);
            writer.Write(ref mnHash);
        }
        static void writeFloats(Memory<byte> cell, IEnumerable<float>? floatEnumerable, int length)
        {
            using var enumerator = (floatEnumerable ?? []).GetEnumerator();
            for (var i = 0; i < length; ++i)
            {
                var element = enumerator.MoveNext() ? enumerator.Current : 0F;
                MemoryMarshal.Write(cell[(i * 4)..((i + 1) * 4)].Span, ref element);
            }
        }
        writer.Write(expectedFileIdentifier.Span);
        writer.Write(ref version);
        var mnTableHeaderOffset = writer.GetOffsetSpanAndAdvance();
        var mnNumTables = Tables.Count;
        writer.Write(ref mnNumTables);
        var mnSchemaOffset = writer.GetOffsetSpanAndAdvance();
        var mnNumSchemas = Tables.Count(table => table.SchemaHash is not null);
        writer.Write(ref mnNumSchemas);
        if (supportVariants)
        {
            var mUnused = uint.MaxValue;
            writer.Write(ref mUnused);
        }
        writer.AlignForNextBlock();
        mnTableHeaderOffset.WriteOffset(writer);
        var tablesAndOriginalIndexesInEncodingOrder = Tables
            .Select((table, index) => (table, index))
            .OrderBy(t => t.table.ColumnCount is not 1 ? DataModelType.OBJECT : t.table.GetColumnType(0))
            .GroupBy(t => t.table.SchemaHash is null)
            .OrderBy(group => group.Key)
            .SelectMany(group => group).ToImmutableArray();
        foreach (var (table, _) in tablesAndOriginalIndexesInEncodingOrder)
        {
            writeHashedStringOffsetToWriter(writer, pendingStringOffsets, table.Name);
            if (table.SchemaHash is null)
                writer.Write(ref nullOffset);
            else
                pendingSchemaOffsets.Enqueue(writer.GetOffsetMemoryAndAdvance());
            var mnDataType = table.ColumnCount == 1
                ? table.GetColumnType(0)
                : DataModelType.OBJECT;
            writer.Write(ref mnDataType);
            var mnRowSize = table.RowSize;
            writer.Write(ref mnRowSize);
            pendingRowOffsets.Enqueue(writer.GetOffsetMemoryAndAdvance());
            var mnRowCount = table.RowCount;
            writer.Write(ref mnRowCount);
        }
        foreach (var (table, tableIndex) in tablesAndOriginalIndexesInEncodingOrder)
        {
            writer.AlignForNextBlock();
            pendingRowOffsets.Dequeue().WriteOffset(writer);
            var columnRanges = table.ColumnRanges;
            var columnTypes = table.ColumnTypes;
            var rowSize = (int)table.RowSize;
            object? value = null;
            for (var rowIndex = 0; rowIndex < table.RowCount; ++rowIndex)
            {
                var rowPosition = writer.WrittenCount;
                var row = writer.GetMemoryAndAdvance(rowSize);
                for (var columnIndex = 0; columnIndex < table.ColumnCount; ++columnIndex)
                {
                    var range = columnRanges[columnIndex];
                    var cell = row[range];
                    valuePositionByIndicies.Add((tableIndex, rowIndex, columnIndex), rowPosition + range.Start.GetOffset(rowSize));
                    value = table.Get(rowIndex, columnIndex);
                    var type = columnTypes[columnIndex];
                    switch (type)
                    {
                        case DataModelType.BOOL:
                            if (value is bool boolean && boolean)
                                MemoryMarshal.Write(cell.Span, ref trueByte);
                            else
                                MemoryMarshal.Write(cell.Span, ref falseByte);
                            break;
                        case DataModelType.CHAR8:
                            if (value is char c)
                            {
                                // watch out, CLR chars are UTF16, so encode CHAR8 as one byte, not two!
                                var cByte = (byte)c;
                                MemoryMarshal.Write(cell.Span, ref cByte);
                            }
                            else
                                MemoryMarshal.Write(cell.Span, ref falseByte);
                            break;
                        case DataModelType.INT8:
                            if (value is sbyte sb)
                                MemoryMarshal.Write(cell.Span, ref sb);
                            else
                                MemoryMarshal.Write(cell.Span, ref falseByte);
                            break;
                        case DataModelType.UINT8:
                            if (value is byte b)
                                MemoryMarshal.Write(cell.Span, ref b);
                            else
                                MemoryMarshal.Write(cell.Span, ref falseByte);
                            break;
                        case DataModelType.INT16:
                            if (value is short s)
                                MemoryMarshal.Write(cell.Span, ref s);
                            else
                                MemoryMarshal.Write(cell.Span, ref nullShort);
                            break;
                        case DataModelType.UINT16:
                            if (value is ushort us)
                                MemoryMarshal.Write(cell.Span, ref us);
                            else
                                MemoryMarshal.Write(cell.Span, ref nullUShort);
                            break;
                        case DataModelType.INT32:
                            if (value is int i)
                                MemoryMarshal.Write(cell.Span, ref i);
                            else
                                MemoryMarshal.Write(cell.Span, ref nullInt);
                            break;
                        case DataModelType.UINT32 or DataModelType.LOCKEY:
                            if (value is uint ui)
                                MemoryMarshal.Write(cell.Span, ref ui);
                            else
                                MemoryMarshal.Write(cell.Span, ref nullUInt);
                            break;
                        case DataModelType.INT64:
                            if (value is long l)
                                MemoryMarshal.Write(cell.Span, ref l);
                            else
                                MemoryMarshal.Write(cell.Span, ref nullLong);
                            break;
                        case DataModelType.UINT64 or DataModelType.TABLESETREFERENCE:
                            if (value is ulong ul)
                                MemoryMarshal.Write(cell.Span, ref ul);
                            else
                                MemoryMarshal.Write(cell.Span, ref nullULong);
                            break;
                        case DataModelType.FLOAT:
                            if (value is float f)
                                MemoryMarshal.Write(cell.Span, ref f);
                            else
                                MemoryMarshal.Write(cell.Span, ref nullFloat);
                            break;
                        case DataModelType.STRING8:
                            pendingReferenceOffsets.Add(new PendingReferenceOffset(value as DataModelString ?? DataModelString.NullReference, cell));
                            break;
                        case DataModelType.HASHEDSTRING8:
                            var @string = value as DataModelString ?? DataModelString.NullReference;
                            pendingReferenceOffsets.Add(new PendingReferenceOffset(@string, cell[0..4]));
                            var mHash = Fnv32.GetHash(@string.Value);
                            MemoryMarshal.Write(cell[4..8].Span, ref mHash);
                            break;
                        case DataModelType.OBJECT:
                            pendingReferenceOffsets.Add(new PendingReferenceOffset(value as DataModelObject ?? DataModelObject.NullReference, cell));
                            break;
                        case DataModelType.VECTOR:
                            var vector = value as DataModelVector ?? DataModelVector.CreateNullReference(default);
                            pendingReferenceOffsets.Add(new PendingReferenceOffset(vector, cell[0..4]));
                            var mCount = (uint)vector.Count;
                            MemoryMarshal.Write(cell[4..8].Span, ref mCount);
                            break;
                        case DataModelType.FLOAT2:
                            writeFloats(cell, value as IEnumerable<float>, 2);
                            break;
                        case DataModelType.FLOAT3:
                            writeFloats(cell, value as IEnumerable<float>, 3);
                            break;
                        case DataModelType.FLOAT4:
                            writeFloats(cell, value as IEnumerable<float>, 4);
                            break;
                        case DataModelType.RESOURCEKEY:
                            var keyInstance = nullULong;
                            var keyType = nullResourceType;
                            var keyGroup = nullUInt;
                            if (value is ResourceKey resourceKey)
                                (keyType, keyGroup, keyInstance) = resourceKey;
                            MemoryMarshal.Write(cell[0..8].Span, ref keyInstance);
                            MemoryMarshal.Write(cell[8..12].Span, ref keyType);
                            MemoryMarshal.Write(cell[12..16].Span, ref keyGroup);
                            break;
                        case DataModelType.VARIANT:
                            var variant = value as DataModelVariant ?? DataModelVariant.CreateNullReference(default);
                            pendingReferenceOffsets.Add(new PendingReferenceOffset(variant, cell[0..4]));
                            var mTypeHash = variant.TypeHash;
                            MemoryMarshal.Write(cell[4..8].Span, ref mTypeHash);
                            break;
                        default:
                            throw new NotSupportedException($"Type {type} is not supported");
                    }
                }
            }
        }
        Span<byte> nullOffsetSpan = stackalloc byte[sizeof(int)];
        MemoryMarshal.Write(nullOffsetSpan, ref nullOffset);
        var tableIndicies = tablesAndOriginalIndexesInEncodingOrder.ToImmutableDictionary(t => t.table, t => t.index);
        foreach (var (reference, offsetMemory) in pendingReferenceOffsets)
            if (reference.IsValid
                && tableIndicies.TryGetValue(reference.Table, out var tableIndex)
                && valuePositionByIndicies.TryGetValue((tableIndex, reference.Row, reference.Column), out var position))
                offsetMemory.WriteOffset(writer, position);
            else
                nullOffsetSpan.CopyTo(offsetMemory.Span);
        var pendingColumnOffsets = new Queue<Memory<byte>>();
        writer.AlignForNextBlock();
        mnSchemaOffset.WriteOffset(writer);
        foreach (var (table, _) in tablesAndOriginalIndexesInEncodingOrder.Where(t => t.table.SchemaHash is not null))
        {
            pendingSchemaOffsets.Dequeue().WriteOffset(writer);
            writeHashedStringOffsetToWriter(writer, pendingStringOffsets, table.SchemaName);
            var mnSchemaHash = table.SchemaHash!.Value;
            writer.Write(ref mnSchemaHash);
            var mnSchemaSize = table.RowSize;
            writer.Write(ref mnSchemaSize);
            pendingColumnOffsets.Enqueue(writer.GetOffsetMemoryAndAdvance());
            var mnNumColumns = (uint)table.ColumnCount;
            writer.Write(ref mnNumColumns);
        }
        foreach (var (table, _) in tablesAndOriginalIndexesInEncodingOrder.Where(t => t.table.SchemaHash is not null))
        {
            pendingColumnOffsets.Dequeue().WriteOffset(writer);
            var schemaSize = (int)table.RowSize;
            var columnRanges = table.ColumnRanges;
            for (var c = 0; c < table.ColumnCount; ++c)
            {
                var name = table.GetColumnName(c) ?? string.Empty;
                writeHashedStringOffsetToWriter(writer, pendingStringOffsets, name);
                var mnDataType = (ushort)table.GetColumnType(c);
                writer.Write(ref mnDataType);
                var mnFlags = table.GetColumnFlags(c);
                writer.Write(ref mnFlags);
                var mnOffset = (uint)columnRanges[c].Start.GetOffset(schemaSize);
                writer.Write(ref mnOffset);
                writer.Write(ref nullOffset); // mnSchemaOffset
            }
        }
        while (pendingStringOffsets.TryDequeue(out var pendingStringOffset))
        {
            var (text, offsetMemory) = pendingStringOffset;
            if (text is null)
            {
                nullOffsetSpan.CopyTo(offsetMemory.Span);
                continue;
            }
            offsetMemory.WriteOffset(writer);
            writer.Write(Encoding.ASCII.GetBytes(text));
            writer.Write(ref falseByte); // stand in for C_STR null terminator
        }
        return writer.WrittenMemory;
    }

    /// <summary>
    /// Gets the table with the specified <paramref name="name"/>
    /// </summary>
    public DataModelTable GetTable(string name) =>
        Tables.Single(table => table.Name == name);
}
