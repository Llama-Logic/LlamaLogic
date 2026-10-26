namespace LlamaLogic.EpicTuningTime.Benchmarking;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 2, iterationCount: 10)]
public class PropertyPanelBenchmarks
{
    OpenedFixture opened;

    [ParamsSource(nameof(Fixtures))]
    public Fixture Fixture { get; set; }

    public IEnumerable<Fixture> Fixtures =>
        FixtureCatalog.All;

    [GlobalCleanup]
    public void GlobalCleanup() =>
        opened?.Dispose();

    [GlobalSetup]
    public void GlobalSetup() =>
        opened = FixtureCatalog.Open(Fixture);

    [Benchmark]
    public int Analyze() =>
        TuningAnalysis.Analyze(opened.Text, opened.Cache).Nodes.Count;

    [Benchmark]
    public int BuildAnnotations() =>
        AnnotationBuilder.Build(opened.Text, opened.Analysis, opened.Entries, opened.Strings, OpenedFixture.GetTypeName).annotations.Count;

    [Benchmark]
    public int BuildPropertyTree() =>
        PropertyTree.Build(opened.Analysis, opened.Entries, opened.Strings, false).Count;

    [Benchmark]
    public int BuildPropertyTreeWithUnset() =>
        PropertyTree.Build(opened.Analysis, opened.Entries, opened.Strings, true).Count;
}
