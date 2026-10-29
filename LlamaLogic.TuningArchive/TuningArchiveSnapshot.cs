namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents the state of the game's tuning at a point in time, as captured in a <see cref="TuningArchiveFile"/>
/// </summary>
public sealed class TuningArchiveSnapshot
{
    internal TuningArchiveSnapshot(long id, long ordinal, string gameVersion, string? label, string? notes, TuningArchiveSnapshotSource source, string? sourcePath, DateTimeOffset capturedAt, string? locale, IReadOnlyList<TuningArchiveSnapshotPack> packs, IReadOnlySet<TuningArchiveResourceKind> kinds)
    {
        Id = id;
        Ordinal = ordinal;
        GameVersion = gameVersion;
        Label = label;
        Notes = notes;
        Source = source;
        SourcePath = sourcePath;
        CapturedAt = capturedAt;
        Locale = locale;
        Packs = packs;
        Kinds = kinds;
    }

    /// <summary>
    /// Gets when the snapshot was captured
    /// </summary>
    public DateTimeOffset CapturedAt { get; }

    /// <summary>
    /// Gets the version of the game the snapshot represents
    /// </summary>
    public string GameVersion { get; }

    /// <summary>
    /// Gets the identifier of the snapshot within its archive
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// Gets the kinds of resources the snapshot contains
    /// </summary>
    public IReadOnlySet<TuningArchiveResourceKind> Kinds { get; }

    /// <summary>
    /// Gets the label the creator gave the snapshot, if any
    /// </summary>
    public string? Label { get; }

    /// <summary>
    /// Gets the locale of the snapshot's strings, if it has any
    /// </summary>
    public string? Locale { get; }

    /// <summary>
    /// Gets the notes the creator wrote about the snapshot, if any
    /// </summary>
    public string? Notes { get; }

    internal long Ordinal { get; }

    /// <summary>
    /// Gets the packs that were present when the snapshot was taken
    /// </summary>
    public IReadOnlyList<TuningArchiveSnapshotPack> Packs { get; }

    /// <summary>
    /// Gets where the contents of the snapshot came from
    /// </summary>
    public TuningArchiveSnapshotSource Source { get; }

    /// <summary>
    /// Gets the path of the installation or folder the snapshot came from, if it is known
    /// </summary>
    public string? SourcePath { get; }

    /// <summary>
    /// Gets the name to display for the snapshot: its label if it has one, otherwise its game version
    /// </summary>
    public string DisplayName =>
        string.IsNullOrWhiteSpace(Label) ? GameVersion : $"{GameVersion} ({Label})";

    /// <summary>
    /// Gets whether the snapshot was captured from an installation of the same game version as the specified <paramref name="installation"/>, with the same packs at the same versions
    /// </summary>
    /// <remarks>
    /// The kinds of resources the snapshot contains are not considered (see <see cref="Kinds"/>).
    /// </remarks>
    public bool IsCaptureOf(GameInstallation installation)
    {
        ArgumentNullException.ThrowIfNull(installation);
        return Source is TuningArchiveSnapshotSource.GameInstallation
            && installation.GameVersion == GameVersion
            && installation.Packs.Count == Packs.Count
            && installation.Packs.All(pack => Packs.Any(captured => string.Equals(captured.Code, pack.Code, StringComparison.OrdinalIgnoreCase) && captured.Version == pack.Version));
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is TuningArchiveSnapshot other && other.Id == Id;

    /// <inheritdoc/>
    public override int GetHashCode() =>
        Id.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() =>
        DisplayName;
}