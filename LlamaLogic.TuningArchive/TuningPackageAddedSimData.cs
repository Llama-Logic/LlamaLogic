namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents the SimData of tuning a package adds (rather than overrides), checked against SimData generated from the tuning with the tuning descriptions of the game version the package is being updated for
/// </summary>
/// <remarks>
/// The tuning descriptions say which columns the game version expects, so SimData made for an older version can be found missing columns without the game having SimData for the tuning.
/// </remarks>
public sealed class TuningPackageAddedSimData
{
    internal TuningPackageAddedSimData(ResourceKey key, ResourceKey tuningKey, string? name, bool hasScriptBeside, string? problem, SimDataModel? simData, SimDataModel? generated, Func<string, bool> isReliable, Func<string, bool> isSet, IReadOnlyList<(string name, string value)> leftOutTunables, ReadOnlyMemory<byte>? amendedTuning, SimDataModel? amendedGenerated)
    {
        Key = key;
        TuningKey = tuningKey;
        Name = name;
        HasScriptBeside = hasScriptBeside;
        this.simData = simData;
        this.generated = generated;
        this.isReliable = isReliable;
        this.isSet = isSet;
        this.amendedTuning = amendedTuning;
        this.amendedGenerated = amendedGenerated;
        LeftOutTunables = [.. leftOutTunables.Select(tunable => $"{tunable.name} ({tunable.value})")];
        if (problem is not null || simData is null || generated is null)
        {
            Problems = TuningPackageAddedSimDataProblems.Unchecked;
            Message = $"Its SimData couldn't be checked: {problem}";
            MissingColumns = [];
            MistypedColumns = [];
            ExtraColumns = [];
            MismatchedHashes = [];
            return;
        }
        var classification = TuningSimDataRepair.Classify(simData, generated, isSet);
        Problems = classification.Problems;
        MissingColumns = classification.MissingColumns;
        MistypedColumns = classification.MistypedColumns;
        ExtraColumns = classification.ExtraColumns;
        MismatchedHashes = classification.MismatchedHashes;
        var parts = new List<string>();
        if (MissingColumns.Count > 0)
            parts.Add($"The tuning descriptions give its SimData {Count(MissingColumns.Count, "column")} it doesn't have ({List(MissingColumns)}).");
        if (MistypedColumns.Count > 0)
            parts.Add($"{Count(MistypedColumns.Count, "column", true)} other types than the tuning descriptions give ({List(MistypedColumns)}).");
        if (ExtraColumns.Count > 0)
            parts.Add($"It has {Count(ExtraColumns.Count, "column")} the tuning descriptions don't give ({List(ExtraColumns)}){(HasScriptBeside ? ", and a script mod beside it could add those tunables to the game" : string.Empty)}.");
        if (MismatchedHashes.Count > 0)
            parts.Add($"The schema {(MismatchedHashes.Count == 1 ? "hash" : "hashes")} of {List(MismatchedHashes)} {(MismatchedHashes.Count == 1 ? "doesn't" : "don't")} match {(MismatchedHashes.Count == 1 ? "its" : "their")} columns.");
        if (classification.FirstDifferingColumn is { } differing)
            parts.Add($"Some of its values aren't what its tuning makes, starting with {differing}, and only you know which is right.");
        if (LeftOutTunables.Count > 0)
            parts.Add($"Its tuning leaves out {List(LeftOutTunables)}, which its SimData {(LeftOutTunables.Count == 1 ? "has" : "have")}.");
        Message = string.Join(" ", parts);
    }

    readonly SimDataModel? amendedGenerated;
    readonly ReadOnlyMemory<byte>? amendedTuning;
    readonly SimDataModel? generated;
    readonly Func<string, bool> isReliable;
    readonly Func<string, bool> isSet;
    readonly Dictionary<TuningPackageAddedSimDataFixes, TuningPackageAddedSimDataPlan> plans = [];
    readonly SimDataModel? simData;

    /// <summary>
    /// Gets the columns, as <c>Schema.column</c>, the SimData has that the tuning descriptions don't give
    /// </summary>
    public IReadOnlyList<string> ExtraColumns { get; }

    /// <summary>
    /// Gets whether the package has a script mod beside it, in the same folder, which could add tunables to the game that no tuning description has
    /// </summary>
    public bool HasScriptBeside { get; }

    /// <summary>
    /// Gets the key of the SimData
    /// </summary>
    public ResourceKey Key { get; }

    /// <summary>
    /// Gets what is wrong with the SimData, for the creator
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the schemas whose hashes don't match their columns
    /// </summary>
    public IReadOnlyList<string> MismatchedHashes { get; }

    /// <summary>
    /// Gets the columns, as <c>Schema.column</c>, the tuning descriptions give that the SimData doesn't have
    /// </summary>
    public IReadOnlyList<string> MissingColumns { get; }

    /// <summary>
    /// Gets the tunables, with values, that the tuning leaves out (so the tuning descriptions' defaults apply) but the SimData has other values for, which adding to the tuning makes the SimData generated from it have
    /// </summary>
    public IReadOnlyList<string> LeftOutTunables { get; }

    /// <summary>
    /// Gets the columns, as <c>Schema.column</c>, whose types aren't what the tuning descriptions give
    /// </summary>
    public IReadOnlyList<string> MistypedColumns { get; }

    /// <summary>
    /// Gets the name of the tuning
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets what is wrong with the SimData
    /// </summary>
    public TuningPackageAddedSimDataProblems Problems { get; }

    /// <summary>
    /// Gets the key of the tuning
    /// </summary>
    public ResourceKey TuningKey { get; }

    static string Count(int count, string noun, bool have = false) =>
        $"{(count == 1 ? $"one {noun}" : $"{count:N0} {noun}s")}{(have ? count == 1 ? " has" : " have" : string.Empty)}";

