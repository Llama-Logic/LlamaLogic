namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class DataBasePackedFileTests :
    TestPackageTests
{
    #region MonitoredDisposeFileStream

#pragma warning disable CS0618

    class MonitoredDisposeFileStream :
        FileStream
    {
        public MonitoredDisposeFileStream(SafeFileHandle handle, FileAccess access) :
            base(handle, access)
        {
        }

        public MonitoredDisposeFileStream(IntPtr handle, FileAccess access) :
            base(handle, access)
        {
        }

        public MonitoredDisposeFileStream(string path, FileMode mode) :
            base(path, mode)
        {
        }

        public MonitoredDisposeFileStream(SafeFileHandle handle, FileAccess access, int bufferSize) : base(handle, access, bufferSize)
        {
        }

        public MonitoredDisposeFileStream(IntPtr handle, FileAccess access, bool ownsHandle) :
            base(handle, access, ownsHandle)
        {
        }

        public MonitoredDisposeFileStream(string path, FileMode mode, FileAccess access) : base(path, mode, access)
        {
        }

        public MonitoredDisposeFileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync) : base(handle, access, bufferSize, isAsync)
        {
        }

        public MonitoredDisposeFileStream(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize) :
            base(handle, access, ownsHandle, bufferSize)
        {
        }

        public MonitoredDisposeFileStream(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode, access, share)
        {
        }

        public MonitoredDisposeFileStream(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize, bool isAsync) :
            base(handle, access, ownsHandle, bufferSize, isAsync)
        {
        }

        public MonitoredDisposeFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize) :
            base(path, mode, access, share, bufferSize)
        {
        }

        public MonitoredDisposeFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync) :
            base(path, mode, access, share, bufferSize, useAsync)
        {
        }

        public MonitoredDisposeFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options) :
            base(path, mode, access, share, bufferSize, options)
        {
        }

        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

