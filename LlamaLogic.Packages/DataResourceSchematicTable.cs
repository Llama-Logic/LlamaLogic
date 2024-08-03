namespace LlamaLogic.Packages;

/// <summary>
/// Represents a table which is associated with a <see cref="DataResourceSchema"/> in a data resource
/// </summary>
public class DataResourceSchematicTable :
    DataResourceTable
{
    /// <summary>
    /// Initializes a new table with the specified <paramref name="name"/>, <paramref name="schema"/>, and <paramref name="type"/>
    /// </summary>
    public DataResourceSchematicTable(string? name, DataResourceSchema schema, DataResourceType type) :
        base(name, type) =>
        Schema = schema;

    /// <summary>
    /// Gets/sets the schema of the table
    /// </summary>
    public DataResourceSchema Schema { get; set; }
}