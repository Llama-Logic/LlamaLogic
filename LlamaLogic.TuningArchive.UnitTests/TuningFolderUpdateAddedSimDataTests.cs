namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningFolderUpdateAddedSimDataTests
{
    const string BuffSad = "<I c=\"Buff\" i=\"buff\" m=\"buffs.buff\" n=\"Buff_Sad\" s=\"201\"><T n=\"buff_name\">0x0000ABCE</T><T n=\"mood_type\">100</T><T n=\"mood_weight\">2</T></I>";
    const ulong addedInstance = 5000;
    static readonly ResourceType buffType = TuningXml.GetResourceType("buff");
    static readonly ResourceKey simDataKey = new(ResourceType.SimData, 0x0017E8F6, addedInstance);
    static readonly ResourceKey tuningKey = new(buffType, 0, addedInstance);

    static string AddedBuff(string name = "0x0000BEEF") =>
        $"<I c=\"Buff\" i=\"buff\" m=\"buffs.buff\" n=\"Buff_Added\" s=\"{addedInstance}\"><T n=\"buff_name\">{name}</T><T n=\"mood_type\">100</T><T n=\"mood_weight\">7</T></I>";

    static TuningDescriptionSet OpenDescriptions(params string[] exported)
    {
        var descriptions = TuningArchiveFixtures.Descriptions.ToDictionary(pair => pair.Key, pair => pair.Value);
        foreach (var name in exported)
            descriptions["Buffs/Descriptions/Buff.tdesc"] = descriptions["Buffs/Descriptions/Buff.tdesc"].Replace($"name=\"{name}\"", $"name=\"{name}\" export_modes=\"client_binary,server_xml\"", StringComparison.Ordinal);
        return TuningArchiveFixtures.OpenDescriptions(descriptions);
    }

    static TuningDescriptionSet OpenNewDescriptions() =>
        OpenDescriptions("buff_name", "mood_weight");

    static SimDataModel Generate(TuningDescriptionSet set, string tuning) =>
        new SimDataGenerator(set).Generate(TuningXml.Parse(tuning));

    static SimDataModel GenerateWithoutMoodWeight(string tuning)
    {
        using var set = OpenDescriptions("buff_name");
        return Generate(set, tuning);
    }

    static object? GetValue(SimDataModel simData, string column) =>
        simData.Instances.Single().Object[column];

    static async Task<TuningFolderUpdate> AnalyzeAsync(TemporaryDirectory directory, TuningArchiveFile archive, TuningDescriptionSet set, SimDataModel modSimData, bool scriptBeside = false, string? gameSimDataTuning = null, string gameTuning = BuffSad, string? addedTuning = null)
    {
        var snapshot = (await archive.ImportFolderAsync(directory.WriteFiles("tuning", new Dictionary<string, string>
        {
            ["BG/Buff_Sad.xml"] = gameTuning,
            ["BG/545AC67A!0017E8F6!00000000000000C9.Buff_Sad.SimData.xml"] = Generate(set, gameSimDataTuning ?? gameTuning).ToXml().ToString()
        }), "1.0.1.1000")).Snapshot;
        var mods = directory.GetPath("Mods");
        Directory.CreateDirectory(Path.Combine(mods, "Creator"));
        using (var package = new DataBasePackedFile())
        {
            package.Set(tuningKey, addedTuning ?? AddedBuff());
            package.Set(simDataKey, modSimData);
            package.SaveAs(Path.Combine(mods, "Creator", "Added.package"));
        }
        if (scriptBeside)
            directory.WriteFile("Mods/Creator/Added.ts4script", "script");
        return await TuningFolderUpdate.AnalyzeAsync(archive, snapshot, mods, TuningSimDataDrafter.Create(archive, snapshot, set));
    }

    static async Task<SimDataModel> WriteAsync(TemporaryDirectory directory, TuningFolderUpdate update, TuningFolderUpdateOptions options)
    {
        var result = await update.WriteAsync(directory.GetPath("Mods (updated)"), options, false);
        var write = result.Packages.Single();
        Assert.AreEqual(TuningPackageUpdateChangeKind.SimDataRepaired, write.Changes.Single().Kind);
        using var updated = new DataBasePackedFile(write.WrittenPath!, true);
        using var original = new DataBasePackedFile(Path.Combine(update.Folder, write.RelativePath), true);
        CollectionAssert.AreEqual(original.Get(tuningKey).ToArray(), updated.Get(tuningKey).ToArray());
        return updated.GetSimData(simDataKey);
    }

    [TestMethod]
    public async Task SimDataMissingAColumnTheNewVersionGivesIsFoundInAPackageWithoutOverrides()
    {
        using var set = OpenNewDescriptions();
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var update = await AnalyzeAsync(directory, archive, set, GenerateWithoutMoodWeight(AddedBuff()));
        var package = update.Packages.Single();
        Assert.AreEqual(0, package.Update!.Overrides.Count);
        var item = package.AddedSimData.Single();
        Assert.AreEqual(TuningPackageAddedSimDataProblems.MissingColumns, item.Problems);
        CollectionAssert.AreEqual(new[] { "Buff.mood_weight" }, item.MissingColumns.ToArray());
        Assert.IsFalse(TuningFolderUpdate.NeedsCreator(item, new()));
        Assert.IsTrue(TuningFolderUpdate.NeedsCreator(item, new(AddedSimDataFixes: TuningPackageAddedSimDataFixes.None)));
    }

    [TestMethod]
    public async Task MissingColumnsAreAddedKeepingTheModsValues()
    {
        using var set = OpenNewDescriptions();
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var modSimData = GenerateWithoutMoodWeight(AddedBuff("0x0000F00D"));
        var update = await AnalyzeAsync(directory, archive, set, modSimData);
        var item = update.Packages.Single().AddedSimData.Single();
        Assert.AreEqual(TuningPackageAddedSimDataProblems.MissingColumns | TuningPackageAddedSimDataProblems.Values, item.Problems);
        Assert.AreEqual(TuningPackageAddedSimDataProblems.Values, item.Plan(TuningPackageAddedSimDataFixes.AddMissingColumns).Remaining);
        Assert.IsTrue(TuningFolderUpdate.NeedsCreator(item, new()));
        var repaired = await WriteAsync(directory, update, new());
        Assert.AreEqual(GetValue(modSimData, "buff_name"), GetValue(repaired, "buff_name"));
        Assert.AreEqual(GetValue(Generate(set, AddedBuff()), "mood_weight"), GetValue(repaired, "mood_weight"));
        Assert.IsTrue(repaired.Schemas.All(schema => schema.Hash == schema.ComputedHash));
    }

    [TestMethod]
    public async Task AColumnTheGeneratorDoesNotReproduceInTheGamesSimDataIsNotAdded()
    {
        using var set = OpenNewDescriptions();
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var update = await AnalyzeAsync(directory, archive, set, GenerateWithoutMoodWeight(AddedBuff()), gameSimDataTuning: BuffSad.Replace(">2<", ">9<", StringComparison.Ordinal));
        var item = update.Packages.Single().AddedSimData.Single();
        var plan = item.Plan(TuningPackageAddedSimDataFixes.AddMissingColumns);
        Assert.IsFalse(plan.ChangesSimData);
        StringAssert.Contains(plan.Message, "Buff.mood_weight can't be generated reliably");
        Assert.IsTrue(TuningFolderUpdate.NeedsCreator(item, new()));
    }

    [TestMethod]
    public async Task ExtraColumnsAreRemovedOnlyWhenChosen()
    {
        using var set = OpenNewDescriptions();
        using var old = OpenDescriptions("buff_name", "mood_type", "mood_weight");
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var update = await AnalyzeAsync(directory, archive, set, Generate(old, AddedBuff()));
        var item = update.Packages.Single().AddedSimData.Single();
        Assert.AreEqual(TuningPackageAddedSimDataProblems.ExtraColumns, item.Problems);
        CollectionAssert.AreEqual(new[] { "Buff.mood_type" }, item.ExtraColumns.ToArray());
        Assert.IsFalse(item.Plan(TuningPackageAddedSimDataFixes.AddMissingColumns).ChangesSimData);
        var repaired = await WriteAsync(directory, update, new(AddedSimDataFixes: TuningPackageAddedSimDataFixes.RemoveExtraColumns));
        Assert.IsNull(repaired.Schemas.Single().GetColumnIndex("mood_type"));
    }

    [TestMethod]
    public async Task ExtraColumnsAreKeptWhenAScriptModIsBeside()
    {
        using var set = OpenNewDescriptions();
        using var old = OpenDescriptions("buff_name", "mood_type", "mood_weight");
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var update = await AnalyzeAsync(directory, archive, set, Generate(old, AddedBuff()), scriptBeside: true);
        var item = update.Packages.Single().AddedSimData.Single();
        Assert.IsTrue(item.HasScriptBeside);
        var plan = item.Plan(TuningPackageAddedSimDataFixes.RemoveExtraColumns);
        Assert.IsFalse(plan.ChangesSimData);
        Assert.AreEqual(TuningPackageAddedSimDataProblems.ExtraColumns, plan.Remaining);
    }

    [TestMethod]
    public async Task AMismatchedSchemaHashIsCorrectedOnlyWhenChosen()
    {
        using var set = OpenNewDescriptions();
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var document = Generate(set, AddedBuff()).ToXml();
        document.Descendants("Schema").Single().SetAttributeValue("schema_hash", "0x12345678");
        var update = await AnalyzeAsync(directory, archive, set, SimDataModel.FromXml(document));
        var item = update.Packages.Single().AddedSimData.Single();
        Assert.AreEqual(TuningPackageAddedSimDataProblems.MismatchedHashes, item.Problems);
        Assert.IsFalse(item.Plan(TuningPackageAddedSimDataFixes.AddMissingColumns).ChangesSimData);
        var repaired = await WriteAsync(directory, update, new(AddedSimDataFixes: TuningPackageAddedSimDataFixes.CorrectSchemaHashes));
        var schema = repaired.Schemas.Single();
        Assert.AreEqual(schema.ComputedHash, schema.Hash);
    }

    [TestMethod]
    public async Task MatchingTheTuningRebuildsTheSimData()
    {
        using var set = OpenNewDescriptions();
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var update = await AnalyzeAsync(directory, archive, set, Generate(set, AddedBuff("0x0000F00D")));
        var item = update.Packages.Single().AddedSimData.Single();
        Assert.AreEqual(TuningPackageAddedSimDataProblems.Values, item.Problems);
        Assert.IsFalse(item.Plan(TuningPackageAddedSimDataFixes.AddMissingColumns).ChangesSimData);
        Assert.AreEqual(TuningPackageAddedSimDataProblems.None, item.Plan(TuningPackageAddedSimDataFixes.MatchTuning).Remaining);
        var repaired = await WriteAsync(directory, update, new(AddedSimDataFixes: TuningPackageAddedSimDataFixes.MatchTuning));
        Assert.AreEqual(GetValue(Generate(set, AddedBuff()), "buff_name"), GetValue(repaired, "buff_name"));
    }

    [TestMethod]
    public async Task TheOrderOfASetsItemsDoesNotMatter()
    {
        var descriptions = TuningArchiveFixtures.Descriptions.ToDictionary(pair => pair.Key, pair => pair.Value);
        descriptions["Buffs/Descriptions/Buff.tdesc"] = descriptions["Buffs/Descriptions/Buff.tdesc"].Replace("</Instance>", "<TunableList name=\"tags\" class=\"TunableSet\" display=\"Tags\" muid=\"4545454545454545\" Deprecated=\"False\" filter=\"0\" group=\"General\" unique_entries=\"true\" export_modes=\"client_binary,server_xml\"><Tunable type=\"int\" class=\"Tunable\" default=\"0\" muid=\"4646464646464646\" Deprecated=\"False\" filter=\"0\" group=\"General\" /></TunableList></Instance>", StringComparison.Ordinal);
        using var set = TuningArchiveFixtures.OpenDescriptions(descriptions);
        static string withTags(string tuning, string tags) =>
            tuning.Replace("</I>", $"<L n=\"tags\">{tags}</L></I>", StringComparison.Ordinal);
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var update = await AnalyzeAsync(directory, archive, set, Generate(set, withTags(AddedBuff(), "<T>4</T><T>3</T>")), gameTuning: withTags(BuffSad, "<T>1</T><T>2</T>"), addedTuning: withTags(AddedBuff(), "<T>3</T><T>4</T>"));
        Assert.AreEqual(0, update.Packages.Count);
    }

    [TestMethod]
    public async Task AValueTheTuningLeavesOutIsAddedToTheTuningOnlyWhenChosen()
    {
        using var set = OpenNewDescriptions();
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var leftOut = AddedBuff().Replace("<T n=\"mood_weight\">7</T>", string.Empty, StringComparison.Ordinal);
        var update = await AnalyzeAsync(directory, archive, set, Generate(set, AddedBuff()), addedTuning: leftOut);
        var item = update.Packages.Single().AddedSimData.Single();
        Assert.AreEqual(TuningPackageAddedSimDataProblems.Values, item.Problems);
        CollectionAssert.AreEqual(new[] { "mood_weight (7)" }, item.LeftOutTunables.ToArray());
        Assert.IsFalse(item.Plan(TuningPackageAddedSimDataFixes.AddMissingColumns).ChangesTuning);
        var plan = item.Plan(TuningPackageAddedSimDataFixes.AmendTuning);
        Assert.IsTrue(plan.ChangesTuning);
        Assert.IsFalse(plan.ChangesSimData);
        Assert.AreEqual(TuningPackageAddedSimDataProblems.None, plan.Remaining);
        var result = await update.WriteAsync(directory.GetPath("Mods (updated)"), new(AddedSimDataFixes: TuningPackageAddedSimDataFixes.AmendTuning), false);
        var write = result.Packages.Single();
        Assert.AreEqual(TuningPackageUpdateChangeKind.TuningAmended, write.Changes.Single().Kind);
        using var updated = new DataBasePackedFile(write.WrittenPath!, true);
        Assert.AreEqual(AddedBuff(), updated.GetText(tuningKey));
    }

    [TestMethod]
    public async Task CurrentSimDataIsNotListed()
    {
        using var set = OpenNewDescriptions();
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var update = await AnalyzeAsync(directory, archive, set, Generate(set, AddedBuff()));
        Assert.AreEqual(0, update.Packages.Count);
    }
}