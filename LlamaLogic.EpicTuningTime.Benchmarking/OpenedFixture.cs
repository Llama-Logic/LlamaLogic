namespace LlamaLogic.EpicTuningTime.Benchmarking;

sealed class OpenedFixture :
    IDisposable
{
    public OpenedFixture(string temporaryDirectory, TuningArchiveFile archive, string tuningName)
    {
        this.temporaryDirectory = temporaryDirectory;
        Archive = archive;
        var snapshots = archive.GetSnapshots();
        Snapshot = snapshots.Last(snapshot => archive.FindTuningDescriptions(snapshot) is not null);
        Cache = archive.GetTuningDescriptionCache(archive.FindTuningDescriptions(Snapshot)!);
        Entries = new SnapshotEntries(archive.GetEntries(Snapshot));
        Entry = Entries.All.FirstOrDefault(entry => entry.Name == tuningName) ?? throw new InvalidOperationException($"{tuningName} is not in the latest snapshot with tuning descriptions");
        Strings = archive.GetStrings(Snapshot);
        Text = archive.GetText(Entry);
        Analysis = TuningAnalysis.Analyze(Text, Cache);
        var itemCounts = Analysis.Nodes
            .Where(node => node.Path.EndsWith(']'))
            .GroupBy(node => node.Path[..node.Path.LastIndexOf('[')], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        LargestList = Analysis.Nodes
            .Where(node => node.Description is ListTunableDescription { Item: not null })
            .OrderByDescending(node => itemCounts.GetValueOrDefault(node.Path))
            .First();
        LargestListCount = itemCounts.GetValueOrDefault(LargestList.Path);
        DeepestValue = Analysis.Nodes
            .Where((node, index) => node.ElementName is "T" && node.Value is { Length: > 0 } && !(index + 1 < Analysis.Nodes.Count && IsWithin(Analysis.Nodes[index + 1].Path, node.Path)))
            .OrderByDescending(node => node.Path.Count(character => character is '.' or '['))
            .First();
        ChangedValue = long.TryParse(DeepestValue.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? (number + 1).ToString(CultureInfo.InvariantCulture)
            : $"{DeepestValue.Value}0";
    }

    readonly string temporaryDirectory;

    public TuningAnalysis Analysis { get; }

    public TuningArchiveFile Archive { get; }

    public TuningDescriptionCache Cache { get; }

    public string ChangedValue { get; }

    public TuningNode DeepestValue { get; }

    public SnapshotEntries Entries { get; }

    public TuningArchiveEntry Entry { get; }

    public TuningNode LargestList { get; }

    public int LargestListCount { get; }

    public TuningArchiveSnapshot Snapshot { get; }

    public IReadOnlyDictionary<uint, string> Strings { get; }

    public string Text { get; }

    public TuningDraft CreateDraft() =>
        new(Snapshot, Entry, Text, Cache);

    public void Dispose()
    {
        Archive.Dispose();
        FixtureCatalog.Unstage(temporaryDirectory);
    }

    public static string GetTypeName(ResourceType type) =>
        type.ToString();

    static bool IsWithin(string path, string ancestorPath) =>
        path.Length > ancestorPath.Length && path.StartsWith(ancestorPath, StringComparison.Ordinal) && path[ancestorPath.Length] is '.' or '[';
}
