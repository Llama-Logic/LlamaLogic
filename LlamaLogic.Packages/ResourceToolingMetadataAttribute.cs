namespace LlamaLogic.Packages;

/// <summary>
/// Specifies that the <see cref="ResourceType"/> is tooling metadata and not intended for use by the game
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class ResourceToolingMetadataAttribute :
    Attribute
{
}
