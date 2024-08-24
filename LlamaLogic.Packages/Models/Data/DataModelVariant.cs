namespace LlamaLogic.Packages.Models.Data;

/// <summary>
/// Represents a <see cref="DataModelType.VARIANT"/> reference in a <see cref="ResourceType.SimData"/> or <see cref="ResourceType.CombinedTuning"/> resource (🔓)
/// </summary>
public sealed class DataModelVariant :
    DataModelObject
{
    /// <summary>
    /// Create a <see cref="DataModelVariant"/> null reference
    /// </summary>
    /// <param name="typeHash">The type hash of the variant</param>
    public static DataModelVariant CreateNullReference(uint typeHash) =>
        new(-1, ^0, ^0, DataModelType.UNDEFINED, typeHash);

    /// <summary>
    /// Initializes a new <see cref="DataModelVariant"/> (🔄️🏃)
    /// </summary>
    /// <param name="table">The table containing the variant being referenced</param>
    /// <param name="rowIndex">The row index within the containing table of the variant being referenced</param>
    /// <param name="columnIndex">The column index within the containing table of the variant being referenced</param>
    /// <param name="type">The type of the variant being referenced</param>
    /// <param name="typeHash">The type hash of the variant</param>
    public DataModelVariant(DataModelTable table, Index rowIndex, Index columnIndex, DataModelType type, uint typeHash) :
        base(table, rowIndex, columnIndex, type) =>
        TypeHash = typeHash;

    internal DataModelVariant(int binaryTableIndex, Index rowIndex, Index columnIndex, DataModelType type, uint typeHash) :
        base(binaryTableIndex, rowIndex, columnIndex, type) =>
        TypeHash = typeHash;

    /// <summary>
    /// Gets/sets the type hash of the variant
    /// </summary>
    public uint TypeHash { get; set; }
}
