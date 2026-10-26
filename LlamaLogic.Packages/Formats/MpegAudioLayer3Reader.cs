namespace LlamaLogic.Packages.Formats;

sealed class MpegAudioLayer3Stream(byte[] mainData, List<ElectronicArtsLayer3Frame> granules, int sampleRate, int channelCount, int? encoderDelay, int? padding)
{
    public int ChannelCount { get; } = channelCount;

    public int? EncoderDelay { get; } = encoderDelay;

    public List<ElectronicArtsLayer3Frame> Granules { get; } = granules;

    public byte[] MainData { get; } = mainData;

    public int? Padding { get; } = padding;

    public int SampleRate { get; } = sampleRate;
}

static class MpegAudioLayer3Reader
{
    static readonly int[] bitrates = [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, -1];

    static bool IsPrintable(ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
            if (value is < 0x20 or > 0x7e)
                return false;
        return true;
    }

    static bool IsZero(ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
            if (value is not 0)
                return false;
        return true;
    }

    static bool IsTagAt(ReadOnlySpan<byte> data, int position, ReadOnlySpan<byte> tag) =>
        position >= 0 && position + tag.Length <= data.Length && data.Slice(position, tag.Length).SequenceEqual(tag);

    public static MpegAudioLayer3Stream Read(ReadOnlySpan<byte> data)
    {
        var position = 0;
        while (IsTagAt(data, position, "ID3"u8) && position + 10 <= data.Length)
        {
            var size = ((data[position + 6] & 0x7f) << 21) | ((data[position + 7] & 0x7f) << 14) | ((data[position + 8] & 0x7f) << 7) | (data[position + 9] & 0x7f);
            position += 10 + size + ((data[position + 5] & 0x10) is 0 ? 0 : 10);
        }
        var mainData = new List<byte>();
        var granules = new List<ElectronicArtsLayer3Frame>();
        int? sampleRate = null;
        int? channelCount = null;
        int? encoderDelay = null;
        int? padding = null;
        var isFirst = true;
        while (position + 4 <= data.Length)
        {
            var header = BinaryPrimitives.ReadUInt32BigEndian(data[position..]);
            if ((header >> 21) is not 0x7ff)
            {
                if (IsTagAt(data, position, "TAG"u8) || IsTagAt(data, position, "APETAGEX"u8) || IsTagAt(data, position, "LYRICSBEGIN"u8) || IsZero(data[position..]))
                    break;
                throw new InvalidDataException($"The MP3 has no frame where one should begin, at {position}");
            }
            if (((header >> 19) & 3) is not 3)
                throw new NotSupportedException("Only MPEG-1 audio (32,000, 44,100 or 48,000 Hz) can be stored as EALayer3 version 1");
            if (((header >> 17) & 3) is not 1)
                throw new InvalidDataException($"The frame at {position} is not Layer III");
            var isProtected = ((header >> 16) & 1) is 0;
            var bitrate = bitrates[(header >> 12) & 0x0f];
            if (bitrate is 0)
                throw new NotSupportedException("Free-format MP3s are not supported");
            if (bitrate < 0)
                throw new InvalidDataException($"The frame at {position} declares an invalid bitrate");
            var sampleRateIndex = (int)((header >> 10) & 3);
            var frameSampleRate = sampleRateIndex switch
            {
                0 => 44100,
                1 => 48000,
                2 => 32000,
                _ => throw new InvalidDataException($"The frame at {position} declares an invalid sample rate")
            };
            var isPadded = ((header >> 9) & 1) is 1;
            var channelMode = (int)((header >> 6) & 3);
            var modeExtension = (int)((header >> 4) & 3);
            var frameChannelCount = channelMode is 3 ? 1 : 2;
            var frameSize = 144 * bitrate * 1000 / frameSampleRate + (isPadded ? 1 : 0);
            if (position + frameSize > data.Length)
                throw new InvalidDataException($"The frame at {position} is cut off");
            if ((sampleRate ?? frameSampleRate) != frameSampleRate || (channelCount ?? frameChannelCount) != frameChannelCount)
                throw new InvalidDataException($"The frame at {position} changes the sample rate or the number of channels");
            sampleRate = frameSampleRate;
            channelCount = frameChannelCount;
            var sideInformationPosition = position + 4 + (isProtected ? 2 : 0);
            var sideInformationSize = frameChannelCount is 1 ? 17 : 32;
            if (isFirst)
            {
                isFirst = false;
                var tagPosition = sideInformationPosition + sideInformationSize;
                if (IsTagAt(data, tagPosition, "Xing"u8) || IsTagAt(data, tagPosition, "Info"u8))
                {
                    var flags = BinaryPrimitives.ReadUInt32BigEndian(data[(tagPosition + 4)..]);
                    var extension = tagPosition + 8 + ((flags & 1) is 0 ? 0 : 4) + ((flags & 2) is 0 ? 0 : 4) + ((flags & 4) is 0 ? 0 : 100) + ((flags & 8) is 0 ? 0 : 4);
                    if (extension + 24 <= position + frameSize && IsPrintable(data.Slice(extension, 4)))
                    {
                        encoderDelay = (data[extension + 21] << 4) | (data[extension + 22] >> 4);
                        padding = ((data[extension + 22] & 0x0f) << 8) | data[extension + 23];
                    }
                    position += frameSize;
                    continue;
                }
                if (IsTagAt(data, position + 36, "VBRI"u8))
                {
                    position += frameSize;
                    continue;
                }
            }
            var reader = new ElectronicArtsLayer3BitReader(data.Slice(sideInformationPosition, sideInformationSize).ToArray(), 0);
            var mainDataBegin = (int)reader.Read(9);
            reader.Read(frameChannelCount is 1 ? 5 : 3);
            var scaleFactorSelection = new uint[2];
            for (var channel = 0; channel < frameChannelCount; ++channel)
                scaleFactorSelection[channel] = reader.Read(4);
            var slotStart = mainData.Count;
            var dataStart = slotStart - mainDataBegin;
            if (dataStart < 0)
                throw new InvalidDataException($"The frame at {position} takes {mainDataBegin} bytes from a bit reservoir the frames before it do not hold");
            var bitOffset = dataStart * 8;
            for (var granuleIndex = 0; granuleIndex < 2; ++granuleIndex)
            {
                var granule = new ElectronicArtsLayer3Frame
                {
                    ChannelCount = frameChannelCount,
                    ChannelMode = channelMode,
                    GranuleIndex = granuleIndex,
                    IsMpeg1 = true,
                    ModeExtension = modeExtension,
                    SampleRate = frameSampleRate,
                    SampleRateIndex = sampleRateIndex,
                    VersionIndex = 3
                };
                for (var channel = 0; channel < frameChannelCount; ++channel)
                {
                    granule.MainDataBitCount[channel] = (int)reader.Read(12);
                    granule.OthersHigh[channel] = reader.Read(32);
                    granule.OthersLow[channel] = reader.Read(15);
                    if (granuleIndex is 1)
                        granule.ScaleFactorSelection[channel] = scaleFactorSelection[channel];
                }
                granule.DataBitOffset = bitOffset;
                bitOffset += granule.MainDataBitCount[0] + granule.MainDataBitCount[1];
                granules.Add(granule);
            }
            var slot = data[(sideInformationPosition + sideInformationSize)..(position + frameSize)];
            foreach (var value in slot)
                mainData.Add(value);
            if (bitOffset > mainData.Count * 8)
                throw new InvalidDataException($"The frame at {position} declares more audio than it and the bit reservoir hold");
            position += frameSize;
        }
        if (granules.Count is 0)
            throw new InvalidDataException("The MP3 has no audio frames");
        return new([.. mainData], granules, sampleRate!.Value, channelCount!.Value, encoderDelay, padding);
    }
}