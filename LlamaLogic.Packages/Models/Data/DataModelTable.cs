/// <summary>
/// Represents a table in a <see cref="ResourceType.SimData"/> or <see cref="ResourceType.CombinedTuning"/> resource (🔓)
/// </summary>
public sealed class DataModelTable(string? name, string? schemaName, uint? schemaHash)
{
    record Column(string? Name, DataModelType Type, ushort Flags);

    readonly Dictionary<string, int> columnIndexByName = [];
    readonly List<(Column descriptor, List<object?> data)> columns = [];

    /// <summary>
    /// Gets the number of columns
    /// </summary>
    public int ColumnCount =>
        columns.Count;

    /// <summary>
    /// Gets/sets the name of the table
    /// </summary>
    public string? Name { get; set; } = name;

    /// <summary>
    /// Gets the raw values of the table
    /// </summary>
    public IEnumerable<object?> RawValues =>
        GetRawValues(0..RowCount);

    /// <summary>
    /// Gets the number of rows
    /// </summary>
    public int RowCount { get; private set; }

    internal uint RowSize
    {
        get
        {
            var size = 0U;
            var maxAlignment = DataModelAlignment.None;
            foreach (var (decriptor, _) in columns.OrderBy(column => column.descriptor.Name))
            {
                var type = decriptor.Type;
                var typeAlignment = type.GetAlignment();
                size = size.Align(typeAlignment);
                size += type.GetSize();
                maxAlignment = typeAlignment > maxAlignment
                    ? typeAlignment
                    : maxAlignment;
            }
            size = size.Align(maxAlignment);
            return size;
        }
    }

    internal IReadOnlyList<Range> ColumnRanges
    {
        get
        {
            var ranges = new Range[columns.Count];
            var offset = 0U;
            Index rangeIndex = default;
            Index lastRangeIndex = default;
            var lastColumnIndex = -1;
            foreach (var (decriptor, columnIndex) in columns
                .Select((column, columnIndex) => (column.descriptor, columnIndex))
                .OrderBy(t => t.descriptor.Name))
            {
                var type = decriptor.Type;
                var typeAlignment = type.GetAlignment();
                offset = offset.Align(typeAlignment);
                rangeIndex = new Index((int)offset);
                if (lastColumnIndex >= 0)
                    ranges[lastColumnIndex] = new Range(lastRangeIndex, rangeIndex);
                lastRangeIndex = rangeIndex;
                lastColumnIndex = columnIndex;
                offset += type.GetSize();
            }
            rangeIndex = new Index((int)offset);
            if (lastColumnIndex >= 0)
                ranges[lastColumnIndex] = new Range(lastRangeIndex, rangeIndex);
            return [.. ranges];
        }
    }

    internal IReadOnlyList<DataModelType> ColumnTypes =>
        columns.Select(column => column.descriptor.Type).ToImmutableArray();

    internal IReadOnlyDictionary<string, uint> ColumnOffsetsByName
    {
        get
        {
            var result = new Dictionary<string, uint>();
            var offset = 0U;
            foreach (var (descriptor, _) in columns.OrderBy(column => column.descriptor.Name))
            {
                result.Add(descriptor.Name ?? string.Empty, offset);
                offset += descriptor.Type.GetSize();
            }
            return result.ToImmutableDictionary();
        }
    }

    internal int ColumnNamesStringTableEntriesLength =>
        columns.Sum(column => (column.descriptor.Name?.Length ?? 0) + 1);

    /// <summary>
    /// Gets/sets the unique identifier for the schema of the table
    /// </summary>
    public uint? SchemaHash { get; set; } = schemaHash;

    /// <summary>
    /// Gets/sets the name of the schema of the table
    /// </summary>
    public string? SchemaName { get; set; } = schemaName;

    /// <summary>
    /// Gets/sets the values of the specified <paramref name="rowIndex"/>
    /// </summary>
    public object? this[Index rowIndex]
    {
        get => Get(rowIndex);
        set => Set(rowIndex, value);
    }

