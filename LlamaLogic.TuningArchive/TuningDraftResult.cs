namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents a draft that makes changes to tuning without overriding it: a script or a snippet
/// </summary>
/// <param name="Source">The source of the draft (Python, or snippet tuning XML)</param>
/// <param name="Diagnostics">What the draft does about each change, and which changes it cannot make</param>
/// <param name="Changes">The number of changes the draft makes</param>
public sealed record TuningDraftResult(string Source, IReadOnlyList<TuningDraftDiagnostic> Diagnostics, int Changes);