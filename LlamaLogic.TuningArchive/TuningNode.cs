namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents an element of a tuning document, as understood through its tuning description
/// </summary>
public sealed class TuningNode
{
    internal TuningNode(int line, int valueStartColumn, int valueEndColumn, string path, string elementName, string? value, TunableDescription? description, TuningValueKind valueKind)
    {
        Line = line;
        ValueStartColumn = valueStartColumn;
        ValueEndColumn = valueEndColumn;
        Path = path;
        ElementName = elementName;
        Value = value;
        Description = description;
        ValueKind = valueKind;
    }

    /// <summary>
    /// Gets the tunable description of the element, if one was found
    /// </summary>
    public TunableDescription? Description { get; }

    /// <summary>
    /// Gets the name of the element (such as <c>T</c>, <c>L</c>, <c>U</c>, <c>V</c> or <c>E</c>)
    /// </summary>
    public string ElementName { get; }

    /// <summary>
    /// Gets the enumeration of the value, if <see cref="ValueKind"/> is <see cref="TuningValueKind.Enum"/> and it was found
    /// </summary>
    public TuningDescriptionClass? Enum { get; internal init; }

    /// <summary>
    /// Gets the numeric value of the enumeration member, if it was found
    /// </summary>
    public long? EnumValue { get; internal init; }

    /// <summary>
    /// Gets the option the element chooses (its <c>t</c> attribute), if it is a variant
    /// </summary>
    public string? Option { get; internal init; }

    /// <summary>
    /// Gets the 1-based line on which the element starts in the canonical text
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the path of the element from the root, such as <c>loot_list[2].stat</c>
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the instance the value refers to, if <see cref="ValueKind"/> is <see cref="TuningValueKind.Reference"/> and it is numeric
    /// </summary>
    public ulong? ReferenceInstance { get; internal init; }

    /// <summary>
    /// Gets the tunables the tuning descriptions declare for the element, in the order they declare them, if it is a tuple; otherwise, an empty list
    /// </summary>
    public IReadOnlyList<TunableDescription> Members { get; internal init; } = [];

    /// <summary>
    /// Gets the name the value refers to, if <see cref="ValueKind"/> is <see cref="TuningValueKind.Reference"/> and it is not numeric
    /// </summary>
    public string? ReferenceName { get; internal init; }

    /// <summary>
    /// Gets the resource type the value refers to, if <see cref="ValueKind"/> is <see cref="TuningValueKind.Reference"/>
    /// </summary>
    public ResourceType? ReferenceType { get; internal init; }

    /// <summary>
    /// Gets the name of the type the value refers to (as the tuning description names it, such as <c>buff</c>), if <see cref="ValueKind"/> is <see cref="TuningValueKind.Reference"/>
    /// </summary>
    public string? ReferenceTypeName { get; internal init; }

    /// <summary>
    /// Gets the resource key of the value, if <see cref="ValueKind"/> is <see cref="TuningValueKind.ResourceKey"/>
    /// </summary>
    public ResourceKey? ResourceKey { get; internal init; }

    /// <summary>
    /// Gets the key of the localized string, if <see cref="ValueKind"/> is <see cref="TuningValueKind.LocalizedString"/>
    /// </summary>
    public uint? StringKey { get; internal init; }

    /// <summary>
    /// Gets the text value of the element, if it has one
    /// </summary>
    public string? Value { get; }

    /// <summary>
    /// Gets the 1-based column at which the element's value ends (exclusive), or 0 if the value is not on <see cref="Line"/>
    /// </summary>
    public int ValueEndColumn { get; }

    /// <summary>
    /// Gets what the value of the element means
    /// </summary>
    public TuningValueKind ValueKind { get; }

    internal TuningNode With(string? referenceTypeName = null, ResourceType? referenceType = null, ulong? referenceInstance = null, string? referenceName = null, uint? stringKey = null, ResourceKey? resourceKey = null, TuningDescriptionClass? enumClass = null, long? enumValue = null, IReadOnlyList<TunableDescription>? members = null, string? option = null) =>
        new(Line, ValueStartColumn, ValueEndColumn, Path, ElementName, Value, Description, ValueKind)
        {
            ReferenceTypeName = referenceTypeName ?? ReferenceTypeName,
            ReferenceType = referenceType ?? ReferenceType,
            ReferenceInstance = referenceInstance ?? ReferenceInstance,
            ReferenceName = referenceName ?? ReferenceName,
            StringKey = stringKey ?? StringKey,
            ResourceKey = resourceKey ?? ResourceKey,
            Enum = enumClass ?? Enum,
            EnumValue = enumValue ?? EnumValue,
            Members = members ?? Members,
            Option = option ?? Option
        };

    /// <summary>
    /// Gets the 1-based column at which the element's value starts, or 0 if the value is not on <see cref="Line"/>
    /// </summary>
    public int ValueStartColumn { get; }
}