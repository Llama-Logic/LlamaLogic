namespace LlamaLogic.Packages.Formats;

/// <summary>
/// Represents a stretch of a sound rebuilt as MP3 by <see cref="ElectronicArtsAudioCore.GetMp3"/>: the whole sound, or the part before or after its loop starts (🔒)
/// </summary>
public sealed class ElectronicArtsAudioCoreMp3Segment(int firstSample, int sampleCount, IReadOnlyList<ReadOnlyMemory<byte>> layers)
{
    /// <summary>
    /// Gets the sample of the sound at which the segment begins
    /// </summary>
    public int FirstSample { get; } = firstSample;

    /// <summary>
    /// Gets the MP3s of the segment, one per pair of channels (the last mono if the sound has an odd number of channels), to be played together
    /// </summary>
    /// <remarks>
    /// Each MP3 is constant-bitrate and carries a gapless playback tag, so a decoder that honors it yields exactly <see cref="SampleCount"/> samples per channel
    /// </remarks>
    public IReadOnlyList<ReadOnlyMemory<byte>> Layers { get; } = layers;

    /// <summary>
    /// Gets the number of samples per channel in the segment
    /// </summary>
    public int SampleCount { get; } = sampleCount;
}