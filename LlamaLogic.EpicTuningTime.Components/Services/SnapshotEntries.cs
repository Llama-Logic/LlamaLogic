namespace LlamaLogic.EpicTuningTime.Components.Services;

/// <summary>
/// Represents the resources of one kind in a snapshot, indexed for the components
/// </summary>
sealed class SnapshotEntries
{
    public SnapshotEntries(IReadOnlyList<TuningArchiveEntry> all)
    {
        All = [.. all.OrderBy(entry => entry.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)];
        foreach (var entry in All)
        {
            byKey.TryAdd((entry.Type, entry.Instance), entry);
            byInstance.TryAdd(entry.Instance, entry);
        }
        TypeCounts = All.GroupBy(entry => entry.Type).ToDictionary(group => group.Key, group => group.Count());
    }

    readonly Dictionary<ulong, TuningArchiveEntry> byInstance = [];
    readonly Dictionary<(ResourceType type, ulong instance), TuningArchiveEntry> byKey = [];

    public IReadOnlyList<TuningArchiveEntry> All { get; }

    public IReadOnlyDictionary<ResourceType, int> TypeCounts { get; }

    public TuningArchiveEntry? Find(ResourceType type, ulong instance) =>
        byKey.TryGetValue((type, instance), out var entry) ? entry : null;

    public TuningArchiveEntry? FindByInstance(ulong instance) =>
        byInstance.TryGetValue(instance, out var entry) ? entry : null;
}