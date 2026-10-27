namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningArchiveUninstalledTuningTests
{
    static async Task<TuningArchiveSnapshot> CaptureAsync(TuningArchiveFile archive, TemporaryDirectory directory, bool withDescriptions = false, TuningArchiveCaptureOptions? options = null)
    {
        var result = await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithUninstalledPack(directory)), options);
        Assert.AreEqual(0, result.Problems.Count, string.Join(Environment.NewLine, result.Problems));
        if (withDescriptions)
        {
            File.WriteAllBytes(directory.GetPath("descriptions.zip"), TuningArchiveFixtures.WriteDescriptionsZip());
            await archive.ImportTuningDescriptionsAsync(directory.GetPath("descriptions.zip"), "1.0.1.1000");
        }
        return result.Snapshot;
    }

    static IReadOnlyList<string> GetComments(string text) =>
        [.. XDocument.Parse(text).DescendantNodes().OfType<XComment>().Select(comment => comment.Value)];

    [TestMethod]
    public void PacksWithOnlyPatchesAreNotInstalled()
    {
        using var directory = new TemporaryDirectory();
        var installation = GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithUninstalledPack(directory));
        CollectionAssert.AreEqual(new[] { "BG" }, installation.Packs.Select(pack => pack.Code).ToArray());
        CollectionAssert.AreEqual(new[] { "EP19" }, installation.UninstalledPacks.Select(pack => pack.Code).ToArray());
        Assert.IsTrue(installation.UninstalledPacks[0].SimulationPackages.All(package => package.IsPatch));
    }

    [TestMethod]
    public async Task OnlyTheNamesOfUninstalledTuningAreCaptured()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await CaptureAsync(archive, directory);
        var entries = archive.GetEntries(snapshot, TuningArchiveResourceKind.Tuning);
        CollectionAssert.AreEquivalent(new[] { "Buff_Happy", "Mood_Happy" }, entries.Select(entry => entry.Name).ToArray());
        var buffType = entries.Single(entry => entry.Name == "Buff_Happy").Type;
        var uninstalled = archive.GetUninstalledTunings(snapshot).Single();
        Assert.AreEqual(new TuningArchiveUninstalledTuning(buffType, TuningArchiveFixtures.UninstalledBuffInstance, "Buff_Patched", "Buff", "EP19"), uninstalled);
        CollectionAssert.AreEqual(new[] { "BG" }, snapshot.Packs.Select(pack => pack.Code).ToArray());
    }

    [TestMethod]
    public async Task UninstalledPacksLeftOutOfTheCaptureAreNotNamed()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await CaptureAsync(archive, directory, options: new() { Packs = ["BG"] });
        Assert.AreEqual(0, archive.GetUninstalledTunings(snapshot).Count);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ReferencesToUninstalledTuningAreCommentedWithItsName(bool withDescriptions)
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await CaptureAsync(archive, directory, withDescriptions);
        var mood = archive.GetEntries(snapshot, TuningArchiveResourceKind.Tuning).Single(entry => entry.Name == "Mood_Happy");
        CollectionAssert.AreEqual(new[] { "Buff: Buff_Happy", "Buff: Buff_Patched" }, GetComments(TuningCommenter.Create(archive, snapshot).Comment(archive.GetText(mood))).ToArray());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ExportsCommentAndListUninstalledTuningWithoutExportingIt(bool withDescriptions)
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await CaptureAsync(archive, directory, withDescriptions);
        await archive.ExportAsync(snapshot, directory.GetPath("out"));
        var files = Directory.EnumerateFiles(directory.GetPath("out"), "*.xml", SearchOption.AllDirectories).ToList();
        Assert.AreEqual(0, files.Count(file => Path.GetFileName(file).Contains($"{TuningArchiveFixtures.UninstalledBuffInstance:X16}", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(file).Contains("Buff_Patched", StringComparison.Ordinal)));
        var mood = files.Single(file => Path.GetFileName(file).EndsWith($"_{TuningArchiveFixtures.MoodHappyInstance:X16}.xml", StringComparison.Ordinal));
        CollectionAssert.AreEqual(new[] { "Buff: Buff_Happy", "Buff: Buff_Patched" }, GetComments(File.ReadAllText(mood)).ToArray());
        var names = File.ReadAllLines(Path.Combine(directory.GetPath("out"), "Reference - All XML Names.txt"));
        CollectionAssert.Contains(names, $"0x{TuningArchiveFixtures.UninstalledBuffInstance:X16}\tBuff\tBuff_Patched");
    }

    [TestMethod]
    public async Task DeletingASnapshotDeletesItsUninstalledTuning()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await CaptureAsync(archive, directory);
        await archive.DeleteSnapshotAsync(snapshot);
        Assert.AreEqual(0, archive.GetUninstalledTunings(snapshot).Count);
    }

    [TestMethod]
    public async Task ArchivesOfTheFirstSchemaAreMigrated()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.GetPath("Tuning.tuningarchive");
        using (var archive = TuningArchiveFile.Open(path))
        {
        }
        using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE snapshot_uninstalled_tuning; UPDATE meta SET value = '1' WHERE key = 'schema_version';";
            command.ExecuteNonQuery();
        }
        using (var archive = TuningArchiveFile.Open(path))
        {
            var snapshot = await CaptureAsync(archive, directory);
            Assert.AreEqual("Buff_Patched", archive.GetUninstalledTunings(snapshot).Single().Name);
        }
        using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT value FROM meta WHERE key = 'schema_version'";
            Assert.AreEqual("2", command.ExecuteScalar());
        }
    }
}