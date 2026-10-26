namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies what the value of an element of a tuning document means, according to its tuning description
/// </summary>
public enum TuningValueKind
{
    /// <summary>
    /// The element has no value of its own, or its meaning is not known
    /// </summary>
    None = 0,

    /// <summary>
    /// The value refers to another tuning or resource by instance (or, occasionally, by name)
    /// </summary>
    Reference,

    /// <summary>
    /// The value is the key of a localized string
    /// </summary>
    LocalizedString,

    /// <summary>
    /// The value is a member of an enumeration
    /// </summary>
    Enum,

    /// <summary>
    /// The value is a resource key
    /// </summary>
    ResourceKey,

    /// <summary>
    /// The value is a number, Boolean or text
    /// </summary>
    Scalar
}