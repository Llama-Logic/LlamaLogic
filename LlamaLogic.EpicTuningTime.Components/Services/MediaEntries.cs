namespace LlamaLogic.EpicTuningTime.Components.Services;

/// <summary>
/// Represents the media in a snapshot, indexed for resolving the references tuning makes to them
/// </summary>
sealed class MediaEntries
{
    public MediaEntries(TuningArchiveSnapshot snapshot, SnapshotEntries images, SnapshotEntries sounds, SnapshotEntries videos)
    {
        this.snapshot = snapshot;
        byKind = new()
        {
            [TuningArchiveResourceKind.Image] = images,
            [TuningArchiveResourceKind.Sound] = sounds,
            [TuningArchiveResourceKind.Video] = videos
        };
    }

    readonly Dictionary<TuningArchiveResourceKind, SnapshotEntries> byKind;
    readonly TuningArchiveSnapshot snapshot;

    /// <summary>
    /// Gets whether the snapshot captured the specified <paramref name="kind"/> of media
    /// </summary>
    public bool Has(TuningArchiveResourceKind kind) =>
        snapshot.Kinds.Contains(kind);

    /// <summary>
    /// Gets the medium to which tuning refers with the specified <paramref name="reference"/>, resolving it as the game does, or <see langword="null"/> if the snapshot does not contain it
    /// </summary>
    public TuningArchiveEntry? Resolve(ResourceKey reference) =>
        TuningArchiveFile.GetMediaKind(reference.Type) is { } kind && byKind.TryGetValue(kind, out var entries)
            ? TuningArchiveFile.GetMediaResourceTypes(reference.Type).Select(type => entries.Find(type, reference.FullInstance)).FirstOrDefault(entry => entry is not null)
            : null;
}