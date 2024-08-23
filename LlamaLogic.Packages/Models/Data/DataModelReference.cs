namespace LlamaLogic.Packages.Models.Data;

/// <summary>
/// Represents a reference in a <see cref="ResourceType.SimData"/> or <see cref="ResourceType.CombinedTuning"/> resource (🔓)
/// </summary>
public abstract class DataModelReference
{
    /// <summary>
    /// Initializes a new <see cref="DataModelReference"/> (🔄️🏃)
    /// </summary>
    /// <param name="table">The table containing the structure being referenced</param>
    /// <param name="rowIndex">The row index within the containing table of the structure being referenced</param>
    /// <param name="columnIndex">The column index within the containing table of the structure being referenced</param>
    /// <param name="type">The type of the structure being referenced</param>
    protected DataModelReference(DataModelTable table, Index rowIndex, Index columnIndex, DataModelType type)
    {
        ArgumentNullException.ThrowIfNull(table);
        table.RowCount.ThrowIfIndexOutOfRange(rowIndex);
        Table = table;
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        Type = type;
        Wire();
    }

    internal DataModelReference(int binaryTableIndex, Index rowIndex, Index columnIndex, DataModelType type)
    {
        Table = default!;
        this.binaryTableIndex = binaryTableIndex;
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        Type = type;
    }

    readonly int binaryTableIndex;

    /// <summary>
    /// Gets the column index within the table containing the reference of the reference
    /// </summary>
    public int Column =>
        ColumnIndex.GetOffset(Table.ColumnCount);

    /// <summary>
    /// Gets the column index within the table containing the reference of the reference
    /// </summary>
    public Index ColumnIndex { get; }

    /// <summary>
    /// Gets whether the reference has remained valid
    /// </summary>
    public virtual bool IsValid
    {
        get
        {
            if (Table is null)
                return false;
            var offset = RowIndex.GetOffset(Table.RowCount);
            return offset >= 0 && offset < Table.RowCount;
        }
    }

    /// <summary>
    /// Gets the row index within the table containing the reference of the reference (or -1 if the row has been removed)
    /// </summary>
    public int Row =>
        RowIndex.GetOffset(Table.RowCount);

    /// <summary>
    /// Gets the row index within the table containing the reference of the reference (or -1 if the row has been removed)
    /// </summary>
    public Index RowIndex { get; internal set; }

    /// <summary>
    /// Gets the table containing the structure being referenced
    /// </summary>
    public DataModelTable Table { get; private set; }

    /// <summary>
    /// Gets the type of the structure being referenced
    /// </summary>
    public DataModelType Type { get; }

    internal virtual void Bind(DataModel model)
    {
        if (binaryTableIndex < 0 && RowIndex is { IsFromEnd: true, Value: 0 })
            return;
        var rowIndex = RowIndex;
        if (rowIndex.Value < 0 || rowIndex.IsFromEnd && rowIndex.Value == 0)
            return;
        try
        {
            Table = model.Tables[binaryTableIndex];
        }
        catch (Exception ex)
        {
            throw new FormatException($"Failed to wire reference to table at index {binaryTableIndex}", ex);
        }
        Table.RowCount.ThrowIfIndexOutOfRange(RowIndex);
        Wire();
    }

    internal virtual void HandleRowInserted(object? sender, DataModelTableRowEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var preInsertionCount = Table.RowCount - 1;
        var objectOffset = RowIndex.GetOffset(preInsertionCount);
        var insertedOffset = e.RowIndex.GetOffset(preInsertionCount);
        if (insertedOffset <= objectOffset)
            RowIndex = objectOffset + 1;
    }

    internal virtual void HandleRowRemoved(object? sender, DataModelTableRowEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var preRemovalCount = Table.RowCount + 1;
        var objectOffset = RowIndex.GetOffset(preRemovalCount);
        var removedOffset = e.RowIndex.GetOffset(preRemovalCount);
        if (removedOffset < objectOffset)
            RowIndex = objectOffset - 1;
        else if (removedOffset == objectOffset)
            Invalidate();
    }

    internal virtual void Invalidate()
    {
        RowIndex = ^0;
        Unwire();
    }

    internal void Unwire()
    {
        Table.RowInserted -= HandleRowInserted;
        Table.RowRemoved -= HandleRowRemoved;
    }

    internal void Wire()
    {
        Table.RowInserted += HandleRowInserted;
        Table.RowRemoved += HandleRowRemoved;
    }
}
