namespace LlamaLogic.EpicTuningTime.Components.State;

/// <summary>
/// Represents the folder Update a folder is reviewing, so that it can be shown again when the creator returns to it
/// </summary>
sealed class UpdateFolderState
{
    public UpdateFolderState(TuningArchiveSession session) =>
        session.ArchiveChanged += Reset;

    /// <summary>
    /// Gets or sets whether generated SimData is added for tuning whose changes reach SimData but has none
    /// </summary>
    public bool AddMissingSimData { get; set; }

    /// <summary>
    /// Gets or sets which fixes are made to the SimData of the tuning the packages add
    /// </summary>
    public TuningPackageAddedSimDataFixes AddedSimDataFixes { get; set; } = TuningPackageAddedSimDataFixes.AddMissingColumns;

    /// <summary>
    /// Gets or sets the identifier of the snapshot the creator says the packages were last updated for, or <see langword="null"/> to infer it
    /// </summary>
    public long? DeclaredBaseId { get; set; }

    /// <summary>
    /// Gets or sets whether reports are saved as Excel workbooks rather than as CSV files
    /// </summary>
    public bool ExcelReport { get; set; } = true;

    /// <summary>
    /// Gets how far the list of packages is scrolled
    /// </summary>
    public ScrollState List { get; } = new();

    /// <summary>
    /// Gets or sets whether only the packages and overrides that need the creator are shown
    /// </summary>
    public bool OnlyNeedingCreator { get; set; } = true;

    /// <summary>
    /// Gets or sets whether overrides the same as the game's new version are removed
    /// </summary>
    public bool RemoveOverridesThatDoNothing { get; set; }

    /// <summary>
    /// Gets or sets what was written the last time the updated copies were written, if they were
    /// </summary>
    public TuningFolderUpdateResult? Result { get; set; }

    /// <summary>
    /// Gets or sets what the list is filtered by
    /// </summary>
    public string Search { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative path of the selected package, if any
    /// </summary>
    public string? Selected { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the snapshot the packages are being updated for, if any
    /// </summary>
    public long? TargetId { get; set; }

    /// <summary>
    /// Gets or sets the review of the folder, if one is open
    /// </summary>
    public TuningFolderUpdate? Update { get; set; }

    /// <summary>
    /// Gets which changes the update makes beyond the merges that need no judgment
    /// </summary>
    public TuningFolderUpdateOptions Options =>
        new(RemoveOverridesThatDoNothing, TuningPackageSimDataChanges.Replace | TuningPackageSimDataChanges.Remove | (AddMissingSimData ? TuningPackageSimDataChanges.Add : TuningPackageSimDataChanges.None), AddedSimDataFixes);

    /// <summary>
    /// Forgets everything
    /// </summary>
    public void Reset()
    {
        AddMissingSimData = false;
        AddedSimDataFixes = TuningPackageAddedSimDataFixes.AddMissingColumns;
        DeclaredBaseId = null;
        ExcelReport = true;
        List.Key = null;
        List.Top = 0;
        OnlyNeedingCreator = true;
        RemoveOverridesThatDoNothing = false;
        Result = null;
        Search = string.Empty;
        Selected = null;
        TargetId = null;
        Update = null;
    }
}