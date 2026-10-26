namespace LlamaLogic.Packages.Models.AudioConfiguration;

/// <summary>
/// Represents the type of the value of an <see cref="AudioConfigurationProperty"/>
/// </summary>
public enum AudioConfigurationPropertyType
{
    /// <summary>
    /// No type (no property of the game's configurations has it)
    /// </summary>
    None = 0,

    /// <summary>
    /// A <see cref="float"/>
    /// </summary>
    Float = 1,

    /// <summary>
    /// A <see cref="uint"/>
    /// </summary>
    UInt32 = 2,

    /// <summary>
    /// A <see cref="ulong"/>
    /// </summary>
    UInt64 = 3,

    /// <summary>
    /// A <see cref="bool"/>
    /// </summary>
    Boolean = 4,

    /// <summary>
    /// A <see cref="string"/>
    /// </summary>
    String = 5,

    /// <summary>
    /// A response curve, a <see cref="Collection{T}"/> of <see cref="AudioConfigurationCurvePoint"/>
    /// </summary>
    Curve = 6,

    /// <summary>
    /// A set of 64-bit values, a <see cref="Collection{T}"/> of <see cref="ulong"/>
    /// </summary>
    LongSet = 7
}