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

    [TestMethod]
    public void CreatePackage()
    {
        using var dbpf = new DataBasePackedFile();
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
    public void Indexer()
    {
        using var dbpf = new DataBasePackedFile(tempPackagePath);
        var tuningXmlObj = dbpf["0904df10:00000000:00000000d1d56a53"];
        Assert.IsInstanceOfType(tuningXmlObj, typeof(string));
        var tuningXDocument = XDocument.Parse((string)tuningXmlObj);
        Assert.AreEqual("relationships.relationship_bit", tuningXDocument.Root!.Attribute("m")!.Value);
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