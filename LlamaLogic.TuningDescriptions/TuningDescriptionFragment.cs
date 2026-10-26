namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents a description shared by the <see cref="FragmentReferenceTunableDescription"/>s that name it (a <c>TdescFrag</c> element), such as <c>TunableTestSet</c> (🔒)
/// </summary>
public sealed class TuningDescriptionFragment :
    TuningDescriptionDocument
{
    internal TuningDescriptionFragment(TuningDescriptionSet set, string path, XElement source, XElement? versionHistory) :
        base(set, path, source, versionHistory)
    {
        var elements = source.Elements().ToList();
        if (elements.Count != 1)
            throw new InvalidDataException($"The fragment in {path} has {elements.Count} descriptions instead of one");
        Description = TunableDescription.Create(elements[0]);
    }

    /// <summary>
    /// Gets the name by which references refer to the fragment (its description's <c>class</c> attribute)
    /// </summary>
    public string Class =>
        Description.Class ?? string.Empty;

    /// <summary>
    /// Gets the description the fragment shares
    /// </summary>
    public TunableDescription Description { get; }
}
