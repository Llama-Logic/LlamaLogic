namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents the progress of a long-running <see cref="TuningArchiveFile"/> operation
/// </summary>
/// <param name="Stage">A description of what the operation is doing</param>
/// <param name="Completed">How many units of work the stage has completed</param>
/// <param name="Total">How many units of work the stage has, or 0 if it is not known</param>
public readonly record struct TuningArchiveProgress(string Stage, int Completed, int Total);