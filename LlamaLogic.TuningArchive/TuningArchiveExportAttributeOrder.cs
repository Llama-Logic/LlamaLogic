namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies the order in which <see cref="TuningArchiveFile.ExportAsync"/> writes the <c>n</c> and <c>t</c> attributes of an element that has both
/// </summary>
public enum TuningArchiveExportAttributeOrder
{
    /// <summary>
    /// The name (<c>n</c>) before the type (<c>t</c>)
    /// </summary>
    NameFirst = 0,

    /// <summary>
    /// The type (<c>t</c>) before the name (<c>n</c>)
    /// </summary>
    TypeFirst = 1
}