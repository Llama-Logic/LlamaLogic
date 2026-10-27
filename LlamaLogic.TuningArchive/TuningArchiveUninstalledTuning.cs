namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents a tuning of a pack that was not installed when a <see cref="TuningArchiveSnapshot"/> was captured, known only from the pack's patches in the game's <c>Delta</c> folder
/// </summary>
/// <remarks>
/// The game does not load a pack's patches unless the pack is installed, so such tuning is not in the snapshot; only its name is kept, so that what refers to it can still be explained.
/// </remarks>
/// <param name="Type">The type of the tuning</param>
/// <param name="Instance">The instance of the tuning</param>
/// <param name="Name">The name of the tuning</param>
/// <param name="Class">The class of the tuning, if it is an instance tuning</param>
/// <param name="Pack">The code of the pack the tuning belongs to</param>
public sealed record TuningArchiveUninstalledTuning(ResourceType Type, ulong Instance, string Name, string? Class, string Pack);