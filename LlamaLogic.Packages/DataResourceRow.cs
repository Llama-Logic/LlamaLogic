namespace LlamaLogic.Packages;

/// <summary>
/// Represents a row of data in a table
/// </summary>
public abstract class DataResourceRow
{
    internal abstract IEnumerable<string> GetNonNullStringsEnumerator();
}