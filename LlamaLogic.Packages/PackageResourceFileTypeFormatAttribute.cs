namespace LlamaLogic.Packages;

/// <summary>
/// Specifies the extension and mime type of a file type of a package resource independent of storage within a package
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class PackageResourceFileTypeFormatAttribute(string extension, string? mimeType = null) :
    Attribute
{

    /// <summary>
    /// Gets the extention used for files in the format
    /// </summary>
    public string Extension { get; } = extension;

    /// <summary>
    /// Gets the MIME type of the format
    /// </summary>
    public string? MimeType { get; } = mimeType;
}
