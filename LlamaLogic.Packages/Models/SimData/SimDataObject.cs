namespace LlamaLogic.Packages.Models.SimData;

/// <summary>
/// Represents an object in a <see cref="ResourceType.SimData"/> resource: a row of values described by a <see cref="SimDataSchema"/> (🔓)
/// </summary>
/// <remarks>
/// Each value's .NET type is determined by its column's <see cref="SimDataType"/>:
/// <see cref="SimDataType.BOOL"/> is <see cref="bool"/>;
/// <see cref="SimDataType.CHAR8"/> and <see cref="SimDataType.UINT8"/> are <see cref="byte"/>;
/// <see cref="SimDataType.INT8"/> is <see cref="sbyte"/>;
/// <see cref="SimDataType.INT16"/> is <see cref="short"/>;
/// <see cref="SimDataType.UINT16"/> is <see cref="ushort"/>;
/// <see cref="SimDataType.INT32"/> is <see cref="int"/>;
/// <see cref="SimDataType.UINT32"/> and <see cref="SimDataType.LOCKEY"/> are <see cref="uint"/>;
/// <see cref="SimDataType.INT64"/> is <see cref="long"/>;
/// <see cref="SimDataType.UINT64"/> and <see cref="SimDataType.TABLESETREFERENCE"/> are <see cref="ulong"/>;
/// <see cref="SimDataType.FLOAT"/> is <see cref="float"/>;
/// <see cref="SimDataType.FLOAT2"/>, <see cref="SimDataType.FLOAT3"/> and <see cref="SimDataType.FLOAT4"/> are <see cref="Vector2"/>, <see cref="Vector3"/> and <see cref="Vector4"/>;
/// <see cref="SimDataType.RESOURCEKEY"/> is <see cref="ResourceKey"/>;
/// <see cref="SimDataType.STRING8"/> and <see cref="SimDataType.HASHEDSTRING8"/> are <see cref="string"/> (or <see langword="null"/>; the hash of a <see cref="SimDataType.HASHEDSTRING8"/> is computed when encoding);
/// <see cref="SimDataType.OBJECT"/> is <see cref="SimDataObject"/>, <see cref="SimDataType.VECTOR"/> is <see cref="SimDataVector"/>, and <see cref="SimDataType.VARIANT"/> is <see cref="SimDataVariant"/> (each or <see langword="null"/>).
/// </remarks>
public sealed class SimDataObject
{
    /// <summary>
    /// Initializes a new <see cref="SimDataObject"/> with the specified <paramref name="schema"/>, all of its values defaulted (zero, or <see langword="null"/> for references)
    /// </summary>
    public SimDataObject(SimDataSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        Schema = schema;
        values = [.. schema.Columns.Select(column => column.Type.GetDefaultValue())];
    }

    readonly object?[] values;

    /// <summary>
    /// Gets the schema of the object
    /// </summary>
    public SimDataSchema Schema { get; }

    /// <summary>
    /// Gets/sets the value of the column at the specified <paramref name="columnIndex"/>
    /// </summary>
    /// <exception cref="ArgumentException">The value is not of the .NET type the column's <see cref="SimDataType"/> requires</exception>
    public object? this[int columnIndex]
    {
        get => values[columnIndex];
        set => values[columnIndex] = Schema.Columns[columnIndex].Type.RequireValue(value, nameof(value));
    }

    /// <summary>
    /// Gets/sets the value of the column with the specified <paramref name="columnName"/>
    /// </summary>
    /// <exception cref="ArgumentException">There is no such column, or the value is not of the .NET type the column's <see cref="SimDataType"/> requires</exception>
    public object? this[string columnName]
    {
        get => values[GetRequiredColumnIndex(columnName)];
        set => this[GetRequiredColumnIndex(columnName)] = value;
    }

    int GetRequiredColumnIndex(string columnName) =>
        Schema.GetColumnIndex(columnName) ?? throw new ArgumentException($"Schema {Schema.Name} has no column named {columnName}", nameof(columnName));
}
