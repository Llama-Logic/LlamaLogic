namespace LlamaLogic.Packages;

/// <summary>
/// Represents a table which is not associated with a <see cref="DataResourceSchema"/> in a data resource
/// </summary>
public class DataResourceGeneralTable :
    DataResourceTable
{
    /// <summary>
    /// Initializes a new table with the specified <paramref name="name"/> and <paramref name="type"/>
    /// </summary>
    public DataResourceGeneralTable(string? name, DataResourceType type) :
        base(name, type)
    {
    }
}
