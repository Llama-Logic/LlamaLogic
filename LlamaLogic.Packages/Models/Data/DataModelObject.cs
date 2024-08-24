namespace LlamaLogic.Packages.Models.Data;

/// <summary>
/// Represents an <see cref="DataModelType.OBJECT"/> reference in a <see cref="ResourceType.SimData"/> or <see cref="ResourceType.CombinedTuning"/> resource (🔓)
/// </summary>
public class DataModelObject :
    DataModelReference
{
    /// <summary>
    /// Gets the null reference <see cref="DataModelObject"/>
    /// </summary>
    public static DataModelObject NullReference { get; } = new(-1, ^0, ^0, DataModelType.UNDEFINED);

    /// <summary>
    /// Initializes a new <see cref="DataModelObject"/> (🔄️🏃)
    /// </summary>
    /// <param name="table">The table containing the object being referenced</param>
    /// <param name="rowIndex">The row index within the containing table of the object being referenced</param>
    /// <param name="columnIndex">The column index within the containing table of the object being referenced</param>
    /// <param name="type">The type of the object being referenced</param>
    public DataModelObject(DataModelTable table, Index rowIndex, Index columnIndex, DataModelType type) :
        base(table, rowIndex, columnIndex, type)
    {
    }

    internal DataModelObject(int binaryTableIndex, Index rowIndex, Index columnIndex, DataModelType type) :
        base(binaryTableIndex, rowIndex, columnIndex, type)
    {
    }

    /// <summary>
    /// Gets/sets the value referenced by the object (💤)
    /// </summary>
    public object? Value
    {
        get =>
              IsValid
            ? Table.GetRawValue(RowIndex)
            : throw new InvalidOperationException("Cannot get values on an invalid reference");
        set
        {
            if (!IsValid)
                throw new InvalidOperationException("Cannot set values on an invalid reference");
            Table.SetRawValue(RowIndex, value);
        }
    }
}
