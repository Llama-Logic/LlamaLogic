namespace LlamaLogic.Packages;

/// <summary>
/// Specifies the file type of a package resource type independent of storage within a package
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class PackageResourceFileTypeAttribute(PackageResourceFileType packageResourceFileType) :
    Attribute
{
    /// <summary>
    /// Gets the file type of the decorated package resource type independent of storage within a package
    /// </summary>
    public PackageResourceFileType PackageResourceFileType { get; } = packageResourceFileType;
}
