namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents how a string differs, if at all, between two snapshots
/// </summary>
/// <param name="Kind">How the string differs (<see cref="TuningArchiveChangeKind.Moved"/> means only the pack it came from changed)</param>
/// <param name="Key">The key of the string</param>
/// <param name="OldText">The text of the string in the older snapshot, if it was there</param>
/// <param name="NewText">The text of the string in the newer snapshot, if it is there</param>
public sealed record TuningArchiveStringChange(TuningArchiveChangeKind Kind, uint Key, string? OldText, string? NewText);