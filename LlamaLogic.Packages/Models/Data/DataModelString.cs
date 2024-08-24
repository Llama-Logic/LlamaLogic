namespace LlamaLogic.Packages.Models.Data;

/// <summary>
/// Represents a <see cref="DataModelType.STRING8"/> or <see cref="DataModelType.HASHEDSTRING8"/> reference in a <see cref="ResourceType.SimData"/> or <see cref="ResourceType.CombinedTuning"/> resource (🔓)
/// </summary>
public sealed class DataModelString :
    DataModelReference
{
    /// <summary>
    /// Gets the null reference <see cref="DataModelString"/>
    /// </summary>
    public static DataModelString NullReference { get; } = new(-1, ^0);

    static Range ProjectRange(DataModelTable table, Index start)
    {
        if (table.ColumnCount != 1)
            throw new InvalidOperationException("Cannot project a range on a table with more than one column");
        if (table.GetColumnType(0) is not DataModelType.CHAR8)
            throw new InvalidOperationException("Cannot project a range on a table with a non-character column");
        var foundNullChar = false;
        var chars = table.GetColumnValues(0, start, value =>
        {
            if (foundNullChar)
                return false;
            foundNullChar = (byte)(char)value! == 0;
            return true;
        });
        return new Range(start.GetOffset(table.RowCount), start.GetOffset(table.RowCount) + chars.Count);
    }

    /// <summary>
    /// Initializes a new <see cref="DataModelString"/> (🔄️🏃)
    /// </summary>
    /// <param name="table">The table, a sub sequence of which, is the null-terminated string of <see cref="DataModelType.CHAR8"/> values</param>
    /// <param name="rowIndex">The row index within the containing table of the string being referenced at which the string begins</param>
    public DataModelString(DataModelTable table, Index rowIndex) :
        base(table, rowIndex, 0, DataModelType.CHAR8) =>
        Length = ProjectRange(table, rowIndex).GetOffsetAndLength(table.RowCount).Length;

    internal DataModelString(int binaryTableIndex, Index rowIndex) :
        base(binaryTableIndex, rowIndex, 0, DataModelType.CHAR8)
    {
    }

    /// <summary>
    /// Gets the length of the string in ASCII characters, including the null terminator
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// Gets the range rows of the string
    /// </summary>
    public Range Range =>
        DataModelVector.ProjectRange(RowIndex, Length);

    /// <summary>
    /// Gets/sets the value of the string (💤)
    /// </summary>
    public string Value
    {
        get =>
              IsValid
            ? Table.GetColumnValues(0, DataModelVector.ProjectRange(RowIndex, Length - 1))
                .Select(value => (char)value!)
                .Aggregate(new StringBuilder(), (sb, c) => sb.Append(c))
                .ToString()
            : throw new InvalidOperationException("Cannot get values on an invalid reference");
        set
        {
            if (!IsValid)
                throw new InvalidOperationException("Cannot set values on an invalid reference");
            Unwire();
            try
            {
                Length = Table.SetRawValues(Range, Encoding.ASCII.GetBytes(value).Concat([(byte)0]).Cast<object?>());
            }
            finally
            {
                Wire();
            }
        }
    }

    internal override void Bind(DataModel model)
    {
        base.Bind(model);
        var rowIndex = RowIndex;
        if (rowIndex.Value < 0 || rowIndex is { IsFromEnd: true, Value: 0 })
            return;
        Length = ProjectRange(Table, rowIndex).GetOffsetAndLength(Table.RowCount).Length;
    }

    internal override void HandleRowInserted(object? sender, DataModelTableRowEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.HandleRowInserted(sender, e);
        var preInsertionCount = Table.RowCount - 1;
        var insertedOffset = e.RowIndex.GetOffset(preInsertionCount);
        var rows = Range;
        if (insertedOffset >= rows.Start.GetOffset(preInsertionCount) && insertedOffset < rows.End.GetOffset(preInsertionCount))
            Invalidate();
    }

    internal override void HandleRowRemoved(object? sender, DataModelTableRowEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.HandleRowRemoved(sender, e);
        var preRemovalCount = Table.RowCount + 1;
        var removedOffset = e.RowIndex.GetOffset(preRemovalCount);
        var rows = Range;
        if (removedOffset >= rows.Start.GetOffset(preRemovalCount) && removedOffset < rows.End.GetOffset(preRemovalCount))
            Invalidate();
    }

    internal override void Invalidate()
    {
        base.Invalidate();
        Length = 0;
    }
}
