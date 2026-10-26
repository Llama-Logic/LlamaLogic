namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents a list a draft changed
/// </summary>
/// <param name="Path">The path of the list</param>
/// <param name="Original">The list as the original has it</param>
/// <param name="Draft">The list as the draft has it</param>
/// <param name="Added">The items the draft added</param>
/// <param name="Removed">The items the draft removed</param>
sealed record TuningDraftListChange(string Path, XElement Original, XElement Draft, IReadOnlyList<XElement> Added, IReadOnlyList<XElement> Removed);