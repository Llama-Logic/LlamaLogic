namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningFolderUpdateTests
{
    static readonly ResourceType buffType = TuningXml.GetResourceType("buff");
    static readonly ResourceKey happyKey = new(buffType, 0, TuningArchiveFixtures.BuffHappyInstance);

    static string BuffHappy(string name, int weight) =>
        $"<I c=\"Buff\" i=\"buff\" m=\"buffs.buff\" n=\"Buff_Happy\" s=\"200\"><T n=\"buff_name\">{name}</T><T n=\"mood_type\">100</T><T n=\"mood_weight\">{weight}</T></I>";

    static void WritePackage(string path, params (ResourceKey key, string content)[] resources)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var package = new DataBasePackedFile();
        foreach (var (key, content) in resources)
            package.Set(key, content);
        package.SaveAs(path);
    }

    static async Task<(TuningArchiveFile archive, TuningArchiveSnapshot first, TuningArchiveSnapshot second, string mods)> SetUpAsync(TemporaryDirectory directory)
    {
        var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var first = (await archive.ImportFolderAsync(directory.WriteFiles("first", TuningArchiveFixtures.First), "1.0.0.1000")).Snapshot;
        var second = (await archive.ImportFolderAsync(directory.WriteFiles("second", TuningArchiveFixtures.Second), "1.0.1.1000")).Snapshot;
        var mods = directory.GetPath("Mods");
        WritePackage(Path.Combine(mods, "Creator", "Merges.package"), (happyKey, BuffHappy("0x0000BEEF", 1)));
        WritePackage(Path.Combine(mods, "Updated.package"), (happyKey, BuffHappy("0x0000BEEF", 3)));
        WritePackage(Path.Combine(mods, "NoOverrides.package"), (new ResourceKey(buffType, 0, 12345), BuffHappy("0x0000BEEF", 1).Replace("s=\"200\"", "s=\"12345\"", StringComparison.Ordinal)));
        return (archive, first, second, mods);
    }

    [TestMethod]
    public async Task OnlyPackagesWithOverridesAreListed()
    {
        using var directory = new TemporaryDirectory();
        var (archive, _, second, mods) = await SetUpAsync(directory);
        using (archive)
        {
            var update = await TuningFolderUpdate.AnalyzeAsync(archive, second, mods);
            CollectionAssert.AreEqual(new[] { Path.Combine("Creator", "Merges.package"), "Updated.package" }, update.Packages.Select(package => package.RelativePath).ToArray());
        }
    }

    [TestMethod]
    public async Task AMergeWhoseBaseIsInferredFromTheOldestSnapshotNeedsTheCreator()
    {
        using var directory = new TemporaryDirectory();
        var (archive, _, second, mods) = await SetUpAsync(directory);
        using (archive)
        {
            var item = (await TuningFolderUpdate.AnalyzeAsync(archive, second, mods)).Packages[0].Update!.Overrides.Single();
            Assert.AreEqual(TuningPackageOverrideOutcome.Merged, item.Outcome);
            Assert.IsTrue(item.BaseIsUncertain);
            Assert.IsFalse(TuningFolderUpdate.IsAutomatic(item));
            Assert.IsTrue(TuningFolderUpdate.NeedsCreator(item, new()));
        }
    }

    [TestMethod]
    public async Task AMergeWithADeclaredBaseIsAutomatic()
    {
        using var directory = new TemporaryDirectory();
        var (archive, first, second, mods) = await SetUpAsync(directory);
        using (archive)
        {
            var item = (await TuningFolderUpdate.AnalyzeAsync(archive, second, mods, declaredBase: first)).Packages[0].Update!.Overrides.Single();
            Assert.IsTrue(item.BaseIsDeclared);
            Assert.IsFalse(item.BaseIsUncertain);
            Assert.IsTrue(TuningFolderUpdate.IsAutomatic(item));
            Assert.IsFalse(TuningFolderUpdate.NeedsCreator(item, new()));
        }
    }

    [TestMethod]
    public async Task AnOverrideCloserToAnotherVersionThanTheDeclaredOneNeedsTheCreator()
    {
        using var directory = new TemporaryDirectory();
        var (archive, first, second, mods) = await SetUpAsync(directory);
        using (archive)
        {
            var item = (await TuningFolderUpdate.AnalyzeAsync(archive, second, mods, declaredBase: first)).Packages[1].Update!.Overrides.Single();
            Assert.IsTrue(item.BaseDisagrees);
            Assert.IsTrue(item.BaseIsUncertain);
            Assert.IsFalse(TuningFolderUpdate.IsAutomatic(item));
            Assert.IsTrue(TuningFolderUpdate.NeedsCreator(item, new()));
        }
    }

    [TestMethod]
    public async Task UpdatedCopiesAndAReportAreWrittenOutsideTheFolder()
    {
        using var directory = new TemporaryDirectory();
        var (archive, first, second, mods) = await SetUpAsync(directory);
        using (archive)
        {
            var original = File.ReadAllBytes(Path.Combine(mods, "Creator", "Merges.package"));
            var update = await TuningFolderUpdate.AnalyzeAsync(archive, second, mods, declaredBase: first);
            var output = directory.GetPath("Mods (updated)");
            var result = await update.WriteAsync(output, new());
            var write = result.Packages.Single();
            Assert.AreEqual(Path.Combine("Creator", "Merges.package"), write.RelativePath);
            Assert.AreEqual(Path.Combine(output, "Creator", "Merges.package"), write.WrittenPath);
            CollectionAssert.AreEqual(original, File.ReadAllBytes(Path.Combine(mods, "Creator", "Merges.package")));
            using (var updated = new DataBasePackedFile(write.WrittenPath!, true))
                Assert.AreEqual(TuningXml.GetCanonicalText(TuningXml.Parse(BuffHappy("0x0000BEEF", 3))), updated.GetText(happyKey));
            Assert.IsFalse(File.Exists(Path.Combine(output, "Updated.package")));
            var report = File.ReadAllLines(result.ReportPath!);
            Assert.AreEqual("Package,Tuning,Type,Instance,Outcome,Needs you,Done,Base,Base source,Base note,Conflicts,SimData,Message,SimData message", report[0]);
            Assert.AreEqual(3, report.Length);
            StringAssert.StartsWith(report[1], $"{Path.Combine("Creator", "Merges.package")},Buff_Happy,buff,200,Merges,,merged,1.0.0.1000,declared,");
            StringAssert.StartsWith(report[2], "Updated.package,Buff_Happy,buff,200,Up to date,yes,,1.0.0.1000,declared,\"another version is closer");
        }
    }

    [TestMethod]
    public async Task TheReportIsWrittenOnlyWhenAsked()
    {
        using var directory = new TemporaryDirectory();
        var (archive, first, second, mods) = await SetUpAsync(directory);
        using (archive)
        {
            var update = await TuningFolderUpdate.AnalyzeAsync(archive, second, mods, declaredBase: first);
            var output = directory.GetPath("Mods (updated)");
            var result = await update.WriteAsync(output, new(), false);
            Assert.IsNull(result.ReportPath);
            Assert.IsNotNull(result.Packages.Single().WrittenPath);
            Assert.AreEqual(0, Directory.GetFiles(output, "*.csv").Length);
        }
    }

    [TestMethod]
    public async Task AnExistingFileIsNotReplaced()
    {
        using var directory = new TemporaryDirectory();
        var (archive, first, second, mods) = await SetUpAsync(directory);
        using (archive)
        {
            var update = await TuningFolderUpdate.AnalyzeAsync(archive, second, mods, declaredBase: first);
            var output = directory.GetPath("Mods (updated)");
            var existing = directory.WriteFile("Mods (updated)/Creator/Merges.package", "mine");
            var write = (await update.WriteAsync(output, new())).Packages.Single();
            Assert.IsNull(write.WrittenPath);
            Assert.IsNotNull(write.Skipped);
            Assert.AreEqual("mine", File.ReadAllText(existing));
        }
    }

    [TestMethod]
    public async Task CopiesCannotBeWrittenInsideTheFolder()
    {
        using var directory = new TemporaryDirectory();
        var (archive, first, second, mods) = await SetUpAsync(directory);
        using (archive)
        {
            var update = await TuningFolderUpdate.AnalyzeAsync(archive, second, mods, declaredBase: first);
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => update.WriteAsync(mods, new()));
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => update.WriteAsync(Path.Combine(mods, "Updated"), new()));
        }
    }

    [TestMethod]
    public async Task TheReportCanBeWrittenOnItsOwn()
    {
        using var directory = new TemporaryDirectory();
        var (archive, first, second, mods) = await SetUpAsync(directory);
        using (archive)
        {
            var update = await TuningFolderUpdate.AnalyzeAsync(archive, second, mods, declaredBase: first);
            var options = new TuningFolderUpdateOptions();
            var path = directory.GetPath("Report.csv");
            await update.WriteReportAsync(path, options);
            var bytes = File.ReadAllBytes(path);
            CollectionAssert.AreEqual(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
            Assert.AreEqual(update.GetReport(options), Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));
            Assert.IsFalse(Directory.Exists(directory.GetPath("Mods (updated)")));
            var rows = update.GetReportRows(options);
            Assert.AreEqual(2, rows.Count);
            Assert.IsTrue(rows.All(row => row.Override is not null && row.Done.Length == 0));
        }
    }
}