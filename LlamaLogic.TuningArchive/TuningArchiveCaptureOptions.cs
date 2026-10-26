namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents the choices a creator makes when capturing a snapshot from an installation of the game
/// </summary>
public sealed class TuningArchiveCaptureOptions
{
    /// <summary>
    /// Gets/sets whether to capture animation state machines (the default is <see langword="true"/>)
    /// </summary>
    public bool IncludeAnimationStateMachines { get; set; } = true;

    /// <summary>
    /// Gets/sets the game version to record, overriding the version the installation states (required when the installation's version cannot be found)
    /// </summary>
    public string? GameVersion { get; set; }

    /// <summary>
    /// Gets/sets whether to capture SimData resources (the default is <see langword="true"/>)
    /// </summary>
    public bool IncludeSimData { get; set; } = true;

    /// <summary>
    /// Gets/sets whether to capture the images to which the captured tuning and SimData refer (the default is <see langword="true"/>)
    /// </summary>
    public bool IncludeImages { get; set; } = true;

    /// <summary>
    /// Gets/sets whether to capture the sounds to which the captured tuning and SimData refer, with the audio they use (the default is <see langword="false"/>, because the audio is large and does not compress)
    /// </summary>
    public bool IncludeSounds { get; set; }

    /// <summary>
    /// Gets/sets whether to capture the videos to which the captured tuning and SimData refer (the default is <see langword="false"/>, because they are large and compress little)
    /// </summary>
    public bool IncludeVideos { get; set; }

    /// <summary>
    /// Gets/sets a label for the snapshot
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets/sets the locale of the strings to capture, or <see langword="null"/> to capture no strings (the default is <c>ENG_US</c>)
    /// </summary>
    public string? Locale { get; set; } = "ENG_US";

    /// <summary>
    /// Gets/sets notes to record with the snapshot
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets/sets the codes of the packs to capture, or <see langword="null"/> to capture all installed packs (the default)
    /// </summary>
    public IReadOnlyCollection<string>? Packs { get; set; }
}