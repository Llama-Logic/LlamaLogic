namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a pair of object definition ids stored in a <see cref="Blueprint"/> (🔓)
/// </summary>
/// <remarks>
/// In every blueprint observed, <see cref="From"/> is the object definition id of one of the blueprint's <see cref="Blueprint.LotObjects"/>, and <see cref="To"/> usually is not.
/// Hypothesis: <see cref="To"/> is the object definition the game substitutes when <see cref="From"/> is unavailable (Sims 4 Studio calls these object fallbacks).
/// </remarks>
public sealed class BlueprintObjectDefinitionMapping
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlueprintObjectDefinitionMapping"/> class
    /// </summary>
    public BlueprintObjectDefinitionMapping(ulong from, ulong to)
    {
        From = from;
        To = to;
    }

    /// <summary>
    /// Gets/sets the object definition id of one of the blueprint's lot objects
    /// </summary>
    public ulong From { get; set; }

    /// <summary>
    /// Gets/sets the object definition id paired with <see cref="From"/>
    /// </summary>
    public ulong To { get; set; }
}
