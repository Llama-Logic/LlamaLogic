namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents the description of a tunable that is a sequence of values described by <see cref="Item"/> (a <c>TunableList</c> element), including sets and mappings (🔒)
/// </summary>
public sealed class ListTunableDescription :
    TunableDescription
{
    internal ListTunableDescription(XElement source) :
        base(source)
    {
    }

    /// <summary>
    /// Gets whether tuning may not repeat an entry (its <c>unique_entries</c> attribute)
    /// </summary>
    public bool AreEntriesUnique =>
        TuningDescriptionUtilities.ParseBoolean(Attributes.GetValueOrDefault("unique_entries")) ?? false;

    /// <summary>
    /// Gets whether the list is a mapping, the items of which are pairs of the members named by <see cref="MappingKey"/> and <see cref="MappingValue"/>
    /// </summary>
    public bool IsMapping =>
        MappingKey is not null;

    /// <summary>
    /// Gets the description of the list's items, or <see langword="null"/> if the description has none
    /// </summary>
    public TunableDescription? Item =>
        Members.Count > 0 ? Members[0] : null;

    /// <summary>
    /// Gets the name of the class of the mapping's pairs (its <c>mapping_class</c> attribute)
    /// </summary>
    public string? MappingClass =>
        Attributes.GetValueOrDefault("mapping_class");

    /// <summary>
    /// Gets the name of the member of <see cref="Item"/> that is the key of each pair (its <c>mapping_key</c> attribute)
    /// </summary>
    public string? MappingKey =>
        Attributes.GetValueOrDefault("mapping_key");

    /// <summary>
    /// Gets the name of the member of <see cref="Item"/> that is the value of each pair (its <c>mapping_value</c> attribute)
    /// </summary>
    public string? MappingValue =>
        Attributes.GetValueOrDefault("mapping_value");
}
