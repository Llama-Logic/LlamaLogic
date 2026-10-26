namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents a place where a merge could not combine the mod's change with the game's
/// </summary>
/// <param name="Index">The position of the conflict among the merge's conflicts, which does not depend on how conflicts are resolved</param>
/// <param name="Kind">Why the changes could not be combined</param>
/// <param name="Path">The path of the element in the merged result, such as <c>loot_list[2].stat</c>, or of an attribute, such as <c>@c</c></param>
/// <param name="Base">The element in the version the mod was made from (the XML of the element on one line, or an attribute's value), if it was there</param>
/// <param name="Mod">The element in the mod, if it is there</param>
/// <param name="Game">The element in the game's new version, if it is there</param>
public sealed record TuningMergeConflict(int Index, TuningMergeConflictKind Kind, string Path, string? Base, string? Mod, string? Game);