namespace LlamaLogic.EpicTuningTime.Components.Shared;

static class EntryExtensions
{
    public static (ResourceType type, uint group, ulong instance) VersionKey(this TuningArchiveEntry entry) =>
        (entry.Type, entry.Group, entry.Instance);
}