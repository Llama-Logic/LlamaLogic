namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents the outcome of exporting a snapshot as files
/// </summary>
public sealed class TuningArchiveExportResult
{
    internal TuningArchiveExportResult(int files, int renamed, int skipped, bool usedTuningDescriptions, IReadOnlyDictionary<string, uint> packIdentifiers, IReadOnlyList<string> problems, TimeSpan duration)
    {
        Files = files;
        Renamed = renamed;
        Skipped = skipped;
        UsedTuningDescriptions = usedTuningDescriptions;
        PackIdentifiers = packIdentifiers;
        Problems = problems;
        Duration = duration;
    }

    /// <summary>
    /// Gets how long the export took
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets how many resource files were written
    /// </summary>
    public int Files { get; }

    /// <summary>
    /// Gets the identifier used as the group in file names for each pack, by pack code
    /// </summary>
    public IReadOnlyDictionary<string, uint> PackIdentifiers { get; }

    /// <summary>
    /// Gets descriptions of anything the creator should know about the export
    /// </summary>
    public IReadOnlyList<string> Problems { get; }

    /// <summary>
    /// Gets how many existing files were renamed to make way for new ones
    /// </summary>
    public int Renamed { get; }

    /// <summary>
    /// Gets how many resource files were not written because a file was already there
    /// </summary>
    public int Skipped { get; }

    /// <summary>
    /// Gets whether string keys and references were found with tuning descriptions (rather than by taking every matching number to be one)
    /// </summary>
    public bool UsedTuningDescriptions { get; }
}