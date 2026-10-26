namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class AudioConfigurationModelTests
{
    static byte[] Configuration(ushort version, Action<BinaryWriter>? afterProperties = null, byte flag = 1)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(version);
        writer.Write(0x7709b3ab0ac9a926UL);
        writer.Write(2U);
        writer.Write(0x1111111111111111UL);
        writer.Write(0x2222222222222222UL);
        writer.Write(1U);
        writer.Write(1U);
        writer.Write(0xaaaaaaaaU);
        writer.Write((ushort)AudioConfigurationPropertyType.Float);
        writer.Write(0.5F);
        writer.Write(1U);
        writer.Write(0xbbbbbbbbU);
        writer.Write(2U);
        writer.Write(1.25F);
        writer.Write(-2.5F);
        foreach (var value in new[] { 1.5F, 2.5F, 3.5F, 4.5F })
        {
            writer.Write(1U);
            writer.Write(0x3333333333333333UL);
            writer.Write(value);
        }
        writer.Write(1U);
        writer.Write(0x4444444444444444UL);
        writer.Write(flag);
        foreach (var value in new[] { 5.5F, 6.5F })
        {
            writer.Write(1U);
            writer.Write(0xccccccccU);
            writer.Write(value);
        }
        writer.Write(-3.0F);
        writer.Write(12.0F);
        writer.Write((byte)2);
        writer.Write((byte)0);
        writer.Write((byte)1);
        writer.Write((byte)0);
        writer.Write(7U);
        writer.Write(0x247071c8U);
        writer.Write((ushort)AudioConfigurationPropertyType.String);
        writer.Write(11U);
        writer.Write("\n          "u8);
        writer.Write(0xc67428e6U);
        writer.Write((ushort)AudioConfigurationPropertyType.UInt32);
        writer.Write(100U);
        writer.Write(0xddddddddU);
        writer.Write((ushort)AudioConfigurationPropertyType.UInt64);
        writer.Write(0x5555555555555555UL);
        writer.Write(0x8d1c9748U);
        writer.Write((ushort)AudioConfigurationPropertyType.Boolean);
        writer.Write(flag);
        writer.Write(0xeeeeeeeeU);
        writer.Write((ushort)AudioConfigurationPropertyType.Curve);
        writer.Write(1U);
        foreach (var value in new[] { 0.1F, 0.2F, 0.3F, 0.4F, 0.5F })
            writer.Write(value);
        writer.Write(0xffffffffU);
        writer.Write((ushort)AudioConfigurationPropertyType.LongSet);
        writer.Write(2U);
        writer.Write(0x6666666666666666UL);
        writer.Write(0x7777777777777777UL);
        writer.Write(0x12345678U);
        writer.Write((ushort)AudioConfigurationPropertyType.Float);
        writer.Write(float.PositiveInfinity);
        afterProperties?.Invoke(writer);
        if (version >= 2)
        {
            writer.Write(1U);
            writer.Write(0x1111111111111111UL);
            writer.Write(3U);
        }
        if (version >= 3)
        {
            writer.Write(1U);
            writer.Write(0x8888888888888888UL);
            writer.Write(0x100U);
        }
        for (var map = 0; map < (version >= 4 ? version : 0); ++map)
        {
            writer.Write((uint)map);
            for (var item = 0; item < map; ++item)
            {
                writer.Write(0x9999999999999999UL + (ulong)item);
                writer.Write((byte)(item & 1));
            }
        }
        return stream.ToArray();
    }

    [TestMethod]
    public void DecodeReadsEveryPart()
    {
        var model = AudioConfigurationModel.Decode(Configuration(7));
        Assert.AreEqual((ushort)7, model.Version);
        Assert.AreEqual(0x7709b3ab0ac9a926UL, model.Parent);
        CollectionAssert.AreEqual(new[] { 0x1111111111111111UL, 0x2222222222222222UL }, model.Samples.ToArray());
        Assert.AreEqual(1, model.GlobalConditions.Count);
        Assert.AreEqual(0xaaaaaaaaU, model.GlobalConditions[0][0].Key);
        Assert.AreEqual(0.5F, model.GlobalConditions[0][0].Value);
        Assert.AreEqual(0xbbbbbbbbU, model.InteractiveMusic[0].Key);
        CollectionAssert.AreEqual(new[] { 1.25F, -2.5F }, model.InteractiveMusic[0].Values.ToArray());
        Assert.AreEqual(new KeyValuePair<ulong, float>(0x3333333333333333UL, 1.5F), model.MoodPitchChanges[0]);
        Assert.AreEqual(2.5F, model.MoodFrequencyPeriods[0].Value);
        Assert.AreEqual(3.5F, model.MoodFrequencyOffsets[0].Value);
        Assert.AreEqual(4.5F, model.MoodAttenuations[0].Value);
        Assert.AreEqual(new KeyValuePair<ulong, bool>(0x4444444444444444UL, true), model.MoodPlays[0]);
        Assert.AreEqual(5.5F, model.VuMeterFacialOverlayModifierOffsets[0].Value);
        Assert.AreEqual(6.5F, model.VuMeterFacialOverlayModifierScales[0].Value);
        Assert.AreEqual(-3.0F, model.Gain);
        Assert.AreEqual(12.0F, model.Attenuation);
        Assert.AreEqual((byte)2, model.AggregateGain);
        Assert.AreEqual((byte)0, model.PolyphonyDiffersFromBase1);
        Assert.AreEqual((byte)1, model.PolyphonyDiffersFromBase2);
        Assert.AreEqual((byte)0, model.PolyphonyDiffersFromBase3);
        CollectionAssert.AreEqual(new[] { AudioConfigurationPropertyType.String, AudioConfigurationPropertyType.UInt32, AudioConfigurationPropertyType.UInt64, AudioConfigurationPropertyType.Boolean, AudioConfigurationPropertyType.Curve, AudioConfigurationPropertyType.LongSet, AudioConfigurationPropertyType.Float }, model.Properties.Select(property => property.Type).ToArray());
        Assert.AreEqual("\n          ", model.Properties[0].Value);
        Assert.AreEqual(100U, model.Properties[1].Value);
        Assert.AreEqual(0x5555555555555555UL, model.Properties[2].Value);
        Assert.AreEqual(true, model.Properties[3].Value);
        var point = ((Collection<AudioConfigurationCurvePoint>)model.Properties[4].Value)[0];
        Assert.AreEqual((0.1F, 0.2F, 0.3F, 0.4F, 0.5F), (point.InputValue, point.OutputValue, point.PreviousInputValue, point.PreviousOutputValue, point.DeltaRatio));
        CollectionAssert.AreEqual(new[] { 0x6666666666666666UL, 0x7777777777777777UL }, ((Collection<ulong>)model.Properties[5].Value).ToArray());
        Assert.AreEqual(float.PositiveInfinity, model.Properties[6].Value);
        Assert.AreEqual(new KeyValuePair<ulong, uint>(0x1111111111111111UL, 3U), model.SampleWeights[0]);
        Assert.AreEqual(new KeyValuePair<ulong, uint>(0x8888888888888888UL, 0x100U), model.UnidentifiedMap[0]);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6 }, model.UnidentifiedFlagMaps.Select(map => map.Count).ToArray());
        Assert.AreEqual(new KeyValuePair<ulong, bool>(0x999999999999999aUL, true), model.UnidentifiedFlagMaps[6][1]);
    }

    [TestMethod]
    public void DecodeRejectsDataCutShort()
    {
        var data = Configuration(3);
        Assert.ThrowsExactly<InvalidDataException>(() => AudioConfigurationModel.Decode(data.AsMemory(0, data.Length - 1)));
    }

    [TestMethod]
    public void DecodeRejectsFlagsOtherThanZeroOrOne() =>
        Assert.ThrowsExactly<InvalidDataException>(() => AudioConfigurationModel.Decode(Configuration(3, flag: 2)));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => AudioConfigurationModel.Decode(Configuration(3).Append((byte)0).ToArray()));

    [TestMethod]
    public void DecodeRejectsUnknownPropertyTypes()
    {
        var data = Configuration(1, writer =>
        {
            writer.Seek(-6, SeekOrigin.Current);
            writer.Write((ushort)9);
            writer.Seek(4, SeekOrigin.Current);
        });
        Assert.ThrowsExactly<InvalidDataException>(() => AudioConfigurationModel.Decode(data));
    }

    [TestMethod]
    [DataRow((ushort)0)]
    [DataRow((ushort)8)]
    public void DecodeRejectsUnsupportedVersions(ushort version)
    {
        var data = Configuration(3);
        BinaryPrimitives.WriteUInt16LittleEndian(data, version);
        Assert.ThrowsExactly<InvalidDataException>(() => AudioConfigurationModel.Decode(data));
    }

    [TestMethod]
    [DataRow((ushort)1)]
    [DataRow((ushort)2)]
    [DataRow((ushort)3)]
    [DataRow((ushort)4)]
    [DataRow((ushort)7)]
    public void EncodeReproducesWhatWasDecoded(ushort version)
    {
        var data = Configuration(version);
        CollectionAssert.AreEqual(data, AudioConfigurationModel.Decode(data).Encode().ToArray());
    }

    [TestMethod]
    public void EncodeRejectsTheWrongNumberOfUnidentifiedFlagMaps()
    {
        var model = AudioConfigurationModel.Decode(Configuration(4));
        model.Version = 5;
        Assert.ThrowsExactly<InvalidOperationException>(() => model.Encode());
    }

    [TestMethod]
    public void EncodeWritesChangedSamples()
    {
        var model = AudioConfigurationModel.Decode(Configuration(3));
        model.Samples.Clear();
        model.Samples.Add(0xfeedfacecafebeefUL);
        var reencoded = AudioConfigurationModel.Decode(model.Encode());
        CollectionAssert.AreEqual(new[] { 0xfeedfacecafebeefUL }, reencoded.Samples.ToArray());
        Assert.AreEqual(model.Parent, reencoded.Parent);
        Assert.AreEqual(model.Properties.Count, reencoded.Properties.Count);
    }

    [TestMethod]
    public void PropertiesRejectValuesOfOtherTypes()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new AudioConfigurationProperty(1, 1));
        var property = new AudioConfigurationProperty(1, 1U);
        Assert.ThrowsExactly<ArgumentException>(() => property.Value = 1.0);
        Assert.AreEqual(AudioConfigurationPropertyType.UInt32, property.Type);
    }
}