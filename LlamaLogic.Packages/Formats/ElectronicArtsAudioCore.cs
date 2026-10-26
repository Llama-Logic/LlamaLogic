namespace LlamaLogic.Packages.Formats;

/// <summary>
/// Provides methods for dealing with sounds in Electronic Arts' AudioCore format: the <see cref="ResourceType.AudioVocals"/> resources of The Sims 4 (a header, followed by its audio if the sound is held in memory) and their <see cref="ResourceType.AudioEffects"/> bodies (the audio of a streamed sound)
/// </summary>
/// <remarks>
/// The rebuilding of EALayer3 into MP3 is adapted from vgmstream (see <c>Formats/Vgmstream/license.txt</c>); unlike vgmstream, it lays the audio out at a constant bitrate with a bit reservoir and adds a gapless playback tag, so that ordinary MP3 decoders play it sample for sample as the game does
/// </remarks>
public static class ElectronicArtsAudioCore
{
    const int blockHeaderSize = 8;
    const int encoderDelay = 576;
    const byte lastBlockFlag = 0x80;
    const int maximumPadding = 0xfff;

    static readonly ushort[] crcTable = CreateCrcTable();

    static ushort[] CreateCrcTable()
    {
        var table = new ushort[256];
        for (var index = 0; index < 256; ++index)
        {
            var crc = (uint)index;
            for (var bit = 0; bit < 8; ++bit)
                crc = (crc & 1) is 1 ? (crc >> 1) ^ 0xa001 : crc >> 1;
            table[index] = (ushort)crc;
        }
        return table;
    }

    static ushort ComputeCrc(ReadOnlySpan<byte> data)
    {
        var crc = 0;
        foreach (var value in data)
            crc = (crc >> 8) ^ crcTable[(crc ^ value) & 0xff];
        return (ushort)crc;
    }

