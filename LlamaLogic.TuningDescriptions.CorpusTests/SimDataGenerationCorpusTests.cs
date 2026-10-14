namespace LlamaLogic.TuningDescriptions.CorpusTests;

[TestClass]
public class SimDataGenerationCorpusTests
{
    public TestContext TestContext { get; set; } = null!;

    public static string GetDisplayName(MethodInfo methodInfo, object[] data) =>
        (string)data[0];

    [TestMethod]
    [DynamicData(nameof(GameCorpus.Packages), typeof(GameCorpus), DynamicDataDisplayName = nameof(GetDisplayName))]
    public async Task GeneratedSimDataMatchesTheGame(string pack, string packagePath)
    {
        using var set = TuningDescriptionSet.FromZip(GameCorpus.TuningDescriptionsPath!);
        var generator = new SimDataGenerator(set);
        generator.AddResourceKeyConversion((ResourceType)0x2F7D0004, 0, (ResourceType)0x00B2D882, 0);
        generator.AddResourceKeyConversion((ResourceType)0x39B2AA4A, 0, (ResourceType)0xFD04E3BE, 0x001407EC);
        var packId = (uint)(set.GetEnum("sims4-common.Pack")?.GetEnumValue(pack) ?? throw new InvalidOperationException($"The tuning descriptions do not have pack {pack}"));
        await using (var basePackage = await DataBasePackedFile.FromPathAsync(GameCorpus.BasePackagePath).ConfigureAwait(false))
            foreach (var key in await basePackage.GetKeysAsync().ConfigureAwait(false))
                if (key.Type is ResourceType.CombinedTuning)
                    generator.AddTuning(await basePackage.GetModelAsync<CombinedTuningModel>(key).ConfigureAwait(false));
                else if (key.Type is ResourceType.SimData && await TryGetSimDataAsync(basePackage, key).ConfigureAwait(false) is { } simData)
                    generator.AddLayouts(simData.Model);
        await using var package = await DataBasePackedFile.FromPathAsync(packagePath).ConfigureAwait(false);
        var keys = await package.GetKeysAsync().ConfigureAwait(false);
        var tunings = new List<CombinedTuningModelTuning>();
        foreach (var key in keys.Where(key => key.Type is ResourceType.CombinedTuning))
        {
            var combinedTuning = await package.GetModelAsync<CombinedTuningModel>(key).ConfigureAwait(false);
            generator.AddTuning(combinedTuning);
            tunings.AddRange(combinedTuning.GetTunings());
        }
        var tuningsByInstance = tunings.GroupBy(tuning => tuning.Instance).ToDictionary(group => group.Key, group => group.ToList());
        var problems = new ProblemLog();
        int identical = 0, orderOnly = 0, withoutTuning = 0, withAmbiguousTuning = 0;
        foreach (var key in keys.Where(key => key.Type is ResourceType.SimData))
        {
            if (await TryGetSimDataAsync(package, key).ConfigureAwait(false) is not { } simData)
                continue;
            if (!tuningsByInstance.TryGetValue(key.FullInstance, out var candidates))
            {
                ++withoutTuning;
                continue;
            }
            if (candidates.Count > 1)
            {
                ++withAmbiguousTuning;
                continue;
            }
            SimDataModel generated;
            try
            {
                generated = generator.Generate(candidates[0].Document);
            }
            catch (SimDataGenerationException ex)
            {
                problems.Add("not generated", $"{key} {candidates[0].Name}: {ex.Message}");
                continue;
            }
            generated.Pack = packId;
            if (generated.Encode().Span.SequenceEqual(simData.Data.Span))
            {
                ++identical;
                continue;
            }
            switch (SimDataComparison.Compare(generated, simData.Model))
            {
                case null:
                    problems.Add("different bytes", $"{key} {candidates[0].Name}");
                    break;
                case { Difference: SimDataComparison.OrderOnly }:
                    ++orderOnly;
                    break;
                case { } difference:
                    problems.Add("different", $"{key} {candidates[0].Name} at {difference.Path}: {difference.Difference}");
                    break;
            }
        }
        TestContext.WriteLine($"{identical} identical, {orderOnly} with a list in a different order, {withoutTuning} without tuning, {withAmbiguousTuning} with ambiguous tuning");
        if (problems.Count > 0)
            Assert.Fail(problems.ToString());
    }

    static async Task<(ReadOnlyMemory<byte> Data, SimDataModel Model)?> TryGetSimDataAsync(DataBasePackedFile package, ResourceKey key)
    {
        ReadOnlyMemory<byte> data;
        try
        {
            data = await package.GetAsync(key).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        return data.Length >= 4 && data.Span[..4].SequenceEqual("DATA"u8) ? (data, SimDataModel.Decode(data)) : null;
    }
}
