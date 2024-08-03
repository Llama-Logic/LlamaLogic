namespace LlamaLogic.Packages;

/// <summary>
/// Represents a field in a data resource
/// </summary>
public class DataResourceField
{
    /// <summary>
    /// Initializes a new field with the specified <paramref name="name"/>, <paramref name="type"/>, and <paramref name="flags"/>
    /// </summary>
    public DataResourceField(string? name, DataResourceType type, ushort flags)
    {
        Name = name;
        Type = type;
        Flags = flags;
    }

    /// <summary>
    /// Gets/sets the flags of the field
    /// </summary>
    public ushort Flags { get; set; }

    /// <summary>
    /// Gets/sets the name of the field
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets/sets the type of the field
    /// </summary>
    public DataResourceType Type { get; set; }

    internal IEnumerable<string> GetNonNullStringsEnumerator()
    {
        if (!string.IsNullOrEmpty(Name))
            yield return Name;
    }
}
