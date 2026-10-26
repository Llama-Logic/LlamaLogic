namespace LlamaLogic.EpicTuningTime.Components.State;

/// <summary>
/// Represents what Broken references is showing, so that it can be shown again when the creator returns to it
/// </summary>
sealed class ProblemsState
{
    public ProblemsState(TuningArchiveSession session) =>
        session.ArchiveChanged += Reset;

    /// <summary>
    /// Gets how far the list is scrolled
    /// </summary>
    public ScrollState List { get; } = new();

    /// <summary>
    /// Gets or sets the identifier of the snapshot shown, if any
    /// </summary>
    public long? SnapshotId { get; set; }

    /// <summary>
    /// Forgets everything
    /// </summary>
    public void Reset()
    {
        List.Key = null;
        List.Top = 0;
        SnapshotId = null;
    }
}