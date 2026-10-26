namespace LlamaLogic.EpicTuningTime.Components.State;

/// <summary>
/// Represents the package Update is reviewing and what the creator has decided about it, so that it can be shown again when the creator returns to it
/// </summary>
sealed class UpdateState
{
    public UpdateState(TuningArchiveSession session) =>
        session.ArchiveChanged += Reset;

    static readonly IReadOnlyCollection<TuningPackageOverrideOutcome> defaultOutcomes =
    [
        TuningPackageOverrideOutcome.Merged,
        TuningPackageOverrideOutcome.Conflicted,
        TuningPackageOverrideOutcome.MatchesGame,
        TuningPackageOverrideOutcome.MatchesOlderGame,
        TuningPackageOverrideOutcome.RemovedFromGame,
        TuningPackageOverrideOutcome.Unreadable
    ];

    /// <summary>
    /// Gets what the creator decided for each override, by key (an override without a decision gets <see cref="GetDefaultDecision"/>)
    /// </summary>
    public Dictionary<ResourceKey, UpdateDecision> Decisions { get; } = [];

    /// <summary>
    /// Gets or sets the identifier of the snapshot the creator says the package was last updated for, or <see langword="null"/> to infer it
    /// </summary>
    public long? DeclaredBaseId { get; set; }

    /// <summary>
    /// Gets where the diff was scrolled to
    /// </summary>
    public EditorViewState Diff { get; } = new();

    /// <summary>
    /// Gets or sets which diff is shown: <c>result</c> (the mod's version against the result), <c>mod</c> (the base against the mod's), <c>game</c> (the base against the game's new version) or <c>final</c> (the game's new version against the result)
    /// </summary>
    public string DiffMode { get; set; } = "result";

    /// <summary>
    /// Gets how far the list of overrides is scrolled
    /// </summary>
    public ScrollState List { get; } = new();

    /// <summary>
    /// Gets the merges the creator resolved conflicts in, by key
    /// </summary>
    public Dictionary<ResourceKey, TuningMergeResult> Merges { get; } = [];

    /// <summary>
    /// Gets or sets the outcomes shown
    /// </summary>
    public IReadOnlyCollection<TuningPackageOverrideOutcome> Outcomes { get; set; } = defaultOutcomes;

    /// <summary>
    /// Gets how the creator resolved each conflict, by key and then by the conflict's index
    /// </summary>
    public Dictionary<ResourceKey, Dictionary<int, TuningMergeChoice>> Resolutions { get; } = [];

    /// <summary>
    /// Gets or sets the key of the selected override, if any
    /// </summary>
    public ResourceKey? Selected { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the snapshot the package is being updated for, if any
    /// </summary>
    public long? TargetId { get; set; }

    /// <summary>
    /// Gets or sets the review of the package, if one is open
    /// </summary>
    public TuningPackageUpdate? Update { get; set; }

    /// <summary>
    /// Gets or sets whether the updated package's SimData is made what its overrides need
    /// </summary>
    public bool UpdateSimData { get; set; } = true;

    /// <summary>
    /// Gets what the updated package does with the specified <paramref name="item"/> unless the creator decides otherwise: the merge, when the mod's changes merge without conflicts, and otherwise what the mod has
    /// </summary>
    public static UpdateDecision GetDefaultDecision(TuningPackageOverride item) =>
        item.Outcome is TuningPackageOverrideOutcome.Merged ? UpdateDecision.Merge : UpdateDecision.Keep;

    /// <summary>
    /// Gets what the updated package does with the specified <paramref name="item"/>
    /// </summary>
    public UpdateDecision GetDecision(TuningPackageOverride item) =>
        Decisions.TryGetValue(item.Key, out var decision) ? decision : GetDefaultDecision(item);

    /// <summary>
    /// Gets the merge of the specified <paramref name="item"/> with its conflicts resolved as the creator chose, if it has a merge
    /// </summary>
    public TuningMergeResult? GetMerge(TuningPackageOverride item) =>
        Merges.TryGetValue(item.Key, out var merge) ? merge : item.Merge;

    /// <summary>
    /// Gets the tuning the updated package has in place of the overrides the creator didn't keep (<see langword="null"/> for those removed)
    /// </summary>
    public IReadOnlyDictionary<ResourceKey, string?> GetTuning()
    {
        var tuning = new Dictionary<ResourceKey, string?>();
        if (Update is null)
            return tuning;
        foreach (var item in Update.Overrides)
            switch (GetDecision(item))
            {
                case UpdateDecision.Merge when GetMerge(item) is { } merge:
                    tuning[item.Key] = merge.Text;
                    break;
                case UpdateDecision.Remove:
                    tuning[item.Key] = null;
                    break;
            }
        return tuning;
    }

    /// <summary>
    /// Starts reviewing the specified <paramref name="update"/>, forgetting the decisions made about any other
    /// </summary>
    public void Open(TuningPackageUpdate update)
    {
        Update = update;
        Decisions.Clear();
        Merges.Clear();
        Resolutions.Clear();
        Selected = null;
        List.Key = null;
        List.Top = 0;
    }

    /// <summary>
    /// Forgets everything
    /// </summary>
    public void Reset()
    {
        Decisions.Clear();
        DeclaredBaseId = null;
        Diff.Key = null;
        Diff.ViewState = null;
        DiffMode = "result";
        List.Key = null;
        List.Top = 0;
        Merges.Clear();
        Outcomes = defaultOutcomes;
        Resolutions.Clear();
        Selected = null;
        TargetId = null;
        Update = null;
        UpdateSimData = true;
    }
}