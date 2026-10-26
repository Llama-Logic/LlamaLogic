namespace LlamaLogic.Packages.Benchmarking;

[MemoryDiagnoser]
public class PackageOpenBenchmarks
{
    string pristinePath;
    string temporaryDirectory;

    [ParamsSource(nameof(Fixtures))]
    public Fixture Fixture { get; set; }

    public IEnumerable<Fixture> Fixtures =>
        FixtureCatalog.All;

    [GlobalCleanup]
    public void GlobalCleanup() =>
        FixtureCatalog.Unstage(temporaryDirectory);

    [GlobalSetup]
    public void GlobalSetup() =>
        (temporaryDirectory, pristinePath) = FixtureCatalog.Stage(Fixture);

    [Benchmark]
    public int OpenAndEnumerateKeys()
    {
        using var package = new DataBasePackedFile(pristinePath);
        return package.GetKeys().Count;
    }

    [Benchmark]
    public async Task<int> OpenAndEnumerateKeysAsync()
    {
        await using var package = await DataBasePackedFile.FromPathAsync(pristinePath).ConfigureAwait(false);
        return (await package.GetKeysAsync().ConfigureAwait(false)).Count;
    }
}
