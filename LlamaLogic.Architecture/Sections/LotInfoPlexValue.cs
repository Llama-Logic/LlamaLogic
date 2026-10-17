namespace LlamaLogic.Architecture;

/// <summary>
/// Represents one entry of the plex map of a <see cref="LotInfoSection"/>: an optional value per plex unit
/// </summary>
/// <remarks>
/// The game hands the map to its plex manager after loading.
/// In-game tests show that the value grows with each paid build change (by 960 for a room, 64 for a floor fill, 64 for a ceiling, 16 for four ground tiles, 4 for one terrain floor tile, 75 for three fence segments) and is unchanged by a rotation or a quarter-tile repaint; that it is the architecture's value is a hypothesis.
/// </remarks>
public sealed class LotInfoPlexValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LotInfoPlexValue"/> class
    /// </summary>
    public LotInfoPlexValue()
    {
    }

    /// <summary>
    /// Gets/sets whether the entry has a value
    /// </summary>
    /// <remarks>
    /// The game writes <see cref="Value"/> even when this is <see langword="false"/>.
    /// It is <see langword="true"/> in all but five of the entries examined.
    /// </remarks>
    public bool HasValue { get; set; }

    /// <summary>
    /// Gets/sets the id of the plex unit the entry belongs to
    /// </summary>
    /// <remarks>
    /// Stored from version 15 on; versions 12 through 14 hold a single entry for unit 0.
    /// </remarks>
    public uint UnitId { get; set; }

    /// <summary>
    /// Gets/sets the value of the entry
    /// </summary>
    public uint Value { get; set; }
}