    /// <summary>
    /// Gets/sets the values of the specified <paramref name="rowIndex"/>
    /// </summary>
    public object? this[int rowIndex]
    {
        get => this[(Index)rowIndex];
        set => Set((Index)rowIndex, value);
    }

    /// <summary>
    /// Gets/sets the value at the specified <paramref name="rowIndex"/> and <paramref name="columnIndex"/>
    /// </summary>
    public object? this[Index rowIndex, Index columnIndex]
    {
        get => Get(rowIndex, columnIndex);
        set => Set(rowIndex, columnIndex, value);
    }

    /// <summary>
    /// Gets/sets the value at the specified <paramref name="rowIndex"/> and <paramref name="columnIndex"/>
    /// </summary>
    public object? this[int rowIndex, int columnIndex]
    {
        get => this[(Index)rowIndex, (Index)columnIndex];
        set => this[(Index)rowIndex, (Index)columnIndex] = value;
    }

    /// <summary>
    /// Gets/sets the value of the specified <paramref name="rowIndex"/> for column with the specified <paramref name="columnName"/>
    /// </summary>
    public object? this[Index rowIndex, string columnName]
    {
        get => Get(rowIndex, columnName);
        set => Set(rowIndex, columnName, value);
    }

    /// <summary>
    /// Gets/sets the value of the specified <paramref name="rowIndex"/> for column with the specified <paramref name="columnName"/>
    /// </summary>
    public object? this[int rowIndex, string columnName]
    {
        get => this[(Index)rowIndex, columnName];
        set => this[(Index)rowIndex, columnName] = value;
    }

    internal event EventHandler<DataModelTableRowEventArgs>? RowInserted;
    internal event EventHandler<DataModelTableRowEventArgs>? RowRemoved;

    /// <summary>
    /// Adds a column with the specified <paramref name="name"/>, <paramref name="type"/>, <paramref name="flags"/>, and <paramref name="values"/>, and returns the index of the column
    /// </summary>
    public int AddColumn(string? name, DataModelType type, ushort flags = 0, IEnumerable<object?>? values = null)
    {
        var index = columns.Count;
        InsertColumn(index, name, type, flags, values);
        return index;
    }

    /// <summary>
    /// Adds the specified <paramref name="row"/> and returns the index of the row
    /// </summary>
    public int AddRow(object? row)
    {
        var index = RowCount;
        InsertRow(index, row);
        return index;
    }

    void BuildColumnNameLookup()
    {
        columnIndexByName.Clear();
        for (var c = 0; c < columns.Count; ++c)
            if (columns[c].descriptor.Name is { } name)
                columnIndexByName.Add(name, c);
    }

    /// <summary>
    /// Gets the values of the row at the specified <paramref name="rowIndex"/>
    /// </summary>
    public object? Get(Index rowIndex)
    {
        if (columns.Count == 0)
            throw new InvalidOperationException("Table has no columns");
        if (columns.Count == 1)
            return Get(rowIndex, 0);
        RowCount.ThrowIfIndexOutOfRange(rowIndex);
        return Enumerable.Range(0, columns.Count).Select(c => columns[c].data[rowIndex]).ToImmutableArray();
    }

    /// <summary>
    /// Gets the values of the row at the specified <paramref name="rowIndex"/>
    /// </summary>
    public object? Get(int rowIndex) =>
        Get((Index)rowIndex);

    /// <summary>
    /// Gets the value at the specified <paramref name="rowIndex"/> and <paramref name="columnIndex"/>
    /// </summary>
    public object? Get(Index rowIndex, Index columnIndex)
    {
        RowCount.ThrowIfIndexOutOfRange(rowIndex);
        columns.Count.ThrowIfIndexOutOfRange(columnIndex);
        return columns[columnIndex].data[rowIndex];
    }

