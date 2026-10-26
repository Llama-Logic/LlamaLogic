namespace LlamaLogic.EpicTuningTime.Components.State;

/// <summary>
/// Specifies what the updated package does with an override
/// </summary>
enum UpdateDecision
{
    /// <summary>
    /// The override stays as the mod has it
    /// </summary>
    Keep,

    /// <summary>
    /// The override is replaced with the merge of the mod's changes onto the game's new version
    /// </summary>
    Merge,

    /// <summary>
    /// The override is removed
    /// </summary>
    Remove
}