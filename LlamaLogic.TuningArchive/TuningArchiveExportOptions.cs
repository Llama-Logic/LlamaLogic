namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents the choices a creator makes when exporting a snapshot as files, the way Scumbumbo's XML Extractor extracts the game's tuning
/// </summary>
public sealed class TuningArchiveExportOptions
{
    /// <summary>
    /// Gets/sets the order of the <c>n</c> and <c>t</c> attributes of elements that have both (the default is <see cref="TuningArchiveExportAttributeOrder.NameFirst"/>)
    /// </summary>
    public TuningArchiveExportAttributeOrder AttributeOrder { get; set; }

    /// <summary>
    /// Gets/sets what to do when a file to be written already exists (the default is <see cref="TuningArchiveExportExistingFiles.Overwrite"/>)
    /// </summary>
    public TuningArchiveExportExistingFiles ExistingFiles { get; set; }

    /// <summary>
    /// Gets/sets whether to write the snapshot's animation state machines, in an <c>AsmJazz</c> folder in each pack's folder (the default is <see langword="false"/>)
    /// </summary>
    public bool IncludeAnimationStateMachines { get; set; }

    /// <summary>
    /// Gets/sets whether to add a comment naming the tuning or animation state machine each reference refers to (the default is <see langword="true"/>)
    /// </summary>
    public bool IncludeReferenceComments { get; set; } = true;

    /// <summary>
    /// Gets/sets whether to write the snapshot's SimData, beside the tuning each belongs to (the default is <see langword="false"/>)
    /// </summary>
    public bool IncludeSimData { get; set; }

    /// <summary>
    /// Gets/sets whether to add a comment giving the text of each string key (the default is <see langword="true"/>)
    /// </summary>
    public bool IncludeStringComments { get; set; } = true;

    /// <summary>
    /// Gets/sets how to name the files (the default is <see cref="TuningArchiveExportNaming.TypeGroupInstance"/>)
    /// </summary>
    public TuningArchiveExportNaming Naming { get; set; }

    /// <summary>
    /// Gets/sets whether to find string keys and references with the snapshot's tuning descriptions when it has them (the default is <see langword="true"/>); otherwise, or without them, every number that matches a string key or the instance of a tuning is taken to be one, as XML Extractor does
    /// </summary>
    public bool UseTuningDescriptions { get; set; } = true;

    /// <summary>
    /// Gets/sets whether to write <c>XML File Index.txt</c> and <c>XML Cross Reference.dat</c>, which Scumbumbo's XML File Finder reads (the default is <see langword="true"/>)
    /// </summary>
    public bool WriteFileIndex { get; set; } = true;

    /// <summary>
    /// Gets/sets whether to write <c>Reference - All Strings.txt</c> and <c>Reference - All XML Names.txt</c> (the default is <see langword="true"/>)
    /// </summary>
    public bool WriteReferenceFiles { get; set; } = true;
}