namespace LlamaLogic.Packages.Models.SimData;

/// <summary>
/// Represents a column of a <see cref="SimDataSchema"/> (🔒)
/// </summary>
public sealed class SimDataColumn
{
    /// <summary>
    /// Initializes a new <see cref="SimDataColumn"/>
    /// </summary>
    /// <param name="name">The name of the column</param>
    /// <param name="type">The type of the column's values</param>
    /// <param name="flags">The column's flags (Maxis sets none)</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> is not a defined <see cref="SimDataType"/></exception>
    public SimDataColumn(string name, SimDataType type, ushort flags = 0)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!type.IsDefined())
            throw new ArgumentOutOfRangeException(nameof(type), type, "Not a SimData type");
        Name = name;
        Type = type;
        Flags = flags;
    }

    /// <summary>
    /// Gets the column's flags
    /// </summary>
    public ushort Flags { get; }

    /// <summary>
    /// Gets the name of the column
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type of the column's values
    /// </summary>
    public SimDataType Type { get; }
}
