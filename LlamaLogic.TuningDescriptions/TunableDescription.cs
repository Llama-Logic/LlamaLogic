namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents the description of a tunable (🔒)
/// </summary>
/// <remarks>
/// Every attribute of the description's XML element is available in <see cref="Attributes"/>, including those this library does not give properties of their own.
/// </remarks>
public abstract class TunableDescription
{
    internal static TunableDescription Create(XElement source) =>
        source.Name.LocalName switch
        {
            "Tunable" => new ScalarTunableDescription(source),
            "TunableTuple" => new TupleTunableDescription(source),
            "TunableVariant" => new VariantTunableDescription(source),
            "TunableEnum" => new EnumTunableDescription(source),
            "TunableList" => new ListTunableDescription(source),
            "TdescFragTag" => new FragmentReferenceTunableDescription(source),
            _ => new UnknownTunableDescription(source)
        };

    internal static ImmutableArray<TunableDescription> CreateMembers(XElement parent) =>
        [.. parent.Elements().Where(element => element.Name.LocalName is not ("Deleted" or "Class" or "EnumItem")).Select(Create)];

    private protected TunableDescription(XElement source)
    {
        Source = source;
        Attributes = TuningDescriptionUtilities.GetAttributes(source);
        members = new(() => CreateMembers(Source));
    }

    readonly Lazy<ImmutableArray<TunableDescription>> members;

    /// <summary>
    /// Gets all of the description's attributes, by name
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>
    /// Gets the Python class of the tunable (its <c>class</c> attribute)
    /// </summary>
    public string? Class =>
        Attributes.GetValueOrDefault("class");

    /// <summary>
    /// Gets the tunable's default value, as written (its <c>default</c> attribute)
    /// </summary>
    public string? Default =>
        Attributes.GetValueOrDefault("default");

    /// <summary>
    /// Gets the description of the tunable for tuners (its <c>description</c> attribute)
    /// </summary>
    public string? Description =>
        Attributes.GetValueOrDefault("description");

    /// <summary>
    /// Gets the display name of the tunable (its <c>display</c> attribute)
    /// </summary>
    public string? Display =>
        Attributes.GetValueOrDefault("display");

    /// <summary>
    /// Gets the destinations to which the tunable is exported (its <c>export_modes</c> attribute), or <see langword="null"/> if the description does not specify them
    /// </summary>
    public TuningDescriptionExportModes? ExportModes =>
        TuningDescriptionUtilities.ParseExportModes(Attributes.GetValueOrDefault("export_modes"));

    /// <summary>
    /// Gets the group of the tunable in tuning editors (its <c>group</c> attribute)
    /// </summary>
    public string? Group =>
        Attributes.GetValueOrDefault("group");

    /// <summary>
    /// Gets whether the tunable is deprecated (its <c>Deprecated</c> attribute)
    /// </summary>
    public bool IsDeprecated =>
        TuningDescriptionUtilities.ParseBoolean(Attributes.GetValueOrDefault("Deprecated")) ?? false;

    /// <summary>
    /// Gets the descriptions nested in this one, in document order
    /// </summary>
    public IReadOnlyList<TunableDescription> Members =>
        members.Value;

    /// <summary>
    /// Gets the unique identifier Maxis assigns the tunable (its <c>muid</c> attribute)
    /// </summary>
    public ulong? Muid =>
        TuningDescriptionUtilities.ParseMuid(Attributes.GetValueOrDefault("muid"));

    /// <summary>
    /// Gets the name of the tunable (its <c>name</c> attribute), or <see langword="null"/> for the item of a list
    /// </summary>
    public string? Name =>
        Attributes.GetValueOrDefault("name");

    internal XElement Source { get; }

    /// <summary>
    /// Gets the nested description with the specified <paramref name="name"/>, or <see langword="null"/> if there is none
    /// </summary>
    public TunableDescription? GetMember(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Members.FirstOrDefault(member => member.Name == name);
    }
}
