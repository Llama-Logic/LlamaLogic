namespace LlamaLogic.Packages.UnitTests;

sealed class ElectronicArtsLayer3TestGranule(int index, int channelCount, uint[] scaleFactorSelection, int[] mainDataBitCount, uint[] othersHigh, uint[] othersLow, bool[][] mainData, int pulseCodeModulationSampleCount)
{
    public int ChannelCount { get; } = channelCount;

    public int Index { get; } = index;

    public bool[][] MainData { get; } = mainData;

    public int[] MainDataBitCount { get; } = mainDataBitCount;

    public uint[] OthersHigh { get; } = othersHigh;

    public uint[] OthersLow { get; } = othersLow;

    public int PulseCodeModulationSampleCount { get; } = pulseCodeModulationSampleCount;

    public uint[] ScaleFactorSelection { get; } = scaleFactorSelection;
}

sealed class ElectronicArtsLayer3TestMp3(int frameCount, int encoderDelay, int padding, int bitrateIndex, IReadOnlyList<int> mainDataBegins, IReadOnlyList<ElectronicArtsLayer3TestGranule> granules)
{
    public int BitrateIndex { get; } = bitrateIndex;

    public int EncoderDelay { get; } = encoderDelay;

    public int FrameCount { get; } = frameCount;

    public IReadOnlyList<ElectronicArtsLayer3TestGranule> Granules { get; } = granules;

    public IReadOnlyList<int> MainDataBegins { get; } = mainDataBegins;

    public int Padding { get; } = padding;
}

static class ElectronicArtsAudioCoreBuilder
{
    static void AddBits(List<bool> bits, int count, uint value)
    {
        for (var bit = count - 1; bit >= 0; --bit)
            bits.Add(((value >> bit) & 1) is 1);
    }

    public static byte[] Block(bool isLast, int sampleCount, byte[] payload)
    {
        var block = new byte[8 + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(block, (isLast ? 0x80000000u : 0) | (uint)block.Length);
        BinaryPrimitives.WriteUInt32BigEndian(block.AsSpan(4), (uint)sampleCount);
        payload.CopyTo(block, 8);
        return block;
    }

    public static List<ElectronicArtsLayer3TestGranule> CreateGranules(Random random, int channelCount, int pairCount, int minimumBits, int maximumBits, bool startsWithUncompressedBlock = true)
    {
        var granules = new List<ElectronicArtsLayer3TestGranule>();
        for (var index = 0; index < pairCount * 2; ++index)
        {
            var scaleFactorSelection = new uint[channelCount];
            var mainDataBitCount = new int[channelCount];
            var othersHigh = new uint[channelCount];
            var othersLow = new uint[channelCount];
            var mainData = new bool[channelCount][];
            for (var channel = 0; channel < channelCount; ++channel)
            {
                scaleFactorSelection[channel] = (index & 1) is 1 ? (uint)random.Next(16) : 0;
                mainDataBitCount[channel] = random.Next(minimumBits, maximumBits + 1);
                othersHigh[channel] = (uint)random.NextInt64(0x100000000);
                othersLow[channel] = (uint)random.Next(1 << 15);
                mainData[channel] = Enumerable.Range(0, mainDataBitCount[channel]).Select(_ => random.Next(2) is 1).ToArray();
            }
            granules.Add(new(index & 1, channelCount, scaleFactorSelection, mainDataBitCount, othersHigh, othersLow, mainData, startsWithUncompressedBlock && index is 0 ? 47 : 0));
        }
        return granules;
    }

    public static byte[] EncodeFrame(ElectronicArtsLayer3TestGranule granule, int sampleRateIndex)
    {
        var bits = new List<bool>();
        AddBits(bits, 2, 3);
        AddBits(bits, 2, (uint)sampleRateIndex);
        AddBits(bits, 2, granule.ChannelCount is 1 ? 3u : 0u);
        AddBits(bits, 2, 0);
        AddBits(bits, 1, (uint)granule.Index);
        if (granule.Index is 1)
            for (var channel = 0; channel < granule.ChannelCount; ++channel)
                AddBits(bits, 4, granule.ScaleFactorSelection[channel]);
        for (var channel = 0; channel < granule.ChannelCount; ++channel)
        {
            AddBits(bits, 12, (uint)granule.MainDataBitCount[channel]);
            AddBits(bits, 32, granule.OthersHigh[channel]);
            AddBits(bits, 15, granule.OthersLow[channel]);
        }
        for (var channel = 0; channel < granule.ChannelCount; ++channel)
            bits.AddRange(granule.MainData[channel]);
        var common = Pack(bits);
        var hasUncompressedBlock = granule.PulseCodeModulationSampleCount > 0;
        var frame = new byte[1 + common.Length + (hasUncompressedBlock ? 8 + 2 * granule.PulseCodeModulationSampleCount * granule.ChannelCount : 0)];
        frame[0] = hasUncompressedBlock ? (byte)0xee : (byte)0;
        common.CopyTo(frame, 1);
        if (hasUncompressedBlock)
        {
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(1 + common.Length + 2), (ushort)granule.PulseCodeModulationSampleCount);
            for (var position = 1 + common.Length + 8; position < frame.Length; ++position)
                frame[position] = (byte)(position * 7);
        }
        return frame;
    }

