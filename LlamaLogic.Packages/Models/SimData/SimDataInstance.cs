namespace LlamaLogic.Packages.Models.SimData;

/// <summary>
/// Represents a named, top-level <see cref="SimDataObject"/> in a <see cref="SimDataModel"/> (🔓)
/// </summary>
public sealed class SimDataInstance
{
    /// <summary>
    /// Initializes a new <see cref="SimDataInstance"/>
    /// </summary>
    public SimDataInstance(string name, SimDataObject @object)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(@object);
        this.name = name;
        Object = @object;
    }

    string name;

    /// <summary>
    /// Gets/sets the name of the instance
    /// </summary>
    public string Name
    {
        get => name;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            name = value;
        }
    }

    /// <summary>
    /// Gets the object
    /// </summary>
    public SimDataObject Object { get; }
}
