namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents a reference to a shared description (a <c>TdescFragTag</c> element), whose <see cref="TunableDescription.Class"/> names a <see cref="TuningDescriptionFragment"/> (🔒)
/// </summary>
/// <remarks>
/// <see cref="TuningDescriptionSet.Resolve(TunableDescription)"/> finds the description the reference stands for.
/// </remarks>
public sealed class FragmentReferenceTunableDescription :
    TunableDescription
{
    internal FragmentReferenceTunableDescription(XElement source) :
        base(source)
    {
    }
}
