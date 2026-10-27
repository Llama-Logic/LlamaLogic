namespace LlamaLogic.EpicTuningTime.Components.Services;

/// <summary>
/// Represents the resources of one kind in a snapshot, indexed for the components
/// </summary>
sealed class SnapshotEntries
{
    public SnapshotEntries(IReadOnlyList<TuningArchiveEntry> all, IReadOnlyList<TuningArchiveUninstalledTuning>? uninstalled = null)
    {
        All = [.. all.OrderBy(entry => entry.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)];
        foreach (var entry in All)
        {
            byKey.TryAdd((entry.Type, entry.Instance), entry);
            byInstance.TryAdd(entry.Instance, entry);
        }
        TypeCounts = All.GroupBy(entry => entry.Type).ToDictionary(group => group.Key, group => group.Count());
        foreach (var tuning in uninstalled ?? [])
        {
            uninstalledByKey.TryAdd((tuning.Type, tuning.Instance), tuning);
            uninstalledByName.TryAdd((tuning.Type, tuning.Name), tuning);
        }
    }

    readonly Dictionary<ulong, TuningArchiveEntry> byInstance = [];
    readonly Dictionary<(ResourceType type, ulong instance), TuningArchiveEntry> byKey = [];
    readonly Dictionary<(ResourceType type, ulong instance), TuningArchiveUninstalledTuning> uninstalledByKey = [];
    readonly Dictionary<(ResourceType type, string name), TuningArchiveUninstalledTuning> uninstalledByName = [];

    public IReadOnlyList<TuningArchiveEntry> All { get; }

    public IReadOnlyDictionary<ResourceType, int> TypeCounts { get; }

    public TuningArchiveEntry? Find(ResourceType type, ulong instance) =>
        byKey.TryGetValue((type, instance), out var entry) ? entry : null;

    public TuningArchiveEntry? FindByInstance(ulong instance) =>
        byInstance.TryGetValue(instance, out var entry) ? entry : null;

    /// <summary>
    /// Finds a tuning of a pack that was not installed when the snapshot was captured, known only by name from the pack's patches
    /// </summary>
    public TuningArchiveUninstalledTuning? FindUninstalled(ResourceType type, ulong? instance, string? name) =>
        instance is { } byInstanceKey ? uninstalledByKey.GetValueOrDefault((type, byInstanceKey)) : name is not null ? uninstalledByName.GetValueOrDefault((type, name)) : null;
}