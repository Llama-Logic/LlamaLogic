namespace LlamaLogic.Packages;

/// <summary>
/// Represents a table in a data resource
/// </summary>
public abstract class DataResourceTable
{
    /// <summary>
    /// Initializes a new table with the specified <paramref name="name"/> and <paramref name="type"/>
    /// </summary>
    protected DataResourceTable(string? name, DataResourceType type)
    {
        Name = name;
        Rows = [];
        Type = type;
    }

    /// <summary>
    /// Gets/sets the name of the table
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets the rows of the table
    /// </summary>
    public Collection<DataResourceRow> Rows { get; }

    /// <summary>
    /// Gets/sets the type of the table
    /// </summary>
    public DataResourceType Type { get; set; }

    internal IEnumerable<string> GetNonNullStringsEnumerator()
    {
        if (!string.IsNullOrEmpty(Name))
            yield return Name;
        foreach (var row in Rows)
            foreach (var rowStr in row.GetNonNullStringsEnumerator())
                yield return rowStr;
    }
}
