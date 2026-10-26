namespace LlamaLogic.EpicTuningTime.Components.Dialogs;

/// <summary>
/// Specifies what <see cref="ValueDialog"/> searches
/// </summary>
public enum ValuePickerKind
{
    /// <summary>
    /// The text of the snapshot's strings
    /// </summary>
    String,

    /// <summary>
    /// The images the snapshot captured
    /// </summary>
    Image,

    /// <summary>
    /// The audio configurations the snapshot captured
    /// </summary>
    Sound
}