    /// <summary>
    /// Gets the value at the specified <paramref name="rowIndex"/> and <paramref name="columnIndex"/>
    /// </summary>
    public object? Get(int rowIndex, int columnIndex) =>
        Get((Index)rowIndex, (Index)columnIndex);

    /// <summary>
    /// Gets the value at the specified <paramref name="rowIndex"/> for the column with the specified <paramref name="columnName"/>
    /// </summary>
    public object? Get(Index rowIndex, string columnName)
    {
        if (columnIndexByName.TryGetValue(columnName, out var columnIndex))
            return Get(rowIndex, columnIndex);
        throw new ArgumentException($"Column {columnName} does not exist in table {Name}", nameof(columnName));
    }

    /// <summary>
    /// Gets the value at the specified <paramref name="rowIndex"/> for the column with the specified <paramref name="columnName"/>
    /// </summary>
    public object? Get(int rowIndex, string columnName) =>
        Get((Index)rowIndex, columnName);

    /// <summary>
    /// Gets the flags of the column at the specified <paramref name="index"/>
    /// </summary>
    public ushort GetColumnFlags(Index index)
    {
        columns.Count.ThrowIfIndexOutOfRange(index);
        return columns[index].descriptor.Flags;
    }

    /// <summary>
    /// Gets the flags of the column at the specified <paramref name="index"/>
    /// </summary>
    public ushort GetColumnFlags(int index) =>
        GetColumnFlags((Index)index);

    /// <summary>
    /// Gets the flags of the column with the specified <paramref name="columnName"/>
    /// </summary>
    public ushort GetColumnFlags(string columnName)
    {
        if (columnIndexByName.TryGetValue(columnName, out var index))
            return GetColumnFlags(index);
        throw new ArgumentException($"Column {columnName} does not exist in table {Name}", nameof(columnName));
    }

    /// <summary>
    /// Gets the name of the column at the specified <paramref name="index"/>
    /// </summary>
    public string? GetColumnName(Index index)
    {
        columns.Count.ThrowIfIndexOutOfRange(index);
        return columns[index].descriptor.Name;
    }

    /// <summary>
    /// Gets the name of the column at the specified <paramref name="index"/>
    /// </summary>
    public string? GetColumnName(int index) =>
        GetColumnName((Index)index);

    /// <summary>
    /// Gets the <see cref="DataModelType"/> of the column at the specified <paramref name="index"/>
    /// </summary>
    public DataModelType GetColumnType(Index index)
    {
        columns.Count.ThrowIfIndexOutOfRange(index);
        return columns[index].descriptor.Type;
    }

    /// <summary>
    /// Gets the <see cref="DataModelType"/> of the column at the specified <paramref name="index"/>
    /// </summary>
    public DataModelType GetColumnType(int index) =>
        GetColumnType((Index)index);

    /// <summary>
    /// Gets the <see cref="DataModelType"/> of the column with the specified <paramref name="columnName"/>
    /// </summary>
    public DataModelType GetColumnType(string columnName)
    {
        if (columnIndexByName.TryGetValue(columnName, out var index))
            return GetColumnType(index);
        throw new ArgumentException($"Column {columnName} does not exist in table {Name}", nameof(columnName));
    }

    /// <summary>
    /// Gets all the values for the column at the specified <paramref name="index"/>
    /// </summary>
    public IReadOnlyList<object?> GetColumnValues(Index index)
    {
        columns.Count.ThrowIfIndexOutOfRange(index);
        return [.. columns[index].data];
    }

    /// <summary>
    /// Gets all the values for the column at the specified <paramref name="index"/>
    /// </summary>
    public IReadOnlyList<object?> GetColumnValues(int index) =>
        GetColumnValues((Index)index);

