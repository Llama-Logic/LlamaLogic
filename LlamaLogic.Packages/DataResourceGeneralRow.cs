namespace LlamaLogic.Packages;

/// <summary>
/// Represents a row in a table which is not associated with a <see cref="DataResourceSchema"/>
/// </summary>
public class DataResourceGeneralRow :
    DataResourceRow
{
    /// <summary>
    /// Initializes a new row with no values
    /// </summary>
    public DataResourceGeneralRow() =>
        Values = [];

    /// <summary>
    /// Gets the list of values in this row
    /// </summary>
    public Collection<object?> Values { get; }

    internal override IEnumerable<string> GetNonNullStringsEnumerator()
    {
        foreach (var value in Values)
            if (value is string str && str.Length > 0)
                yield return str;
    }
}
