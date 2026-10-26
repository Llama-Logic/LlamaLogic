namespace LlamaLogic.Packages.Models.SimData;

/// <summary>
/// Represents a <see cref="SimDataType.VARIANT"/> value: a type hash and an optional value of any <see cref="SimDataType"/> (🔒)
/// </summary>
public sealed class SimDataVariant
{
    /// <summary>
    /// Initializes a new <see cref="SimDataVariant"/> with the specified <paramref name="typeHash"/> and no value
    /// </summary>
    public SimDataVariant(uint typeHash)
    {
        TypeHash = typeHash;
    }

    /// <summary>
    /// Initializes a new <see cref="SimDataVariant"/> with the specified <paramref name="typeHash"/> and a <paramref name="value"/> of the specified <paramref name="type"/>
    /// </summary>
    /// <param name="typeHash">The type hash of the variant</param>
    /// <param name="type">The type of <paramref name="value"/></param>
    /// <param name="value">The value, which may be <see langword="null"/> only for <see cref="SimDataType.STRING8"/>, <see cref="SimDataType.HASHEDSTRING8"/>, <see cref="SimDataType.VECTOR"/> and <see cref="SimDataType.VARIANT"/></param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> is not a defined <see cref="SimDataType"/></exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not of the .NET type <see cref="SimDataObject"/> describes for <paramref name="type"/>, or is <see langword="null"/> for <see cref="SimDataType.OBJECT"/></exception>
    public SimDataVariant(uint typeHash, SimDataType type, object? value)
    {
        if (!type.IsDefined())
            throw new ArgumentOutOfRangeException(nameof(type), type, "Not a SimData type");
        if (type is SimDataType.OBJECT && value is null)
            throw new ArgumentException("The value of a variant of an object cannot be null", nameof(value));
        TypeHash = typeHash;
        Type = type;
        Value = type.RequireValue(value, nameof(value));
    }

    /// <summary>
    /// Gets the type of <see cref="Value"/>, or <see langword="null"/> if the variant has no value
    /// </summary>
    public SimDataType? Type { get; }

    /// <summary>
    /// Gets the type hash of the variant
    /// </summary>
    public uint TypeHash { get; }

    /// <summary>
    /// Gets the value of the variant (<see langword="null"/> when <see cref="Type"/> is <see langword="null"/>, and possibly when it is a type that admits <see langword="null"/>)
    /// </summary>
    public object? Value { get; }
}