#pragma warning restore CS0618

    #endregion MonitoredDisposeFileStream

    static byte[] EncodeTiff(int width, int height)
    {
        using var image = new Image<L8>(width, height);
        using var stream = new MemoryStream();
        image.SaveAsTiff(stream);
        return stream.ToArray();
    }

    static byte[] EncodeTranslucentJpeg(byte[] alphaData, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var jpegStream = new MemoryStream();
        image.SaveAsJpeg(jpegStream);
        var jpegData = jpegStream.ToArray();
        var insertionIndex = 4 + ((jpegData[4] << 8) | jpegData[5]);
        var sectionSize = alphaData.Length + 10;
        using var stream = new MemoryStream();
        stream.Write(jpegData, 0, insertionIndex);
        stream.Write([0xff, 0xe0, (byte)(sectionSize >> 8), (byte)sectionSize]);
        stream.Write("ALFA"u8);
        stream.Write([(byte)(alphaData.Length >> 24), (byte)(alphaData.Length >> 16), (byte)(alphaData.Length >> 8), (byte)alphaData.Length]);
        stream.Write(alphaData);
        stream.Write(jpegData, insertionIndex, jpegData.Length - insertionIndex);
        return stream.ToArray();
    }

    [TestMethod]
    public void CopyToRoundTripsEntriesWithoutExtendedCompression()
    {
        var key = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 1);
        var content = "content"u8.ToArray();
        using var source = new DataBasePackedFile(new MemoryStream(RawPackageBuilder.Build([(key, content, false)])));
        var copy = new MemoryStream();
        source.CopyTo(copy);
        using var reopened = new DataBasePackedFile(copy);
        CollectionAssert.AreEqual(content, reopened.Get(key).ToArray());
    }

    [TestMethod]
    public void CreatePackage()
    {
        using var dbpf = new DataBasePackedFile();
    }

    [TestMethod]
    public void FirstDuplicateKeyWins()
    {
        var key = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 1);
        var first = "first"u8.ToArray();
        using var dbpf = new DataBasePackedFile(new MemoryStream(RawPackageBuilder.Build([(key, first, true), (key, "second"u8.ToArray(), true)])));
        Assert.AreEqual(1, dbpf.Count);
        CollectionAssert.AreEqual(first, dbpf.Get(key).ToArray());
    }

    [TestMethod]
    public void GetNamesToleratesUndefinedResourceType()
    {
        using var dbpf = new DataBasePackedFile();
        dbpf.Set(new ResourceKey((ResourceType)0xb6c8b6a0, 0, 1), "DDS "u8.ToArray(), CompressionMode.ForceZLib);
        Assert.AreEqual(0, dbpf.GetNames().Count);
    }

    [TestMethod]
    public void GetNonsenseRelationshipBitTuning()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        dbpf.GetXml("0904df10:00000000:00000000d1d56a53");
    }

    [TestMethod]
    public void GetNonsenseRelationshipBitTuningName()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        Assert.AreEqual("LlamaLogicPackageTests:NonsenseRelationshipBit", dbpf.GetNameByKey("0904df10:00000000:00000000d1d56a53"));
    }

    [TestMethod]
    public void GetTranslucentJpegAsPngRejectsOtherAlphaFormats()
    {
        var key = new ResourceKey(ResourceType.CASPartThumbnail, 0, 1);
        using var dbpf = new DataBasePackedFile();
        dbpf.Set(key, EncodeTranslucentJpeg(EncodeTiff(4, 4), 4, 4), CompressionMode.ForceOff);
        Assert.Throws<ImageFormatException>(() => dbpf.GetTranslucentJpegAsPng(key));
    }

    [TestMethod]
    public void Indexer()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        var tuningXmlObj = dbpf["0904df10:00000000:00000000d1d56a53"];
        Assert.IsInstanceOfType(tuningXmlObj, typeof(string));
        var tuningXDocument = XDocument.Parse((string)tuningXmlObj);
        Assert.AreEqual("relationships.relationship_bit", tuningXDocument.Root!.Attribute("m")!.Value);
    }

    [TestMethod]
    public void IndexerToleratesUndefinedResourceType()
    {
        var key = new ResourceKey((ResourceType)0xb6c8b6a0, 0, 1);
        var content = "DDS "u8.ToArray();
        using var dbpf = new DataBasePackedFile();
        dbpf.Set(key, content, CompressionMode.ForceZLib);
        CollectionAssert.AreEqual(content, ((ReadOnlyMemory<byte>)dbpf[key]!).ToArray());
    }

    [TestMethod]
    public void OpenTestPackageViaPath()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        Assert.AreEqual(20, dbpf.Count);
    }

    [TestMethod]
    public async Task OpenTestPackageViaPathAsync()
    {
        await using var dbpf = await DataBasePackedFile.FromPathAsync(tempPackagePath);
        Assert.AreEqual(20, dbpf.Count);
    }

    [TestMethod]
    public void OpenTestPackageViaStream()
    {
        var stream = new MonitoredDisposeFileStream(tempPackagePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.RandomAccess);
        using (var dbpf = new DataBasePackedFile(stream))
            Assert.AreEqual(20, dbpf.Count);
        Assert.IsTrue(stream.IsDisposed);
    }

    [TestMethod]
    public async Task OpenTestPackageViaStreamAsync()
    {
        var stream = new MonitoredDisposeFileStream(tempPackagePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.RandomAccess);
        await using (var dbpf = await DataBasePackedFile.FromStreamAsync(stream))
            Assert.AreEqual(20, dbpf.Count);
        Assert.IsTrue(stream.IsDisposed);
    }

    [TestMethod]
    public void SaveAndCopyToAgreeOnDeletedRecords()
    {
        var live = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 1);
        var deleted = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 2);
        byte[] packageBytes;
        using (var source = new DataBasePackedFile())
        {
            source.Set(live, "live"u8.ToArray());
            source.Set(deleted, "deleted"u8.ToArray(), CompressionMode.SetDeletedFlag);
            using var packageStream = new MemoryStream();
            source.CopyTo(packageStream);
            packageBytes = packageStream.ToArray();
        }
        var copiedStream = new MemoryStream();
        using (var dbpf = new DataBasePackedFile(new MemoryStream(packageBytes)))
            dbpf.CopyTo(copiedStream);
        var savedStream = new MemoryStream();
        savedStream.Write(packageBytes);
        using (var dbpf = new DataBasePackedFile(savedStream))
            dbpf.Save();
        using var copied = new DataBasePackedFile(new MemoryStream(copiedStream.ToArray()));
        using var saved = new DataBasePackedFile(new MemoryStream(savedStream.ToArray()));
        CollectionAssert.AreEquivalent(copied.Keys.ToArray(), saved.Keys.ToArray());
        foreach (var key in copied.Keys)
            CollectionAssert.AreEqual(copied.GetRaw(key, true).ToArray(), saved.GetRaw(key, true).ToArray(), key.ToString());
    }

    [TestMethod]
    public void SaveKeepsDeletedRecords()
    {
        var key = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 1);
        var content = "deleted"u8.ToArray();
        byte[] packageBytes;
        using (var source = new DataBasePackedFile())
        {
            source.Set(key, content, CompressionMode.SetDeletedFlag);
            using var packageStream = new MemoryStream();
            source.CopyTo(packageStream);
            packageBytes = packageStream.ToArray();
        }
        var savedStream = new MemoryStream();
        savedStream.Write(packageBytes);
        using (var dbpf = new DataBasePackedFile(savedStream))
            dbpf.Save();
        using var saved = new DataBasePackedFile(new MemoryStream(savedStream.ToArray()));
        Assert.AreEqual(CompressionMode.SetDeletedFlag, saved.GetExplicitCompressionMode(key));
        CollectionAssert.AreEqual(content, saved.GetRaw(key, true).ToArray());
    }

    [TestMethod]
    [DataRow(CompressionMode.CallerSuppliedInternal)]
    [DataRow(CompressionMode.CallerSuppliedStreamable)]
    [DataRow(CompressionMode.CallerSuppliedZLib)]
    public async Task SetAsyncRejectsCallerSuppliedCompression(CompressionMode compressionMode)
    {
        var key = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 1);
        await using var dbpf = new DataBasePackedFile();
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => dbpf.SetAsync(key, "content"u8.ToArray(), compressionMode));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => dbpf.SetAsync(key, "content", compressionMode));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => dbpf.SetAsync(key, new XDocument(new XElement("I")), compressionMode));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => dbpf.SetAsync(key, new StringTableModel(), compressionMode));
        Assert.AreEqual(0, dbpf.Count);
    }

    [TestMethod]
    public void SetNonsenseRelationshipBitTuning()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        dbpf.SetXml
        (
            "0904df10:00000000:00000000d1d56a53",
            """
            <I c="RelationshipBit" i="relbit" m="relationships.relationship_bit" n="LlamaLogicPackageTests:NonsenseRelationshipBit" s="3520424531">
              <T n="bit_description">0xDB5AC988<!--Disappointed in the cooking of the duck meat.--></T>
              <L n="collection_ids">
                <E>Romance</E>
                <E>Engaged</E>
              </L>
              <E n="directionality">UNIDIRECTIONAL</E>
              <T n="display_name">0xD4FC0137<!--TSHI--></T>
              <T n="display_priority">2</T>
              <E n="group_id">Romantic</E>
              <T p="InEP04\UI\Icons\Whims\whim_PetScratchingPost.png" n="icon">2F7D0004:00000000:297CB1FD39511EF4<!--297CB1FD39511EF4--></T>
              <V n="sim_profile_background" t="enabled">
                <T p="InEP05\UI\Icons\Buffs\Buff_LotteryTicket.png" n="enabled">2F7D0004:00000000:DE8E092788735948<!--DE8E092788735948--></T>
              </V>
              <V n="small_icon" t="enabled">
                <T p="InEP05\UI\Icons\Events\Holidays\holidays_HarvestFest.png" n="enabled">2F7D0004:00000000:573597DDCDBAD0EA<!--573597DDCDBAD0EA--></T>
              </V>
              <T n="visible">False</T>
            </I>
            """
        );
        Assert.AreEqual(2, dbpf.GetXml("0904df10:00000000:00000000d1d56a53").XPathSelectElements("/I[@c = 'RelationshipBit' and @i = 'relbit' and @m = 'relationships.relationship_bit' and @n = 'LlamaLogicPackageTests:NonsenseRelationshipBit' and @s = '3520424531']/L[@n = 'collection_ids']/E").Count());
    }

    [TestMethod]
    public void SetPngAsDdsRejectsOtherImageFormats()
    {
        using var dbpf = new DataBasePackedFile();
        Assert.Throws<ImageFormatException>(() => dbpf.SetPngAsDds(new ResourceKey(ResourceType.DSTImage, 0, 1), EncodeTiff(4, 4)));
    }

    [TestMethod]
    public void SetPngAsTranslucentJpegRejectsOtherImageFormats()
    {
        using var dbpf = new DataBasePackedFile();
        Assert.Throws<ImageFormatException>(() => dbpf.SetPngAsTranslucentJpeg(new ResourceKey(ResourceType.CASPartThumbnail, 0, 1), EncodeTiff(4, 4)));
    }

    [TestMethod]
    [DataRow(CompressionMode.CallerSuppliedInternal)]
    [DataRow(CompressionMode.CallerSuppliedStreamable)]
    [DataRow(CompressionMode.CallerSuppliedZLib)]
    public void SetRejectsCallerSuppliedCompression(CompressionMode compressionMode)
    {
        var key = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 1);
        using var dbpf = new DataBasePackedFile();
        Assert.ThrowsExactly<ArgumentException>(() => dbpf.Set(key, "content"u8.ToArray(), compressionMode));
        Assert.ThrowsExactly<ArgumentException>(() => dbpf.Set(key, "content", compressionMode));
        Assert.ThrowsExactly<ArgumentException>(() => dbpf.Set(key, new XDocument(new XElement("I")), compressionMode));
        Assert.ThrowsExactly<ArgumentException>(() => dbpf.Set(key, new StringTableModel(), compressionMode));
        Assert.AreEqual(0, dbpf.Count);
    }

    [TestMethod]
    public void SetToleratesUndefinedResourceType()
    {
        var key = new ResourceKey((ResourceType)0xb6c8b6a0, 0, 1);
        var content = "DDS "u8.ToArray();
        using var dbpf = new DataBasePackedFile();
        dbpf.Set(key, content);
        CollectionAssert.AreEqual(content, dbpf.Get(key).ToArray());
    }

    [TestMethod]
    public void ShortReadsAreTolerated()
    {
        var packageBytes = File.ReadAllBytes(tempPackagePath);
        using var expected = new DataBasePackedFile(new MemoryStream(packageBytes));
        using var actual = new DataBasePackedFile(new ShortReadStream(new MemoryStream(packageBytes), 3));
        foreach (var key in expected.Keys)
            CollectionAssert.AreEqual(expected.Get(key).ToArray(), actual.Get(key).ToArray(), key.ToString());
    }

    [TestMethod]
    public async Task ShortReadsAreToleratedAsync()
    {
        var packageBytes = await File.ReadAllBytesAsync(tempPackagePath);
        await using var expected = await DataBasePackedFile.FromStreamAsync(new MemoryStream(packageBytes));
        await using var actual = await DataBasePackedFile.FromStreamAsync(new ShortReadStream(new MemoryStream(packageBytes), 3));
        foreach (var key in await expected.GetKeysAsync())
            CollectionAssert.AreEqual((await expected.GetAsync(key)).ToArray(), (await actual.GetAsync(key)).ToArray(), key.ToString());
    }

    [TestMethod]
    public void TS2PackageIndexIsReadable()
    {
        var packageBytes = File.ReadAllBytes(tempTS2PackagePath);
        using var dbpf = new DataBasePackedFile(new MemoryStream(packageBytes));
        var firstKey = new ResourceKey((ResourceType)0x54544173, 0xffffffff, 0x80);
        Assert.AreEqual(31, dbpf.Count);
        Assert.AreEqual(firstKey, dbpf.Keys[0]);
        CollectionAssert.AreEqual(packageBytes[0x60..(0x60 + 0x1ebb)], dbpf.GetRaw(firstKey).ToArray());
        Assert.AreEqual(129878, dbpf.Keys.Sum(key => dbpf.GetRaw(key).Length));
    }

    [TestMethod]
    public void TS2PackageIsReadable()
    {
        using var dbpf = new DataBasePackedFile(tempTS2PackagePath);
        Assert.AreEqual(1, dbpf.FileVersion.Major);
        Assert.AreEqual(1, dbpf.FileVersion.Minor);
    }

    [TestMethod]
    public void TS3PackageIsReadable()
    {
        using var dbpf = new DataBasePackedFile(tempTS3PackagePage);
        Assert.AreEqual(2, dbpf.FileVersion.Major);
        Assert.AreEqual(0, dbpf.FileVersion.Minor);
    }
}