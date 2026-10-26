namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents the outcome of capturing or importing a snapshot into a <see cref="TuningArchiveFile"/>
/// </summary>
public sealed class TuningArchiveCaptureResult
{
    internal TuningArchiveCaptureResult(TuningArchiveSnapshot snapshot, int tunings, int simData, int animationStateMachines, int strings, int newContent, long newContentBytes, IReadOnlyList<string> problems, TimeSpan duration, int images = 0, int sounds = 0, int videos = 0)
    {
        Images = images;
        Sounds = sounds;
        Videos = videos;
        Snapshot = snapshot;
        Tunings = tunings;
        SimData = simData;
        AnimationStateMachines = animationStateMachines;
        Strings = strings;
        NewContent = newContent;
        NewContentBytes = newContentBytes;
        Problems = problems;
        Duration = duration;
    }

    /// <summary>
    /// Gets how many animation state machines the snapshot contains
    /// </summary>
    public int AnimationStateMachines { get; }

    /// <summary>
    /// Gets how long the operation took
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets how many images the snapshot contains
    /// </summary>
    public int Images { get; }

    /// <summary>
    /// Gets how many documents the archive did not already contain
    /// </summary>
    public int NewContent { get; }

    /// <summary>
    /// Gets how many bytes the archive's documents grew by, compressed
    /// </summary>
    public long NewContentBytes { get; }

    /// <summary>
    /// Gets descriptions of content that could not be captured, and of anything else the creator should know about the snapshot
    /// </summary>
    public IReadOnlyList<string> Problems { get; }

    /// <summary>
    /// Gets how many SimData resources the snapshot contains
    /// </summary>
    public int SimData { get; }

    /// <summary>
    /// Gets how many sounds the snapshot contains (audio configurations and the audio they use)
    /// </summary>
    public int Sounds { get; }

    /// <summary>
    /// Gets the snapshot that was created
    /// </summary>
    public TuningArchiveSnapshot Snapshot { get; }

    /// <summary>
    /// Gets how many strings the snapshot contains
    /// </summary>
    public int Strings { get; }

    /// <summary>
    /// Gets how many tunings the snapshot contains
    /// </summary>
    public int Tunings { get; }

    /// <summary>
    /// Gets how many videos the snapshot contains
    /// </summary>
    public int Videos { get; }
}