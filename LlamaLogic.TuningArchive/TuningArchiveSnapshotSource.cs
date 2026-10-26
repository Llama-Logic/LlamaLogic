namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies where the contents of a <see cref="TuningArchiveSnapshot"/> came from
/// </summary>
public enum TuningArchiveSnapshotSource
{
    /// <summary>
    /// The snapshot was captured from an installation of the game
    /// </summary>
    GameInstallation = 0,

    /// <summary>
    /// The snapshot was imported from a folder of tuning files extracted by another tool
    /// </summary>
    TuningFolder = 1
}