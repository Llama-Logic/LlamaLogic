namespace LlamaLogic.Architecture;

/// <summary>
/// Represents one entry of an <see cref="UnknownC9106475Section"/>
/// </summary>
public sealed class UnknownC9106475Entry
{
    /// <summary>
    /// Gets/sets the id of the entry, which the game uses as the key of a map
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// Gets the levels of the entry
    /// </summary>
    public Collection<UnknownC9106475Level> Levels { get; } = [];
}
