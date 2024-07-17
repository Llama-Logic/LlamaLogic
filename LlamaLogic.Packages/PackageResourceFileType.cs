namespace LlamaLogic.Packages;

/// <summary>
/// The file type of a package resource independent of storage within a package
/// </summary>
public enum PackageResourceFileType
{
    /// <summary>
    /// DDS Image
    /// </summary>
    [PackageResourceFileTypeFormat("dds", "image/vnd.ms-dds")]
    DirectDrawSurface,

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
