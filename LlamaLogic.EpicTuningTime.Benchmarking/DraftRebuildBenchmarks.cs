namespace LlamaLogic.EpicTuningTime.Benchmarking;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 2, iterationCount: 10)]
public class DraftRebuildBenchmarks
{
    TuningDraft draft;
    OpenedFixture opened;
    IReadOnlyList<TuningDraftTarget> targets;

    [ParamsSource(nameof(Fixtures))]
    public Fixture Fixture { get; set; }

    public IEnumerable<Fixture> Fixtures =>
        FixtureCatalog.All;

    [GlobalCleanup]
    public void GlobalCleanup() =>
        opened?.Dispose();

    [GlobalSetup]
    public void GlobalSetup()
    {
        opened = FixtureCatalog.Open(Fixture);
        draft = opened.CreateDraft();
        draft.SetValue(opened.DeepestValue.Path, opened.ChangedValue);
        draft.Remove($"{opened.LargestList.Path}[0]");
        targets = [new TuningDraftTarget(draft.Entry, draft.Original, draft.Draft)];
    }

    [Benchmark]
    public int BuildDraftAnnotations() =>
        AnnotationBuilder.Build(draft.Text, draft.Draft, opened.Entries, opened.Strings, OpenedFixture.GetTypeName).annotations.Count;

    [Benchmark]
    public int BuildDraftPropertyTree() =>
        PropertyTree.Build(draft.Draft, opened.Entries, opened.Strings, false, draft.ChangedPaths).Count;

    [Benchmark]
    public int BuildOriginalAnnotations() =>
        AnnotationBuilder.Build(draft.OriginalText, draft.Original, opened.Entries, opened.Strings, OpenedFixture.GetTypeName).annotations.Count;

    [Benchmark]
    public int GenerateCoreLibrary() =>
        TuningCoreLibraryDraft.Generate(targets, "benchmark_core_library").Source.Length;

    [Benchmark]
    public int GeneratePython() =>
        TuningPythonDraft.Generate(targets, opened.Snapshot.GameVersion, "benchmark.log").Source.Length;

    [Benchmark]
    public int GenerateXmlInjector() =>
        TuningXmlInjectorDraft.Generate(targets, "benchmark_xml_injector").Source.Length;

    [Benchmark]
    public int Rebuild()
    {
        var python = TuningPythonDraft.Generate(targets, opened.Snapshot.GameVersion, "benchmark.log");
        var xmlInjector = TuningXmlInjectorDraft.Generate(targets, "benchmark_xml_injector");
        var coreLibrary = TuningCoreLibraryDraft.Generate(targets, "benchmark_core_library");
        var roots = PropertyTree.Build(draft.Draft, opened.Entries, opened.Strings, false, draft.ChangedPaths);
        var original = AnnotationBuilder.Build(draft.OriginalText, draft.Original, opened.Entries, opened.Strings, OpenedFixture.GetTypeName);
        var modified = AnnotationBuilder.Build(draft.Text, draft.Draft, opened.Entries, opened.Strings, OpenedFixture.GetTypeName);
        return python.Source.Length + xmlInjector.Source.Length + coreLibrary.Source.Length + roots.Count + original.annotations.Count + modified.annotations.Count;
    }
}