    /// <summary>
    /// Gets all the values for the column with the specified <paramref name="columnName"/>
    /// </summary>
    public IReadOnlyList<object?> GetColumnValues(string columnName)
    {
        if (columnIndexByName.TryGetValue(columnName, out var index))
            return GetColumnValues(index);
        throw new ArgumentException($"Column {columnName} does not exist in table {Name}", nameof(columnName));
    }

    /// <summary>
    /// Gets the values for the column at the specified <paramref name="columnIndex"/> for the specified <paramref name="rows"/>
    /// </summary>
    public IReadOnlyList<object?> GetColumnValues(Index columnIndex, Range rows)
    {
        columns.Count.ThrowIfIndexOutOfRange(columnIndex);
        return RowCount.GetOffsets(rows).Select(r => columns[columnIndex].data[r]).ToImmutableArray();
    }

    /// <summary>
    /// Gets the values for the column at the specified <paramref name="columnIndex"/> for the specified number of <paramref name="rows"/> beginning at the specified <paramref name="rowIndex"/>
    /// </summary>
    public IReadOnlyList<object?> GetColumnValues(int columnIndex, int rowIndex, int rows) =>
        GetColumnValues((Index)columnIndex, new Range(rowIndex, rowIndex + rows));

    /// <summary>
    /// Gets the values for the column with the specified <paramref name="columnName"/> for the specified <paramref name="rows"/>
    /// </summary>
    public IReadOnlyList<object?> GetColumnValues(string columnName, Range rows)
    {
        if (columnIndexByName.TryGetValue(columnName, out var index))
            return GetColumnValues(index, rows);
        throw new ArgumentException($"Column {columnName} does not exist in table {Name}", nameof(columnName));
    }

    /// <summary>
    /// Gets the values for the column with the specified <paramref name="columnName"/> for the specified number of <paramref name="rows"/> beginning at the specified <paramref name="rowIndex"/>
    /// </summary>
    public IReadOnlyList<object?> GetColumnValues(string columnName, int rowIndex, int rows) =>
        GetColumnValues(columnName, new Range(rowIndex, rowIndex + rows));

    /// <summary>
    /// Gets the values for the specified <paramref name="columnIndex"/> starting at the specified <paramref name="startingRowIndex"/> for as long as the <paramref name="takeWhile"/> predicate returns <see langword="true"/>
    /// </summary>
    public IReadOnlyList<object?> GetColumnValues(Index columnIndex, Index startingRowIndex, Predicate<object?> takeWhile)
    {
        columns.Count.ThrowIfIndexOutOfRange(columnIndex);
        RowCount.ThrowIfIndexOutOfRange(startingRowIndex);
        ArgumentNullException.ThrowIfNull(takeWhile);
        var values = new List<object?>();
        var data = columns[columnIndex].data;
        var rowCount = RowCount;
        for (var r = startingRowIndex.GetOffset(RowCount); r < rowCount; ++r)
        {
            var value = data[r];
            if (!takeWhile(value))
                break;
            values.Add(value);
        }
        return values;
    }

    /// <summary>
    /// Gets the values for the specified <paramref name="columnIndex"/> starting at the specified <paramref name="startingRowIndex"/> for as long as the <paramref name="takeWhile"/> predicate returns <see langword="true"/>
    /// </summary>
    public IReadOnlyList<object?> GetColumnValues(int columnIndex, int startingRowIndex, Predicate<object?> takeWhile) =>
        GetColumnValues((Index)columnIndex, (Index)startingRowIndex, takeWhile);

    /// <summary>
    /// Gets the values for the column with the specified <paramref name="columnName"/> starting at the specified <paramref name="startingRowIndex"/> for as long as the <paramref name="takeWhile"/> predicate returns <see langword="true"/>
    /// </summary>
    public IReadOnlyList<object?> GetColumnValues(string columnName, Index startingRowIndex, Predicate<object?> takeWhile)
    {
        if (columnIndexByName.TryGetValue(columnName, out var index))
            return GetColumnValues(index, startingRowIndex, takeWhile);
        throw new ArgumentException($"Column {columnName} does not exist in table {Name}", nameof(columnName));
    }

