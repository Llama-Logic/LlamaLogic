namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents the description of a Python module's tuning and enumerations (a <c>Module</c> element), such as <c>sims.sim_info_types</c> (🔒)
/// </summary>
public sealed class TuningDescriptionModule :
    TuningDescriptionDocument
{
    internal TuningDescriptionModule(TuningDescriptionSet set, string path, XElement source, XElement? versionHistory) :
        base(set, path, source, versionHistory)
    {
        Name = Attributes.GetValueOrDefault("name") ?? string.Empty;
        Classes = [.. source.Elements("Class").Select(@class => new TuningDescriptionClass(this, null, @class))];
    }

    /// <summary>
    /// Gets the module's classes, in document order
    /// </summary>
    public IReadOnlyList<TuningDescriptionClass> Classes { get; }

    /// <summary>
    /// Gets the name of the module (its <c>name</c> attribute)
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the class of the module with the specified <paramref name="name"/>, or <see langword="null"/> if there is none
    /// </summary>
    public TuningDescriptionClass? GetClass(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Classes.FirstOrDefault(@class => @class.Name == name);
    }
}
