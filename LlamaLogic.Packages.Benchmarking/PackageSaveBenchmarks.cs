namespace LlamaLogic.Packages.Benchmarking;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 2, iterationCount: 10)]
public class PackageSaveBenchmarks
{
    ReadOnlyMemory<byte> mutationContent;
    ResourceKey mutationKey;
    string outputPath;
    string pristinePath;
    string temporaryDirectory;
    string workingPath;

    [ParamsSource(nameof(Fixtures))]
    public Fixture Fixture { get; set; }

    public IEnumerable<Fixture> Fixtures =>
        FixtureCatalog.All;

    [GlobalCleanup]
    public void GlobalCleanup() =>
        FixtureCatalog.Unstage(temporaryDirectory);

    [GlobalSetup]
    public void GlobalSetup()
    {
        (temporaryDirectory, pristinePath) = FixtureCatalog.Stage(Fixture);
        workingPath = Path.Combine(temporaryDirectory, "working.package");
        outputPath = Path.Combine(temporaryDirectory, "output.package");
        using var package = new DataBasePackedFile(pristinePath);
        mutationKey = package.GetKeys().First(key => Enum.IsDefined(key.Type));
        mutationContent = package.Get(mutationKey);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        File.Copy(pristinePath, workingPath, true);
        if (File.Exists(outputPath))
            File.Delete(outputPath);
    }

    [Benchmark]
    public void Save()
    {
        using var package = new DataBasePackedFile(workingPath, false);
        package.Save();
    }

    [Benchmark]
    public void SaveAs()
    {
        using var package = new DataBasePackedFile(workingPath);
        package.SaveAs(outputPath);
    }

    [Benchmark]
    public void SetOneThenSave()
    {
        using var package = new DataBasePackedFile(workingPath, false);
        package.Set(mutationKey, mutationContent);
        package.Save();
    }

    [Benchmark]
    public void SetOneThenSaveAs()
    {
        using var package = new DataBasePackedFile(workingPath);
        package.Set(mutationKey, mutationContent);
        package.SaveAs(outputPath);
    }

    [Benchmark]
    public async Task SetOneThenSaveAsync()
    {
        await using var package = await DataBasePackedFile.FromPathAsync(workingPath, false).ConfigureAwait(false);
        await package.SetAsync(mutationKey, mutationContent).ConfigureAwait(false);
        await package.SaveAsync().ConfigureAwait(false);
    }
}
