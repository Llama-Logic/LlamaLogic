namespace LlamaLogic.Packages.Models.Data;

sealed class DataModelTableRowEventArgs :
    EventArgs
{
    public Index RowIndex { get; init; }
}