    static ReadOnlyMemory<byte> CreateMp3(byte[] data, List<ElectronicArtsLayer3Frame> frames, int sampleCount)
    {
        if (frames.Count is 0)
            throw new InvalidDataException("A layer of the sound has no audio");
        var first = frames[0];
        var isMpeg1 = first.IsMpeg1;
        var pairs = new List<(ElectronicArtsLayer3Frame first, ElectronicArtsLayer3Frame? second)>();
        if (isMpeg1)
        {
            if ((frames.Count & 1) is 1)
                throw new InvalidDataException("A layer of the sound ends with an unpaired EALayer3 granule");
            for (var index = 0; index < frames.Count; index += 2)
            {
                var granule0 = frames[index];
                var granule1 = frames[index + 1];
                if (granule0.GranuleIndex is not 0 || granule1.GranuleIndex is not 1 || !granule0.IsMpeg1 || !granule1.IsMpeg1 || granule0.SampleRate != first.SampleRate || granule1.SampleRate != first.SampleRate)
                    throw new InvalidDataException($"EALayer3 granules {index} and {index + 1} of a layer do not make an MPEG-1 frame");
                pairs.Add((granule0, granule1));
            }
        }
        else
            foreach (var frame in frames)
            {
                if (frame.IsMpeg1 || frame.SampleRate != first.SampleRate)
                    throw new InvalidDataException("A layer of the sound mixes MPEG versions or sample rates");
                pairs.Add((frame, null));
            }
        var samplesPerFrame = isMpeg1 ? 1152 : 576;
        var padding = pairs.Count * samplesPerFrame - encoderDelay - sampleCount;
        if (padding is < 0 or > maximumPadding)
            throw new InvalidDataException($"{pairs.Count} MPEG frames cannot hold the {sampleCount} samples the sound declares");
        var bitrate = isMpeg1 ? 320 : 160;
        var numerator = (isMpeg1 ? 144 : 72) * bitrate * 1000;
        var maximumMainDataBegin = isMpeg1 ? 511 : 255;
        var remainder = 0;
        (int size, bool isPadded) NextFrameSize()
        {
            remainder += numerator % first.SampleRate;
            var isPadded = remainder >= first.SampleRate;
            if (isPadded)
                remainder -= first.SampleRate;
            return (numerator / first.SampleRate + (isPadded ? 1 : 0), isPadded);
        }
        int GetSideInformationSize(int channelCount) =>
            isMpeg1 ? (channelCount is 1 ? 17 : 32) : (channelCount is 1 ? 9 : 17);
        var writer = new ElectronicArtsLayer3BitWriter();
        var infoFrame = NextFrameSize();
        var frameSizes = new (int size, bool isPadded)[pairs.Count];
        var mainData = new byte[pairs.Count][];
        var mainDataStarts = new int[pairs.Count];
        var mainDataBegins = new int[pairs.Count];
        var cursor = 0;
        var slotStart = 0;
        for (var index = 0; index < pairs.Count; ++index)
        {
            var (granule0, granule1) = pairs[index];
            frameSizes[index] = NextFrameSize();
            var capacity = frameSizes[index].size - 4 - GetSideInformationSize(granule0.ChannelCount);
            writer.Reset();
            ElectronicArtsLayer3.WriteMainData(writer, data, granule0, granule1);
            writer.Align();
            mainData[index] = writer.GetWrittenBytes().ToArray();
            var start = Math.Max(cursor, slotStart - maximumMainDataBegin);
            if (start + mainData[index].Length > slotStart + capacity)
                throw new InvalidDataException($"MPEG frame {index} of a layer needs more room than the bit reservoir can give it");
            mainDataStarts[index] = start;
            mainDataBegins[index] = slotStart - start;
            cursor = start + mainData[index].Length;
            slotStart += capacity;
        }
        var reservoir = new byte[slotStart];
        for (var index = 0; index < pairs.Count; ++index)
            mainData[index].CopyTo(reservoir, mainDataStarts[index]);
        var audioLength = frameSizes.Sum(frameSize => frameSize.size);
        var mp3 = new byte[infoFrame.size + audioLength];
        void WriteHeader(Span<byte> destination, ElectronicArtsLayer3Frame frame, bool isPadded, int modeExtension)
        {
            writer.Reset();
            writer.Write(11, 0x7ff);
            writer.Write(2, (uint)frame.VersionIndex);
            writer.Write(2, 1);
            writer.Write(1, 1);
            writer.Write(4, 14);
            writer.Write(2, (uint)frame.SampleRateIndex);
            writer.Write(1, isPadded ? 1u : 0u);
            writer.Write(1, 0);
            writer.Write(2, (uint)frame.ChannelMode);
            writer.Write(2, (uint)modeExtension);
            writer.Write(1, 0);
            writer.Write(1, 1);
            writer.Write(2, 0);
            writer.GetWrittenBytes().CopyTo(destination);
        }
        var position = infoFrame.size;
        var reservoirPosition = 0;
        for (var index = 0; index < pairs.Count; ++index)
        {
            var (granule0, granule1) = pairs[index];
            var (size, isPadded) = frameSizes[index];
            WriteHeader(mp3.AsSpan(position), granule0, isPadded, granule0.ModeExtension);
            writer.Reset();
            ElectronicArtsLayer3.WriteSideInformation(writer, granule0, granule1, mainDataBegins[index]);
            writer.Align();
            var sideInformation = writer.GetWrittenBytes();
            sideInformation.CopyTo(mp3.AsSpan(position + 4));
            var capacity = size - 4 - sideInformation.Length;
            reservoir.AsSpan(reservoirPosition, capacity).CopyTo(mp3.AsSpan(position + 4 + sideInformation.Length));
            reservoirPosition += capacity;
            position += size;
        }
        var info = mp3.AsSpan(0, infoFrame.size);
        WriteHeader(info, first, infoFrame.isPadded, 0);
        var tag = info[(4 + GetSideInformationSize(first.ChannelCount))..];
        "Info"u8.CopyTo(tag);
        BinaryPrimitives.WriteUInt32BigEndian(tag[4..], 0x0f);
        BinaryPrimitives.WriteUInt32BigEndian(tag[8..], (uint)pairs.Count);
        BinaryPrimitives.WriteUInt32BigEndian(tag[12..], (uint)mp3.Length);
        for (var index = 0; index < 100; ++index)
            tag[16 + index] = (byte)(index * 256 / 100);
        var lame = tag[120..];
        "LAME3.100"u8.CopyTo(lame);
        lame[9] = 0x01;
        lame[20] = (byte)Math.Min(bitrate, 255);
        lame[21] = encoderDelay >> 4;
        lame[22] = (byte)(((encoderDelay & 0x0f) << 4) | (padding >> 8));
        lame[23] = (byte)(padding & 0xff);
        BinaryPrimitives.WriteUInt32BigEndian(lame[28..], (uint)mp3.Length);
        BinaryPrimitives.WriteUInt16BigEndian(lame[32..], ComputeCrc(mp3.AsSpan(infoFrame.size)));
        var tagCrcOffset = info.Length - lame.Length + 34;
        BinaryPrimitives.WriteUInt16BigEndian(lame[34..], ComputeCrc(info[..tagCrcOffset]));
        return mp3;
    }

