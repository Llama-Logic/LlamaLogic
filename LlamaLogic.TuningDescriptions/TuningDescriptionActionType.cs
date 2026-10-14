namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// The type of a <see cref="TuningDescriptionAction"/>
/// </summary>
public enum TuningDescriptionActionType
{
    /// <summary>
    /// A type this library does not recognize (see <see cref="TuningDescriptionAction.TypeName"/>)
    /// </summary>
    Unknown,

    /// <summary>
    /// A tunable was removed (<c>kRemove</c>)
    /// </summary>
    Remove,

    /// <summary>
    /// A tunable was renamed (<c>kRename</c>)
    /// </summary>
    Rename,

    /// <summary>
    /// The value naming a variant's option changed (<c>kUpdateVariantTypeField</c>)
    /// </summary>
    UpdateVariantTypeField,

    /// <summary>
    /// A class's inheritance changed (<c>kChangeInheritance</c>)
    /// </summary>
    ChangeInheritance
}
