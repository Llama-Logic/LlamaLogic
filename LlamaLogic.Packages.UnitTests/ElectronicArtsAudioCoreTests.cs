namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class ElectronicArtsAudioCoreTests
{
    static void AssertGranulesAreKept(IReadOnlyList<ElectronicArtsLayer3TestGranule> expected, ElectronicArtsLayer3TestMp3 actual)
    {
        Assert.AreEqual(expected.Count, actual.Granules.Count);
        for (var index = 0; index < expected.Count; ++index)
        {
            var wanted = expected[index];
            var found = actual.Granules[index];
            Assert.AreEqual(wanted.Index, found.Index, $"granule {index}");
            Assert.AreEqual(wanted.ChannelCount, found.ChannelCount, $"granule {index}");
            CollectionAssert.AreEqual(wanted.ScaleFactorSelection, found.ScaleFactorSelection, $"granule {index}");
            CollectionAssert.AreEqual(wanted.MainDataBitCount, found.MainDataBitCount, $"granule {index}");
            CollectionAssert.AreEqual(wanted.OthersHigh, found.OthersHigh, $"granule {index}");
            CollectionAssert.AreEqual(wanted.OthersLow, found.OthersLow, $"granule {index}");
            for (var channel = 0; channel < wanted.ChannelCount; ++channel)
                CollectionAssert.AreEqual(wanted.MainData[channel], found.MainData[channel], $"granule {index}, channel {channel}");
        }
    }

    static int EncodedFrameSize(ElectronicArtsLayer3TestGranule granule) =>
        1 + (9 + (granule.Index is 1 ? 4 * granule.ChannelCount : 0) + granule.ChannelCount * 59 + granule.MainDataBitCount.Sum() + 7) / 8;

    static ElectronicArtsLayer3TestMp3 ReadSound(ElectronicArtsAudioCoreSound sound, int segment = 0, int layer = 0) =>
        ElectronicArtsAudioCoreBuilder.ReadMp3(ElectronicArtsAudioCore.GetMp3(sound.AudioVocalsData, sound.AudioEffectsData).Segments[segment].Layers[layer]);

    static int SampleCountFor(int pairCount) =>
        pairCount * 1152 - 576 - 300;

    [TestMethod]
    [DataRow(0, 44100)]
    [DataRow(1, 48000)]
    [DataRow(2, 32000)]
    public void CreateFromMp3AcceptsEverySampleRateTheGameUses(int sampleRateIndex, int sampleRate)
    {
        var granules = ElectronicArtsAudioCoreBuilder.CreateGranules(new Random(30 + sampleRateIndex), 2, 6, 100, 1500);
        var sound = ElectronicArtsAudioCore.CreateFromMp3(ElectronicArtsAudioCoreBuilder.Mp3(granules, sampleRateIndex: sampleRateIndex, gaplessTag: (576, 300)), ElectronicArtsAudioCoreStorage.Memory);
        Assert.AreEqual(sampleRate, sound.Header.SampleRate);
        AssertGranulesAreKept(granules, ReadSound(sound));
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public void CreateFromMp3KeepsEveryGranule(int channelCount)
    {
        var granules = ElectronicArtsAudioCoreBuilder.CreateGranules(new Random(10 + channelCount), channelCount, 12, 100, 1500);
        var sound = ElectronicArtsAudioCore.CreateFromMp3(ElectronicArtsAudioCoreBuilder.Mp3(granules, gaplessTag: (576, 300)), ElectronicArtsAudioCoreStorage.Memory);
        Assert.AreEqual(ElectronicArtsAudioCoreCodec.ElectronicArtsLayer3Version1, sound.Header.Codec);
        Assert.AreEqual(channelCount, sound.Header.ChannelCount);
        Assert.AreEqual(44100, sound.Header.SampleRate);
        Assert.AreEqual(ElectronicArtsAudioCoreStorage.Memory, sound.Header.Storage);
        Assert.AreEqual(12 * 1152 - 576 - 300, sound.Header.SampleCount);
        Assert.IsNull(sound.Header.LoopStartSample);
        Assert.IsTrue(sound.AudioEffectsData.IsEmpty);
        AssertGranulesAreKept(granules, ReadSound(sound));
    }

    [TestMethod]
    public void CreateFromMp3LaysStreamedLoopsOutAsTheGameDoes()
    {
        var random = new Random(18);
        var intro = ElectronicArtsAudioCoreBuilder.CreateGranules(random, 2, 5, 100, 1500);
        var loop = ElectronicArtsAudioCoreBuilder.CreateGranules(random, 2, 7, 100, 1500);
        var introSampleCount = 5 * 1152 - 576 - 600;
        var loopSampleCount = 7 * 1152 - 576 - 700;
        var sound = ElectronicArtsAudioCore.CreateFromMp3([[ElectronicArtsAudioCoreBuilder.Mp3(intro, gaplessTag: (576, 600))], [ElectronicArtsAudioCoreBuilder.Mp3(loop, gaplessTag: (576, 700))]], ElectronicArtsAudioCoreStorage.Stream, true);
        Assert.AreEqual(introSampleCount, sound.Header.LoopStartSample);
        Assert.AreEqual(introSampleCount + loopSampleCount, sound.Header.SampleCount);
        var body = sound.AudioEffectsData.ToArray();
        var blocks = new List<(int offset, byte flag, int samples)>();
        for (var offset = 0; offset < body.Length;)
        {
            blocks.Add((offset, body[offset], (int)BinaryPrimitives.ReadUInt32BigEndian(body.AsSpan(offset + 4))));
            offset += (int)(BinaryPrimitives.ReadUInt32BigEndian(body.AsSpan(offset)) & 0xffffff);
        }
        var loopBlock = blocks.FindIndex(block => block.offset == sound.Header.LoopOffset);
        Assert.IsTrue(loopBlock > 0);
        Assert.AreEqual(0, sound.Header.LoopOffset!.Value % 64);
        Assert.AreEqual((byte)0x80, blocks[loopBlock - 1].flag);
        Assert.AreEqual(introSampleCount, blocks.Take(loopBlock).Sum(block => block.samples));
        Assert.AreEqual(47, blocks[loopBlock].samples);
        Assert.AreEqual(loopSampleCount, blocks.Skip(loopBlock).Sum(block => block.samples));
        Assert.AreEqual((byte)0x80, blocks[^1].flag);
        Assert.AreEqual(0, body.Length % 64);
        AssertGranulesAreKept(intro, ReadSound(sound, 0));
        AssertGranulesAreKept(loop, ReadSound(sound, 1));
    }

    [TestMethod]
    public void CreateFromMp3LaysStreamsOutAsTheGameDoes()
    {
        var granules = ElectronicArtsAudioCoreBuilder.CreateGranules(new Random(13), 2, 20, 100, 1500);
        var sound = ElectronicArtsAudioCore.CreateFromMp3(ElectronicArtsAudioCoreBuilder.Mp3(granules, sampleRateIndex: 1, gaplessTag: (576, 1800)), ElectronicArtsAudioCoreStorage.Stream, true);
        var sampleCount = 20 * 1152 - 576 - 1800;
        Assert.AreEqual(ElectronicArtsAudioCoreStorage.Stream, sound.Header.Storage);
        Assert.AreEqual(48000, sound.Header.SampleRate);
        Assert.AreEqual(sampleCount, sound.Header.SampleCount);
        Assert.AreEqual(0, sound.Header.LoopStartSample);
        Assert.AreEqual(0, sound.Header.LoopOffset);
        Assert.AreEqual(16, sound.AudioVocalsData.Length);
        var body = sound.AudioEffectsData.ToArray();
        Assert.AreEqual(0, body.Length % 64);
        var blocks = new List<(byte flag, int samples)>();
        for (var offset = 0; offset < body.Length;)
        {
            var size = (int)(BinaryPrimitives.ReadUInt32BigEndian(body.AsSpan(offset)) & 0xffffff);
            blocks.Add((body[offset], (int)BinaryPrimitives.ReadUInt32BigEndian(body.AsSpan(offset + 4))));
            offset += size;
        }
        Assert.AreEqual(19, blocks.Count);
        Assert.AreEqual(((byte)0, 47), blocks[0]);
        Assert.IsTrue(blocks.Skip(1).Take(17).All(block => block == (0, 1152)));
        Assert.AreEqual(((byte)0x80, sampleCount - 47 - 17 * 1152), blocks[^1]);
        AssertGranulesAreKept(granules, ReadSound(sound));
    }

    [TestMethod]
    public void CreateFromMp3MarksTheSecondGranuleOfEachSegmentUncompressed()
    {
        var random = new Random(14);
        var intro = ElectronicArtsAudioCoreBuilder.CreateGranules(random, 1, 4, 100, 900);
        var loop = ElectronicArtsAudioCoreBuilder.CreateGranules(random, 1, 6, 100, 900);
        var introSampleCount = 4 * 1152 - 576 - 200;
        var loopSampleCount = 6 * 1152 - 576 - 400;
        var sound = ElectronicArtsAudioCore.CreateFromMp3([[ElectronicArtsAudioCoreBuilder.Mp3(intro, gaplessTag: (576, 200))], [ElectronicArtsAudioCoreBuilder.Mp3(loop, gaplessTag: (576, 400))]], ElectronicArtsAudioCoreStorage.Memory, true);
        Assert.AreEqual(introSampleCount, sound.Header.LoopStartSample);
        Assert.AreEqual(introSampleCount + loopSampleCount, sound.Header.SampleCount);
        var data = sound.AudioVocalsData.ToArray();
        var offset = sound.Header.Size;
        foreach (var (granules, sampleCount) in new[] { (intro, introSampleCount), (loop, loopSampleCount) })
        {
            var blockSize = (int)(BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset)) & 0xffffff);
            Assert.AreEqual(0, data[offset]);
            Assert.AreEqual(sampleCount, (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 4)));
            var first = offset + 8;
            Assert.AreEqual(0, data[first]);
            var second = first + EncodedFrameSize(granules[0]);
            Assert.AreEqual(0xee, data[second]);
            var block = second + EncodedFrameSize(granules[1]);
            CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 47, 0, 0, 0, 0 }, data[block..(block + 8)]);
            Assert.IsTrue(data.AsSpan(block + 8, 2 * 47).ToArray().All(value => value is 0));
            offset += blockSize;
        }
        Assert.AreEqual(data.Length, offset);
        AssertGranulesAreKept(intro, ReadSound(sound, 0));
        AssertGranulesAreKept(loop, ReadSound(sound, 1));
    }

    [TestMethod]
    public void CreateFromMp3PutsLayersTogether()
    {
        var random = new Random(15);
        var layers = new[] { 2, 2, 1 }.Select(channelCount => ElectronicArtsAudioCoreBuilder.CreateGranules(random, channelCount, 10, 100, 1200)).ToArray();
        var sound = ElectronicArtsAudioCore.CreateFromMp3([layers.Select(layer => (ReadOnlyMemory<byte>)ElectronicArtsAudioCoreBuilder.Mp3(layer, gaplessTag: (576, 1000))).ToList()], ElectronicArtsAudioCoreStorage.Stream, false);
        Assert.AreEqual(5, sound.Header.ChannelCount);
        for (var layer = 0; layer < 3; ++layer)
            AssertGranulesAreKept(layers[layer], ReadSound(sound, 0, layer));
    }

    [TestMethod]
    public void CreateFromMp3RejectsEncoderDelaysShorterThanTheGamesSkip() =>
        Assert.ThrowsExactly<InvalidDataException>(() => ElectronicArtsAudioCore.CreateFromMp3(ElectronicArtsAudioCoreBuilder.Mp3(ElectronicArtsAudioCoreBuilder.CreateGranules(new Random(16), 2, 4, 100, 200), gaplessTag: (500, 0)), ElectronicArtsAudioCoreStorage.Memory));

    [TestMethod]
    public void CreateFromMp3RejectsMpeg2() =>
        Assert.ThrowsExactly<NotSupportedException>(() => ElectronicArtsAudioCore.CreateFromMp3(ElectronicArtsAudioCoreBuilder.Mp3(ElectronicArtsAudioCoreBuilder.CreateGranules(new Random(17), 2, 4, 100, 200), versionBits: 2), ElectronicArtsAudioCoreStorage.Memory));

    [TestMethod]
    public void CreateFromMp3RejectsTwoSegmentsThatDoNotLoop()
    {
        var mp3 = ElectronicArtsAudioCoreBuilder.Mp3(ElectronicArtsAudioCoreBuilder.CreateGranules(new Random(19), 2, 4, 100, 200), gaplessTag: (576, 0));
        Assert.ThrowsExactly<ArgumentException>(() => ElectronicArtsAudioCore.CreateFromMp3([[mp3], [mp3]], ElectronicArtsAudioCoreStorage.Memory, false));
    }

    [TestMethod]
    public void CreateFromMp3RemakesWhatGetMp3Rebuilt()
    {
        var granules = ElectronicArtsAudioCoreBuilder.CreateGranules(new Random(20), 2, 9, 100, 1500);
        var sampleCount = SampleCountFor(9);
        var header = ElectronicArtsAudioCoreBuilder.Header(2, 44100, ElectronicArtsAudioCoreStorage.Memory, sampleCount, 0);
        var original = header.Concat(ElectronicArtsAudioCoreBuilder.Block(false, sampleCount, ElectronicArtsAudioCoreBuilder.Interleave(0, granules))).ToArray();
        var mp3 = ElectronicArtsAudioCore.GetMp3(original);
        var remade = ElectronicArtsAudioCore.CreateFromMp3(mp3);
        CollectionAssert.AreEqual(header, remade.AudioVocalsData[..header.Length].ToArray());
        CollectionAssert.AreEqual(mp3.Segments[0].Layers[0].ToArray(), ElectronicArtsAudioCore.GetMp3(remade.AudioVocalsData).Segments[0].Layers[0].ToArray());
    }

    [TestMethod]
    public void CreateFromMp3SkipsTagsAndChecksums()
    {
        var granules = ElectronicArtsAudioCoreBuilder.CreateGranules(new Random(21), 2, 6, 100, 1500);
        var plain = ElectronicArtsAudioCore.CreateFromMp3(ElectronicArtsAudioCoreBuilder.Mp3(granules, gaplessTag: (576, 500)), ElectronicArtsAudioCoreStorage.Memory);
        var decorated = ElectronicArtsAudioCore.CreateFromMp3(ElectronicArtsAudioCoreBuilder.Mp3(granules, isProtected: true, gaplessTag: (576, 500), hasId3Tags: true), ElectronicArtsAudioCoreStorage.Memory);
        CollectionAssert.AreEqual(plain.AudioVocalsData.ToArray(), decorated.AudioVocalsData.ToArray());
    }

    [TestMethod]
    public void CreateFromMp3TakesAnMp3WithoutAGaplessTagToEndWithItsLastFrame()
    {
        var sound = ElectronicArtsAudioCore.CreateFromMp3(ElectronicArtsAudioCoreBuilder.Mp3(ElectronicArtsAudioCoreBuilder.CreateGranules(new Random(22), 2, 8, 100, 200)), ElectronicArtsAudioCoreStorage.Memory);
        Assert.AreEqual(8 * 1152 - 1105, sound.Header.SampleCount);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public void GetMp3KeepsEveryGranule(int channelCount)
    {
        var random = new Random(channelCount);
        var granules = ElectronicArtsAudioCoreBuilder.CreateGranules(random, channelCount, 20, 100, 1500);
        var sampleCount = SampleCountFor(20);
        var data = ElectronicArtsAudioCoreBuilder.Header(channelCount, 44100, ElectronicArtsAudioCoreStorage.Memory, sampleCount)
            .Concat(ElectronicArtsAudioCoreBuilder.Block(true, sampleCount, ElectronicArtsAudioCoreBuilder.Interleave(0, granules)))
            .ToArray();
        var mp3 = ElectronicArtsAudioCore.GetMp3(data);
        Assert.AreEqual(1, mp3.Segments.Count);
        Assert.AreEqual(0, mp3.Segments[0].FirstSample);
        Assert.AreEqual(sampleCount, mp3.Segments[0].SampleCount);
        Assert.AreEqual(1, mp3.Segments[0].Layers.Count);
        var read = ElectronicArtsAudioCoreBuilder.ReadMp3(mp3.Segments[0].Layers[0]);
        Assert.AreEqual(20, read.FrameCount);
        Assert.AreEqual(14, read.BitrateIndex);
        Assert.AreEqual(576, read.EncoderDelay);
        Assert.AreEqual(300, read.Padding);
        AssertGranulesAreKept(granules, read);
    }

    [TestMethod]
    public void GetMp3LaysOutFramesThatOutgrowTheirBitrateWithTheBitReservoir()
    {
        var random = new Random(3);
        var granules = ElectronicArtsAudioCoreBuilder.CreateGranules(random, 2, 10, 100, 200);
        granules.AddRange(ElectronicArtsAudioCoreBuilder.CreateGranules(random, 2, 1, 2400, 2400, false));
        var sampleCount = SampleCountFor(11);
        var data = ElectronicArtsAudioCoreBuilder.Header(2, 44100, ElectronicArtsAudioCoreStorage.Memory, sampleCount)
            .Concat(ElectronicArtsAudioCoreBuilder.Block(true, sampleCount, ElectronicArtsAudioCoreBuilder.Interleave(0, granules)))
            .ToArray();
        var read = ElectronicArtsAudioCoreBuilder.ReadMp3(ElectronicArtsAudioCore.GetMp3(data).Segments[0].Layers[0]);
        Assert.IsTrue(read.MainDataBegins[^1] > 0);
        AssertGranulesAreKept(granules, read);
    }

    [TestMethod]
    public void GetMp3RejectsAudioTheBitReservoirCannotHold()
    {
        var granules = ElectronicArtsAudioCoreBuilder.CreateGranules(new Random(4), 2, 4, 4095, 4095);
        var sampleCount = SampleCountFor(4);
        var data = ElectronicArtsAudioCoreBuilder.Header(2, 44100, ElectronicArtsAudioCoreStorage.Memory, sampleCount)
            .Concat(ElectronicArtsAudioCoreBuilder.Block(true, sampleCount, ElectronicArtsAudioCoreBuilder.Interleave(0, granules)))
            .ToArray();
        Assert.ThrowsExactly<InvalidDataException>(() => ElectronicArtsAudioCore.GetMp3(data));
    }

    [TestMethod]
    public void GetMp3RejectsOtherCodecs() =>
        Assert.ThrowsExactly<NotSupportedException>(() => ElectronicArtsAudioCore.GetMp3(ElectronicArtsAudioCoreBuilder.Header(2, 44100, ElectronicArtsAudioCoreStorage.Memory, 1000, codec: ElectronicArtsAudioCoreCodec.PulseCodeModulation)));

    [TestMethod]
    public void GetMp3RejectsSoundsLongerThanTheirAudio()
    {
        var granules = ElectronicArtsAudioCoreBuilder.CreateGranules(new Random(5), 2, 4, 100, 200);
        var data = ElectronicArtsAudioCoreBuilder.Header(2, 44100, ElectronicArtsAudioCoreStorage.Memory, 4 * 1152)
            .Concat(ElectronicArtsAudioCoreBuilder.Block(true, 4 * 1152, ElectronicArtsAudioCoreBuilder.Interleave(0, granules)))
            .ToArray();
        Assert.ThrowsExactly<InvalidDataException>(() => ElectronicArtsAudioCore.GetMp3(data));
    }

    [TestMethod]
    public void GetMp3RejectsStreamedSoundsWithoutTheirBodies() =>
        Assert.ThrowsExactly<ArgumentException>(() => ElectronicArtsAudioCore.GetMp3(ElectronicArtsAudioCoreBuilder.Header(2, 44100, ElectronicArtsAudioCoreStorage.Stream, 1000)));

    [TestMethod]
    public void GetMp3SplitsMemorySoundsThatLoopPartWayAtTheirSecondBlock()
    {
        var random = new Random(6);
        var intro = ElectronicArtsAudioCoreBuilder.CreateGranules(random, 1, 6, 100, 900);
        var loop = ElectronicArtsAudioCoreBuilder.CreateGranules(random, 1, 9, 100, 900);
        var introSampleCount = SampleCountFor(6);
        var loopSampleCount = SampleCountFor(9);
        var data = ElectronicArtsAudioCoreBuilder.Header(1, 48000, ElectronicArtsAudioCoreStorage.Memory, introSampleCount + loopSampleCount, introSampleCount)
            .Concat(ElectronicArtsAudioCoreBuilder.Block(false, introSampleCount, ElectronicArtsAudioCoreBuilder.Interleave(1, intro)))
            .Concat(ElectronicArtsAudioCoreBuilder.Block(true, loopSampleCount, ElectronicArtsAudioCoreBuilder.Interleave(1, loop)))
            .ToArray();
        var mp3 = ElectronicArtsAudioCore.GetMp3(data);
        Assert.AreEqual(introSampleCount, mp3.Header.LoopStartSample);
        Assert.AreEqual(2, mp3.Segments.Count);
        Assert.AreEqual(0, mp3.Segments[0].FirstSample);
        Assert.AreEqual(introSampleCount, mp3.Segments[0].SampleCount);
        Assert.AreEqual(introSampleCount, mp3.Segments[1].FirstSample);
        Assert.AreEqual(loopSampleCount, mp3.Segments[1].SampleCount);
        AssertGranulesAreKept(intro, ElectronicArtsAudioCoreBuilder.ReadMp3(mp3.Segments[0].Layers[0]));
        AssertGranulesAreKept(loop, ElectronicArtsAudioCoreBuilder.ReadMp3(mp3.Segments[1].Layers[0]));
    }

    [TestMethod]
    public void GetMp3SplitsMultichannelSoundsIntoLayers()
    {
        var random = new Random(7);
        var layers = new[] { 2, 2, 1 }.Select(channelCount => ElectronicArtsAudioCoreBuilder.CreateGranules(random, channelCount, 12, 100, 1200)).ToArray();
        var sampleCount = SampleCountFor(12);
        var firstHalf = ElectronicArtsAudioCoreBuilder.Interleave(0, layers.Select(layer => layer.Take(12).ToList()).ToArray());
        var secondHalf = ElectronicArtsAudioCoreBuilder.Interleave(0, layers.Select(layer => layer.Skip(12).ToList()).ToArray());
        var body = ElectronicArtsAudioCoreBuilder.Block(false, 6 * 1152, firstHalf)
            .Concat(ElectronicArtsAudioCoreBuilder.Block(true, sampleCount - 6 * 1152, secondHalf))
            .ToArray();
        var mp3 = ElectronicArtsAudioCore.GetMp3(ElectronicArtsAudioCoreBuilder.Header(5, 44100, ElectronicArtsAudioCoreStorage.Stream, sampleCount), body);
        Assert.AreEqual(1, mp3.Segments.Count);
        Assert.AreEqual(3, mp3.Segments[0].Layers.Count);
        for (var layer = 0; layer < 3; ++layer)
            AssertGranulesAreKept(layers[layer], ElectronicArtsAudioCoreBuilder.ReadMp3(mp3.Segments[0].Layers[layer]));
    }

    [TestMethod]
    public void GetMp3SplitsStreamedSoundsAtTheirLoopOffset()
    {
        var random = new Random(8);
        var introBlocks = new[] { ElectronicArtsAudioCoreBuilder.CreateGranules(random, 2, 3, 100, 900), ElectronicArtsAudioCoreBuilder.CreateGranules(random, 2, 3, 100, 900, false) };
        var loop = ElectronicArtsAudioCoreBuilder.CreateGranules(random, 2, 5, 100, 900);
        var introSampleCount = SampleCountFor(6);
        var loopSampleCount = SampleCountFor(5);
        var firstBlock = ElectronicArtsAudioCoreBuilder.Block(false, 1000, ElectronicArtsAudioCoreBuilder.Interleave(0, introBlocks[0]));
        var secondBlock = ElectronicArtsAudioCoreBuilder.Block(true, 1000, ElectronicArtsAudioCoreBuilder.Interleave(0, introBlocks[1]));
        var loopBlock = ElectronicArtsAudioCoreBuilder.Block(true, loopSampleCount, ElectronicArtsAudioCoreBuilder.Interleave(0, loop));
        var body = firstBlock.Concat(secondBlock).Concat(loopBlock).ToArray();
        var mp3 = ElectronicArtsAudioCore.GetMp3(ElectronicArtsAudioCoreBuilder.Header(2, 44100, ElectronicArtsAudioCoreStorage.Stream, introSampleCount + loopSampleCount, introSampleCount, firstBlock.Length + secondBlock.Length), body);
        Assert.AreEqual(2, mp3.Segments.Count);
        Assert.AreEqual(introSampleCount, mp3.Segments[1].FirstSample);
        AssertGranulesAreKept(introBlocks[0].Concat(introBlocks[1]).ToList(), ElectronicArtsAudioCoreBuilder.ReadMp3(mp3.Segments[0].Layers[0]));
        AssertGranulesAreKept(loop, ElectronicArtsAudioCoreBuilder.ReadMp3(mp3.Segments[1].Layers[0]));
    }

    [TestMethod]
    public void GetMp3StopsAtTheZerosThatPadTheLastBlock()
    {
        var granules = ElectronicArtsAudioCoreBuilder.CreateGranules(new Random(9), 2, 8, 100, 900);
        var sampleCount = SampleCountFor(8);
        var header = ElectronicArtsAudioCoreBuilder.Header(2, 44100, ElectronicArtsAudioCoreStorage.Memory, sampleCount);
        var payload = ElectronicArtsAudioCoreBuilder.Interleave(0, granules);
        var unpadded = ElectronicArtsAudioCore.GetMp3(header.Concat(ElectronicArtsAudioCoreBuilder.Block(true, sampleCount, payload)).ToArray());
        var padded = ElectronicArtsAudioCore.GetMp3(header.Concat(ElectronicArtsAudioCoreBuilder.Block(true, sampleCount, [.. payload, .. new byte[13]])).ToArray());
        CollectionAssert.AreEqual(unpadded.Segments[0].Layers[0].ToArray(), padded.Segments[0].Layers[0].ToArray());
    }

    [TestMethod]
    public void ReadReadsTheHeadersOfLoopingStreamedSounds()
    {
        var header = ElectronicArtsAudioCoreHeader.Read(ElectronicArtsAudioCoreBuilder.Header(16, 48000, ElectronicArtsAudioCoreStorage.Stream, 123456, 789, 4321));
        Assert.AreEqual(0, header.Version);
        Assert.AreEqual(ElectronicArtsAudioCoreCodec.ElectronicArtsLayer3Version1, header.Codec);
        Assert.AreEqual(16, header.ChannelCount);
        Assert.AreEqual(48000, header.SampleRate);
        Assert.AreEqual(ElectronicArtsAudioCoreStorage.Stream, header.Storage);
        Assert.AreEqual(123456, header.SampleCount);
        Assert.AreEqual(789, header.LoopStartSample);
        Assert.AreEqual(4321, header.LoopOffset);
        Assert.AreEqual(16, header.Size);
    }

    [TestMethod]
    public void ReadRejectsHeadersCutShort()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => ElectronicArtsAudioCoreHeader.Read(new byte[6]));
        Assert.ThrowsExactly<InvalidDataException>(() => ElectronicArtsAudioCoreHeader.Read(ElectronicArtsAudioCoreBuilder.Header(2, 44100, ElectronicArtsAudioCoreStorage.Memory, 1000, 10).AsSpan(0, 8)));
    }
}