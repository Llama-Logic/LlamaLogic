namespace LlamaLogic.Packages.Models.AudioConfiguration;

/// <summary>
/// Represents an entry of the interactive music of an <see cref="AudioConfigurationModel"/> (🔓)
/// </summary>
public sealed class AudioConfigurationInteractiveMusicEntry
{
    /// <summary>
    /// Gets/sets the hashed key of the entry
    /// </summary>
    public uint Key { get; set; }

    /// <summary>
    /// Gets the values of the entry
    /// </summary>
    public Collection<float> Values { get; } = [];
}