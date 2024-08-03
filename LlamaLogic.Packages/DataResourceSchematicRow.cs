namespace LlamaLogic.Packages;

/// <summary>
/// Represents a row in a table which is associated with a <see cref="DataResourceSchema"/>
/// </summary>
public class DataResourceSchematicRow :
    DataResourceRow
{
    /// <summary>
    /// Initializes a new row with no values
    /// </summary>
    public DataResourceSchematicRow() =>
        Values = [];

    /// <summary>
    /// Gets the dictionary of values in this row
    /// </summary>
    public Dictionary<DataResourceField, object?> Values { get; }

    internal override IEnumerable<string> GetNonNullStringsEnumerator()
    {
        foreach (var value in Values.Values)
            if (value is string str && str.Length > 0)
                yield return str;
    }
}