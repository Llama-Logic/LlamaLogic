namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents a set of tuning descriptions (TDESCs) imported into a <see cref="TuningArchiveFile"/>
/// </summary>
/// <param name="Id">The identifier of the set within its archive</param>
/// <param name="GameVersion">The version of the game the set describes</param>
/// <param name="FileName">The name of the file the set was imported from</param>
/// <param name="ImportedAt">When the set was imported</param>
public sealed record TuningArchiveDescriptions(long Id, string GameVersion, string? FileName, DateTimeOffset ImportedAt);