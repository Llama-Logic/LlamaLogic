namespace LlamaLogic.Packages.Models.Data;

/// <summary>
/// Represents a <see cref="DataModelType.VECTOR"/> reference in a <see cref="ResourceType.SimData"/> or <see cref="ResourceType.CombinedTuning"/> resource (🔓)
/// </summary>
public sealed class DataModelVector :
    DataModelReference
{
    /// <summary>
    /// Create a <see cref="DataModelVector"/> null reference
    /// </summary>
    /// <param name="count">The number of elements (probably should be `0`)</param>
    public static DataModelVector CreateNullReference(int count = 0) =>
        new(-1, ^0, DataModelType.UNDEFINED, count);

    internal static Range ProjectRange(Index start, int count) =>
        start.IsFromEnd
            ? new Range(start, new Index(start.Value - (count - 1), true))
            : new Range(start, start.Value + (count - 1));

    /// <summary>
    /// Initializes a new <see cref="DataModelVector"/> (🔄️🏃)
    /// </summary>
    /// <param name="table">The table, a sub sequence of which, is the vector</param>
    /// <param name="rowIndex">The row index within the containing table of the vector being referenced at which the vector begins</param>
    /// <param name="type">The type of the objects being referenced</param>
    /// <param name="count">The number of elements in the vector</param>
    public DataModelVector(DataModelTable table, Index rowIndex, DataModelType type, int count) :
        base(table, rowIndex, 0, type)
    {
        Table.RowCount.ThrowIfIndexOutOfRange(ProjectRange(rowIndex, count));
        Count = count;
    }

    /// <summary>
    /// Initializes a new <see cref="DataModelVector"/> (🔄️🏃)
    /// </summary>
    /// <param name="table">The table, a sub sequence of which, is the vector</param>
    /// <param name="rowIndex">The row index within the containing table of the vector being referenced at which the vector begins</param>
    /// <param name="type">The type of the objects being referenced</param>
    /// <param name="count">The number of elements in the vector</param>
    public DataModelVector(DataModelTable table, int rowIndex, DataModelType type, int count) :
        this(table, (Index)rowIndex, type, count)
    {
    }

    internal DataModelVector(int binaryTableIndex, Index rowIndex, DataModelType type, int count) :
        base(binaryTableIndex, rowIndex, 0, type) =>
        Count = count;

    /// <summary>
    /// Gets the number of elements in the vector
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets the indicies of the vector
    /// </summary>
    public IEnumerable<int> Indicies =>
        Table.RowCount.GetOffsets(Range);

    /// <inheritdoc />
    public override bool IsValid
    {
        get
        {
            if (!base.IsValid)
                return false;
            var endOffset = Range.End.GetOffset(Table.RowCount);
            return endOffset >= 0 && endOffset < Table.RowCount;
        }
    }

    /// <summary>
    /// Gets the range rows of the vector
    /// </summary>
    public Range Range =>
        ProjectRange(RowIndex, Count);

    /// <summary>
    /// Gets/sets the sequence of values referenced by the vector (💤)
    /// </summary>
    public IEnumerable<object?> Values
    {
        get =>
              IsValid
            ? Table.GetRawValues(Range)
            : throw new InvalidOperationException("Cannot get values on an invalid reference");
        set
        {
            if (!IsValid)
                throw new InvalidOperationException("Cannot set values on an invalid reference");
            Unwire();
            try
            {
                Count = Table.SetRawValues(Range, value);
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
        Table.RowCount.ThrowIfIndexOutOfRange(Range);
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
        Count = 0;
    }
}