    /// <summary>
    /// Gets the values for the column with the specified <paramref name="columnName"/> starting at the specified <paramref name="startingRowIndex"/> for as long as the <paramref name="takeWhile"/> predicate returns <see langword="true"/>
    /// </summary>
    public IReadOnlyList<object?> GetColumnValues(string columnName, int startingRowIndex, Predicate<object?> takeWhile) =>
        GetColumnValues(columnName, (Index)startingRowIndex, takeWhile);

    /// <summary>
    /// Gets the value of a raw table for the specified <paramref name="row"/>
    /// </summary>
    public object? GetRawValue(Index row)
    {
        if (columns.Count == 1)
            return Get(row, 0);
        throw new InvalidOperationException("Only valid when the table has a single column");
    }

    /// <summary>
    /// Gets the value of a raw table for the specified <paramref name="row"/>
    /// </summary>
    public object? GetRawValue(int row) =>
        GetRawValue((Index)row);

    /// <summary>
    /// Gets the values of a raw table for the specified <paramref name="rows"/>
    /// </summary>
    public IReadOnlyList<object?> GetRawValues(Range rows)
    {
        if (columns.Count == 1)
            return GetColumnValues(0, rows);
        throw new InvalidOperationException("Only valid when the table has a single column");
    }

    /// <summary>
    /// Gets the values of a raw table for the specified number of <paramref name="rows"/> beginning at the specified <paramref name="rowIndex"/>
    /// </summary>
    public IReadOnlyList<object?> GetRawValues(int rowIndex, int rows) =>
        GetRawValues(new Range(rowIndex, rowIndex + rows));

    /// <summary>
    /// Inserts a column with the specified <paramref name="name"/>, <paramref name="type"/>, and <paramref name="flags"/> at the specified <paramref name="index"/>
    /// </summary>
    public void InsertColumn(Index index, string? name, DataModelType type, ushort flags = 0, IEnumerable<object?>? values = null)
    {
        var columnCount = columns.Count;
        columnCount.ThrowIfIndexOutOfRangeForInsertion(index);
        var valuesList = (values ?? []).Select(value => DataModel.CoerceValue(value, type)).ToList();
        var isFirstColumn = columns.Count == 0;
        if (!isFirstColumn && valuesList.Count != RowCount)
            throw new ArgumentException("Incorrect number of values", nameof(values));
        columns.Insert(index.GetOffset(columnCount), (new Column(name, type, flags), valuesList));
        if (isFirstColumn)
            RowCount = valuesList.Count;
        BuildColumnNameLookup();
    }

    /// <summary>
    /// Inserts a column with the specified <paramref name="name"/>, <paramref name="type"/>, and <paramref name="flags"/> at the specified <paramref name="index"/>
    /// </summary>
    public void InsertColumn(int index, string? name, DataModelType type, ushort flags = 0, IEnumerable<object?>? values = null) =>
        InsertColumn((Index)index, name, type, flags, values);

    /// <summary>
    /// Inserts the specified <paramref name="row"/> at the specified <paramref name="index"/>
    /// </summary>
    public void InsertRow(Index index, object? row)
    {
        var rowCount = RowCount;
        rowCount.ThrowIfIndexOutOfRangeForInsertion(index);
        var values = row is IEnumerable<object?> enumerable
            ? enumerable.ToImmutableArray()
            : [row];
        if (values.Length != columns.Count)
            throw new ArgumentException("Incorrect number of values", nameof(row));
        var offset = index.GetOffset(rowCount);
        for (var c = 0; c < columns.Count; ++c)
        {
            var (descriptor, data) = columns[c];
            data.Insert(offset, DataModel.CoerceValue(values[c], descriptor.Type));
        }
        ++RowCount;
        OnRowInserted(new DataModelTableRowEventArgs { RowIndex = index });
    }

