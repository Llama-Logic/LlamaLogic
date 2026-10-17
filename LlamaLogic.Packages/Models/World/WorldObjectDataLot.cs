namespace LlamaLogic.Packages.Models.World;

/// <summary>
/// Represents the objects and locators of one lot (or, with lot id zero, of the world outside lots) in a <see cref="ResourceType.WorldObjectData"/> resource (🔓)
/// </summary>
public sealed class WorldObjectDataLot
{
    /// <summary>
    /// Gets the objects for which the game creates only a routing footprint
    /// </summary>
    public Collection<WorldObjectDataObject> FootprintObjects { get; } = [];

    /// <summary>
    /// Gets/sets the key of a footprint container resource the game loads for the lot, or a key whose type is <c>0xffffffff</c> when there is none
    /// </summary>
    /// <remarks>
    /// In world files the key's type is <c>0x4f726bbe</c> (<see cref="ResourceType.WorldLandingStrip"/>) or <c>0xffffffff</c>; what the resource holds is not established.
    /// </remarks>
    public ResourceKey FootprintKey { get; set; }

    /// <summary>
    /// Gets/sets the id of the lot
    /// </summary>
    public uint LotId { get; set; }

    /// <summary>
    /// Gets the objects the game spawns, parents before children
    /// </summary>
    public Collection<WorldObjectDataObject> Objects { get; } = [];

    /// <summary>
    /// Gets the locators the game hands to the simulation as sim locators
    /// </summary>
    public Collection<WorldObjectDataLocator> SimLocators { get; } = [];

    /// <summary>
    /// Gets the stair locators
    /// </summary>
    public Collection<WorldObjectDataStairLocator> StairLocators { get; } = [];
}
