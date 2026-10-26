namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents a pack that was present when a <see cref="TuningArchiveSnapshot"/> was taken
/// </summary>
/// <param name="Code">The code of the pack (<c>BG</c> for the base game)</param>
/// <param name="Version">The version of the pack, if it was known</param>
public sealed record TuningArchiveSnapshotPack(string Code, string? Version);