    static string List(IReadOnlyList<string> items) =>
        items.Count <= 6 ? string.Join(", ", items) : $"{string.Join(", ", items.Take(6))} and {items.Count - 6:N0} more";

    static string DescribeFixed(TuningPackageAddedSimDataProblems problems)
    {
        var parts = new List<string>();
        if (problems.HasFlag(TuningPackageAddedSimDataProblems.MissingColumns))
            parts.Add("adds the missing columns with values generated from its tuning");
        if (problems.HasFlag(TuningPackageAddedSimDataProblems.MistypedColumns))
            parts.Add("gives its columns the types the tuning descriptions give, with values generated from its tuning");
        if (problems.HasFlag(TuningPackageAddedSimDataProblems.ExtraColumns))
            parts.Add("removes the columns the tuning descriptions don't give");
        if (problems.HasFlag(TuningPackageAddedSimDataProblems.MismatchedHashes))
            parts.Add("corrects its schema hashes");
        return parts.Count switch
        {
            0 => string.Empty,
            1 => parts[0],
            _ => $"{string.Join(", ", parts.Take(parts.Count - 1))} and {parts[^1]}"
        };
    }

    /// <summary>
    /// Gets what the specified <paramref name="fixes"/> do to the SimData
    /// </summary>
    public TuningPackageAddedSimDataPlan Plan(TuningPackageAddedSimDataFixes fixes)
    {
        lock (plans)
        {
            if (!plans.TryGetValue(fixes, out var plan))
                plans[fixes] = plan = CreatePlan(fixes);
            return plan;
        }
    }

    TuningPackageAddedSimDataPlan CreatePlan(TuningPackageAddedSimDataFixes fixes)
    {
        if (simData is null || generated is null || Problems is TuningPackageAddedSimDataProblems.Unchecked)
            return new(TuningPackageAddedSimDataProblems.None, Problems, "Nothing is changed, because it couldn't be checked.");
        if (Problems is TuningPackageAddedSimDataProblems.None || fixes is TuningPackageAddedSimDataFixes.None)
            return new(TuningPackageAddedSimDataProblems.None, Problems, "Nothing is changed.");
        var keptExtra = HasScriptBeside && fixes.HasFlag(TuningPackageAddedSimDataFixes.RemoveExtraColumns) && Problems.HasFlag(TuningPackageAddedSimDataProblems.ExtraColumns);
        if (HasScriptBeside)
            fixes &= ~TuningPackageAddedSimDataFixes.RemoveExtraColumns;
        var amends = fixes.HasFlag(TuningPackageAddedSimDataFixes.AmendTuning) && amendedTuning is not null && amendedGenerated is not null;
        var expected = amends ? amendedGenerated! : generated;
        var notes = new List<string>();
        if (keptExtra)
            notes.Add("Its extra columns are kept, because a script mod beside it could add those tunables to the game.");
        var (repaired, failure, rebuildFailure) = TuningSimDataRepair.Repair(simData, expected, fixes, isReliable);
        if (rebuildFailure is not null)
            notes.Add($"It isn't rebuilt from its tuning, because {rebuildFailure}.");
        var noteText = notes.Count > 0 ? $" {string.Join(" ", notes)}" : string.Empty;
        var amendment = amends ? $"The update adds {List(LeftOutTunables)} to its tuning, which left {(LeftOutTunables.Count == 1 ? "it" : "them")} out." : string.Empty;
        if (repaired is not null && (TuningSimDataRepair.Classify(repaired, expected, isSet).Problems & (TuningPackageAddedSimDataProblems.MissingColumns | TuningPackageAddedSimDataProblems.MistypedColumns)) is not TuningPackageAddedSimDataProblems.None)
        {
            repaired = null;
            failure = "the fixes couldn't give it the columns the tuning descriptions give";
        }
        if (repaired is null)
        {
            if (!amends)
                return new(TuningPackageAddedSimDataProblems.None, Problems, $"Nothing is changed, because {failure}.{noteText}");
            var left = TuningSimDataRepair.Classify(simData, expected, isSet).Problems;
            return new(Problems & ~left, left, $"{amendment} Its SimData isn't changed, because {failure}.{noteText}")
            {
                Tuning = amendedTuning
            };
        }
        var remaining = TuningSimDataRepair.Classify(repaired, expected, isSet).Problems;
        var fixedProblems = Problems & ~remaining;
        if (fixedProblems is TuningPackageAddedSimDataProblems.None && !amends)
            return new(TuningPackageAddedSimDataProblems.None, Problems, $"Nothing is changed, because none of the chosen fixes apply.{noteText}");
        var changesSimData = TuningSimDataDrafter.GetComparable(repaired) != TuningSimDataDrafter.GetComparable(simData);
        var rebuilt = changesSimData && fixes.HasFlag(TuningPackageAddedSimDataFixes.MatchTuning) && rebuildFailure is null;
        var simDataFixes = fixedProblems & ~TuningPackageAddedSimDataProblems.Values;
        var simDataMessage = !changesSimData ? string.Empty
            : rebuilt ? "The update rebuilds its SimData from its tuning."
            : $"The update {DescribeFixed(simDataFixes)}{(simDataFixes.HasFlag(TuningPackageAddedSimDataProblems.MissingColumns) || simDataFixes.HasFlag(TuningPackageAddedSimDataProblems.MistypedColumns) ? ", keeping the mod's other values" : string.Empty)}.";
        return new(fixedProblems, remaining, string.Join(" ", new[] { amendment, simDataMessage }.Where(part => part.Length > 0)) + noteText)
        {
            SimData = changesSimData ? repaired : null,
            Tuning = amends ? amendedTuning : null
        };
    }
}