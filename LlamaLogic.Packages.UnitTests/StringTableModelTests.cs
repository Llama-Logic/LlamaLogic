namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class StringTableModelTests :
    TestPackageTests
{
    static byte[] BuildStringTable(params byte[][] values)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("STBL"u8);
        writer.Write((ushort)5);
        writer.Write((byte)0);
        writer.Write((ulong)values.Length);
        writer.Write((ushort)0);
        writer.Write((uint)values.Sum(value => value.Length + 1));
        for (var i = 0; i < values.Length; ++i)
        {
            writer.Write(0xd4fc0137U + (uint)i);
            writer.Write((byte)0);
            writer.Write((ushort)values[i].Length);
            writer.Write(values[i]);
        }
        writer.Flush();
        return stream.ToArray();
    }

    [TestMethod]
    public void DecodeReadsWellFormedTables()
    {
        var stringTable = StringTableModel.Decode(BuildStringTable("first"u8.ToArray(), "second"u8.ToArray()));
        Assert.AreEqual(2, stringTable.Count);
        Assert.AreEqual("second", stringTable[0xd4fc0138]);
    }

    [TestMethod]
    public void DecodeRejectsEntryCountsBeyondTheData()
    {
        var data = BuildStringTable("first"u8.ToArray(), "second"u8.ToArray());
        BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(7), 3);
        Assert.ThrowsExactly<InvalidDataException>(() => StringTableModel.Decode(data));
    }

    [TestMethod]
    public void DecodeRejectsInvalidUtf8()
    {
        var data = BuildStringTable("first"u8.ToArray(), [0x73, 0xff, 0x63]);
        Assert.ThrowsExactly<InvalidDataException>(() => StringTableModel.Decode(data));
    }

    [TestMethod]
    public void DecodeRejectsMismatchedStringLength()
    {
        var data = BuildStringTable("first"u8.ToArray(), "second"u8.ToArray());
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(17), BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(17)) + 1);
        Assert.ThrowsExactly<InvalidDataException>(() => StringTableModel.Decode(data));
    }

    [TestMethod]
    public void DecodeRejectsOtherData() =>
        Assert.ThrowsExactly<InvalidDataException>(() => StringTableModel.Decode("DATA and then some more bytes"u8.ToArray()));

    [TestMethod]
    public void DecodeRejectsTrailingBytes() =>
        Assert.ThrowsExactly<InvalidDataException>(() => StringTableModel.Decode((byte[])[.. BuildStringTable("first"u8.ToArray(), "second"u8.ToArray()), 0]));

    [TestMethod]
    [DataRow(0)]
    [DataRow(3)]
    [DataRow(20)]
    [DataRow(25)]
    [DataRow(45)]
    public void DecodeRejectsTruncatedData(int length)
    {
        var data = BuildStringTable("first"u8.ToArray(), "second"u8.ToArray());
        Assert.ThrowsExactly<InvalidDataException>(() => StringTableModel.Decode(data.AsMemory(0, length)));
    }

    [TestMethod]
    [DataRow((ushort)4)]
    [DataRow((ushort)6)]
    public void DecodeRejectsUnsupportedVersions(ushort version)
    {
        var data = BuildStringTable("first"u8.ToArray(), "second"u8.ToArray());
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(4), version);
        Assert.ThrowsExactly<InvalidDataException>(() => StringTableModel.Decode(data));
    }

    [TestMethod]
    public void FirstDuplicateKeyHashWins()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        var values = new[] { "first"u8.ToArray(), "second"u8.ToArray() };
        writer.Write("STBL"u8);
        writer.Write((ushort)5);
        writer.Write((byte)0);
        writer.Write((ulong)values.Length);
        writer.Write((ushort)0);
        writer.Write((uint)values.Sum(value => value.Length + 1));
        foreach (var value in values)
        {
            writer.Write(0xd4fc0137U);
            writer.Write((byte)0);
            writer.Write((ushort)value.Length);
            writer.Write(value);
        }
        writer.Flush();
        var stringTable = StringTableModel.Decode(stream.ToArray());
        Assert.AreEqual(1, stringTable.Count);
        Assert.AreEqual("first", stringTable[0xd4fc0137]);
    }

    [TestMethod]
    public void StringTableManipulation()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        var stringTable = dbpf.GetStringTable("220557da:80000000:002100b7f5cfd646");
        Assert.AreEqual("TSHI", stringTable[0xd4fc0137]);
        Assert.AreEqual("Disappointed in the cooking of the duck meat.", stringTable[0xdb5ac988]);
        var howBadKey = stringTable.AddNew("How bad could it really be?");
        dbpf.Set("220557da:80000000:002100b7f5cfd646", stringTable);
        stringTable = dbpf.GetStringTable("220557da:80000000:002100b7f5cfd646");
        Assert.AreEqual("How bad could it really be?", stringTable[howBadKey]);
        Assert.ThrowsExactly<ArgumentException>(() => stringTable.AddNew("How bad could it really be?"));
    }

    [TestMethod]
    public void StringTableKeyUtilities()
    {
        var englishKey = ResourceKey.Parse("220557da:80000000:002100b7f5cfd646");
        var english = englishKey.GetStringTableLocale();
        Assert.AreEqual("English (United States)", english.EnglishName);
        var polishKey = englishKey.GetResourceKeyForLocale(new CultureInfo("pl"));
        Assert.AreEqual(0x0f, (byte)((polishKey.FullInstance & 0xFF00000000000000) >> 56));
        var polish = polishKey.GetStringTableLocale();
        Assert.AreEqual("Polish", polish.EnglishName);
    }
}
