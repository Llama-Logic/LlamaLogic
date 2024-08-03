namespace LlamaLogic.Packages;

/// <summary>
/// Represents a data resource in a package
/// </summary>
public class DataResource
{
    /// <summary>
    /// Initializes a new data resource
    /// </summary>
    public DataResource()
    {
        Schemas = [];
        Tables = [];
    }

    /// <summary>
    /// Initializes a new data resource using the specified <paramref name="data"/>
    /// </summary>
    /// <exception cref="FormatException">A schema's fields offset is invalid</exception>
    /// <exception cref="NotSupportedException">An unsupported data type was encountered</exception>
    public DataResource(ReadOnlyMemory<byte> data) :
        this()
    {
        var dataSpan = data.Span;
        var readPosition = 0U;
        dataSpan.ReadBinaryStructAndAdvancePosition<BinaryDataResourceHeader>(out var binaryHeader, ref readPosition);
        binaryHeader.Read(in readPosition, out var tablesPosition, out var schemasPosition);
        readPosition = schemasPosition;
        var schemaByPosition = new Dictionary<uint, DataResourceSchema>();
        var fieldCharacteristicsBySchemaIndex = new List<(uint fieldsPosition, uint fieldCount)>();
        var dataOffsetByField = new Dictionary<DataResourceField, uint>();
        for (var s = 0; s < binaryHeader.SchemaCount; ++s)
        {
            var schemaPosition = readPosition;
            var binarySchemaHeader = BinarySchemaHeader.Read(in dataSpan, ref readPosition, out var name, out var fieldsPosition);
            var schema = new DataResourceSchema(name, binarySchemaHeader.SchemaHash);
            Schemas.Add(schema);
            schemaByPosition.Add(schemaPosition, schema);
            if (binarySchemaHeader.FieldCount > 0)
                fieldCharacteristicsBySchemaIndex.Add((fieldsPosition ?? throw new FormatException("invalid fields offset"), binarySchemaHeader.FieldCount));
        }
        for (var s = 0; s < fieldCharacteristicsBySchemaIndex.Count; ++s)
        {
            var fields = Schemas[s].Fields;
            var (fieldsPosition, fieldCount) = fieldCharacteristicsBySchemaIndex[s];
            readPosition = fieldsPosition;
            for (var f = 0; f < fieldCount; ++f)
            {
                var schemaField = BinarySchemaField.Read(in dataSpan, ref readPosition, out var name, out var schemaPosition);
                var field = new DataResourceField
                (
                    name,
                    schemaField.Type,
                    schemaField.Flags
                );
                fields.Add(field);
                dataOffsetByField.Add(field, schemaField.DataOffset);
            }
        }
        readPosition = tablesPosition;
        for (var t = 0; t < binaryHeader.TableCount; ++t)
        {
            var binaryTableHeader = BinaryTableHeader.Read(in dataSpan, ref readPosition, out var name, out var schemaPosition, out var type, out var rowsPosition);
            DataResourceSchema? schema = null;
            if (schemaPosition is { } nonNullSchemaPosition)
                schema = schemaByPosition[nonNullSchemaPosition];
            var offsetOrderedFields = schema?.Fields
                .Select(field => (field, dataOffset: dataOffsetByField[field]))
                .OrderBy(t => t.dataOffset)
                .Select(t => t.field)
                .ToList()
                .AsReadOnly();
            DataResourceTable table = schema is null
                ? new DataResourceGeneralTable(name, type)
                : new DataResourceSchematicTable(name, schema, type);
            Tables.Add(table);
            var columnCount = offsetOrderedFields?.Count ?? ((int)binaryTableHeader.RowSize / type.GetSize());
            var rowAlignmentPadding = schema is null ? 0U : schema.GetSize() - (uint)schema.Fields.Sum(field => field.Type.GetSize());
            var positionAtEndOfTableHeader = readPosition;
            readPosition = rowsPosition;
            for (var r = 0; r < binaryTableHeader.RowCount; ++r)
            {
                DataResourceRow row = schema is null
                    ? new DataResourceGeneralRow()
                    : new DataResourceSchematicRow();
                table.Rows.Add(row);
                for (var c = 0; c < columnCount; ++c)
                {
                    object? value = null;
                    var scalarType = offsetOrderedFields?[c].Type ?? type;
                    if (scalarType is DataResourceType.Boolean)
                        value = dataSpan.ReadAndAdvancePosition<byte>(ref readPosition) != BinaryUtilities.FalseByte;
                    else if (scalarType is DataResourceType.Character)
                        value = dataSpan.ReadAndAdvancePosition<char>(ref readPosition);
                    else if (scalarType is DataResourceType.SignedByte)
                        value = dataSpan.ReadAndAdvancePosition<sbyte>(ref readPosition);
                    else if (scalarType is DataResourceType.Byte)
                        value = dataSpan.ReadAndAdvancePosition<byte>(ref readPosition);
                    else if (scalarType is DataResourceType.ShortInteger)
                        value = dataSpan.ReadAndAdvancePosition<short>(ref readPosition);
                    else if (scalarType is DataResourceType.UnsignedShortInteger)
                        value = dataSpan.ReadAndAdvancePosition<ushort>(ref readPosition);
                    else if (scalarType is DataResourceType.Integer)
                        value = dataSpan.ReadAndAdvancePosition<int>(ref readPosition);
                    else if (scalarType is DataResourceType.UnsignedInteger
                        or DataResourceType.Object
                        or DataResourceType.LocalizationKey)
                        value = dataSpan.ReadAndAdvancePosition<uint>(ref readPosition);
                    else if (scalarType is DataResourceType.LongInteger)
                        value = dataSpan.ReadAndAdvancePosition<long>(ref readPosition);
                    else if (scalarType is DataResourceType.UnsignedLongInteger
                        or DataResourceType.TableSetReference)
                        value = dataSpan.ReadAndAdvancePosition<ulong>(ref readPosition);
                    else if (scalarType is DataResourceType.FloatingPoint)
                        value = dataSpan.ReadAndAdvancePosition<float>(ref readPosition);
                    else if (scalarType is DataResourceType.String)
                    {
                        BinaryString.Read(in dataSpan, ref readPosition, out var str);
                        value = str;
                    }
                    else if (scalarType is DataResourceType.HashedString)
                    {
                        BinaryHashedString.Read(dataSpan, ref readPosition, out var str);
                        value = str;
                    }
                    else if (scalarType is DataResourceType.Vector)
                        value = BinaryVector.Read(in dataSpan, ref readPosition);
                    else if (scalarType is DataResourceType.FloatingPoint2)
                        value = BinaryFloatingPoint2.Read(in dataSpan, ref readPosition);
                    else if (scalarType is DataResourceType.FloatingPoint3)
                        value = BinaryFloatingPoint3.Read(in dataSpan, ref readPosition);
                    else if (scalarType is DataResourceType.FloatingPoint4)
                        value = BinaryFloatingPoint4.Read(in dataSpan, ref readPosition);
                    else if (scalarType is DataResourceType.ResourceKey)
                        value = BinaryDataResourceKey.Read(in dataSpan, ref readPosition);
                    else
                        throw new NotSupportedException($"cannot decode type {scalarType}");
                    if (schema is null)
                        ((DataResourceGeneralRow)row).Values.Add(value);
                    else
                        ((DataResourceSchematicRow)row).Values.Add(offsetOrderedFields![c], value);
                }
                readPosition += rowAlignmentPadding;
            }
        }
    }

