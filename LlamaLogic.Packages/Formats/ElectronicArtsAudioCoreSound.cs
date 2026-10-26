namespace LlamaLogic.Packages.Formats;

/// <summary>
/// Represents a sound in Electronic Arts' AudioCore format made by <see cref="ElectronicArtsAudioCore.CreateFromMp3(ReadOnlyMemory{byte}, ElectronicArtsAudioCoreStorage, bool)"/>: the content of its <see cref="ResourceType.AudioVocals"/> resource and, if it is streamed, of its <see cref="ResourceType.AudioEffects"/> resource (🔒)
/// </summary>
public sealed class ElectronicArtsAudioCoreSound(ElectronicArtsAudioCoreHeader header, ReadOnlyMemory<byte> audioVocalsData, ReadOnlyMemory<byte> audioEffectsData)
{
    /// <summary>
    /// Gets the content of the sound's <see cref="ResourceType.AudioEffects"/> resource (its streamed body), or an empty value if the sound is held in memory
    /// </summary>
    public ReadOnlyMemory<byte> AudioEffectsData { get; } = audioEffectsData;

    /// <summary>
    /// Gets the content of the sound's <see cref="ResourceType.AudioVocals"/> resource (its header, followed by its audio if it is held in memory)
    /// </summary>
    public ReadOnlyMemory<byte> AudioVocalsData { get; } = audioVocalsData;

    /// <summary>
    /// Gets the header of the sound
    /// </summary>
    public ElectronicArtsAudioCoreHeader Header { get; } = header;
}