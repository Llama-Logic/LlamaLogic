namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a pond of the <see cref="PondsSection"/>
/// </summary>
/// <remarks>
/// The layout follows the game's reader.
/// </remarks>
public sealed class Pond
{
    /// <summary>
    /// Gets the tile quarters the pond covers
    /// </summary>
    public Collection<PondCell> Cells { get; } = [];

    /// <summary>
    /// Gets/sets the key of the pond's floor pattern, a CTProductFloorPattern (0xB4F762C9)
    /// </summary>
    /// <remarks>
    /// The <see cref="LotArchitectureSectionType.PondBottoms"/> grid holds this key's id on the pond's quarters in the lots examined.
    /// </remarks>
    public ResourceKey FloorPatternKey { get; set; }

    /// <summary>
    /// Gets/sets the pond's id
    /// </summary>
    /// <remarks>
    /// The game keys its pond map and its cell-to-pond map by it; 1, 2, 3 or 11 in the lots examined.
    /// </remarks>
    public ushort Id { get; set; }

    /// <summary>
    /// Gets a list of values of unknown meaning, written from version 3
    /// </summary>
    /// <remarks>
    /// The game reads it into a set; it is empty in every lot examined.
    /// </remarks>
    public Collection<ushort> Unknown2 { get; } = [];

    /// <summary>
    /// Gets/sets the pond's water level, in steps of 0.25 m
    /// </summary>
    /// <remarks>
    /// It equals <see cref="TerrainHeightsSection.GetWaterLevel(int, int, FloorQuarter)"/> on every quarter of the pond, which the game multiplies by 0.25.
    /// Creating a pond in game set it to −1, and raising the pond's water level one step changed it from −3 to −2 together with the water level of every quarter of the pond in the <see cref="TerrainHeightsSection"/>, while the pond spread to more cells (in-game tests).
    /// It is −1, −2, −3 or 0 in the lots examined.
    /// </remarks>
    public int WaterLevel { get; set; }
}
