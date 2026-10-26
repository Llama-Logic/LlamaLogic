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

    /// <summary>
    /// Gets the versions for each voice, with their voices' codes, of the audio configuration to which tuning refers with the specified <paramref name="reference"/>, in the order of <see cref="TuningArchiveFile.VoiceCodes"/>, or none if it is not a reference to one (see <see cref="TuningArchiveFile.GetVoiceVersionInstance(ulong, string)"/>)
    /// </summary>
    public IReadOnlyList<(string voiceCode, TuningArchiveEntry entry)> ResolveVoiceVersions(ResourceKey reference) =>
        reference.Type is ResourceType.AudioConfigurationReference or ResourceType.AudioConfiguration && byKind.TryGetValue(TuningArchiveResourceKind.Sound, out var entries)
            ? [.. TuningArchiveFile.VoiceCodes.Select(voiceCode => (voiceCode, entry: entries.Find(ResourceType.AudioConfiguration, TuningArchiveFile.GetVoiceVersionInstance(reference.FullInstance, voiceCode)))).Where(version => version.entry is not null).Select(version => (version.voiceCode, version.entry!))]
            : [];
}