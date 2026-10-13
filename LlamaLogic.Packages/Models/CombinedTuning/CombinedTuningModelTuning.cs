namespace LlamaLogic.Packages.Models.CombinedTuning;

/// <summary>
/// Represents an individual tuning document from a <see cref="CombinedTuningModel"/> (🔓)
/// </summary>
public sealed class CombinedTuningModelTuning
{
    internal CombinedTuningModelTuning(string typeName, ulong instance, string? name, XDocument document)
    {
        TypeName = typeName;
        Instance = instance;
        Name = name;
        Document = document;
    }

    /// <summary>
    /// Gets the tuning document, as it would appear in a <see cref="ResourceFileType.TuningMarkup"/> resource
    /// </summary>
    public XDocument Document { get; }

    /// <summary>
    /// Gets the instance of the tuning (its <c>s</c> attribute)
    /// </summary>
    public ulong Instance { get; }

    /// <summary>
    /// Gets the name of the tuning (its <c>n</c> attribute), if it has one
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the name the merged document gives the tuning's type (the <c>n</c> attribute of its <c>&lt;R&gt;</c> group, e.g. <c>buff</c> or <c>Renderer_GlobalShadowSettings</c>)
    /// </summary>
    public string TypeName { get; }
}
