namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents a change Maxis recorded in a document's version history, identifying tunables by their <see cref="TunableDescription.Muid"/> (🔒)
/// </summary>
public sealed class TuningDescriptionAction
{
    internal TuningDescriptionAction(XElement source)
    {
        Attributes = TuningDescriptionUtilities.GetAttributes(source);
        TypeName = Attributes.GetValueOrDefault("type") ?? string.Empty;
        Type = TypeName switch
        {
            "kRemove" => TuningDescriptionActionType.Remove,
            "kRename" => TuningDescriptionActionType.Rename,
            "kUpdateVariantTypeField" => TuningDescriptionActionType.UpdateVariantTypeField,
            "kChangeInheritance" => TuningDescriptionActionType.ChangeInheritance,
            _ => TuningDescriptionActionType.Unknown
        };
        OldMuid = TuningDescriptionUtilities.ParseMuid(Attributes.GetValueOrDefault("old_muid"));
        NewMuid = TuningDescriptionUtilities.ParseMuid(Attributes.GetValueOrDefault("new_muid"));
        TargetMuid = TuningDescriptionUtilities.ParseMuid(Attributes.GetValueOrDefault("target_muid"));
        SourceTypeValue = Attributes.GetValueOrDefault("source_type_value");
        TargetTypeValue = Attributes.GetValueOrDefault("target_type_value");
    }

    /// <summary>
    /// Gets all of the action's attributes, by name
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>
    /// Gets the MUID that replaces <see cref="OldMuid"/> (its <c>new_muid</c> attribute)
    /// </summary>
    public ulong? NewMuid { get; }

    /// <summary>
    /// Gets the MUID the action removes or renames (its <c>old_muid</c> attribute)
    /// </summary>
    public ulong? OldMuid { get; }

    /// <summary>
    /// Gets the option value the action changes from (its <c>source_type_value</c> attribute)
    /// </summary>
    public string? SourceTypeValue { get; }

    /// <summary>
    /// Gets the MUID of the tunable the action changes (its <c>target_muid</c> attribute)
    /// </summary>
    public ulong? TargetMuid { get; }

    /// <summary>
    /// Gets the option value the action changes to (its <c>target_type_value</c> attribute)
    /// </summary>
    public string? TargetTypeValue { get; }

    /// <summary>
    /// Gets the type of the action
    /// </summary>
    public TuningDescriptionActionType Type { get; }

    /// <summary>
    /// Gets the type of the action as written (its <c>type</c> attribute)
    /// </summary>
    public string TypeName { get; }
}
