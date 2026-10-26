namespace LlamaLogic.Packages.Formats;

/// <summary>
/// Represents the codec of a sound in Electronic Arts' AudioCore format, as its header names it
/// </summary>
public enum ElectronicArtsAudioCoreCodec
{
    /// <summary>
    /// No codec
    /// </summary>
    None = 0x00,

    /// <summary>
    /// Big-endian 16-bit pulse-code modulation
    /// </summary>
    PulseCodeModulation = 0x02,

    /// <summary>
    /// EA-XMA
    /// </summary>
    ElectronicArtsXma = 0x03,

    /// <summary>
    /// EA-XAS
    /// </summary>
    ElectronicArtsXas = 0x04,

    /// <summary>
    /// EALayer3 version 1, the codec of The Sims 4's sounds
    /// </summary>
    ElectronicArtsLayer3Version1 = 0x05,

    /// <summary>
    /// EALayer3 version 2 "PCM"
    /// </summary>
    ElectronicArtsLayer3Version2PulseCodeModulation = 0x06,

    /// <summary>
    /// EALayer3 version 2 "Spike"
    /// </summary>
    ElectronicArtsLayer3Version2Spike = 0x07,

    /// <summary>
    /// GameCube adaptive differential pulse-code modulation
    /// </summary>
    GameCubeAdaptiveDifferentialPulseCodeModulation = 0x08,

    /// <summary>
    /// EA Speex
    /// </summary>
    ElectronicArtsSpeex = 0x09,

    /// <summary>
    /// EATrax (ATRAC9)
    /// </summary>
    ElectronicArtsTrax = 0x0a,

    /// <summary>
    /// EA MP3
    /// </summary>
    ElectronicArtsMp3 = 0x0b,

    /// <summary>
    /// EA Opus
    /// </summary>
    ElectronicArtsOpus = 0x0c,

    /// <summary>
    /// EA ATRAC9
    /// </summary>
    ElectronicArtsAtrac9 = 0x0d,

    /// <summary>
    /// EA Opus, multistream
    /// </summary>
    ElectronicArtsOpusMultistream = 0x0e,

    /// <summary>
    /// EA Opus, multistream with uncoupled channels
    /// </summary>
    ElectronicArtsOpusMultistreamUncoupled = 0x0f
}