namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents the description of a tunable whose value is one of several named options (a <c>TunableVariant</c> element) (🔒)
/// </summary>
/// <remarks>
/// <see cref="TunableDescription.Default"/> is the name of the option tuning gets when it does not choose one.
/// </remarks>
public sealed class VariantTunableDescription :
    TunableDescription
{
    internal VariantTunableDescription(XElement source) :
        base(source)
    {
    }

    /// <summary>
    /// Gets the options of the variant, in document order
    /// </summary>
    public IReadOnlyList<TunableDescription> Options =>
        Members;

    /// <summary>
    /// Gets the option with the specified <paramref name="name"/>, or <see langword="null"/> if there is none
    /// </summary>
    public TunableDescription? GetOption(string name) =>
        GetMember(name);
}
