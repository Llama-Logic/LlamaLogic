namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents the result of a merge of a mod's changes onto the game's new version of a tuning
/// </summary>
/// <param name="Text">The merged tuning (canonical text, see <see cref="TuningXml"/>), with each conflict resolved as the merge was told to resolve it</param>
/// <param name="Conflicts">The places where the mod's changes could not be combined with the game's</param>
public sealed record TuningMergeResult(string Text, IReadOnlyList<TuningMergeConflict> Conflicts);