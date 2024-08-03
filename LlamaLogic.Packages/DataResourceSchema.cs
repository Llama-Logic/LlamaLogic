namespace LlamaLogic.Packages;

/// <summary>
/// Represents a schema in a data resource
/// </summary>
public class DataResourceSchema
{
    /// <summary>
    /// Initializes a new schema with the specified <paramref name="name"/> and <paramref name="hash"/>
    /// </summary>
    public DataResourceSchema(string? name, uint hash)
    {
        Fields = [];
        Hash = hash;
        Name = name;
    }

    /// <summary>
    /// Gets the fields of the schema
    /// </summary>
    public Collection<DataResourceField> Fields { get; }

    /// <summary>
    /// Gets/sets the hash of the schema
    /// </summary>
    public uint Hash { get; set; }

    /// <summary>
    /// Gets/sets the name of the schema
    /// </summary>
    public string? Name { get; set; }

    internal uint GetSize()
    {
        var size = 0U;
        var alignment = Alignment.None;
        foreach (var field in Fields.OrderBy(field => field.Name))
        {
            var type = field.Type;
            size += type.GetSize();
            alignment = alignment.AggregateAlignment(type);
        }
        size = size.AlignForNextRow(ref alignment);
        return size;
    }

    internal IEnumerable<string> GetNonNullStringsEnumerator()
    {
        if (!string.IsNullOrEmpty(Name))
            yield return Name;
        foreach (var field in Fields)
            foreach (var fieldStr in field.GetNonNullStringsEnumerator())
                yield return fieldStr;
    }
}
