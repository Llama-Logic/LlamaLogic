/* Copyright (c) 2008-2025 Adam Gashlin, Fastelbja, Ronny Elfert, bnnm,
 *                         Christopher Snowhill, NicknineTheEagle, bxaimc,
 *                         Thealexbarney, CyberBotX, EdnessP, et al
 *
 * Permission to use, copy, modify, and distribute this software for any
 * purpose with or without fee is hereby granted, provided that the above
 * copyright notice and this permission notice appear in all copies.
 *
 * THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
 * WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
 * ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
 * WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
 * ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
 * OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
 *
 * Ported to C# from vgmstream's src/coding/mpeg_custom_utils_ealayer3.c (EALayer3 version 1 as found in SNR/SNS only).
 */

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace LlamaLogic.Packages.Formats;
#pragma warning restore IDE0130 // Namespace does not match folder structure

sealed class ElectronicArtsLayer3Frame
{
    public int ChannelCount;
    public int ChannelMode;
    public int DataBitOffset;
    public int GranuleIndex;
    public bool IsMpeg1;
    public readonly int[] MainDataBitCount = new int[2];
    public int ModeExtension;
    public readonly uint[] OthersHigh = new uint[2];
    public readonly uint[] OthersLow = new uint[2];
    public int PulseCodeModulationSampleCount;
    public int SampleRate;
    public int SampleRateIndex;
    public readonly uint[] ScaleFactorSelection = new uint[2];
    public int Size;
    public int VersionIndex;
}

static class ElectronicArtsLayer3
{
    const int pulseCodeModulationFlag = 0xee;

    static readonly int[] versions = [3, -1, 2, 1];
    static readonly int[][] sampleRates =
    [
        [11025, 12000, 8000, -1],
        [-1, -1, -1, -1],
        [22050, 24000, 16000, -1],
        [44100, 48000, 32000, -1]
    ];

    public static int GetOthersLowBitCount(bool isMpeg1) =>
        isMpeg1 ? 47 - 32 : 51 - 32;

    public static ElectronicArtsLayer3Frame Parse(byte[] data, int offset)
    {
        var reader = new ElectronicArtsLayer3BitReader(data, offset * 8);
        var frame = new ElectronicArtsLayer3Frame();
        var flag = (int)reader.Read(8);
        if (flag is not (0 or pulseCodeModulationFlag))
            throw new InvalidDataException($"The EALayer3 frame at {offset} begins with 0x{flag:x2} rather than 0x00 or 0xee");
        var commonStart = reader.Position;
        frame.VersionIndex = (int)reader.Read(2);
        frame.SampleRateIndex = (int)reader.Read(2);
        frame.ChannelMode = (int)reader.Read(2);
        frame.ModeExtension = (int)reader.Read(2);
        if (frame.VersionIndex is 0 && frame.SampleRateIndex is 0 && frame.ChannelMode is 0 && frame.ModeExtension is 0)
            throw new InvalidDataException($"The EALayer3 frame at {offset} is empty");
        var version = versions[frame.VersionIndex];
        var sampleRate = sampleRates[frame.VersionIndex][frame.SampleRateIndex];
        if (version < 0 || sampleRate < 0)
            throw new InvalidDataException($"The EALayer3 frame at {offset} declares an illegal version or sample rate");
        frame.SampleRate = sampleRate;
        frame.IsMpeg1 = version is 1;
        frame.ChannelCount = frame.ChannelMode is 3 ? 1 : 2;
        var othersLowBitCount = GetOthersLowBitCount(frame.IsMpeg1);
        frame.GranuleIndex = (int)reader.Read(1);
        if (frame.IsMpeg1 && frame.GranuleIndex is 1)
            for (var channel = 0; channel < frame.ChannelCount; ++channel)
                frame.ScaleFactorSelection[channel] = reader.Read(4);
        for (var channel = 0; channel < frame.ChannelCount; ++channel)
        {
            frame.MainDataBitCount[channel] = (int)reader.Read(12);
            frame.OthersHigh[channel] = reader.Read(32);
            frame.OthersLow[channel] = reader.Read(othersLowBitCount);
        }
        frame.DataBitOffset = reader.Position;
        var baseBitCount = reader.Position - commonStart;
        var dataBitCount = frame.MainDataBitCount[0] + frame.MainDataBitCount[1];
        var commonSize = (baseBitCount + dataBitCount + 7) / 8;
        var size = 1 + commonSize;
        if (flag is pulseCodeModulationFlag)
        {
            if (offset + size + 8 > data.Length)
                throw new InvalidDataException($"The EALayer3 frame at {offset} is cut off before its uncompressed block");
            frame.PulseCodeModulationSampleCount = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + size + 2));
            size += 8 + 2 * frame.PulseCodeModulationSampleCount * frame.ChannelCount;
        }
        if (offset + size > data.Length)
            throw new InvalidDataException($"The EALayer3 frame at {offset} is cut off");
        frame.Size = size;
        return frame;
    }

    public static void WriteMainData(ElectronicArtsLayer3BitWriter writer, byte[] data, ElectronicArtsLayer3Frame first, ElectronicArtsLayer3Frame? second)
    {
        foreach (var frame in second is null ? new[] { first } : new[] { first, second })
        {
            var reader = new ElectronicArtsLayer3BitReader(data, frame.DataBitOffset);
            for (var channel = 0; channel < frame.ChannelCount; ++channel)
                writer.Copy(ref reader, frame.MainDataBitCount[channel]);
        }
    }

    public static void WriteSideInformation(ElectronicArtsLayer3BitWriter writer, ElectronicArtsLayer3Frame first, ElectronicArtsLayer3Frame? second, int mainDataBegin)
    {
        var othersLowBitCount = GetOthersLowBitCount(first.IsMpeg1);
        if (first.IsMpeg1)
        {
            writer.Write(9, (uint)mainDataBegin);
            writer.Write(first.ChannelCount is 1 ? 5 : 3, 0);
            for (var channel = 0; channel < second!.ChannelCount; ++channel)
                writer.Write(4, second.ScaleFactorSelection[channel]);
        }
        else
        {
            writer.Write(8, (uint)mainDataBegin);
            writer.Write(first.ChannelCount is 1 ? 1 : 2, 0);
        }
        foreach (var frame in second is null ? new[] { first } : new[] { first, second })
            for (var channel = 0; channel < frame.ChannelCount; ++channel)
            {
                writer.Write(12, (uint)frame.MainDataBitCount[channel]);
                writer.Write(32, frame.OthersHigh[channel]);
                writer.Write(othersLowBitCount, frame.OthersLow[channel]);
            }
    }
}