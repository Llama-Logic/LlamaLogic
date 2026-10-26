namespace LlamaLogic.EpicTuningTime.Components.Services;

/// <summary>
/// Provides what the components need from the application hosting them
/// </summary>
public interface IEpicTuningTimePlatform
{
    /// <summary>
    /// Gets whether the host can show native file and folder pickers (when it cannot, paths are typed)
    /// </summary>
    bool CanPick { get; }

    /// <summary>
    /// Gets the folder in which new archives are suggested
    /// </summary>
    string DefaultArchiveFolder { get; }

    /// <summary>
    /// Gets a stored setting, or <see langword="null"/> if it has not been stored
    /// </summary>
    string? GetSetting(string key);

    /// <summary>
    /// Lets the creator pick an existing file with one of the specified <paramref name="extensions"/> (such as <c>.zip</c>), returning its path, or <see langword="null"/> if they cancel
    /// </summary>
    Task<string?> PickFileAsync(string title, IReadOnlyList<string> extensions);

    /// <summary>
    /// Lets the creator pick a folder, returning its path, or <see langword="null"/> if they cancel
    /// </summary>
    Task<string?> PickFolderAsync(string title);

    /// <summary>
    /// Lets the creator choose where to create a new file, suggesting the specified <paramref name="suggestedFileName"/>, returning the path of the empty file created there, or <see langword="null"/> if they cancel
    /// </summary>
    Task<string?> PickNewFileAsync(string title, string suggestedFileName);

    /// <summary>
    /// Opens the specified folder or file in the operating system's file browser
    /// </summary>
    Task RevealAsync(string path);

    /// <summary>
    /// Stores a setting (or removes it, when <paramref name="value"/> is <see langword="null"/>)
    /// </summary>
    void SetSetting(string key, string? value);
}