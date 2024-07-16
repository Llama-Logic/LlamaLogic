namespace LlamaLogic.Packages;

/// <summary>
/// The file type of a package resource independent of storage within a package
/// </summary>
public enum PackageResourceFileType
{
    /// <summary>
    /// XML
    /// </summary>
    [PackageResourceFileTypeEncoding(typeof(UTF8Encoding))]
    Xml
}
