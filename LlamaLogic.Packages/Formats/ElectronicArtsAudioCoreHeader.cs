namespace LlamaLogic.Packages.Formats;

/// <summary>
/// Represents the header of a sound in Electronic Arts' AudioCore format, the content of a <see cref="ResourceType.AudioVocals"/> resource up to its audio (🔒)
/// </summary>
/// <remarks>
/// Adapted from vgmstream's reading of EAAudioCore headers
/// </remarks>
public sealed class ElectronicArtsAudioCoreHeader
{
    const int maximumSampleRate = 200000;

    ElectronicArtsAudioCoreHeader(int version, ElectronicArtsAudioCoreCodec codec, int channelCount, int sampleRate, ElectronicArtsAudioCoreStorage storage, int sampleCount, int? loopStartSample, int? loopOffset, int size)
    {
        ChannelCount = channelCount;
        Codec = codec;
        LoopOffset = loopOffset;
        LoopStartSample = loopStartSample;
        SampleCount = sampleCount;
        SampleRate = sampleRate;
        Size = size;
        Storage = storage;
        Version = version;
    }

    /// <summary>
    /// Gets the number of channels
    /// </summary>
    public int ChannelCount { get; }

    /// <summary>
    /// Gets the codec of the audio
    /// </summary>
    public ElectronicArtsAudioCoreCodec Codec { get; }

    /// <summary>
    /// Gets the offset in the streamed body of the block where the loop starts, if the sound is streamed and loops
    /// </summary>
    public int? LoopOffset { get; }

    /// <summary>
    /// Gets the sample at which the loop starts, or <see langword="null"/> if the sound does not loop
    /// </summary>
    public int? LoopStartSample { get; }

    /// <summary>
    /// Gets the number of samples per channel
    /// </summary>
    public int SampleCount { get; }

    /// <summary>
    /// Gets the number of samples per second
    /// </summary>
    public int SampleRate { get; }

    /// <summary>
    /// Gets the size of the header in bytes (where the audio begins, for a sound stored in <see cref="ElectronicArtsAudioCoreStorage.Memory"/>)
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// Gets where the audio is stored
    /// </summary>
    public ElectronicArtsAudioCoreStorage Storage { get; }

    /// <summary>
    /// Gets the version of the header
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Reads the header of a sound
    /// </summary>
    /// <param name="data">The data of the sound, beginning with its header</param>
    /// <exception cref="InvalidDataException">The data is too short for the header it declares, or declares an unknown version, storage or sample rate</exception>
    public static ElectronicArtsAudioCoreHeader Read(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8)
            throw new InvalidDataException($"{data.Length} bytes are too few for an AudioCore header");
        var first = BinaryPrimitives.ReadUInt32BigEndian(data);
        var second = BinaryPrimitives.ReadUInt32BigEndian(data[4..]);
        var version = (int)(first >> 28);
        var codec = (ElectronicArtsAudioCoreCodec)((first >> 24) & 0x0f);
        var channelCount = (int)((first >> 18) & 0x3f) + 1;
        var sampleRate = (int)(first & 0x3ffff);
        var storage = (int)(second >> 30);
        var isLooping = ((second >> 29) & 1) is 1;
        var sampleCount = (int)(second & 0x1fffffff);
        if (version is not (0 or 1))
            throw new InvalidDataException($"AudioCore header version {version} is not supported");
        if (storage is not (0 or 1 or 2))
            throw new InvalidDataException($"AudioCore storage {storage} is unknown");
        if (sampleRate is 0 or > maximumSampleRate)
            throw new InvalidDataException($"AudioCore sample rate {sampleRate} is out of range");
        var size = 8;
        int? loopStartSample = null;
        int? loopOffset = null;
        if (isLooping)
            loopStartSample = ReadOptionalValue(data, ref size);
        if (storage is 1 && isLooping)
            loopOffset = ReadOptionalValue(data, ref size);
        else if (storage is 2)
        {
            var prefetchSampleCount = ReadOptionalValue(data, ref size);
            if (isLooping && loopStartSample >= prefetchSampleCount)
                loopOffset = ReadOptionalValue(data, ref size);
        }
        if (loopStartSample is < 0 || loopStartSample > sampleCount)
            throw new InvalidDataException($"AudioCore loop start {loopStartSample} is outside the sound's {sampleCount} samples");
        return new(version, codec, channelCount, sampleRate, (ElectronicArtsAudioCoreStorage)storage, sampleCount, loopStartSample, loopOffset, size);
    }

    static int ReadOptionalValue(ReadOnlySpan<byte> data, ref int size)
    {
        if (data.Length < size + 4)
            throw new InvalidDataException($"{data.Length} bytes are too few for the AudioCore header they declare");
        var value = BinaryPrimitives.ReadInt32BigEndian(data[size..]);
        size += 4;
        return value;
    }
}