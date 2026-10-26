namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents the SimData an override of a tuning needs
/// </summary>
/// <param name="Kind">Whether the override needs new SimData, and whether it could be generated</param>
/// <param name="Key">The key of the tuning's SimData, if it has SimData</param>
/// <param name="SimData">The generated SimData, if <paramref name="Kind"/> is <see cref="TuningSimDataDraftKind.Generated"/></param>
/// <param name="Message">What happened, for the creator</param>
public sealed record TuningSimDataDraftResult(TuningSimDataDraftKind Kind, ResourceKey? Key, SimDataModel? SimData, string Message);