namespace LlamaLogic.Packages;

/// <summary>
/// Specifies the file type of a package resource type independent of storage within a <see cref="DataBasePackedFile"/>
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class ResourceFileTypeAttribute(ResourceFileType packageResourceFileType) :
    Attribute
{
    /// <summary>
    /// Gets the file type of the decorated package resource type independent of storage within a <see cref="DataBasePackedFile"/>
    /// </summary>
    public ResourceFileType ResourceFileType { get; } = packageResourceFileType;
}
