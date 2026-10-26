namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents a mod's override of a game tuning, and what a game version means for it
/// </summary>
public sealed class TuningPackageOverride
{
    internal TuningPackageOverride(ResourceKey key, string? name, TuningPackageOverrideOutcome outcome, string message, string? modText, TuningArchiveSnapshot? baseSnapshot, int baseDistance, bool baseIsAmbiguous, bool baseMayBeOlder, bool baseIsDeclared, bool baseDisagrees, string? baseText, TuningArchiveEntry? gameEntry, string? gameText, TuningMergeResult? merge)
    {
        Key = key;
        Name = name;
        Outcome = outcome;
        Message = message;
        ModText = modText;
        Base = baseSnapshot;
        BaseDistance = baseDistance;
        BaseDisagrees = baseDisagrees;
        BaseIsAmbiguous = baseIsAmbiguous;
        BaseIsDeclared = baseIsDeclared;
        BaseMayBeOlder = baseMayBeOlder;
        BaseText = baseText;
        GameEntry = gameEntry;
        GameText = gameText;
        Merge = merge;
    }

    /// <summary>
    /// Gets the snapshot whose version of the tuning the mod was made from: the one the creator declared (see <see cref="BaseIsDeclared"/>), or else the one with the fewest differences from the override (see <see cref="BaseIsAmbiguous"/>), the newest with that version
    /// </summary>
    public TuningArchiveSnapshot? Base { get; }

    /// <summary>
    /// Gets the number of differences between <see cref="BaseText"/> and <see cref="ModText"/>
    /// </summary>
    public int BaseDistance { get; }

    /// <summary>
    /// Gets whether the base is the version the creator said the package was last updated for, but another version of the tuning is closer to the override
    /// </summary>
    public bool BaseDisagrees { get; }

    /// <summary>
    /// Gets whether the base is the version the creator said the package was last updated for, rather than inferred
    /// </summary>
    public bool BaseIsDeclared { get; }

    /// <summary>
    /// Gets whether the base may not be what the mod was made from: it was inferred and is ambiguous or may be older than the archive, or it was declared and another version is closer
    /// </summary>
    public bool BaseIsUncertain =>
        BaseIsDeclared ? BaseDisagrees : BaseIsAmbiguous || BaseMayBeOlder;

    /// <summary>
    /// Gets whether a version of the tuning with different content is as close to the override as the base (the base is the oldest such version, so that a change the game also made shows as a conflict)
    /// </summary>
    public bool BaseIsAmbiguous { get; }

    /// <summary>
    /// Gets whether the mod may have been made from a version older than any in the archive: the archive's oldest snapshot already has the base's version of the tuning, and the override differs from it
    /// </summary>
    /// <remarks>
    /// When it was, some of what look like the mod's changes are the game's changes since that older version, undone, and a merge carries them forward as if the mod had made them.
    /// </remarks>
    public bool BaseMayBeOlder { get; }

    /// <summary>
    /// Gets the base's version of the tuning (canonical text, see <see cref="TuningXml"/>)
    /// </summary>
    public string? BaseText { get; }

    /// <summary>
    /// Gets the tuning in the game's new version
    /// </summary>
    public TuningArchiveEntry? GameEntry { get; }

    /// <summary>
    /// Gets the game's new version of the tuning (canonical text)
    /// </summary>
    public string? GameText { get; }

    /// <summary>
    /// Gets the key of the override in the package
    /// </summary>
    public ResourceKey Key { get; }

    /// <summary>
    /// Gets the merge of the mod's changes onto the game's new version, with every conflict resolved as the game's version, if the game changed the tuning since the base
    /// </summary>
    public TuningMergeResult? Merge { get; }

    /// <summary>
    /// Gets what the outcome means, for the creator
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the override (canonical text), if it could be read
    /// </summary>
    public string? ModText { get; }

    /// <summary>
    /// Gets the name of the tuning
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets what the game's new version means for the override
    /// </summary>
    public TuningPackageOverrideOutcome Outcome { get; }

    /// <summary>
    /// Gets whether the mod's SimData for the override is what the tuning in <see cref="TuningPackageUpdate.GetRecommendedTuning"/> needs, if SimData was checked
    /// </summary>
    public TuningPackageSimDataState SimData { get; internal set; }

    /// <summary>
    /// Gets what <see cref="SimData"/> means, for the creator
    /// </summary>
    public string? SimDataMessage { get; internal set; }

    /// <summary>
    /// Merges the mod's changes onto the game's new version again, resolving each conflict as <paramref name="resolve"/> says
    /// </summary>
    /// <exception cref="InvalidOperationException">The game did not change the tuning since the base, so there is nothing to merge</exception>
    public TuningMergeResult Remerge(Func<TuningMergeConflict, TuningMergeChoice> resolve)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        if (Merge is null || BaseText is null || ModText is null || GameText is null)
            throw new InvalidOperationException("There is nothing to merge for this override");
        return TuningMerge.Merge(BaseText, ModText, GameText, resolve);
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Name ?? Key.ToString()}: {Outcome}";
}