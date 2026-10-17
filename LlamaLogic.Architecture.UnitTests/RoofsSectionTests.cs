namespace LlamaLogic.Architecture.UnitTests;

[TestClass]
public class RoofsSectionTests
{
    static readonly ResourceKey roofKey = new((ResourceType)0x91EDBD3E, 0, 0x144D);
    static readonly ResourceKey trimKey = new((ResourceType)0xB0311D0F, 0, 0x22);
    static readonly ResourceKey patternKey = new((ResourceType)0xF1EDBD86, 0, 0x33);
    static readonly ResourceKey wallKey = new((ResourceType)0xD5F0F921, 0, 0x44);

    static byte[] Record(uint version, uint innerVersion = 3, byte firstSide = 1, byte header = 1)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(version);
        if (version < 8)
            WriteKey(writer, default);
        WriteKey(writer, default);
        writer.Write(0.5F);
        writer.Write(0U);
        writer.Write(header);
        writer.Write(1U);
        writer.Write(7U);
        if (version < 10)
            writer.Write(new byte[] { 2, 3, 0xFF, 6, 8, 0xFF });
        WriteKey(writer, roofKey);
        WriteKey(writer, trimKey);
        WriteKey(writer, trimKey);
        if (version > 5)
            WriteKey(writer, patternKey);
        if (version > 10)
            WriteKey(writer, wallKey);
        if (version < 10)
        {
            writer.Write((byte)6);
            writer.Write(firstSide);
            writer.Write(0.25F);
            writer.Write(0.55F);
            if (version > 8)
                writer.Write(6.25F);
        }
        else
        {
            writer.Write(innerVersion);
            writer.Write(0x00020302U);
            writer.Write(0x05020806U);
            writer.Write(0x00020502U);
            writer.Write(0x00020806U);
            writer.Write(0x00020503U);
            writer.Write(0x00020705U);
            writer.Write((byte)2);
            writer.Write(0.55F);
            writer.Write(0.25F);
            if (innerVersion > 2)
            {
                writer.Write(0.66F);
                writer.Write(0.33F);
                writer.Write(-1F);
                writer.Write(1F);
            }
            writer.Write(6.25F);
            if (innerVersion < 2)
                writer.Write(firstSide);
            else
                writer.Write(new byte[] { firstSide, 1, 2, 0 });
            writer.Write(1U);
        }
        writer.Flush();
        return stream.ToArray();
    }

    static void WriteKey(BinaryWriter writer, ResourceKey key)
    {
        writer.Write((uint)key.Type);
        writer.Write(key.Group);
        writer.Write(key.FullInstance);
    }

    [TestMethod]
    [DataRow(5U, 3U)]
    [DataRow(6U, 3U)]
    [DataRow(8U, 3U)]
    [DataRow(9U, 3U)]
    [DataRow(10U, 1U)]
    [DataRow(11U, 2U)]
    [DataRow(12U, 2U)]
    [DataRow(13U, 3U)]
    public void DecodeAndEncodeRoundTrip(uint version, uint innerVersion)
    {
        var data = Record(version, innerVersion);
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Roofs, data));
        Assert.AreEqual(version, architecture.Roofs!.Version);
        CollectionAssert.AreEqual(data, TestBundles.Encode(architecture, LotArchitectureSectionType.Roofs));
    }

    [TestMethod]
    public void DecodeKeepsUnsupportedInnerVersionsUndecoded() =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.Roofs, Record(13, 4), architecture => architecture.Roofs);

    [TestMethod]
    [DataRow(4U)]
    [DataRow(14U)]
    public void DecodeKeepsUnsupportedVersionsUndecoded(uint version) =>
        TestBundles.AssertUndecoded(LotArchitectureSectionType.Roofs, [.. BitConverter.GetBytes(version), .. Record(13).Skip(4)], architecture => architecture.Roofs);

    [TestMethod]
    public void DecodeReadsCurrentLayout()
    {
        var section = TestBundles.Decode((LotArchitectureSectionType.Roofs, Record(13))).Roofs!;
        Assert.AreEqual(0.5F, section.Unknown1);
        Assert.IsTrue(section.Unknown3);
        var roof = section.Roofs.Single();
        Assert.AreEqual(7U, roof.Id);
        Assert.AreEqual(roofKey, roof.RoofKey);
        Assert.AreEqual(trimKey, roof.SecondTrimKey);
        Assert.AreEqual(patternKey, roof.PatternKey);
        Assert.AreEqual(wallKey, roof.WallKey);
        Assert.AreEqual(3U, roof.InnerVersion);
        Assert.AreEqual(new RoofPoint { X = 2, Z = 3, Level = 2 }, roof.BoundsMinimum);
        Assert.AreEqual(new RoofPoint { X = 6, Z = 8, Level = 2, Reserved = 5 }, roof.BoundsMaximum);
        Assert.AreEqual(new RoofPoint { X = 5, Z = 7, Level = 2 }, roof.Corner4);
        Assert.AreEqual((byte)2, roof.Direction);
        Assert.AreEqual(0.55F, roof.Pitch);
        Assert.AreEqual(0.25F, roof.Curvature);
        Assert.AreEqual(-1F, roof.Unknown3);
        Assert.AreEqual(6.25F, roof.BaseElevation);
        Assert.AreEqual((byte)2, roof.Unknown7);
        Assert.AreEqual(1U, roof.Flags);
    }

    [TestMethod]
    public void DecodeReadsLegacyLayout()
    {
        var section = TestBundles.Decode((LotArchitectureSectionType.Roofs, Record(9))).Roofs!;
        var roof = section.Roofs.Single();
        Assert.AreEqual(new RoofPoint { X = 2, Z = 3, Level = -1 }, roof.BoundsMinimum);
        Assert.AreEqual(new RoofPoint { X = 6, Z = 8, Level = -1 }, roof.BoundsMaximum);
        Assert.AreEqual((byte)6, roof.Direction);
        Assert.AreEqual((byte)1, roof.Unknown8);
        Assert.AreEqual(0.25F, roof.Curvature);
        Assert.AreEqual(0.55F, roof.Pitch);
        Assert.AreEqual(6.25F, roof.BaseElevation);
    }

    [TestMethod]
    public void DecodeRejectsNonBooleanHeaderValue() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Roofs, Record(13, header: 2))));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => TestBundles.Decode((LotArchitectureSectionType.Roofs, [.. Record(13), 0])));

    [TestMethod]
    public void EncodeRejectsRoofFieldsOlderVersionsCannotStore()
    {
        var changes = new Action<Roof>[]
        {
            roof => roof.Flags = 1,
            roof => roof.Corner1 = new RoofPoint { X = 1 },
            roof => roof.BoundsMaximum = new RoofPoint { Reserved = 1 },
            roof => roof.InnerVersion = 2,
            roof => roof.Unknown1 = 0.5F,
            roof => roof.WallKey = wallKey,
            roof => roof.PatternKey = patternKey
        };
        foreach (var change in changes)
        {
            var roof = new Roof();
            change(roof);
            var section = new RoofsSection { Version = 5 };
            section.Roofs.Add(roof);
            Assert.ThrowsExactly<InvalidOperationException>(() => new LotArchitecture { Roofs = section }.ToRecordBundle());
        }
    }

    [TestMethod]
    [DataRow(8U, 3U)]
    [DataRow(13U, 1U)]
    public void EncodeRejectsUnequalSidesInSingleSideLayouts(uint version, uint innerVersion)
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Roofs, Record(version, innerVersion)));
        architecture.Roofs!.Roofs[0].Unknown6 = 2;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeRejectsUnknownFloatsInnerVersion2CannotStore()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Roofs, Record(13)));
        var roof = architecture.Roofs!.Roofs[0];
        roof.InnerVersion = 2;
        roof.Unknown1 = 0.5F;
        Assert.ThrowsExactly<InvalidOperationException>(() => architecture.ToRecordBundle());
    }

    [TestMethod]
    public void EncodeWritesAnEarlierInnerVersionWhenTheFloatsHoldDefaults()
    {
        var architecture = TestBundles.Decode((LotArchitectureSectionType.Roofs, Record(13)));
        architecture.Roofs!.Roofs[0].InnerVersion = 2;
        CollectionAssert.AreEqual(Record(13, 2), TestBundles.Encode(architecture, LotArchitectureSectionType.Roofs));
    }

    [TestMethod]
    public void EncodeWritesDefaultRoofInOlderVersions()
    {
        var section = new RoofsSection { Version = 9 };
        section.Roofs.Add(new Roof { Id = 3 });
        var encoded = TestBundles.Encode(new LotArchitecture { Roofs = section }, LotArchitectureSectionType.Roofs);
        var roof = TestBundles.Decode((LotArchitectureSectionType.Roofs, encoded)).Roofs!.Roofs.Single();
        Assert.AreEqual(3U, roof.Id);
        Assert.AreEqual(3U, roof.InnerVersion);
        Assert.AreEqual(0.66F, roof.Unknown1);
    }

    [TestMethod]
    public void InnerVersionRejectsUnsupportedValues() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Roof { InnerVersion = 4 });

    [TestMethod]
    public void VersionRejectsUnsupportedValues() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RoofsSection { Version = 14 });
}
