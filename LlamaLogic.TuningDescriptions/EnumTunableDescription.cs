namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents the description of a tunable whose value is a member of an enumeration (a <c>TunableEnum</c> element) (🔒)
/// </summary>
public sealed class EnumTunableDescription :
    TunableDescription
{
    internal EnumTunableDescription(XElement source) :
        base(source)
    {
    }

    /// <summary>
    /// Gets the type the tunable's value takes when it is exported (its <c>binary_type</c> attribute)
    /// </summary>
    public string? BinaryType =>
        Attributes.GetValueOrDefault("binary_type");

    /// <summary>
    /// Gets the module tuning that adds members to the enumeration at run time (its <c>dynamic_entries</c> attribute), such as <c>tag.Tag._elements</c>
    /// </summary>
    /// <remarks>
    /// Tuning descriptions do not contain these members; the game's module tuning does.
    /// </remarks>
    public string? DynamicEntries =>
        Attributes.GetValueOrDefault("dynamic_entries");

    /// <summary>
    /// Gets the members of the enumeration tuning may not use (its <c>invalid_entries</c> attribute), as written
    /// </summary>
    public string? InvalidEntries =>
        Attributes.GetValueOrDefault("invalid_entries");

    /// <summary>
    /// Gets the enumeration the tunable's values come from (its <c>static_entries</c> attribute), which <see cref="TuningDescriptionSet.GetEnum(string)"/> finds
    /// </summary>
    public string? StaticEntries =>
        Attributes.GetValueOrDefault("static_entries");

    /// <summary>
    /// Gets the name of the enumeration (its <c>type</c> attribute)
    /// </summary>
    public string? Type =>
        Attributes.GetValueOrDefault("type");
}