    public static byte[] Header(int channelCount, int sampleRate, ElectronicArtsAudioCoreStorage storage, int sampleCount, int? loopStartSample = null, int? loopOffset = null, ElectronicArtsAudioCoreCodec codec = ElectronicArtsAudioCoreCodec.ElectronicArtsLayer3Version1)
    {
        var header = new List<byte>();
        var first = ((uint)codec << 24) | ((uint)(channelCount - 1) << 18) | (uint)sampleRate;
        var second = ((uint)storage << 30) | (loopStartSample is null ? 0 : 0x20000000u) | (uint)sampleCount;
        void Add(uint value)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
            header.AddRange(bytes);
        }
        Add(first);
        Add(second);
        if (loopStartSample is { } start)
            Add((uint)start);
        if (loopOffset is { } offset)
            Add((uint)offset);
        return [.. header];
    }

    public static byte[] Interleave(int sampleRateIndex, params List<ElectronicArtsLayer3TestGranule>[] layers)
    {
        var payload = new List<byte>();
        for (var index = 0; index < layers[0].Count; ++index)
            foreach (var layer in layers)
                payload.AddRange(EncodeFrame(layer[index], sampleRateIndex));
        return [.. payload];
    }

    public static byte[] Mp3(IReadOnlyList<ElectronicArtsLayer3TestGranule> granules, int sampleRateIndex = 0, bool isProtected = false, (int delay, int padding)? gaplessTag = null, bool hasId3Tags = false, int versionBits = 3)
    {
        var channelCount = granules[0].ChannelCount;
        var sampleRate = sampleRateIndex switch { 0 => 44100, 1 => 48000, _ => 32000 };
        var sideInformationSize = channelCount is 1 ? 17 : 32;
        var numerator = 144 * 320 * 1000;
        var remainder = 0;
        var mp3 = new List<byte>();
        if (hasId3Tags)
            mp3.AddRange([.. "ID3"u8, 4, 0, 0, 0, 0, 0, 10, .. new byte[10]]);
        byte[] Header(bool isPadded)
        {
            var header = 0xffe00000u | ((uint)versionBits << 19) | (1u << 17) | (isProtected ? 0u : 1u << 16) | (14u << 12) | ((uint)sampleRateIndex << 10) | (isPadded ? 1u << 9 : 0) | ((channelCount is 1 ? 3u : 0u) << 6) | (1u << 2);
            var bytes = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, header);
            return bytes;
        }
        int NextSize(out bool isPadded)
        {
            remainder += numerator % sampleRate;
            isPadded = remainder >= sampleRate;
            if (isPadded)
                remainder -= sampleRate;
            return numerator / sampleRate + (isPadded ? 1 : 0);
        }
        if (gaplessTag is { } tag)
        {
            var size = NextSize(out var isPadded);
            var frame = new byte[size];
            Header(isPadded).CopyTo(frame, 0);
            var position = 4 + (isProtected ? 2 : 0) + sideInformationSize;
            "Info"u8.CopyTo(frame.AsSpan(position));
            BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(position + 4), 0x0f);
            BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(position + 8), (uint)(granules.Count / 2));
            var lame = position + 120;
            "LAME3.100"u8.CopyTo(frame.AsSpan(lame));
            frame[lame + 21] = (byte)(tag.delay >> 4);
            frame[lame + 22] = (byte)(((tag.delay & 0x0f) << 4) | (tag.padding >> 8));
            frame[lame + 23] = (byte)(tag.padding & 0xff);
            mp3.AddRange(frame);
        }
        for (var index = 0; index < granules.Count; index += 2)
        {
            var size = NextSize(out var isPadded);
            var bits = new List<bool>();
            AddBits(bits, 9, 0);
            AddBits(bits, channelCount is 1 ? 5 : 3, 0);
            for (var channel = 0; channel < channelCount; ++channel)
                AddBits(bits, 4, granules[index + 1].ScaleFactorSelection[channel]);
            foreach (var granule in new[] { granules[index], granules[index + 1] })
                for (var channel = 0; channel < channelCount; ++channel)
                {
                    AddBits(bits, 12, (uint)granule.MainDataBitCount[channel]);
                    AddBits(bits, 32, granule.OthersHigh[channel]);
                    AddBits(bits, 15, granule.OthersLow[channel]);
                }
            foreach (var granule in new[] { granules[index], granules[index + 1] })
                for (var channel = 0; channel < channelCount; ++channel)
                    bits.AddRange(granule.MainData[channel]);
            var frame = new byte[size];
            Header(isPadded).CopyTo(frame, 0);
            if (isProtected)
            {
                frame[4] = 0x12;
                frame[5] = 0x34;
            }
            Pack(bits).CopyTo(frame, 4 + (isProtected ? 2 : 0));
            mp3.AddRange(frame);
        }
        if (hasId3Tags)
            mp3.AddRange([.. "TAG"u8, .. new byte[125]]);
        return [.. mp3];
    }

    static byte[] Pack(List<bool> bits)
    {
        var bytes = new byte[(bits.Count + 7) / 8];
        for (var index = 0; index < bits.Count; ++index)
            if (bits[index])
                bytes[index >> 3] |= (byte)(0x80 >> (index & 7));
        return bytes;
    }

    public static ElectronicArtsLayer3TestMp3 ReadMp3(ReadOnlyMemory<byte> mp3)
    {
        var data = mp3.ToArray();
        int frameCount = 0, encoderDelay = 0, padding = 0, bitrateIndex = 0;
        var mainDataBegins = new List<int>();
        var frames = new List<(int channelCount, int sideInformationSize, int slotStart, int slotLength, int mainDataBegin, int[] scaleFactorSelection, int[][] bitCounts, uint[][] othersHigh, uint[][] othersLow)>();
        var reservoir = new List<byte>();
        var position = 0;
        var isFirst = true;
        while (position + 4 <= data.Length)
        {
            var header = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(position));
            if ((header >> 21) is not 0x7ff)
                throw new InvalidDataException($"No MPEG frame sync at {position}");
            bitrateIndex = (int)((header >> 12) & 0x0f);
            var sampleRate = ((header >> 10) & 3) switch { 0 => 44100, 1 => 48000, _ => 32000 };
            var isPadded = ((header >> 9) & 1) is 1;
            var channelCount = ((header >> 6) & 3) is 3 ? 1 : 2;
            var size = 144 * new[] { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320 }[bitrateIndex] * 1000 / sampleRate + (isPadded ? 1 : 0);
            var sideInformationSize = channelCount is 1 ? 17 : 32;
            if (isFirst)
            {
                var tag = data.AsSpan(position + 4 + sideInformationSize);
                if (!tag[..4].SequenceEqual("Info"u8))
                    throw new InvalidDataException("The first frame is not an Info frame");
                frameCount = (int)BinaryPrimitives.ReadUInt32BigEndian(tag[8..]);
                var lame = tag[120..];
                encoderDelay = (lame[21] << 4) | (lame[22] >> 4);
                padding = ((lame[22] & 0x0f) << 8) | lame[23];
                isFirst = false;
            }
            else
            {
                var reader = new List<bool>();
                foreach (var value in data.AsSpan(position + 4, sideInformationSize))
                    for (var bit = 7; bit >= 0; --bit)
                        reader.Add(((value >> bit) & 1) is 1);
                var cursor = 0;
                uint Take(int count)
                {
                    var value = 0u;
                    for (var bit = 0; bit < count; ++bit)
                        value = (value << 1) | (reader[cursor++] ? 1u : 0u);
                    return value;
                }
                var mainDataBegin = (int)Take(9);
                Take(channelCount is 1 ? 5 : 3);
                var scaleFactorSelection = Enumerable.Range(0, channelCount).Select(_ => (int)Take(4)).ToArray();
                var bitCounts = new int[2][];
                var othersHigh = new uint[2][];
                var othersLow = new uint[2][];
                for (var granule = 0; granule < 2; ++granule)
                {
                    bitCounts[granule] = new int[channelCount];
                    othersHigh[granule] = new uint[channelCount];
                    othersLow[granule] = new uint[channelCount];
                    for (var channel = 0; channel < channelCount; ++channel)
                    {
                        bitCounts[granule][channel] = (int)Take(12);
                        othersHigh[granule][channel] = Take(32);
                        othersLow[granule][channel] = Take(15);
                    }
                }
                frames.Add((channelCount, sideInformationSize, reservoir.Count, size - 4 - sideInformationSize, mainDataBegin, scaleFactorSelection, bitCounts, othersHigh, othersLow));
                mainDataBegins.Add(mainDataBegin);
                reservoir.AddRange(data.AsSpan(position + 4 + sideInformationSize, size - 4 - sideInformationSize).ToArray());
            }
            position += size;
        }
        if (position != data.Length)
            throw new InvalidDataException("The MP3 ends in the middle of a frame");
        var granules = new List<ElectronicArtsLayer3TestGranule>();
        foreach (var frame in frames)
        {
            var bitPosition = (frame.slotStart - frame.mainDataBegin) * 8;
            for (var granule = 0; granule < 2; ++granule)
            {
                var mainData = new bool[frame.channelCount][];
                for (var channel = 0; channel < frame.channelCount; ++channel)
                {
                    mainData[channel] = new bool[frame.bitCounts[granule][channel]];
                    for (var bit = 0; bit < mainData[channel].Length; ++bit, ++bitPosition)
                        mainData[channel][bit] = ((reservoir[bitPosition >> 3] >> (7 - (bitPosition & 7))) & 1) is 1;
                }
                granules.Add(new(granule, frame.channelCount, granule is 1 ? frame.scaleFactorSelection.Select(value => (uint)value).ToArray() : new uint[frame.channelCount], frame.bitCounts[granule], frame.othersHigh[granule], frame.othersLow[granule], mainData, 0));
            }
        }
        return new(frameCount, encoderDelay, padding, bitrateIndex, mainDataBegins, granules);
    }
}