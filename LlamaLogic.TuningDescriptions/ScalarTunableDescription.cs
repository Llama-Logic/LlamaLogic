namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents the description of a single-valued tunable (a <c>Tunable</c> element) (🔒)
/// </summary>
public sealed class ScalarTunableDescription :
    TunableDescription
{
    internal ScalarTunableDescription(XElement source) :
        base(source)
    {
    }

    /// <summary>
    /// Gets whether the tunable may be <c>None</c> (its <c>allow_none</c> attribute), or <see langword="null"/> if the description does not say
    /// </summary>
    public bool? AllowNone =>
        TuningDescriptionUtilities.ParseBoolean(Attributes.GetValueOrDefault("allow_none"));

    /// <summary>
    /// Gets the type the tunable's value takes when it is exported (its <c>binary_type</c> attribute)
    /// </summary>
    public string? BinaryType =>
        Attributes.GetValueOrDefault("binary_type");

    /// <summary>
    /// Gets the type of the tunable's value (its <c>type</c> attribute): a Python type such as <c>int</c>, <c>ResourceKey</c>, or the instance type of the tuning it references
    /// </summary>
    public string? Type =>
        Attributes.GetValueOrDefault("type");
}
