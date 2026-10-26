namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents what a draft does, or cannot do, about a change to a tunable
/// </summary>
/// <param name="Severity">Whether the draft makes the change</param>
/// <param name="Entry">The tuning that was changed</param>
/// <param name="Path">The path of the tunable that was changed, such as <c>loot_list[2].stat</c></param>
/// <param name="Message">What the draft does, or why it cannot</param>
public sealed record TuningDraftDiagnostic(TuningDraftSeverity Severity, TuningArchiveEntry Entry, string Path, string Message);