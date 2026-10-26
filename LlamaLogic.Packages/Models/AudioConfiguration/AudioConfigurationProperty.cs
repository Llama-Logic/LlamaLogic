namespace LlamaLogic.Packages.Models.AudioConfiguration;

/// <summary>
/// Represents a property of an <see cref="AudioConfigurationModel"/>: a hashed key and a typed value (🔓)
/// </summary>
public sealed class AudioConfigurationProperty
{
    /// <summary>
    /// Initializes a new property
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not of a type a property can have (see <see cref="AudioConfigurationPropertyType"/>)</exception>
    public AudioConfigurationProperty(uint key, object value)
    {
        Key = key;
        Value = value;
    }

    object value = 0F;

    /// <summary>
    /// Gets/sets the hashed key of the property
    /// </summary>
    public uint Key { get; set; }

    /// <summary>
    /// Gets the type of the property's value
    /// </summary>
    public AudioConfigurationPropertyType Type =>
        GetType(value)!.Value;

    /// <summary>
    /// Gets/sets the value of the property: a <see cref="float"/>, <see cref="uint"/>, <see cref="ulong"/>, <see cref="bool"/>, <see cref="string"/>, <see cref="Collection{T}"/> of <see cref="AudioConfigurationCurvePoint"/> or <see cref="Collection{T}"/> of <see cref="ulong"/>
    /// </summary>
    /// <exception cref="ArgumentException">The value is not of one of those types</exception>
    public object Value
    {
        get => value;
        set
        {
            if (GetType(value) is null)
                throw new ArgumentException($"A property cannot have a value of type {value?.GetType().Name ?? "null"}", nameof(value));
            this.value = value;
        }
    }

    static AudioConfigurationPropertyType? GetType(object? value) =>
        value switch
        {
            float => AudioConfigurationPropertyType.Float,
            uint => AudioConfigurationPropertyType.UInt32,
            ulong => AudioConfigurationPropertyType.UInt64,
            bool => AudioConfigurationPropertyType.Boolean,
            string => AudioConfigurationPropertyType.String,
            Collection<AudioConfigurationCurvePoint> => AudioConfigurationPropertyType.Curve,
            Collection<ulong> => AudioConfigurationPropertyType.LongSet,
            _ => null
        };
}