namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies what a draft can do about a change
/// </summary>
public enum TuningDraftSeverity
{
    /// <summary>
    /// The draft makes the change
    /// </summary>
    Information = 0,

    /// <summary>
    /// The draft makes the change, with a caveat the creator should know
    /// </summary>
    Warning = 1,

    /// <summary>
    /// The draft cannot make the change, so it leaves it out
    /// </summary>
    Error = 2
}