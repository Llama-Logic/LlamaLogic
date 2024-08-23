namespace LlamaLogic.Packages.Models.Data;

[AttributeUsage(AttributeTargets.Field)]
sealed class DataModelTypeAlignmentAttribute :
    Attribute
{
    public DataModelTypeAlignmentAttribute(DataModelAlignment alignment) =>
        Alignment = alignment;

    public DataModelAlignment Alignment { get; }
}
