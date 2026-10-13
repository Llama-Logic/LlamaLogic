namespace LlamaLogic.Packages.Models.CombinedTuning;

/// <summary>
/// Represents an individual tuning document from a <see cref="CombinedTuningModel"/> (🔓)
/// </summary>
public sealed class CombinedTuningModelTuning
{
    static uint GetCasePreservingFnv32Hash(string text)
    {
        using var fnv32 = new Fnv32();
        return BinaryPrimitives.ReadUInt32LittleEndian(fnv32.ComputeHash(Encoding.UTF8.GetBytes(text)));
    }

    internal CombinedTuningModelTuning(string typeName, ulong instance, string? name, XDocument document)
    {
        TypeName = typeName;
        Instance = instance;
        Name = name;
        Document = document;
        ResourceType = typeName switch
        {
            "notebook_entry" => ResourceType.NotebookEntryTuning,
            "relbit" => ResourceType.RelationshipBitTuning,
            "tun" => ResourceType.Tuning,
            _ => (ResourceType)GetCasePreservingFnv32Hash(typeName)
        };
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
    /// Gets the type of the tuning's resource, derived from <see cref="TypeName"/> the way the game does
    /// </summary>
    /// <remarks>
    /// The game's type is the FNV-1 32-bit hash of the type name, except for <c>tun</c>, <c>relbit</c> and <c>notebook_entry</c>, which it assigns <see cref="ResourceType.Tuning"/>, <see cref="ResourceType.RelationshipBitTuning"/> and <see cref="ResourceType.NotebookEntryTuning"/>.
    /// The value may not be a defined <see cref="LlamaLogic.Packages.ResourceType"/> member when the game introduces a type this library does not yet name.
    /// </remarks>
    public ResourceType ResourceType { get; }

    /// <summary>
    /// Gets the name the merged document gives the tuning's type (the <c>n</c> attribute of its <c>&lt;R&gt;</c> group, e.g. <c>buff</c> or <c>Renderer_GlobalShadowSettings</c>)
    /// </summary>
    public string TypeName { get; }
}
