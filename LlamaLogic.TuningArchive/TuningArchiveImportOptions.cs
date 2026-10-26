namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents the choices a creator makes when importing a folder of tuning files extracted by another tool as a snapshot
/// </summary>
public sealed class TuningArchiveImportOptions
{
    /// <summary>
    /// Gets/sets the code of the pack to attribute files to when their path does not name one (the default is <c>BG</c>)
    /// </summary>
    public string DefaultPack { get; set; } = "BG";

    /// <summary>
    /// Gets/sets a label for the snapshot
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets/sets the locale of the strings in the folder's string reference file, if it has one (the default is <c>ENG_US</c>)
    /// </summary>
    public string Locale { get; set; } = "ENG_US";

    /// <summary>
    /// Gets/sets notes to record with the snapshot
    /// </summary>
    public string? Notes { get; set; }
}