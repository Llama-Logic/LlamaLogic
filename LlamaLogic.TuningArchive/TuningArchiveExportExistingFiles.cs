namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies what <see cref="TuningArchiveFile.ExportAsync"/> does when a file it would write already exists
/// </summary>
public enum TuningArchiveExportExistingFiles
{
    /// <summary>
    /// Replace the file
    /// </summary>
    Overwrite = 0,

    /// <summary>
    /// Rename the file by appending <c>(Copy 1)</c> (or the next free number) to its name, then write the new one
    /// </summary>
    Rename = 1,

    /// <summary>
    /// Leave the file as it is and write nothing in its place
    /// </summary>
    Skip = 2
}