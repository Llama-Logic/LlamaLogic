namespace LlamaLogic.Packages.Models.World;

/// <summary>
/// Represents an object manager in a <see cref="ResourceType.WorldData"/> resource: a group of either lots or objects (🔓)
/// </summary>
/// <remarks>
/// In world files a manager whose <see cref="Id"/> is zero holds the objects of the world outside lots, and every other manager holds one lot, with the lot's id in the low 32 bits of <see cref="Id"/>.
/// </remarks>
public sealed class WorldDataObjectManager
{
    /// <summary>
    /// Gets/sets the id of the manager
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// Gets the lots of the manager
    /// </summary>
    public Collection<WorldDataLot> Lots { get; } = [];

    /// <summary>
    /// Gets the objects of the manager
    /// </summary>
    public Collection<WorldDataObject> Objects { get; } = [];
}
