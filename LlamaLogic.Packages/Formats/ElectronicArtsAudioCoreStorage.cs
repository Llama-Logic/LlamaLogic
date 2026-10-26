namespace LlamaLogic.Packages.Formats;

/// <summary>
/// Represents where the audio of a sound in Electronic Arts' AudioCore format is stored
/// </summary>
public enum ElectronicArtsAudioCoreStorage
{
    /// <summary>
    /// The audio follows the header (in The Sims 4, in the <see cref="ResourceType.AudioVocals"/> resource itself)
    /// </summary>
    Memory = 0,

    /// <summary>
    /// The audio is streamed from a separate body (in The Sims 4, the <see cref="ResourceType.AudioEffects"/> resource with the same instance)
    /// </summary>
    Stream = 1,

    /// <summary>
    /// Some of the audio follows the header and the rest is streamed from a separate body
    /// </summary>
    Gigasample = 2
}