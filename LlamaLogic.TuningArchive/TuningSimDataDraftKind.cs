namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies what an override of a tuning needs from its SimData
/// </summary>
public enum TuningSimDataDraftKind
{
    /// <summary>
    /// The tuning has no SimData, or the changes do not reach it, so the game's SimData stays correct
    /// </summary>
    Unaffected,

    /// <summary>
    /// The changes reach the tuning's SimData, and new SimData was generated
    /// </summary>
    Generated,

    /// <summary>
    /// The changes may reach the tuning's SimData, but SimData that could be trusted could not be generated
    /// </summary>
    Failed
}