    /// <summary>
    /// Inserts the specified <paramref name="row"/> at the specified <paramref name="index"/>
    /// </summary>
    public void InsertRow(int index, object? row) =>
        InsertRow((Index)index, row);

    void OnRowInserted(DataModelTableRowEventArgs e) =>
        RowInserted?.Invoke(this, e);

    void OnRowRemoved(DataModelTableRowEventArgs e) =>
        RowRemoved?.Invoke(this, e);

    /// <summary>
    /// Removes the column at the specified <paramref name="index"/>
    /// </summary>
    public void RemoveColumn(Index index)
    {
        var columnCount = columns.Count;
        columnCount.ThrowIfIndexOutOfRange(index);
        columns.RemoveAt(index.GetOffset(columnCount));
        BuildColumnNameLookup();
    }

    /// <summary>
    /// Removes the column at the specified <paramref name="index"/>
    /// </summary>
    public void RemoveColumn(int index) =>
        RemoveColumn((Index)index);

    /// <summary>
    /// Removes the column with the specified <paramref name="columnName"/> and returns the index at which the column was removed
    /// </summary>
    public int RemoveColumn(string columnName)
    {
        if (columnIndexByName.TryGetValue(columnName, out var index))
        {
            RemoveColumn(index);
            return index;
        }
        throw new ArgumentException($"Column {columnName} does not exist in table {Name}", nameof(columnName));
    }

    /// <summary>
    /// Removes the row at the specified <paramref name="index"/>
    /// </summary>
    public void RemoveRow(Index index)
    {
        var rowCount = RowCount;
        rowCount.ThrowIfIndexOutOfRange(index);
        for (var c = 0; c < columns.Count; ++c)
            columns[c].data.RemoveAt(index.GetOffset(rowCount));
        --RowCount;
        OnRowRemoved(new DataModelTableRowEventArgs { RowIndex = index });
    }

    /// <summary>
    /// Removes the row at the specified <paramref name="index"/>
    /// </summary>
    public void RemoveRow(int index) =>
        RemoveRow((Index)index);

    /// <summary>
    /// Sets the <paramref name="row"/> at the specified <paramref name="index"/>
    /// </summary>
    public void Set(Index index, object? row)
    {
        RowCount.ThrowIfIndexOutOfRange(index);
        var values = row is IEnumerable<object?> enumerable
            ? enumerable.ToImmutableArray()
            : [row];
        if (values.Length != columns.Count)
            throw new ArgumentException("Incorrect number of values", nameof(row));
        for (var c = 0; c < columns.Count; ++c)
        {
            var (descriptor, data) = columns[c];
            data[index] = DataModel.CoerceValue(values[c], descriptor.Type);
        }
    }

    /// <summary>
    /// Sets the <paramref name="row"/> at the specified <paramref name="index"/>
    /// </summary>
    public void Set(int index, object? row) =>
        Set((Index)index, row);

    /// <summary>
    /// Sets the value at the specified <paramref name="rowIndex"/> and <paramref name="columnIndex"/>
    /// </summary>
    public void Set(Index rowIndex, Index columnIndex, object? value)
    {
        RowCount.ThrowIfIndexOutOfRange(rowIndex);
        columns.Count.ThrowIfIndexOutOfRange(columnIndex);
        var (descriptor, data) = columns[columnIndex];
        data[rowIndex] = DataModel.CoerceValue(value, descriptor.Type);
    }

    /// <summary>
    /// Sets the value at the specified <paramref name="rowIndex"/> and <paramref name="columnIndex"/>
    /// </summary>
    public void Set(int rowIndex, int columnIndex, object? value) =>
        Set((Index)rowIndex, (Index)columnIndex, value);

