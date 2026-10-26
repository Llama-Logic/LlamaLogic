namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningPackageUpdateTests
{
    static readonly ResourceType buffType = TuningXml.GetResourceType("buff");
    static readonly ResourceType moodType = TuningXml.GetResourceType("mood");

    const string BuffSad = "<I c=\"Buff\" i=\"buff\" m=\"buffs.buff\" n=\"Buff_Sad\" s=\"201\"><T n=\"buff_name\">0x0000ABCE</T><T n=\"mood_type\">100</T><T n=\"mood_weight\">2</T></I>";

    static string BuffHappy(string name, int weight) =>
        $"<I c=\"Buff\" i=\"buff\" m=\"buffs.buff\" n=\"Buff_Happy\" s=\"200\"><T n=\"buff_name\">{name}</T><T n=\"mood_type\">100</T><T n=\"mood_weight\">{weight}</T></I>";

    static void WritePackage(string path, params (ResourceKey key, object content)[] resources)
    {
        using var package = new DataBasePackedFile();
        foreach (var (key, content) in resources)
            if (content is SimDataModel simData)
                package.Set(key, simData);
            else if (content is byte[] bytes)
                package.Set(key, bytes);
            else
                package.Set(key, (string)content);
        package.SaveAs(path);
    }

    static async Task<(TuningArchiveFile archive, TuningArchiveSnapshot first, TuningArchiveSnapshot second)> ImportAsync(TemporaryDirectory directory)
    {
        var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var first = (await archive.ImportFolderAsync(directory.WriteFiles("first", TuningArchiveFixtures.First), "1.0.0.1000")).Snapshot;
        var second = (await archive.ImportFolderAsync(directory.WriteFiles("second", TuningArchiveFixtures.Second), "1.0.1.1000")).Snapshot;
        return (archive, first, second);
    }

    [TestMethod]
    public async Task EachOverrideGetsAnOutcome()
    {
        using var directory = new TemporaryDirectory();
        var (archive, _, second) = await ImportAsync(directory);
        using (archive)
        {
            var moodHappy = TuningXml.GetCanonicalText(TuningXml.Parse(archive.GetText(archive.GetEntry(second, TuningArchiveResourceKind.Tuning, moodType, TuningArchiveFixtures.MoodHappyInstance)!)));
            var buffGlad = archive.GetText(archive.GetEntry(second, TuningArchiveResourceKind.Tuning, buffType, TuningArchiveFixtures.BuffGladInstance)!);
            var packagePath = directory.GetPath("Mod.package");
            WritePackage(packagePath,
                (new ResourceKey(buffType, 0, TuningArchiveFixtures.BuffHappyInstance), BuffHappy("0x0000BEEF", 1)),
                (new ResourceKey(moodType, 0, TuningArchiveFixtures.MoodHappyInstance), moodHappy.Replace("<E n=\"age\">ADULT</E>", "<E n=\"age\">BABY</E>", StringComparison.Ordinal)),
                (new ResourceKey(buffType, 0, TuningArchiveFixtures.BuffSadInstance), BuffSad),
                (new ResourceKey(buffType, 0, TuningArchiveFixtures.BuffGladInstance), buffGlad),
                (new ResourceKey(buffType, 0, 12345), BuffHappy("0x0000BEEF", 1).Replace("s=\"200\"", "s=\"12345\"", StringComparison.Ordinal)));
            var update = await TuningPackageUpdate.AnalyzeAsync(archive, second, packagePath);
            Assert.AreEqual(4, update.Overrides.Count);
            var happy = update.Overrides.Single(item => item.Name == "Buff_Happy");
            Assert.AreEqual(TuningPackageOverrideOutcome.Merged, happy.Outcome);
            Assert.AreEqual("1.0.0.1000", happy.Base!.GameVersion);
            Assert.IsTrue(happy.BaseMayBeOlder);
            Assert.AreEqual(TuningXml.GetCanonicalText(TuningXml.Parse(BuffHappy("0x0000BEEF", 3))), happy.Merge!.Text);
            Assert.AreEqual(TuningPackageOverrideOutcome.UpToDate, update.Overrides.Single(item => item.Name == "Mood_Happy").Outcome);
            Assert.AreEqual(TuningPackageOverrideOutcome.RemovedFromGame, update.Overrides.Single(item => item.Name == "Buff_Sad").Outcome);
            Assert.AreEqual(TuningPackageOverrideOutcome.MatchesGame, update.Overrides.Single(item => item.Name == "Buff_Glad").Outcome);
            Assert.AreEqual(TuningPackageSimDataState.NotChecked, happy.SimData);
            CollectionAssert.AreEquivalent(new[] { happy.Key }, update.GetRecommendedTuning().Keys.ToArray());
        }
    }

    [TestMethod]
    public async Task AnOverrideThatIsACopyOfAnOlderVersionIsFound()
    {
        using var directory = new TemporaryDirectory();
        var (archive, first, second) = await ImportAsync(directory);
        using (archive)
        {
            var packagePath = directory.GetPath("Mod.package");
            WritePackage(packagePath, (new ResourceKey(buffType, 0, TuningArchiveFixtures.BuffHappyInstance), archive.GetText(archive.GetEntry(first, TuningArchiveResourceKind.Tuning, buffType, TuningArchiveFixtures.BuffHappyInstance)!)));
            var item = (await TuningPackageUpdate.AnalyzeAsync(archive, second, packagePath)).Overrides.Single();
            Assert.AreEqual(TuningPackageOverrideOutcome.MatchesOlderGame, item.Outcome);
            Assert.AreEqual(0, item.BaseDistance);
            Assert.IsFalse(item.BaseMayBeOlder);
        }
    }

    [TestMethod]
    public async Task AChangeTheGameAlsoMadeIsAConflict()
    {
        using var directory = new TemporaryDirectory();
        var (archive, _, second) = await ImportAsync(directory);
        using (archive)
        {
            var packagePath = directory.GetPath("Mod.package");
            WritePackage(packagePath, (new ResourceKey(buffType, 0, TuningArchiveFixtures.BuffHappyInstance), BuffHappy("0x0000ABCD", 5)));
            var item = (await TuningPackageUpdate.AnalyzeAsync(archive, second, packagePath)).Overrides.Single();
            Assert.IsTrue(item.BaseIsAmbiguous);
            Assert.AreEqual("1.0.0.1000", item.Base!.GameVersion);
            Assert.AreEqual(TuningPackageOverrideOutcome.Conflicted, item.Outcome);
            Assert.AreEqual("mood_weight", item.Merge!.Conflicts.Single().Path);
        }
    }

    [TestMethod]
    public async Task AnUnreadableOverrideIsReported()
    {
        using var directory = new TemporaryDirectory();
        var (archive, _, second) = await ImportAsync(directory);
        using (archive)
        {
            var packagePath = directory.GetPath("Mod.package");
            WritePackage(packagePath, (new ResourceKey(buffType, 0, TuningArchiveFixtures.BuffHappyInstance), "<I n=\"Buff_Happy\""));
            Assert.AreEqual(TuningPackageOverrideOutcome.Unreadable, (await TuningPackageUpdate.AnalyzeAsync(archive, second, packagePath)).Overrides.Single().Outcome);
        }
    }

    [TestMethod]
    public async Task TheUpdatedPackageHasTheMergeAndEverythingElseUnchanged()
    {
        using var directory = new TemporaryDirectory();
        var (archive, _, second) = await ImportAsync(directory);
        using (archive)
        {
            var packagePath = directory.GetPath("Mod.package");
            var otherKey = new ResourceKey(ResourceType.StringTable, 0x80000000, 0x0000000000000042);
            var otherContent = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
            var happyKey = new ResourceKey(buffType, 0, TuningArchiveFixtures.BuffHappyInstance);
            var sadKey = new ResourceKey(buffType, 0, TuningArchiveFixtures.BuffSadInstance);
            WritePackage(packagePath, (happyKey, BuffHappy("0x0000BEEF", 1)), (sadKey, BuffSad), (otherKey, otherContent));
            var originalBytes = File.ReadAllBytes(packagePath);
            var update = await TuningPackageUpdate.AnalyzeAsync(archive, second, packagePath);
            var tuning = update.GetRecommendedTuning().ToDictionary(pair => pair.Key, pair => pair.Value);
            tuning[sadKey] = null;
            var updatedPath = directory.GetPath("Mod (updated).package");
            var changes = await update.WriteAsync(updatedPath, tuning);
            CollectionAssert.AreEquivalent(new[] { TuningPackageUpdateChangeKind.TuningReplaced, TuningPackageUpdateChangeKind.TuningRemoved }, changes.Select(change => change.Kind).ToArray());
            CollectionAssert.AreEqual(originalBytes, File.ReadAllBytes(packagePath));
            using var updated = new DataBasePackedFile(updatedPath, true);
            CollectionAssert.AreEquivalent(new[] { happyKey, otherKey }, updated.Keys.ToArray());
            Assert.AreEqual(TuningXml.GetCanonicalText(TuningXml.Parse(BuffHappy("0x0000BEEF", 3))), updated.GetText(happyKey));
            CollectionAssert.AreEqual(otherContent, updated.Get(otherKey).ToArray());
        }
    }

    [TestMethod]
    public async Task TheUpdatedPackageCannotReplaceThePackage()
    {
        using var directory = new TemporaryDirectory();
        var (archive, _, second) = await ImportAsync(directory);
        using (archive)
        {
            var packagePath = directory.GetPath("Mod.package");
            WritePackage(packagePath, (new ResourceKey(buffType, 0, TuningArchiveFixtures.BuffHappyInstance), BuffHappy("0x0000BEEF", 1)));
            var update = await TuningPackageUpdate.AnalyzeAsync(archive, second, packagePath);
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => update.WriteAsync(packagePath, update.GetRecommendedTuning()));
        }
    }

    static TuningDescriptionSet OpenExportingDescriptions()
    {
        var descriptions = TuningArchiveFixtures.Descriptions.ToDictionary(pair => pair.Key, pair => pair.Value);
        descriptions["Buffs/Descriptions/Buff.tdesc"] = descriptions["Buffs/Descriptions/Buff.tdesc"].Replace("name=\"mood_weight\"", "name=\"mood_weight\" export_modes=\"client_binary,server_xml\"", StringComparison.Ordinal);
        return TuningArchiveFixtures.OpenDescriptions(descriptions);
    }

    static async Task<(TuningPackageUpdate update, IReadOnlyList<TuningPackageUpdateChange> changes, string updatedPath)> UpdateSimDataAsync(TemporaryDirectory directory, TuningArchiveFile archive, TuningDescriptionSet set, SimDataModel? modSimData, string? modText = null, string? gameSimDataTuning = null)
    {
        var simDataKey = new ResourceKey(ResourceType.SimData, 0x0017E8F6, TuningArchiveFixtures.BuffSadInstance);
        var snapshot = (await archive.ImportFolderAsync(directory.WriteFiles("tuning", new Dictionary<string, string>
        {
            ["BG/Buff_Sad.xml"] = BuffSad,
            ["BG/545AC67A!0017E8F6!00000000000000C9.Buff_Sad.SimData.xml"] = new SimDataGenerator(set).Generate(TuningXml.Parse(gameSimDataTuning ?? BuffSad)).ToXml().ToString()
        }), "1.0.1.1000")).Snapshot;
        var packagePath = directory.GetPath("Mod.package");
        var resources = new List<(ResourceKey, object)> { (new ResourceKey(buffType, 0, TuningArchiveFixtures.BuffSadInstance), modText ?? BuffSad.Replace(">2<", ">3<", StringComparison.Ordinal)) };
        if (modSimData is not null)
            resources.Add((simDataKey, modSimData));
        WritePackage(packagePath, [.. resources]);
        var update = await TuningPackageUpdate.AnalyzeAsync(archive, snapshot, packagePath, TuningSimDataDrafter.Create(archive, snapshot, set));
        var updatedPath = directory.GetPath("Mod (updated).package");
        return (update, await update.WriteAsync(updatedPath, update.GetRecommendedTuning()), updatedPath);
    }

    static string GetWeight(SimDataModel simData) =>
        simData.ToXml().Descendants("T").Single(element => element.Attribute("name")?.Value == "mood_weight").Value;

    [TestMethod]
    public async Task MissingSimDataIsAdded()
    {
        using var set = OpenExportingDescriptions();
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var (update, changes, updatedPath) = await UpdateSimDataAsync(directory, archive, set, null);
        var item = update.Overrides.Single();
        Assert.AreEqual(TuningPackageOverrideOutcome.UpToDate, item.Outcome);
        Assert.AreEqual(TuningPackageSimDataState.Missing, item.SimData);
        Assert.AreEqual(TuningPackageUpdateChangeKind.SimDataAdded, changes.Single().Kind);
        using var updated = new DataBasePackedFile(updatedPath, true);
        Assert.AreEqual("3", GetWeight(updated.GetSimData(new ResourceKey(ResourceType.SimData, 0x0017E8F6, TuningArchiveFixtures.BuffSadInstance))));
    }

    [TestMethod]
    public async Task StaleSimDataIsReplaced()
    {
        using var set = OpenExportingDescriptions();
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var stale = new SimDataGenerator(set).Generate(TuningXml.Parse(BuffSad));
        var (update, changes, updatedPath) = await UpdateSimDataAsync(directory, archive, set, stale);
        Assert.AreEqual(TuningPackageSimDataState.Stale, update.Overrides.Single().SimData);
        Assert.AreEqual(TuningPackageUpdateChangeKind.SimDataReplaced, changes.Single().Kind);
        using var updated = new DataBasePackedFile(updatedPath, true);
        Assert.AreEqual("3", GetWeight(updated.GetSimData(new ResourceKey(ResourceType.SimData, 0x0017E8F6, TuningArchiveFixtures.BuffSadInstance))));
    }

    [TestMethod]
    public async Task CurrentSimDataIsLeftAlone()
    {
        using var set = OpenExportingDescriptions();
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var current = new SimDataGenerator(set).Generate(TuningXml.Parse(BuffSad.Replace(">2<", ">3<", StringComparison.Ordinal)));
        var (update, changes, _) = await UpdateSimDataAsync(directory, archive, set, current);
        Assert.AreEqual(TuningPackageSimDataState.Current, update.Overrides.Single().SimData);
        Assert.AreEqual(0, changes.Count);
    }

    [TestMethod]
    public async Task UnneededSimDataIsRemoved()
    {
        using var set = OpenExportingDescriptions();
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var modSimData = new SimDataGenerator(set).Generate(TuningXml.Parse(BuffSad));
        var (update, changes, updatedPath) = await UpdateSimDataAsync(directory, archive, set, modSimData, BuffSad.Replace("0x0000ABCE", "0x0000ABCF", StringComparison.Ordinal));
        Assert.AreEqual(TuningPackageSimDataState.Unneeded, update.Overrides.Single().SimData);
        Assert.AreEqual(TuningPackageUpdateChangeKind.SimDataRemoved, changes.Single().Kind);
        using var updated = new DataBasePackedFile(updatedPath, true);
        Assert.IsFalse(updated.ContainsKey(new ResourceKey(ResourceType.SimData, 0x0017E8F6, TuningArchiveFixtures.BuffSadInstance)));
    }

    [TestMethod]
    public async Task SimDataThatSeemsUnneededIsKeptWhenTheGamesCannotBeReproduced()
    {
        using var set = OpenExportingDescriptions();
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var modSimData = new SimDataGenerator(set).Generate(TuningXml.Parse(BuffSad));
        var (update, changes, updatedPath) = await UpdateSimDataAsync(directory, archive, set, modSimData, BuffSad.Replace("0x0000ABCE", "0x0000ABCF", StringComparison.Ordinal), BuffSad.Replace(">2<", ">5<", StringComparison.Ordinal));
        Assert.AreEqual(TuningPackageSimDataState.Unverifiable, update.Overrides.Single().SimData);
        Assert.AreEqual(TuningPackageUpdateChangeKind.SimDataUnverifiable, changes.Single().Kind);
        using var updated = new DataBasePackedFile(updatedPath, true);
        Assert.IsTrue(updated.ContainsKey(new ResourceKey(ResourceType.SimData, 0x0017E8F6, TuningArchiveFixtures.BuffSadInstance)));
    }
}