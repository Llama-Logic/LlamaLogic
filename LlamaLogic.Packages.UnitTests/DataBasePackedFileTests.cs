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

    static readonly Rgba32[] quadrantColors = [new(255, 0, 0, 255), new(0, 255, 0, 128), new(0, 0, 255, 0), new(255, 255, 255, 255)];
    static readonly ResourceKey ts2DirectoryKey = new((ResourceType)0xe86b1eef, 0xe86b1eef, 0x286b1f03);

    static void AssertTranslucentJpegQuadrants(Rgba32[] pixels, int size)
    {
        Rgba32[] expected = [new(255, 0, 0, 255), new(127, 255, 127, 128), new(255, 255, 255, 0), new(255, 255, 255, 255)];
        Assert.AreEqual(size * size, pixels.Length);
        for (var y = 0; y < size; ++y)
            for (var x = 0; x < size; ++x)
            {
                var actual = pixels[(y * size) + x];
                var expectedPixel = expected[(y >= size / 2 ? 2 : 0) + (x >= size / 2 ? 1 : 0)];
                Assert.AreEqual(expectedPixel.A, actual.A);
                Assert.IsTrue(Math.Abs(expectedPixel.R - actual.R) <= 4 && Math.Abs(expectedPixel.G - actual.G) <= 4 && Math.Abs(expectedPixel.B - actual.B) <= 4, $"({x}, {y}): expected {expectedPixel}, actual {actual}");
            }
    }

    static ReadOnlyMemory<byte> CompressAsCallerSupplied(ReadOnlyMemory<byte> content, CompressionMode compressionMode)
    {
        if (compressionMode is CompressionMode.CallerSuppliedZLib)
            return DataBasePackedFile.ZLibCompress(content);
        var compressed = DataBasePackedFile.InternalCompress(content, out var wasCompressed);
        Assert.IsTrue(wasCompressed);
        return compressed;
    }

    static byte[] CreateCompressibleContent() =>
        [.. Enumerable.Repeat("compressible content "u8.ToArray(), 20).SelectMany(bytes => bytes)];

    static byte[] CreateOversizedDds()
    {
        var dds = new byte[192];
        "DDS "u8.CopyTo(dds);
        BinaryPrimitives.WriteUInt32LittleEndian(dds.AsSpan(4), 124);
        BinaryPrimitives.WriteUInt32LittleEndian(dds.AsSpan(8), 0x1007);
        BinaryPrimitives.WriteUInt32LittleEndian(dds.AsSpan(12), 100000);
        BinaryPrimitives.WriteUInt32LittleEndian(dds.AsSpan(16), 100000);
        BinaryPrimitives.WriteUInt32LittleEndian(dds.AsSpan(76), 32);
        BinaryPrimitives.WriteUInt32LittleEndian(dds.AsSpan(80), 4);
        "DXT5"u8.CopyTo(dds.AsSpan(84));
        BinaryPrimitives.WriteUInt32LittleEndian(dds.AsSpan(108), 0x1000);
        return dds;
    }

    static byte[] CreateOversizedPng()
    {
        using var stream = new MemoryStream();
        stream.Write([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);
        var header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header, 100000);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), 100000);
        header[8] = 8;
        header[9] = 6;
        WritePngChunk(stream, "IHDR", header);
        WritePngChunk(stream, "IDAT", [0x78, 0x9c, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01]);
        WritePngChunk(stream, "IEND", []);
        return stream.ToArray();
    }

    static Rgba32[] DecodeRgba(ReadOnlyMemory<byte> pngData)
    {
        using var image = Image.Load<Rgba32>(pngData.Span);
        var pixels = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(pixels);
        return pixels;
    }

    static byte[] EncodeQuadrantsPng(int size)
    {
        using var image = new Image<Rgba32>(size, size);
        for (var y = 0; y < size; ++y)
            for (var x = 0; x < size; ++x)
                image[x, y] = quadrantColors[(y >= size / 2 ? 2 : 0) + (x >= size / 2 ? 1 : 0)];
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

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

    static uint GetCrc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xffffffffU;
        foreach (var b in data)
        {
            crc ^= b;
            for (var bit = 0; bit < 8; ++bit)
                crc = (crc & 1) is not 0 ? (crc >> 1) ^ 0xedb88320U : crc >> 1;
        }
        return ~crc;
    }

    static void WritePngChunk(Stream stream, string type, byte[] data)
    {
        var chunk = new byte[data.Length + 12];
        BinaryPrimitives.WriteUInt32BigEndian(chunk, (uint)data.Length);
        Encoding.ASCII.GetBytes(type).CopyTo(chunk, 4);
        data.CopyTo(chunk, 8);
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(data.Length + 8), GetCrc32(chunk.AsSpan(4, data.Length + 4)));
        stream.Write(chunk);
    }

    [TestMethod]
    public void CopyToRejectsResourcesBeyondFourGigabytes()
    {
        const int largeSize = 0x40000000;
        var smallKey = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 5);
        var small = "small"u8.ToArray();
        using var dbpf = new DataBasePackedFile(RawPackageBuilder.BuildSparse([(smallKey, 96, small.Length, small), .. Enumerable.Range(1, 4).Select(i => (new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, (ulong)i), 96L + small.Length + (i - 1) * (long)largeSize, largeSize, Array.Empty<byte>()))]));
        var copy = new SparseStream();
        Assert.ThrowsExactly<InvalidOperationException>(() => dbpf.CopyTo(copy, ResourceKeyOrder.InstanceTypeGroup));
        Assert.AreEqual(0, copy.Length);
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
    public void CopyToWritesBeyondTwoGigabytes()
    {
        const int largeSize = 0x40000000;
        var firstKey = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 1);
        var secondKey = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 2);
        var farKey = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 3);
        var far = "far"u8.ToArray();
        using var dbpf = new DataBasePackedFile(RawPackageBuilder.BuildSparse([(firstKey, 96, largeSize, []), (secondKey, 96 + largeSize, largeSize, []), (farKey, 96 + 2L * largeSize, far.Length, far)]));
        var copy = new SparseStream();
        dbpf.CopyTo(copy);
        var header = new byte[96];
        copy.Position = 0;
        Assert.AreEqual(header.Length, copy.Read(header));
        Assert.AreEqual(96 + 2L * largeSize + far.Length, BitConverter.ToInt64(header, 64));
        using var reopened = new DataBasePackedFile(copy);
        Assert.AreEqual(largeSize, reopened.GetSize(secondKey));
        CollectionAssert.AreEqual(far, reopened.Get(farKey).ToArray());
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
    public void GetDdsAsPngRejectsOversizedImages()
    {
        var key = new ResourceKey(ResourceType.DSTImage, 0, 1);
        using var dbpf = new DataBasePackedFile();
        dbpf.Set(key, CreateOversizedDds(), CompressionMode.ForceOff);
        Assert.ThrowsExactly<InvalidDataException>(() => dbpf.GetDdsAsPng(key));
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
    public void GetRawReadsStoredSizeOfUncompressedEntries()
    {
        var key = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 1);
        var stored = "stored"u8.ToArray();
        using var dbpf = new DataBasePackedFile(new MemoryStream(RawPackageBuilder.Build([(key, stored, (uint)(stored.Length * 2), (ushort?)0), (new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 2), "following"u8.ToArray(), 9U, (ushort?)0)])));
        CollectionAssert.AreEqual(stored, dbpf.GetRaw(key).ToArray());
        CollectionAssert.AreEqual(stored, dbpf.Get(key).ToArray());
    }

    [TestMethod]
    public void GetReadsBeyondTwoGigabytes()
    {
        var nearKey = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 1);
        var farKey = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 2);
        var near = "near"u8.ToArray();
        var far = "far"u8.ToArray();
        using var dbpf = new DataBasePackedFile(RawPackageBuilder.BuildSparse([(nearKey, 96, near.Length, near), (farKey, 0x80000100L, far.Length, far)]));
        CollectionAssert.AreEqual(near, dbpf.Get(nearKey).ToArray());
        CollectionAssert.AreEqual(near, dbpf.GetRaw(nearKey).ToArray());
        CollectionAssert.AreEqual(far, dbpf.Get(farKey).ToArray());
        CollectionAssert.AreEqual(far, dbpf.GetRaw(farKey).ToArray());
    }

    [TestMethod]
    public async Task GetReadsBeyondTwoGigabytesAsync()
    {
        var nearKey = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 1);
        var farKey = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 2);
        var near = "near"u8.ToArray();
        var far = "far"u8.ToArray();
        await using var dbpf = await DataBasePackedFile.FromStreamAsync(RawPackageBuilder.BuildSparse([(nearKey, 96, near.Length, near), (farKey, 0x80000100L, far.Length, far)]));
        CollectionAssert.AreEqual(near, (await dbpf.GetAsync(nearKey)).ToArray());
        CollectionAssert.AreEqual(near, (await dbpf.GetRawAsync(nearKey)).ToArray());
        CollectionAssert.AreEqual(far, (await dbpf.GetAsync(farKey)).ToArray());
        CollectionAssert.AreEqual(far, (await dbpf.GetRawAsync(farKey)).ToArray());
    }

    [TestMethod]
    public void GetTranslucentJpegAsPngRejectsOtherAlphaFormats()
    {
        var key = new ResourceKey(ResourceType.CASPartThumbnail, 0, 1);
        using var dbpf = new DataBasePackedFile();
        dbpf.Set(key, EncodeTranslucentJpeg(EncodeTiff(4, 4), 4, 4), CompressionMode.ForceOff);
        Assert.ThrowsExactly<InvalidDataException>(() => dbpf.GetTranslucentJpegAsPng(key));
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
    public async Task SetPngAsDdsAsyncRoundTrips()
    {
        var key = new ResourceKey(ResourceType.DSTImage, 0, 1);
        var pngData = EncodeQuadrantsPng(8);
        await using var dbpf = new DataBasePackedFile();
        await dbpf.SetPngAsDdsAsync(key, pngData);
        CollectionAssert.AreEqual(DecodeRgba(pngData), DecodeRgba(await dbpf.GetDdsAsPngAsync(key)));
    }

    [TestMethod]
    public void SetPngAsDdsRejectsOtherImageFormats()
    {
        using var dbpf = new DataBasePackedFile();
        Assert.ThrowsExactly<InvalidDataException>(() => dbpf.SetPngAsDds(new ResourceKey(ResourceType.DSTImage, 0, 1), EncodeTiff(4, 4)));
    }

    [TestMethod]
    public void SetPngAsDdsRejectsOversizedImages()
    {
        using var dbpf = new DataBasePackedFile();
        Assert.ThrowsExactly<InvalidDataException>(() => dbpf.SetPngAsDds(new ResourceKey(ResourceType.DSTImage, 0, 1), CreateOversizedPng()));
    }

    [TestMethod]
    public void SetPngAsDdsRoundTrips()
    {
        var key = new ResourceKey(ResourceType.DSTImage, 0, 1);
        var pngData = EncodeQuadrantsPng(8);
        using var dbpf = new DataBasePackedFile();
        dbpf.SetPngAsDds(key, pngData);
        CollectionAssert.AreEqual(DecodeRgba(pngData), DecodeRgba(dbpf.GetDdsAsPng(key)));
    }

    [TestMethod]
    public async Task SetPngAsTranslucentJpegAsyncRoundTrips()
    {
        var key = new ResourceKey(ResourceType.CASPartThumbnail, 0, 1);
        await using var dbpf = new DataBasePackedFile();
        await dbpf.SetPngAsTranslucentJpegAsync(key, EncodeQuadrantsPng(32));
        AssertTranslucentJpegQuadrants(DecodeRgba(await dbpf.GetTranslucentJpegAsPngAsync(key)), 32);
    }

    [TestMethod]
    public void SetPngAsTranslucentJpegRejectsOtherImageFormats()
    {
        using var dbpf = new DataBasePackedFile();
        Assert.ThrowsExactly<InvalidDataException>(() => dbpf.SetPngAsTranslucentJpeg(new ResourceKey(ResourceType.CASPartThumbnail, 0, 1), EncodeTiff(4, 4)));
    }

    [TestMethod]
    public void SetPngAsTranslucentJpegRejectsOversizedImages()
    {
        using var dbpf = new DataBasePackedFile();
        Assert.ThrowsExactly<InvalidDataException>(() => dbpf.SetPngAsTranslucentJpeg(new ResourceKey(ResourceType.CASPartThumbnail, 0, 1), CreateOversizedPng()));
    }

    [TestMethod]
    public void SetPngAsTranslucentJpegRoundTrips()
    {
        var key = new ResourceKey(ResourceType.CASPartThumbnail, 0, 1);
        using var dbpf = new DataBasePackedFile();
        dbpf.SetPngAsTranslucentJpeg(key, EncodeQuadrantsPng(32));
        AssertTranslucentJpegQuadrants(DecodeRgba(dbpf.GetTranslucentJpegAsPng(key)), 32);
    }

    [TestMethod]
    [DataRow(CompressionMode.CallerSuppliedInternal)]
    [DataRow(CompressionMode.CallerSuppliedStreamable)]
    [DataRow(CompressionMode.CallerSuppliedZLib)]
    public async Task SetRawAsyncStoresContentAsSupplied(CompressionMode compressionMode)
    {
        var key = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 1);
        var content = CreateCompressibleContent();
        var raw = CompressAsCallerSupplied(content, compressionMode).ToArray();
        await using var dbpf = new DataBasePackedFile();
        await dbpf.SetRawAsync(key, raw, compressionMode, content.Length);
        CollectionAssert.AreEqual(raw, (await dbpf.GetRawAsync(key)).ToArray());
        CollectionAssert.AreEqual(content, (await dbpf.GetAsync(key)).ToArray());
        Assert.AreEqual(content.Length, await dbpf.GetSizeAsync(key));
        var copy = new MemoryStream();
        await dbpf.CopyToAsync(copy);
        copy.Position = 0;
        await using var reopened = await DataBasePackedFile.FromStreamAsync(copy);
        CollectionAssert.AreEqual(raw, (await reopened.GetRawAsync(key)).ToArray());
        CollectionAssert.AreEqual(content, (await reopened.GetAsync(key)).ToArray());
        Assert.AreEqual(content.Length, await reopened.GetSizeAsync(key));
    }

    [TestMethod]
    [DataRow(CompressionMode.CallerSuppliedInternal)]
    [DataRow(CompressionMode.CallerSuppliedStreamable)]
    [DataRow(CompressionMode.CallerSuppliedZLib)]
    public void SetRawInvalidatesNames(CompressionMode compressionMode)
    {
        static XDocument createSnippetTuning(string name) =>
            new(new XElement("I", new XAttribute("c", "Snippet"), new XAttribute("i", "snippet"), new XAttribute("m", "snippets"), new XAttribute("n", name), new XAttribute("s", "1"), Enumerable.Range(0, 64).Select(i => new XElement("T", new XAttribute("n", $"value{i}"), "0"))));
        var key = new ResourceKey(ResourceType.SnippetTuning, 0x80000000, 1);
        using var dbpf = new DataBasePackedFile();
        dbpf.Set(key, createSnippetTuning("LlamaLogicPackageTests:Original"));
        dbpf.LoadNames();
        using var replacementStream = new MemoryStream();
        createSnippetTuning("LlamaLogicPackageTests:Replacement").Save(replacementStream);
        var replacement = replacementStream.ToArray();
        dbpf.SetRaw(key, CompressAsCallerSupplied(replacement, compressionMode), compressionMode, replacement.Length);
        Assert.AreEqual("LlamaLogicPackageTests:Replacement", dbpf.GetNameByKey(key));
    }

    [TestMethod]
    public void SetRawRejectsNegativeSize()
    {
        var key = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 1);
        using var dbpf = new DataBasePackedFile();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => dbpf.SetRaw(key, DataBasePackedFile.ZLibCompress(CreateCompressibleContent()), CompressionMode.CallerSuppliedZLib, -1));
        Assert.AreEqual(0, dbpf.Count);
    }

    [TestMethod]
    [DataRow(CompressionMode.Auto)]
    [DataRow(CompressionMode.ForceOff)]
    [DataRow(CompressionMode.ForceZLib)]
    [DataRow(CompressionMode.ForceInternal)]
    [DataRow(CompressionMode.SetDeletedFlag)]
    public async Task SetRawRejectsOtherCompressionModes(CompressionMode compressionMode)
    {
        var key = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 1);
        await using var dbpf = new DataBasePackedFile();
        Assert.ThrowsExactly<ArgumentException>(() => dbpf.SetRaw(key, "content"u8.ToArray(), compressionMode, 7));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => dbpf.SetRawAsync(key, "content"u8.ToArray(), compressionMode, 7));
        Assert.AreEqual(0, dbpf.Count);
    }

    [TestMethod]
    [DataRow(CompressionMode.CallerSuppliedInternal)]
    [DataRow(CompressionMode.CallerSuppliedStreamable)]
    [DataRow(CompressionMode.CallerSuppliedZLib)]
    public void SetRawStoresContentAsSupplied(CompressionMode compressionMode)
    {
        var key = new ResourceKey(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 1);
        var content = CreateCompressibleContent();
        var raw = CompressAsCallerSupplied(content, compressionMode).ToArray();
        using var dbpf = new DataBasePackedFile();
        dbpf.SetRaw(key, raw, compressionMode, content.Length);
        CollectionAssert.AreEqual(raw, dbpf.GetRaw(key).ToArray());
        CollectionAssert.AreEqual(content, dbpf.Get(key).ToArray());
        Assert.AreEqual(content.Length, dbpf.GetSize(key));
        var copy = new MemoryStream();
        dbpf.CopyTo(copy);
        using var reopened = new DataBasePackedFile(new MemoryStream(copy.ToArray()));
        CollectionAssert.AreEqual(raw, reopened.GetRaw(key).ToArray());
        CollectionAssert.AreEqual(content, reopened.Get(key).ToArray());
        Assert.AreEqual(content.Length, reopened.GetSize(key));
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
    public async Task SetXmlAsyncRoundTrips()
    {
        var key = new ResourceKey(ResourceType.SnippetTuning, 0x80000000, 1);
        await using var dbpf = new DataBasePackedFile();
        await dbpf.SetXmlAsync(key, "<I c=\"Snippet\" i=\"snippet\" m=\"snippets\" n=\"LlamaLogicPackageTests:Async\" s=\"1\" />");
        Assert.AreEqual("LlamaLogicPackageTests:Async", (await dbpf.GetXmlAsync(key)).Root!.Attribute("n")!.Value);
        Assert.AreEqual("LlamaLogicPackageTests:Async", await dbpf.GetNameByKeyAsync(key));
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
    [DataRow(1U)]
    [DataRow(2U)]
    public void TS2CompressedPackageRoundTrips(uint indexMinorVersion)
    {
        var uncompressedKey = new ResourceKey((ResourceType)0x54544173, 0xffffffff, 0x80);
        var uncompressed = "uncompressed"u8.ToArray();
        var compressedKey = new ResourceKey((ResourceType)0x53545223, 0x7fd46cd0, indexMinorVersion >= 2 ? 0x100000080UL : 0x80UL);
        var content = CreateCompressibleContent();
        var stored = RawPackageBuilder.BuildVersion1Compressed(content);
        var packageBytes = RawPackageBuilder.BuildVersion1([(uncompressedKey, uncompressed), (compressedKey, stored), (ts2DirectoryKey, RawPackageBuilder.BuildVersion1Directory([(compressedKey, content.Length)], indexMinorVersion))], indexMinorVersion);
        using var dbpf = new DataBasePackedFile(new MemoryStream(packageBytes));
        CollectionAssert.AreEqual(content, dbpf.Get(compressedKey).ToArray());
        Assert.AreEqual(content.Length, dbpf.GetSize(compressedKey));
        CollectionAssert.AreEqual(stored, dbpf.GetRaw(compressedKey).ToArray());
        Assert.AreEqual(CompressionMode.ForceInternal, dbpf.GetExplicitCompressionMode(compressedKey));
        CollectionAssert.AreEqual(uncompressed, dbpf.Get(uncompressedKey).ToArray());
        Assert.AreEqual(CompressionMode.ForceOff, dbpf.GetExplicitCompressionMode(uncompressedKey));
        var copy = new MemoryStream();
        dbpf.CopyTo(copy);
        CollectionAssert.AreEqual(packageBytes, copy.ToArray());
    }

    [TestMethod]
    [DataRow(1U)]
    [DataRow(2U)]
    public async Task TS2CompressedPackageRoundTripsAsync(uint indexMinorVersion)
    {
        var uncompressedKey = new ResourceKey((ResourceType)0x54544173, 0xffffffff, 0x80);
        var uncompressed = "uncompressed"u8.ToArray();
        var compressedKey = new ResourceKey((ResourceType)0x53545223, 0x7fd46cd0, indexMinorVersion >= 2 ? 0x100000080UL : 0x80UL);
        var content = CreateCompressibleContent();
        var stored = RawPackageBuilder.BuildVersion1Compressed(content);
        var packageBytes = RawPackageBuilder.BuildVersion1([(uncompressedKey, uncompressed), (compressedKey, stored), (ts2DirectoryKey, RawPackageBuilder.BuildVersion1Directory([(compressedKey, content.Length)], indexMinorVersion))], indexMinorVersion);
        await using var dbpf = await DataBasePackedFile.FromStreamAsync(new MemoryStream(packageBytes));
        CollectionAssert.AreEqual(content, (await dbpf.GetAsync(compressedKey)).ToArray());
        Assert.AreEqual(content.Length, await dbpf.GetSizeAsync(compressedKey));
        CollectionAssert.AreEqual(stored, (await dbpf.GetRawAsync(compressedKey)).ToArray());
        var copy = new MemoryStream();
        await dbpf.CopyToAsync(copy);
        CollectionAssert.AreEqual(packageBytes, copy.ToArray());
        Assert.IsTrue(await dbpf.SetAsync(uncompressedKey, content));
        var editedCopy = new MemoryStream();
        await dbpf.CopyToAsync(editedCopy);
        editedCopy.Position = 0;
        await using var reopened = await DataBasePackedFile.FromStreamAsync(editedCopy);
        CollectionAssert.AreEqual(content, (await reopened.GetAsync(uncompressedKey)).ToArray());
        Assert.AreEqual(CompressionMode.ForceInternal, reopened.GetExplicitCompressionMode(uncompressedKey));
        CollectionAssert.AreEqual(content, (await reopened.GetAsync(compressedKey)).ToArray());
    }

    [TestMethod]
    public void TS2FileVersionChangeRejectsCompressedResources()
    {
        var key = new ResourceKey((ResourceType)0x53545223, 0x7fd46cd0, 0x80);
        var content = CreateCompressibleContent();
        using var version1 = new DataBasePackedFile(new MemoryStream(RawPackageBuilder.BuildVersion1([(key, RawPackageBuilder.BuildVersion1Compressed(content)), (ts2DirectoryKey, RawPackageBuilder.BuildVersion1Directory([(key, content.Length)], 1))], 1)));
        Assert.ThrowsExactly<InvalidOperationException>(() => version1.FileVersion = new Version(2, 1));
        Assert.AreEqual(new Version(1, 1), version1.FileVersion);
        version1.Set(key, content, CompressionMode.ForceOff);
        version1.FileVersion = new Version(2, 1);
        Assert.AreEqual(new Version(2, 1), version1.FileVersion);
        using var version2 = new DataBasePackedFile();
        version2.Set(key, content, CompressionMode.ForceZLib);
        Assert.ThrowsExactly<InvalidOperationException>(() => version2.FileVersion = new Version(1, 1));
        Assert.AreEqual(new Version(2, 1), version2.FileVersion);
    }

    [TestMethod]
    public void TS2NewPackageIsWritable()
    {
        var key = new ResourceKey((ResourceType)0x53545223, 0x7fd46cd0, 0x100000080UL);
        var content = CreateCompressibleContent();
        using var dbpf = new DataBasePackedFile { FileVersion = new Version(1, 1) };
        Assert.IsTrue(dbpf.Set(key, content));
        var copy = new MemoryStream();
        dbpf.CopyTo(copy);
        var packageBytes = copy.ToArray();
        Assert.AreEqual(7U, BitConverter.ToUInt32(packageBytes, 0x20));
        Assert.AreEqual(2U, BitConverter.ToUInt32(packageBytes, 0x3c));
        using var reopened = new DataBasePackedFile(new MemoryStream(packageBytes));
        Assert.AreEqual(new Version(1, 1), reopened.FileVersion);
        CollectionAssert.AreEqual(content, reopened.Get(key).ToArray());
        Assert.AreEqual(CompressionMode.ForceInternal, reopened.GetExplicitCompressionMode(key));
    }

    [TestMethod]
    public void TS2PackageCopiesUnchanged()
    {
        var packageBytes = File.ReadAllBytes(tempTS2PackagePath);
        using var dbpf = new DataBasePackedFile(new MemoryStream(packageBytes));
        var copy = new MemoryStream();
        dbpf.CopyTo(copy);
        CollectionAssert.AreEqual(packageBytes, copy.ToArray());
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
    public void TS2SetCompressesInternally()
    {
        var key = new ResourceKey((ResourceType)0x53545223, 0x7fd46cd0, 0x80);
        var content = CreateCompressibleContent();
        using var dbpf = new DataBasePackedFile(new MemoryStream(RawPackageBuilder.BuildVersion1([(key, content)], 1)));
        Assert.IsTrue(dbpf.Set(key, content));
        var copy = new MemoryStream();
        dbpf.CopyTo(copy);
        using var reopened = new DataBasePackedFile(new MemoryStream(copy.ToArray()));
        CollectionAssert.AreEqual(content, reopened.Get(key).ToArray());
        Assert.AreEqual(CompressionMode.ForceInternal, reopened.GetExplicitCompressionMode(key));
        var stored = reopened.GetRaw(key).ToArray();
        Assert.AreEqual((uint)stored.Length, BitConverter.ToUInt32(stored, 0));
        CollectionAssert.AreEqual(new byte[] { 0x10, 0xfb }, stored[4..6]);
        CollectionAssert.AreEqual(RawPackageBuilder.BuildVersion1Directory([(key, content.Length)], 1), reopened.GetRaw(ts2DirectoryKey).ToArray());
    }

    [TestMethod]
    public void TS2SetForceOffRemovesDirectory()
    {
        var key = new ResourceKey((ResourceType)0x53545223, 0x7fd46cd0, 0x80);
        var content = CreateCompressibleContent();
        using var dbpf = new DataBasePackedFile(new MemoryStream(RawPackageBuilder.BuildVersion1([(key, RawPackageBuilder.BuildVersion1Compressed(content)), (ts2DirectoryKey, RawPackageBuilder.BuildVersion1Directory([(key, content.Length)], 1))], 1)));
        Assert.IsFalse(dbpf.Set(key, content, CompressionMode.ForceOff));
        var copy = new MemoryStream();
        dbpf.CopyTo(copy);
        using var reopened = new DataBasePackedFile(new MemoryStream(copy.ToArray()));
        Assert.IsFalse(reopened.ContainsKey(ts2DirectoryKey));
        CollectionAssert.AreEqual(content, reopened.GetRaw(key).ToArray());
        Assert.AreEqual(CompressionMode.ForceOff, reopened.GetExplicitCompressionMode(key));
    }

    [TestMethod]
    [DataRow(CompressionMode.CallerSuppliedStreamable)]
    [DataRow(CompressionMode.CallerSuppliedZLib)]
    public void TS2SetRawRejectsUnsupportedCompression(CompressionMode compressionMode)
    {
        var key = new ResourceKey((ResourceType)0x53545223, 0x7fd46cd0, 0x80);
        var original = "original"u8.ToArray();
        using var dbpf = new DataBasePackedFile(new MemoryStream(RawPackageBuilder.BuildVersion1([(key, original)], 1)));
        var content = CreateCompressibleContent();
        Assert.ThrowsExactly<NotSupportedException>(() => dbpf.SetRaw(key, CompressAsCallerSupplied(content, compressionMode), compressionMode, content.Length));
        CollectionAssert.AreEqual(original, dbpf.Get(key).ToArray());
    }

    [TestMethod]
    public void TS2SetRawStoresContentAsSupplied()
    {
        var key = new ResourceKey((ResourceType)0x53545223, 0x7fd46cd0, 0x80);
        var content = CreateCompressibleContent();
        var stored = RawPackageBuilder.BuildVersion1Compressed(content);
        using var dbpf = new DataBasePackedFile(new MemoryStream(RawPackageBuilder.BuildVersion1([(key, "original"u8.ToArray())], 1)));
        dbpf.SetRaw(key, stored, CompressionMode.CallerSuppliedInternal, content.Length);
        var copy = new MemoryStream();
        dbpf.CopyTo(copy);
        using var reopened = new DataBasePackedFile(new MemoryStream(copy.ToArray()));
        CollectionAssert.AreEqual(stored, reopened.GetRaw(key).ToArray());
        CollectionAssert.AreEqual(content, reopened.Get(key).ToArray());
        CollectionAssert.AreEqual(RawPackageBuilder.BuildVersion1Directory([(key, content.Length)], 1), reopened.GetRaw(ts2DirectoryKey).ToArray());
    }

    [TestMethod]
    [DataRow(CompressionMode.ForceZLib)]
    [DataRow(CompressionMode.SetDeletedFlag)]
    public void TS2SetRejectsUnsupportedCompression(CompressionMode compressionMode)
    {
        var key = new ResourceKey((ResourceType)0x53545223, 0x7fd46cd0, 0x80);
        var original = "original"u8.ToArray();
        using var dbpf = new DataBasePackedFile(new MemoryStream(RawPackageBuilder.BuildVersion1([(key, original)], 1)));
        Assert.ThrowsExactly<NotSupportedException>(() => dbpf.Set(key, CreateCompressibleContent(), compressionMode));
        CollectionAssert.AreEqual(original, dbpf.Get(key).ToArray());
    }

    [TestMethod]
    public void TS3PackageIsReadable()
    {
        using var dbpf = new DataBasePackedFile(tempTS3PackagePage);
        Assert.AreEqual(2, dbpf.FileVersion.Major);
        Assert.AreEqual(0, dbpf.FileVersion.Minor);
    }
}