    /// <summary>
    /// Sets the value of the specified <paramref name="rowIndex"/> for column with the specified <paramref name="columnName"/>
    /// </summary>
    public void Set(Index rowIndex, string columnName, object? value)
    {
        if (!columnIndexByName.TryGetValue(columnName, out var index))
            throw new ArgumentException($"Column {columnName} does not exist in table {Name}", nameof(columnName));
        Set(rowIndex, index, value);
    }

    /// <summary>
    /// Sets the value of the specified <paramref name="rowIndex"/> for column with the specified <paramref name="columnName"/>
    /// </summary>
    public void Set(int rowIndex, string columnName, object? value) =>
        Set((Index)rowIndex, columnName, value);

    /// <summary>
    /// Sets the flags of the column at the specified <paramref name="index"/>
    /// </summary>
    public void SetColumnFlags(Index index, ushort flags)
    {
        columns.Count.ThrowIfIndexOutOfRange(index);
        var (descriptor, data) = columns[index];
        descriptor = descriptor with { Flags = flags };
        columns[index] = (descriptor, data);
    }

    /// <summary>
    /// Sets the flags of the column at the specified <paramref name="index"/>
    /// </summary>
    public void SetColumnFlags(int index, ushort flags) =>
        SetColumnFlags((Index)index, flags);

    /// <summary>
    /// Sets the flags of the column with the specified <paramref name="columnName"/>
    /// </summary>
    public void SetColumnFlags(string columnName, ushort flags)
    {
        if (!columnIndexByName.TryGetValue(columnName, out var index))
            throw new ArgumentException($"Column {columnName} does not exist in table {Name}", nameof(columnName));
        SetColumnFlags(index, flags);
    }

    /// <summary>
    /// Sets the name of the column at the specified <paramref name="index"/>
    /// </summary>
    public void SetColumnName(Index index, string? name)
    {
        columns.Count.ThrowIfIndexOutOfRange(index);
        var (descriptor, data) = columns[index];
        descriptor = descriptor with { Name = name };
        columns[index] = (descriptor, data);
    }

    /// <summary>
    /// Sets the name of the column at the specified <paramref name="index"/>
    /// </summary>
    public void SetColumnName(int index, string? name) =>
        SetColumnName((Index)index, name);

    /// <summary>
    /// Sets the <paramref name="value"/> of a raw table for the specified <paramref name="row"/>
    /// </summary>
    public void SetRawValue(Index row, object? value)
    {
        if (columns.Count != 1)
            throw new InvalidOperationException("Only valid when the table has a single column");
        Set(row, 0, value);
    }

    /// <summary>
    /// Sets the <paramref name="value"/> of a raw table for the specified <paramref name="row"/>
    /// </summary>
    public void SetRawValue(int row, object? value) =>
        SetRawValue((Index)row, value);

    /// <summary>
    /// Replaces the specified <paramref name="rows"/> in a raw table with the specified <paramref name="values"/>, returning the length of <paramref name="values"/>
    /// </summary>
    public int SetRawValues(Range rows, IEnumerable<object?> values)
    {
        if (columns.Count != 1)
            throw new InvalidOperationException("Only valid when the table has a single column");
        var type = columns[0].descriptor.Type;
        var coercedValues = values.Select(value => DataModel.CoerceValue(value, type)).ToImmutableArray();
        var (offset, length) = rows.GetOffsetAndLength(RowCount);
        for (var r = 0; r < length; ++r)
            RemoveRow(offset);
        for (var v = 0; v < coercedValues.Length; ++v)
            InsertRow(offset + v, coercedValues[v]);
        return coercedValues.Length;
    }

    /// <summary>
    /// Replaces the specified number of <paramref name="rows"/> beginning as the specified <paramref name="rowIndex"/> in a raw table with the specified <paramref name="values"/>, returning the length of <paramref name="values"/>
    /// </summary>
    public int SetRawValues(int rowIndex, int rows, IEnumerable<object?> values) =>
        SetRawValues(new Range(rowIndex, rowIndex + rows), values);
}