    /// <summary>
    /// Gets the schemas in the data resource
    /// </summary>
    public Collection<DataResourceSchema> Schemas { get; }

    /// <summary>
    /// Gets the tables in the data resource
    /// </summary>
    public Collection<DataResourceTable> Tables { get; }

    /// <summary>
    /// Encodes the data resource into binary format
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public ReadOnlyMemory<byte> Encode()
    {
        var schemaSizes = Schemas.ToDictionary(schema => schema, schema => schema.GetSize());

        byte[] stringTerminator = [0];
        var stringTable = Tables
            .SelectMany(table => table.GetNonNullStringsEnumerator())
            .Concat(Schemas.SelectMany(schema => schema.GetNonNullStringsEnumerator()))
            .Distinct()
            .ToDictionary(str => str, str => (Memory<byte>)Encoding.ASCII.GetBytes(str).Concat(stringTerminator).ToArray());

        var totalSize =
              sizeof(byte) * 4 // preamble
            + sizeof(uint) // version
            + sizeof(uint) // tables position
            + sizeof(uint) // table count
            + sizeof(uint) // schemas position
            + sizeof(uint) // schema count
            + sizeof(uint) // ?
            + sizeof(uint) // ?
            + Tables.Sum
            (
                table =>
                  sizeof(uint) // name offset
                + sizeof(uint) // name hash
                + sizeof(uint) // schema offset
                + sizeof(uint) // type
                + sizeof(uint) // row size
                + sizeof(uint) // rows offset
                + sizeof(uint) // row count
                + (table is DataResourceGeneralTable
                 ? table.Type.GetSize() * table.Rows.Cast<DataResourceGeneralRow>().Max(row => row.Values.Count)
                 : 0)
                + (table is DataResourceSchematicTable schematicTable
                 ? schemaSizes[schematicTable.Schema] * table.Rows.Count
                 : 0)
            )
            + sizeof(ulong) // post table headers maximum alignment padding
            + Schemas.Sum
            (
                schema =>
                  sizeof(uint) // name offset
                + sizeof(uint) // name hash
                + sizeof(uint) // schema hash
                + sizeof(uint) // size
                + sizeof(uint) // fields offset
                + sizeof(uint) // field count
                + schema.Fields.Sum
                (
                    field =>
                      sizeof(uint) // name offset
                    + sizeof(uint) // name hash
                    + sizeof(DataResourceType) // type
                    + sizeof(ushort) // flags
                    + sizeof(uint) // data offset
                    + sizeof(uint) // schema offset
                )
            )
            + stringTable.Values.Sum(encodedString => encodedString.Length);

        var stringContainers = new List<(string? str, IStringOffsetContainer container, Memory<byte> containerMemory, Range containerMemoryRange)>();
        var schematicTableSchemaOffsets = new Dictionary<DataResourceSchema, List<(BinaryTableHeader schematicTableHeader, Range schematicTableHeaderMemoryRange)>>();
        var tableRowsOffsets = new Dictionary<DataResourceTable, (BinaryTableHeader tableHeader, Range tableHeaderMemoryRange)>();
        var encoding = new ArrayBufferWriter<byte>((int)totalSize);
        BinaryDataResourceHeader.CreateForSerialization(encoding, out var header, out var headerSpan, out var headerSpanRange);
        header.TableCount = (uint)Tables.Count;
        header.SchemaCount = (uint)Schemas.Count;
        header.SetTablesOffset(encoding, in headerSpanRange);
        foreach (var table in Tables)
        {
            BinaryTableHeader.CreateForSerializationAsMemory(encoding, out var tableHeader, out var tableHeaderMemory, out var tableHeaderMemoryRange);
            stringContainers.Add((table.Name, tableHeader, tableHeaderMemory, tableHeaderMemoryRange));
            var tableHeaderAndTableHeaderMemoryRange = (tableHeader, tableHeaderMemoryRange);
            DataResourceSchema? schema = null;
            if (table is DataResourceSchematicTable schematicTable)
            {
                schema = schematicTable.Schema;
#if IS_NET_6_0_OR_GREATER
                ref var thisSchemaOffsets = ref CollectionsMarshal.GetValueRefOrAddDefault(schematicTableSchemaOffsets, schema, out var thisSchemaOffsetsExisted);
                if (!thisSchemaOffsetsExisted)
                    thisSchemaOffsets = [tableHeaderAndTableHeaderMemoryRange];
                else
                    thisSchemaOffsets!.Add(tableHeaderAndTableHeaderMemoryRange);
#else
                if (schematicTableSchemaOffsets.TryGetValue(schema, out var thisSchemaOffsets))
                    thisSchemaOffsets.Add(tableHeaderAndTableHeaderMemoryRange);
                else
                    schematicTableSchemaOffsets.Add(schema, [tableHeaderAndTableHeaderMemoryRange]);
#endif
            }
            tableHeader.Type = (uint)table.Type;
            tableHeader.RowSize = schema is null
                ? table.Type.GetSize() * (uint)table.Rows.Cast<DataResourceGeneralRow>().Max(row => row.Values.Count)
                : schemaSizes[schema];
            tableHeader.RowCount = (uint)table.Rows.Count;
            tableRowsOffsets.Add(table, tableHeaderAndTableHeaderMemoryRange);
        }
        encoding.AlignForFirstRow(out _);
        var dataOffsetByField = new Dictionary<DataResourceField, uint>();
        foreach (var schema in Schemas)
        {
            var cuumulativeDataOffset = 0U;
            foreach (var field in schema.Fields.OrderBy(field => field.Name))
            {
                dataOffsetByField.Add(field, cuumulativeDataOffset);
                cuumulativeDataOffset += field.Type.GetSize();
            }
        }
        foreach (var table in Tables
            .GroupBy(table => table.GetType())
            .Select(tablesGroupedByType => (type: tablesGroupedByType.Key, tables: (IEnumerable<DataResourceTable>)tablesGroupedByType))
            .Select(tablesGroupedByTypeTuple =>
                  tablesGroupedByTypeTuple.type == typeof(DataResourceSchematicTable)
                ? (tablesGroupedByTypeTuple.type, tables: tablesGroupedByTypeTuple.tables.Cast<DataResourceSchematicTable>().OrderBy(table => Schemas.IndexOf(table.Schema)))
                : tablesGroupedByTypeTuple)
            .Select(tablesGroupedByTypeTuple => (sortOrder: tablesGroupedByTypeTuple.type == typeof(DataResourceSchematicTable) ? 0 : 1, tablesGroupedByTypeTuple.tables))
            .OrderBy(tablesGroupedByTypeTuple => tablesGroupedByTypeTuple.sortOrder)
            .SelectMany(tablesGroupedByTypeTuple => tablesGroupedByTypeTuple.tables))
        {
            encoding.AlignForFirstRow(out _);
            var (tableHeader, tableHeaderMemoryRange) = tableRowsOffsets[table];
            tableHeader.SetRowsOffset(encoding, in tableHeaderMemoryRange);
            IReadOnlyList<DataResourceType> rowTypes;
            Func<DataResourceRow, IEnumerable<object?>> getOrderedRowValues;
            var rowAlignmentPadding = 0;
            if (table is DataResourceGeneralTable generalTable)
            {
                var columnCount = generalTable.Rows.Cast<DataResourceGeneralRow>().Max(row => row.Values.Count);
                var rowTypesList = new List<DataResourceType>();
                for (var c = 0; c < columnCount; ++c)
                    rowTypesList.Add(generalTable.Type);
                rowTypes = rowTypesList.ToList().AsReadOnly();
                getOrderedRowValues = row => ((DataResourceGeneralRow)row).Values;
            }
            else if (table is DataResourceSchematicTable schematicTable)
            {
                var schema = schematicTable.Schema;
                rowTypes = schema.Fields.OrderBy(field => dataOffsetByField[field]).Select(field => field.Type).ToList().AsReadOnly();
                getOrderedRowValues = row => ((DataResourceSchematicRow)row).Values.OrderBy(kv => dataOffsetByField[kv.Key]).Select(kv => kv.Value);
                rowAlignmentPadding = (int)schema.GetSize() - (int)schema.Fields.Sum(field => field.Type.GetSize());
            }
            else
                throw new NotSupportedException($"{table?.GetType().Name} table is not supported");
            foreach (var (row, rowIndex) in table.Rows.Select((row, index) => (row, index)))
            {
                var values = getOrderedRowValues(row).ToList();
                while (values.Count < rowTypes.Count)
                    values.Add(null);
                var readOnlyValues = values.AsReadOnly();
                if (readOnlyValues.Count > rowTypes.Count)
                    throw new InvalidOperationException($"table {table.Name} row {rowIndex} has too many values");
                for (var columnIndex = 0; columnIndex < readOnlyValues.Count; ++columnIndex)
                {
                    var scalarType = rowTypes[columnIndex];
                    var value = readOnlyValues[columnIndex];
                    if (scalarType is DataResourceType.Boolean)
                    {
                        var typed = value is bool b && b ? BinaryUtilities.TrueByte : BinaryUtilities.FalseByte;
                        encoding.Write(ref typed);
                    }
                    else if (scalarType is DataResourceType.Character)
                    {
                        var typed = value is char c ? c : (char)0;
                        encoding.Write(ref typed);
                    }
                    else if (scalarType is DataResourceType.SignedByte)
                    {
                        var typed = value is sbyte sb ? sb : (sbyte)0;
                        encoding.Write(ref typed);
                    }
                    else if (scalarType is DataResourceType.Byte)
                    {
                        var typed = value is byte b ? b : (byte)0;
                        encoding.Write(ref typed);
                    }
                    else if (scalarType is DataResourceType.ShortInteger)
                    {
                        var typed = value is short s ? s : (short)0;
                        encoding.Write(ref typed);
                    }
                    else if (scalarType is DataResourceType.UnsignedShortInteger)
                    {
                        var typed = value is ushort us ? us : (ushort)0;
                        encoding.Write(ref typed);
                    }
                    else if (scalarType is DataResourceType.Integer)
                    {
                        var typed = value is int i ? i : 0;
                        encoding.Write(ref typed);
                    }
                    else if (scalarType is DataResourceType.UnsignedInteger
                        or DataResourceType.Object
                        or DataResourceType.LocalizationKey)
                    {
                        var typed = value is uint ui ? ui : 0;
                        encoding.Write(ref typed);
                    }
                    else if (scalarType is DataResourceType.LongInteger)
                    {
                        var typed = value is long l ? l : 0;
                        encoding.Write(ref typed);
                    }
                    else if (scalarType is DataResourceType.UnsignedLongInteger
                        or DataResourceType.TableSetReference)
                    {
                        var typed = value is ulong ul ? ul : 0;
                        encoding.Write(ref typed);
                    }
                    else if (scalarType is DataResourceType.FloatingPoint)
                    {
                        var typed = value is float f ? f : 0;
                        encoding.Write(ref typed);
                    }
                    else if (scalarType is DataResourceType.String)
                    {
                        BinaryString.CreateForSerialization(encoding, out var str, out var stringMemory, out var stringMemoryRange);
                        stringContainers.Add((value as string, str, stringMemory, stringMemoryRange));
                    }
                    else if (scalarType is DataResourceType.HashedString)
                    {
                        BinaryHashedString.CreateForSerialization(encoding, out var hashedString, out var hashedStringMemory, out var hashedStringMemoryRange);
                        stringContainers.Add((value as string, hashedString, hashedStringMemory, hashedStringMemoryRange));
                    }
                    else if (scalarType is DataResourceType.Vector)
                        BinaryVector.Write(value as DataResourceVectorValue, encoding);
                    else if (scalarType is DataResourceType.FloatingPoint2)
                        BinaryFloatingPoint2.Write(value as DataResourceFloatingPoint2Value, encoding);
                    else if (scalarType is DataResourceType.FloatingPoint3)
                        BinaryFloatingPoint3.Write(value as DataResourceFloatingPoint3Value, encoding);
                    else if (scalarType is DataResourceType.FloatingPoint4)
                        BinaryFloatingPoint4.Write(value as DataResourceFloatingPoint4Value, encoding);
                    else if (scalarType is DataResourceType.ResourceKey)
                        BinaryDataResourceKey.Write(value is PackageResourceKey key ? key : default, encoding);
                    else
                        throw new InvalidOperationException($"cannot encode type {scalarType}");
                }
                if (rowAlignmentPadding > 0)
                    encoding.Advance(rowAlignmentPadding);
            }
        }
        header.SetSchemasOffset(encoding, in headerSpanRange);
        headerSpan.WriteBinaryStruct(ref header);
        var schemaFieldsOffsets = new Dictionary<DataResourceSchema, (BinarySchemaHeader schemaHeader, Range schemaHeaderMemoryRange)>();
        foreach (var schema in Schemas)
        {
            if (schematicTableSchemaOffsets.TryGetValue(schema, out var schematicTablesAndMemoryRanges))
                foreach (var (schematicTableHeader, schematicTableHeaderMemoryRange) in schematicTablesAndMemoryRanges)
                    schematicTableHeader.SetSchemaOffset(encoding, in schematicTableHeaderMemoryRange);
            BinarySchemaHeader.CreateForSerialization(encoding, out var schemaHeader, out var schemaHeaderMemory, out var schemaHeaderMemoryRange);
            stringContainers.Add((schema.Name, schemaHeader, schemaHeaderMemory, schemaHeaderMemoryRange));
            schemaHeader.SchemaHash = schema.Hash;
            schemaHeader.Size = schemaSizes[schema];
            schemaFieldsOffsets.Add(schema, (schemaHeader, schemaHeaderMemoryRange));
            schemaHeader.FieldCount = (uint)schema.Fields.Count;
        }
        foreach (var schema in Schemas)
        {
            var (schemaHeader, schemaHeaderMemoryRange) = schemaFieldsOffsets[schema];
            schemaHeader.SetFieldsOffset(encoding, in schemaHeaderMemoryRange);
            foreach (var field in schema.Fields
                .Select(field =>
                (
                    field,
                    name: field.Name,
                    type: field.Type,
                    flags: field.Flags,
                    size: field.Type.GetSize()
                ))
                .OrderBy(field => Fnv32.GetHash(field.name)))
            {
                BinarySchemaField.CreateForSerialization(encoding, out var schemaField, out var binarySchemaFieldMemory, out var binarySchemaFieldMemoryRange);
                stringContainers.Add((field.name, schemaField, binarySchemaFieldMemory, binarySchemaFieldMemoryRange));
                schemaField.Type = field.type;
                schemaField.Flags = field.flags;
                schemaField.DataOffset = dataOffsetByField[field.field];
            }
        }
        if (stringTable.Count > 0)
        {
            foreach (var containerGroup in stringContainers.GroupBy(t => t.str))
            {
                if (containerGroup.Key is { } str)
                {
                    var cStr = stringTable[str];
                    var containerGroupHashNecessityLookup = containerGroup.ToLookup(t => t.container.IsExpectingHash);
                    foreach (var (_, container, containerMemory, containerMemoryRange) in containerGroupHashNecessityLookup[false])
                    {
                        container.SetStringOffset(encoding, in containerMemoryRange);
                        container.Commit(in containerMemory);
                    }
                    uint? hash = null;
                    foreach (var (_, container, containerMemory, containerMemoryRange) in containerGroupHashNecessityLookup[true])
                    {
                        container.SetStringOffsetAndHash(encoding, in containerMemoryRange, hash ??= Fnv32.GetHash(str));
                        container.Commit(in containerMemory);
                    }
                    encoding.Write(cStr.Span);
                }
                else
                    foreach (var (_, container, containerMemory, _) in containerGroup)
                        container.Commit(in containerMemory);
            }
        }
        else
            foreach (var (_, container, containerMemory, _) in stringContainers)
                container.Commit(in containerMemory);
        return encoding.WrittenMemory;
    }
}
