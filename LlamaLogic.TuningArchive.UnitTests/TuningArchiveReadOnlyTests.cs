namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningArchiveReadOnlyTests
{
    static async Task<TuningArchiveSnapshot> ImportAsync(TuningArchiveFile archive, TemporaryDirectory directory, string folder, IReadOnlyDictionary<string, string> files, string gameVersion)
    {
        var result = await archive.ImportFolderAsync(directory.WriteFiles(folder, files), gameVersion);
        Assert.AreEqual(0, result.Problems.Count, string.Join(Environment.NewLine, result.Problems));
        return result.Snapshot;
    }

    static async Task<string> WriteIndexedArchiveAsync(TemporaryDirectory directory)
    {
        var path = directory.GetPath("Tuning.tuningarchive");
        using var archive = TuningArchiveFile.Open(path);
        await ImportAsync(archive, directory, "first", TuningArchiveFixtures.First, "1.0.1.1000");
        File.WriteAllBytes(directory.GetPath("1.0.1.1000-TDESC.zip"), TuningArchiveFixtures.WriteDescriptionsZip());
        await archive.ImportTuningDescriptionsAsync(directory.GetPath("1.0.1.1000-TDESC.zip"));
        Assert.AreEqual(3, await archive.IndexReferencesAsync());
        return path;
    }

    static void SetMeta(string path, string key, string value)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE meta SET value = $value WHERE key = $key";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        Assert.AreEqual(1, command.ExecuteNonQuery());
    }

    static object? QueryScalar(string path, string sql)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    [TestMethod]
    public async Task ArchivesOpenedReadOnlyFindNoReferencesInAnIndexAnotherVersionBuilt()
    {
        using var directory = new TemporaryDirectory();
        var path = await WriteIndexedArchiveAsync(directory);
        SetMeta(path, "reference_index_version", "3");
        using (var archive = TuningArchiveFile.OpenReadOnly(path))
        {
            var snapshot = archive.GetSnapshots().Single();
            Assert.AreEqual((0, 3), archive.GetReferenceIndexStatus());
            Assert.AreEqual(0, archive.GetReferrers(snapshot, ResourceType.MoodTuning, TuningArchiveFixtures.MoodHappyInstance).Count);
            Assert.AreEqual(0, archive.GetStringReferrers(snapshot, [0x0000ABCE]).Count);
            Assert.AreEqual(0, archive.GetBrokenReferences(snapshot).Count);
            Assert.AreEqual(0, archive.GetBrokenMediaReferences(snapshot).Count);
        }
        Assert.AreEqual("3", QueryScalar(path, "SELECT value FROM meta WHERE key = 'reference_index_version'"));
        Assert.AreNotEqual(0L, QueryScalar(path, "SELECT COUNT(*) FROM reference"));
        Assert.AreEqual(3L, QueryScalar(path, "SELECT COUNT(references_indexed_by) FROM blob"));
    }

    [TestMethod]
    public async Task ArchivesOpenedReadOnlyReadWhatWasWritten()
    {
        using var directory = new TemporaryDirectory();
        var path = await WriteIndexedArchiveAsync(directory);
        using var archive = TuningArchiveFile.OpenReadOnly(path);
        Assert.IsTrue(archive.IsReadOnly);
        var snapshot = archive.GetSnapshots().Single();
        Assert.AreEqual("1.0.1.1000", snapshot.GameVersion);
        Assert.AreEqual(3, archive.GetEntries(snapshot).Count);
        var buffHappy = archive.GetEntry(snapshot, TuningArchiveResourceKind.Tuning, ResourceType.BuffTuning, TuningArchiveFixtures.BuffHappyInstance)!;
        StringAssert.Contains(archive.GetText(buffHappy), "Buff_Happy");
        Assert.AreEqual((3, 3), archive.GetReferenceIndexStatus());
        CollectionAssert.AreEqual
        (
            new[] { "Buff_Happy", "Buff_Sad" },
            archive.GetReferrers(snapshot, ResourceType.MoodTuning, TuningArchiveFixtures.MoodHappyInstance).Select(reference => reference.Source.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray()
        );
        Assert.AreEqual("Buff_Sad", archive.GetStringReferrers(snapshot, [0x0000ABCE]).Single().Source.Name);
        Assert.AreEqual("1.0.1.1000", archive.FindTuningDescriptions(snapshot, exactGameVersion: true)!.GameVersion);
    }

    [TestMethod]
    public async Task ArchivesOpenedReadOnlyRefuseChangesAndLeaveTheFileAlone()
    {
        using var directory = new TemporaryDirectory();
        var path = await WriteIndexedArchiveAsync(directory);
        var before = File.ReadAllBytes(path);
        using (var archive = TuningArchiveFile.OpenReadOnly(path))
        {
            var snapshot = archive.GetSnapshots().Single();
            var descriptions = archive.GetTuningDescriptions().Single();
            var folder = directory.WriteFiles("second", TuningArchiveFixtures.Second);
            await Assert.ThrowsExactlyAsync<NotSupportedException>(() => archive.ImportFolderAsync(folder, "1.0.2.1000"));
            await Assert.ThrowsExactlyAsync<NotSupportedException>(() => archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithSimData(directory))));
            await Assert.ThrowsExactlyAsync<NotSupportedException>(() => archive.ImportPythonAsync(snapshot, folder));
            await Assert.ThrowsExactlyAsync<NotSupportedException>(() => archive.ImportTuningDescriptionsAsync(directory.GetPath("1.0.1.1000-TDESC.zip")));
            await Assert.ThrowsExactlyAsync<NotSupportedException>(() => archive.DeleteTuningDescriptionsAsync(descriptions));
            await Assert.ThrowsExactlyAsync<NotSupportedException>(() => archive.IndexReferencesAsync());
            await Assert.ThrowsExactlyAsync<NotSupportedException>(() => archive.UpdateSnapshotAsync(snapshot, "Label", null));
            await Assert.ThrowsExactlyAsync<NotSupportedException>(() => archive.DeleteSnapshotAsync(snapshot));
            Assert.AreEqual(1, archive.GetSnapshots().Count);
            Assert.IsNull(archive.GetSnapshots().Single().Label);
        }
        CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
    }

    [TestMethod]
    public async Task ArchivesOpenedReadOnlySeeWhatAWriterWritesWhileItIsOpen()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.GetPath("Tuning.tuningarchive");
        using var writer = TuningArchiveFile.Open(path);
        await ImportAsync(writer, directory, "first", TuningArchiveFixtures.First, "1.0.1.1000");
        using var reader = TuningArchiveFile.OpenReadOnly(path);
        Assert.AreEqual(1, reader.GetSnapshots().Count);
        await ImportAsync(writer, directory, "second", TuningArchiveFixtures.Second, "1.0.2.1000");
        var snapshots = reader.GetSnapshots();
        CollectionAssert.AreEqual(new[] { "1.0.1.1000", "1.0.2.1000" }, snapshots.Select(snapshot => snapshot.GameVersion).ToArray());
        Assert.AreEqual(4, reader.Compare(snapshots[0], snapshots[1]).Count);
    }

    [TestMethod]
    public void OpeningAFileThatIsNotAnArchiveReadOnlyFails()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.WriteFile("Tuning.tuningarchive", "This is not a database.");
        Assert.ThrowsExactly<InvalidDataException>(() => TuningArchiveFile.OpenReadOnly(path));
    }

    [TestMethod]
    public void OpeningAMissingArchiveReadOnlyFailsWithoutCreatingIt()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.GetPath("Tuning.tuningarchive");
        Assert.ThrowsExactly<FileNotFoundException>(() => TuningArchiveFile.OpenReadOnly(path));
        Assert.IsFalse(File.Exists(path));
    }

    [TestMethod]
    [DataRow("1")]
    [DataRow("99")]
    public async Task OpeningAnArchiveOfAnotherSchemaReadOnlyFails(string schemaVersion)
    {
        using var directory = new TemporaryDirectory();
        var path = await WriteIndexedArchiveAsync(directory);
        SetMeta(path, "schema_version", schemaVersion);
        Assert.ThrowsExactly<InvalidDataException>(() => TuningArchiveFile.OpenReadOnly(path));
        Assert.AreEqual(schemaVersion, QueryScalar(path, "SELECT value FROM meta WHERE key = 'schema_version'"));
    }
}