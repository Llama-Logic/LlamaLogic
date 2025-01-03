namespace LlamaLogic.Packages;

/// <summary>
/// The file type of a package resource independent of storage within a <see cref="DataBasePackedFile"/>
/// </summary>
public enum ResourceFileType
{
    /// <summary>
    /// Binary
    /// </summary>
    [ResourceFileTypeFormat("bin", "application/octet-stream")]
    Binary,

    /// <summary>
    /// DDS Image
    /// </summary>
    [ResourceFileTypeFormat("dds", "image/vnd.ms-dds")]
    DirectDrawSurface,

    /// <summary>
    /// JavaScript Object Notation
    /// </summary>
    [ResourceFileTypeFormat("json", "application/json")]
    JavaScriptObjectNotation,

    /// <summary>
    /// PNG Image
    /// </summary>
    [ResourceFileTypeFormat("png", "image/png")]
    PortableNetworkGraphic,

    /// <summary>
    /// Tuning XML
    /// </summary>
    [ResourceFileTypeFormat("xml", "application/xml")]
    TuningMarkup,

    /// <summary>
    /// The Sims 4's proprietary image format in which a PNG alpha mask is embedded in the headers of a JPEG
    /// </summary>
    [ResourceFileTypeFormat("jpg", "image/jpeg")]
    Ts4TranslucentJointPhotographicExpertsGroupImage,

    /// <summary>
    /// Yet Another Markup Language
    /// </summary>
    [ResourceFileTypeFormat("yml", "application/x-yaml")]
    YetAnotherMarkupLanguage
}
