namespace LlamaLogic.TuningArchive;

/// <summary>
/// Generates the SimData that overrides of a snapshot's tuning need, trusting the result only when the generator reproduces the game's own SimData for the original tuning
/// </summary>
/// <remarks>
/// The generator learns the entries of dynamic enumerations from the snapshot's module tuning, the names of its tuning, and, for each tuning, the column orders of its own SimData.
/// Strings the game writes as null and the generator writes as empty count as the same, since the game and Sims 4 Studio read both as empty.
/// </remarks>
public sealed class TuningSimDataDrafter
{
    TuningSimDataDrafter(TuningArchiveFile archive, TuningArchiveSnapshot snapshot, TuningDescriptionSet descriptions, SimDataGenerator generator)
    {
        this.archive = archive;
        this.snapshot = snapshot;
        this.descriptions = descriptions;
        this.generator = generator;
    }

    readonly TuningArchiveFile archive;
    readonly TuningDescriptionSet descriptions;
    readonly SimDataGenerator generator;
    readonly List<SimDataModel> layouts = [];
    readonly ConcurrentDictionary<string, bool> sets = new(StringComparer.Ordinal);
    Dictionary<string, (int same, int different)>? observations;
    readonly TuningArchiveSnapshot snapshot;

    /// <summary>
    /// Creates a drafter for the tuning of the specified <paramref name="snapshot"/> in the specified <paramref name="archive"/>, which generates SimData with the specified <paramref name="descriptions"/>
    /// </summary>
    public static TuningSimDataDrafter Create(TuningArchiveFile archive, TuningArchiveSnapshot snapshot, TuningDescriptionSet descriptions)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(descriptions);
        return new(archive, snapshot, descriptions, CreateGenerator(archive, snapshot, descriptions));
    }

    static SimDataGenerator CreateGenerator(TuningArchiveFile archive, TuningArchiveSnapshot snapshot, TuningDescriptionSet descriptions)
    {
        var generator = new SimDataGenerator(descriptions);
        foreach (var entry in archive.GetEntries(snapshot, TuningArchiveResourceKind.Tuning))
            if (entry.Type == ResourceType.Tuning)
                generator.AddTuning(TuningXml.Parse(archive.GetText(entry)));
            else if (entry.Name is { } name)
                generator.AddTuning(new XDocument(new XElement("I", new XAttribute("n", name), new XAttribute("s", entry.Instance.ToString(CultureInfo.InvariantCulture)))));
        return generator;
    }

    /// <summary>
    /// Gets whether a column, as <c>Schema.column</c>, is a tunable set, whose items are the same in any order, by the tuning descriptions of an instance class named for the schema
    /// </summary>
    internal bool IsSet(string key) =>
        sets.GetOrAdd(key, _ =>
        {
            var separator = key.IndexOf('.', StringComparison.Ordinal);
            if (separator < 0 || !descriptions.InstanceClasses.Contains(key[..separator]))
                return false;
            var name = key[(separator + 1)..];
            return descriptions.GetInstance(key[..separator])?.GetEffectiveMembers().FirstOrDefault(member => member.Name == name) is { } member
                && descriptions.Resolve(member) is ListTunableDescription { AreEntriesUnique: true };
        });

    /// <summary>
    /// Gets whether the specified <paramref name="name"/> is, by the tuning descriptions, a scalar tunable of the specified <paramref name="instanceClass"/>, whose value tuning gives as text
    /// </summary>
    internal bool IsScalar(string instanceClass, string name) =>
        descriptions.InstanceClasses.Contains(instanceClass)
        && descriptions.GetInstance(instanceClass)?.GetEffectiveMembers().FirstOrDefault(member => member.Name == name) is { } member
        && descriptions.Resolve(member) is ScalarTunableDescription;

    /// <summary>
    /// Creates a generator for tuning that mods add, which knows the snapshot's tuning and the column orders of all of its SimData, and which the mods' tuning can be added to without changing this drafter's
    /// </summary>
    /// <remarks>
    /// Call with the drafter locked, after <see cref="GetReliableColumns"/>.
    /// </remarks>
    internal SimDataGenerator CreateAddedTuningGenerator()
    {
        var added = CreateGenerator(archive, snapshot, descriptions);
        foreach (var layout in layouts)
            added.AddLayouts(layout);
        return added;
    }

    /// <summary>
    /// Gets whether the generator reproduces a column, as <c>Schema.column</c>, in every SimData of the snapshot's that has it, learning which it does the first time
    /// </summary>
    /// <remarks>
    /// Call with the drafter locked.
    /// </remarks>
    internal Func<string, bool> GetReliableColumns(IProgress<TuningArchiveProgress>? progress, CancellationToken cancellationToken)
    {
        if (observations is null)
        {
            var learned = new Dictionary<string, (int same, int different)>(StringComparer.Ordinal);
            var schemaNames = new HashSet<string>(StringComparer.Ordinal);
            var tunings = archive.GetEntries(snapshot, TuningArchiveResourceKind.Tuning).ToList();
            var simDataByInstance = archive.GetEntries(snapshot, TuningArchiveResourceKind.SimData).GroupBy(entry => entry.Instance).ToDictionary(group => group.Key, group => (IReadOnlyList<TuningArchiveEntry>)[.. group]);
            for (var index = 0; index < tunings.Count; ++index)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (index % 500 == 0)
                    progress?.Report(new("Learning which SimData the tuning descriptions reproduce", index, tunings.Count));
                var tuning = tunings[index];
                if (!simDataByInstance.TryGetValue(tuning.Instance, out var candidates) || FindSimData(tuning, candidates) is not { } simDataEntry)
                    continue;
                try
                {
                    var game = SimDataModel.FromXml(XDocument.Parse(archive.GetText(simDataEntry)));
                    generator.AddLayouts(game);
                    if (game.Schemas.Any(schema => schemaNames.Add(schema.Name)))
                        layouts.Add(game);
                    var generated = generator.Generate(TuningXml.Parse(archive.GetText(tuning)));
                    TuningSimDataRepair.Observe(game, generated, learned, IsSet);
                }
                catch (Exception exception) when (exception is SimDataGenerationException or InvalidDataException or InvalidOperationException or XmlException)
                {
                }
            }
            progress?.Report(new("Learning which SimData the tuning descriptions reproduce", tunings.Count, tunings.Count));
            observations = learned;
        }
        var reliable = observations;
        return key => reliable.TryGetValue(key, out var counts) && counts.same > 0 && counts.different == 0;
    }

    internal static string GetComparable(SimDataModel simData)
    {
        var document = simData.ToXml();
        foreach (var value in document.Descendants("T").Where(element => element.IsEmpty))
            value.Value = string.Empty;
        return document.ToString(SaveOptions.DisableFormatting);
    }

    TuningArchiveEntry? FindSimData(TuningArchiveEntry tuning) =>
        FindSimData(tuning, archive.GetEntriesByInstance(snapshot, TuningArchiveResourceKind.SimData, tuning.Instance));

    static TuningArchiveEntry? FindSimData(TuningArchiveEntry tuning, IReadOnlyList<TuningArchiveEntry> candidates)
    {
        if (candidates.Count <= 1)
            return candidates.FirstOrDefault();
        return candidates.FirstOrDefault(candidate => candidate.Name == tuning.Name)
            ?? candidates.FirstOrDefault(candidate => tuning.Type == ResourceType.Tuning && tuning.Name is { } module && candidate.Name?.StartsWith($"{module}.", StringComparison.Ordinal) is true);
    }

    /// <summary>
    /// Gets whether the generator reproduces the game's own SimData for the specified <paramref name="tuning"/> (<see langword="false"/> if the game has none)
    /// </summary>
    internal bool CanReproduce(TuningArchiveEntry tuning)
    {
        if (FindSimData(tuning) is not { } simDataEntry)
            return false;
        try
        {
            var game = SimDataModel.FromXml(XDocument.Parse(archive.GetText(simDataEntry)));
            generator.AddLayouts(game);
            var original = generator.Generate(TuningXml.Parse(archive.GetText(tuning)));
            original.Pack = game.Pack;
            original.Version = game.Version;
            return GetComparable(original) == GetComparable(game);
        }
        catch (Exception exception) when (exception is SimDataGenerationException or InvalidDataException or InvalidOperationException or XmlException)
        {
            return false;
        }
    }

    /// <summary>
    /// Generates the SimData an override of the specified <paramref name="tuning"/> with the specified <paramref name="draftText"/> needs
    /// </summary>
    public TuningSimDataDraftResult Draft(TuningArchiveEntry tuning, string draftText)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(draftText);
        if (FindSimData(tuning) is not { } simDataEntry)
            return new(TuningSimDataDraftKind.Unaffected, null, null, "The game has no SimData for this tuning.");
        var key = simDataEntry.Key;
        try
        {
            var game = SimDataModel.FromXml(XDocument.Parse(archive.GetText(simDataEntry)));
            generator.AddLayouts(game);
            SimDataModel generate(string text)
            {
                var simData = generator.Generate(TuningXml.Parse(text));
                simData.Pack = game.Pack;
                simData.Version = game.Version;
                return simData;
            }
            var original = generate(archive.GetText(tuning));
            var draft = generate(draftText);
            var draftComparable = GetComparable(draft);
            if (draftComparable == GetComparable(original))
                return new(TuningSimDataDraftKind.Unaffected, key, null, "The changes don't reach this tuning's SimData, so the game's SimData stays correct.");
            if (GetComparable(original) != GetComparable(game))
                return new(TuningSimDataDraftKind.Failed, key, null, "The changes reach this tuning's SimData, but its SimData can't be reproduced from the tuning descriptions, so new SimData couldn't be trusted; the game's SimData stays as it was.");
            return new(TuningSimDataDraftKind.Generated, key, draft, "The changes reach this tuning's SimData, so new SimData is generated with it.");
        }
        catch (Exception exception) when (exception is SimDataGenerationException or InvalidDataException or InvalidOperationException or XmlException)
        {
            return new(TuningSimDataDraftKind.Failed, key, null, $"This tuning's SimData couldn't be generated ({exception.Message}); the game's SimData stays as it was.");
        }
    }
}