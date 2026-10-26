namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents a class described in a <see cref="TuningDescriptionModule"/> (a <c>Class</c> element): an enumeration, the holder of module tuning, or both (🔒)
/// </summary>
public sealed class TuningDescriptionClass
{
    internal TuningDescriptionClass(TuningDescriptionModule module, TuningDescriptionClass? parent, XElement source)
    {
        Module = module;
        Parent = parent;
        Attributes = TuningDescriptionUtilities.GetAttributes(source);
        Name = Attributes.GetValueOrDefault("name") ?? string.Empty;
        StaticEntriesName = $"{parent?.StaticEntriesName ?? module.Name.Replace('.', '-')}.{Name}";
        Classes = [.. source.Elements("Class").Select(@class => new TuningDescriptionClass(module, this, @class))];
        EnumItems = [.. source.Elements("EnumItem").Select(item => new TuningDescriptionEnumItem(item))];
        Members = TunableDescription.CreateMembers(source);
    }

    /// <summary>
    /// Gets all of the class's attributes, by name
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>
    /// Gets the classes nested in this one, in document order
    /// </summary>
    public IReadOnlyList<TuningDescriptionClass> Classes { get; }

    /// <summary>
    /// Gets the members of the enumeration the class is, in document order
    /// </summary>
    public IReadOnlyList<TuningDescriptionEnumItem> EnumItems { get; }

    /// <summary>
    /// Gets whether the class is an enumeration of bit flags (its <c>enum_bit_flag</c> attribute)
    /// </summary>
    public bool IsBitFlagEnum =>
        TuningDescriptionUtilities.ParseBoolean(Attributes.GetValueOrDefault("enum_bit_flag")) ?? false;

    /// <summary>
    /// Gets whether the class is an enumeration
    /// </summary>
    public bool IsEnum =>
        EnumItems.Count > 0 || Attributes.ContainsKey("enum_bit_flag");

    /// <summary>
    /// Gets whether the enumeration's members are fixed (its <c>enum_locked</c> attribute)
    /// </summary>
    public bool IsLockedEnum =>
        TuningDescriptionUtilities.ParseBoolean(Attributes.GetValueOrDefault("enum_locked")) ?? false;

    /// <summary>
    /// Gets the module tuning the class declares, in document order
    /// </summary>
    public IReadOnlyList<TunableDescription> Members { get; }

    /// <summary>
    /// Gets the module in which the class is described
    /// </summary>
    public TuningDescriptionModule Module { get; }

    /// <summary>
    /// Gets the unique identifier Maxis assigns the class (its <c>muid</c> attribute)
    /// </summary>
    public ulong? Muid =>
        TuningDescriptionUtilities.ParseMuid(Attributes.GetValueOrDefault("muid"));

    /// <summary>
    /// Gets the name of the class (its <c>name</c> attribute)
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the class in which this one is nested, or <see langword="null"/> if it is not nested
    /// </summary>
    public TuningDescriptionClass? Parent { get; }

    /// <summary>
    /// Gets the name by which a <see cref="EnumTunableDescription.StaticEntries"/> attribute refers to the class, such as <c>sims-sim_info_types.Age</c>
    /// </summary>
    public string StaticEntriesName { get; }

    /// <summary>
    /// Gets the nested class with the specified <paramref name="name"/>, or <see langword="null"/> if there is none
    /// </summary>
    public TuningDescriptionClass? GetClass(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Classes.FirstOrDefault(@class => @class.Name == name);
    }

    /// <summary>
    /// Gets the value of the enumeration member with the specified <paramref name="name"/>, or <see langword="null"/> if there is none
    /// </summary>
    public long? GetEnumValue(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return EnumItems.FirstOrDefault(item => item.Name == name)?.Value;
    }
}
