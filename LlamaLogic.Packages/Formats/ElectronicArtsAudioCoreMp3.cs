namespace LlamaLogic.Packages.Formats;

/// <summary>
/// Represents a sound in Electronic Arts' AudioCore format rebuilt as MP3 by <see cref="ElectronicArtsAudioCore.GetMp3"/> (🔒)
/// </summary>
public sealed class ElectronicArtsAudioCoreMp3(ElectronicArtsAudioCoreHeader header, IReadOnlyList<ElectronicArtsAudioCoreMp3Segment> segments)
{
    /// <summary>
    /// Gets the header of the sound
    /// </summary>
    public ElectronicArtsAudioCoreHeader Header { get; } = header;

    /// <summary>
    /// Gets the segments of the sound, in order: one for the whole sound, or, for a sound that loops from somewhere other than its start, one for the part before the loop and one for the loop
    /// </summary>
    public IReadOnlyList<ElectronicArtsAudioCoreMp3Segment> Segments { get; } = segments;
}