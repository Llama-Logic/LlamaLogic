namespace LlamaLogic.Packages;

/// <summary>
/// The file type of a package resource independent of storage within a package
/// </summary>
public enum PackageResourceFileType
{
    /// <summary>
    /// Binary
    /// </summary>
    Binary,

    /// <summary>
    /// DDS Image
    /// </summary>
    [PackageResourceFileTypeFormat("dds", "image/vnd.ms-dds")]
    DirectDrawSurface,

    /// <summary>
    /// JavaScript Object Notation
    /// </summary>
    [PackageResourceFileTypeFormat("json", "application/json")]
    JavaScriptObjectNotation,

    /// <summary>
    /// PNG Image
    /// </summary>
    [PackageResourceFileTypeFormat("png", "image/png")]
    PortableNetworkGraphic,

    /// <summary>
    /// Tuning XML
    /// </summary>
    [PackageResourceFileTypeFormat("xml", "application/xml")]
    TuningMarkup
}
