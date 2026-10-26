namespace LlamaLogic.EpicTuningTime.Benchmarking;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 2, iterationCount: 10)]
public class DraftEditBenchmarks
{
    TuningDraft draft;
    OpenedFixture opened;

    [ParamsSource(nameof(Fixtures))]
    public Fixture Fixture { get; set; }

    public IEnumerable<Fixture> Fixtures =>
        FixtureCatalog.All;

    string FirstItemPath =>
        $"{opened.LargestList.Path}[0]";

    [GlobalCleanup]
    public void GlobalCleanup() =>
        opened?.Dispose();

    [GlobalSetup]
    public void GlobalSetup() =>
        opened = FixtureCatalog.Open(Fixture);

    [IterationSetup(Targets = [nameof(AddItemToLargestList), nameof(DuplicateFirstItemOfLargestList), nameof(RemoveFirstItemOfLargestList), nameof(SetDeepestValue)])]
    public void IterationSetup() =>
        draft = opened.CreateDraft();

    [IterationSetup(Target = nameof(RevertFirstItemOfLargestList))]
    public void IterationSetupRevert()
    {
        draft = opened.CreateDraft();
        draft.Remove(FirstItemPath);
    }

    [Benchmark]
    public void AddItemToLargestList() =>
        draft.AddItem(opened.LargestList.Path, (ListTunableDescription)opened.LargestList.Description);

    [Benchmark]
    public void DuplicateFirstItemOfLargestList() =>
        draft.Duplicate(FirstItemPath);

    [Benchmark]
    public void RemoveFirstItemOfLargestList() =>
        draft.Remove(FirstItemPath);

    [Benchmark]
    public void RevertFirstItemOfLargestList() =>
        draft.Revert(FirstItemPath);

    [Benchmark]
    public void SetDeepestValue() =>
        draft.SetValue(opened.DeepestValue.Path, opened.ChangedValue);
}
