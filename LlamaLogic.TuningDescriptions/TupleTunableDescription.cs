namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents the description of a tunable composed of named tunables (a <c>TunableTuple</c> element) (🔒)
/// </summary>
public sealed class TupleTunableDescription :
    TunableDescription
{
    internal TupleTunableDescription(XElement source) :
        base(source)
    {
    }
}