    /// <summary>
    /// Rebuilds a sound encoded with <see cref="ElectronicArtsAudioCoreCodec.ElectronicArtsLayer3Version1"/> as MP3, without re-encoding it
    /// </summary>
    /// <param name="headerData">The data of the sound's <see cref="ResourceType.AudioVocals"/> resource</param>
    /// <param name="streamData">The data of the sound's <see cref="ResourceType.AudioEffects"/> resource, if the sound is streamed</param>
    /// <exception cref="ArgumentException">The sound is streamed and <paramref name="streamData"/> is empty</exception>
    /// <exception cref="InvalidDataException">The header or the audio is malformed</exception>
    /// <exception cref="NotSupportedException">The sound uses another codec or another version of the header (The Sims 4's sounds are version 0), or is stored as a <see cref="ElectronicArtsAudioCoreStorage.Gigasample"/></exception>
    public static ElectronicArtsAudioCoreMp3 GetMp3(ReadOnlyMemory<byte> headerData, ReadOnlyMemory<byte> streamData = default)
    {
        var header = ElectronicArtsAudioCoreHeader.Read(headerData.Span);
        if (header.Codec is not ElectronicArtsAudioCoreCodec.ElectronicArtsLayer3Version1)
            throw new NotSupportedException($"Sounds encoded with {header.Codec} cannot be rebuilt as MP3");
        if (header.Version is not 0)
            throw new NotSupportedException($"Version {header.Version} AudioCore sounds cannot be rebuilt as MP3");
        if (header.Storage is ElectronicArtsAudioCoreStorage.Gigasample)
            throw new NotSupportedException("Gigasample sounds cannot be rebuilt as MP3");
        if (header.Storage is ElectronicArtsAudioCoreStorage.Stream && streamData.IsEmpty)
            throw new ArgumentException("A streamed sound cannot be rebuilt without its body", nameof(streamData));
        var data = header.Storage is ElectronicArtsAudioCoreStorage.Memory ? headerData[header.Size..] : streamData;
        var blocks = ReadBlocks(data.Span);
        var splitBlock = blocks.Count;
        if (header.LoopStartSample is > 0)
        {
            splitBlock = header.Storage is ElectronicArtsAudioCoreStorage.Memory ? 1 : blocks.FindIndex(block => block.offset == header.LoopOffset);
            if (splitBlock < 1 || splitBlock >= blocks.Count)
                throw new InvalidDataException("The block where the sound's loop starts could not be found");
        }
        var ranges = splitBlock == blocks.Count
            ? new[] { (firstBlock: 0, blockCount: blocks.Count, firstSample: 0, sampleCount: header.SampleCount) }
            : new[] { (firstBlock: 0, blockCount: splitBlock, firstSample: 0, sampleCount: header.LoopStartSample!.Value), (firstBlock: splitBlock, blockCount: blocks.Count - splitBlock, firstSample: header.LoopStartSample!.Value, sampleCount: header.SampleCount - header.LoopStartSample!.Value) };
        var layerCount = (header.ChannelCount + 1) / 2;
        var segments = new List<ElectronicArtsAudioCoreMp3Segment>();
        foreach (var (firstBlock, blockCount, firstSample, sampleCount) in ranges)
        {
            var payload = new byte[blocks.Skip(firstBlock).Take(blockCount).Sum(block => block.length)];
            var payloadPosition = 0;
            foreach (var (_, start, length) in blocks.Skip(firstBlock).Take(blockCount))
            {
                data.Span.Slice(start, length).CopyTo(payload.AsSpan(payloadPosition));
                payloadPosition += length;
            }
            var layers = Enumerable.Range(0, layerCount).Select(_ => new List<ElectronicArtsLayer3Frame>()).ToList();
            var offset = 0;
            var layer = 0;
            while (offset < payload.Length && !IsZero(payload.AsSpan(offset)))
            {
                var frame = ElectronicArtsLayer3.Parse(payload, offset);
                var expectedChannelCount = layer == layerCount - 1 && (header.ChannelCount & 1) is 1 ? 1 : 2;
                if (frame.ChannelCount != expectedChannelCount)
                    throw new InvalidDataException($"The EALayer3 frame at {offset} has {frame.ChannelCount} channels where layer {layer} of a {header.ChannelCount}-channel sound needs {expectedChannelCount}");
                layers[layer].Add(frame);
                offset += frame.Size;
                layer = (layer + 1) % layerCount;
            }
            segments.Add(new(firstSample, sampleCount, layers.Select(frames => CreateMp3(payload, frames, sampleCount)).ToList().AsReadOnly()));
        }
        return new(header, segments.AsReadOnly());
    }

    static bool IsZero(ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
            if (value is not 0)
                return false;
        return true;
    }

    static List<(int offset, int start, int length)> ReadBlocks(ReadOnlySpan<byte> data)
    {
        var blocks = new List<(int offset, int start, int length)>();
        var offset = 0;
        while (offset + blockHeaderSize <= data.Length)
        {
            var flag = data[offset];
            var size = (int)(BinaryPrimitives.ReadUInt32BigEndian(data[offset..]) & 0xffffff);
            if (size < blockHeaderSize || offset + size > data.Length)
                throw new InvalidDataException($"The audio block at {offset} declares a size of {size}, which does not fit");
            blocks.Add((offset, offset + blockHeaderSize, size - blockHeaderSize));
            offset += size;
            if (flag is lastBlockFlag)
                break;
        }
        if (blocks.Count is 0)
            throw new InvalidDataException("The sound has no audio blocks");
        return blocks;
    }
}