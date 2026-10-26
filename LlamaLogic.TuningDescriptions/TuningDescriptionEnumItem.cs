namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents a member of an enumeration described by a <see cref="TuningDescriptionClass"/> (an <c>EnumItem</c> element) (🔒)
/// </summary>
public sealed class TuningDescriptionEnumItem
{
    internal TuningDescriptionEnumItem(XElement source)
    {
        Attributes = TuningDescriptionUtilities.GetAttributes(source);
        Name = Attributes.GetValueOrDefault("name") ?? string.Empty;
        Value = long.TryParse(Attributes.GetValueOrDefault("enum_value"), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new InvalidDataException($"Enumeration member {Name} does not have an integer enum_value");
    }

    /// <summary>
    /// Gets all of the member's attributes, by name
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>
    /// Gets the unique identifier Maxis assigns the member (its <c>muid</c> attribute)
    /// </summary>
    public ulong? Muid =>
        TuningDescriptionUtilities.ParseMuid(Attributes.GetValueOrDefault("muid"));

    /// <summary>
    /// Gets the name of the member (its <c>name</c> attribute)
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the value of the member (its <c>enum_value</c> attribute)
    /// </summary>
    public long Value { get; }
}
