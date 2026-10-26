namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents an element that differs between two versions of a tuning document
/// </summary>
/// <param name="Kind">How the element differs</param>
/// <param name="Path">The path of the element from the root, such as <c>loot_list[2].stat</c> (list indices are those of the newer version, except for removed elements)</param>
/// <param name="OldValue">The element's value in the older version (the XML of the element, on one line, if it has children), if it was there</param>
/// <param name="NewValue">The element's value in the newer version (the XML of the element, on one line, if it has children), if it is there</param>
/// <param name="OldLine">The 1-based line of the element in the older version's canonical text, or 0 if it was not there</param>
/// <param name="NewLine">The 1-based line of the element in the newer version's canonical text, or 0 if it is not there</param>
public sealed record TuningSemanticChange(TuningSemanticChangeKind Kind, string Path, string? OldValue, string? NewValue, int OldLine, int NewLine);