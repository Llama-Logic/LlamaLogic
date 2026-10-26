namespace LlamaLogic.TuningArchive;

/// <summary>
/// Keeps the documents of a <see cref="TuningDescriptionSet"/> that analysis has used, so that analyzing many tunings does not read and parse the same descriptions repeatedly (🔒)
/// </summary>
public sealed class TuningDescriptionCache
{
    /// <summary>
    /// Initializes a new cache for the specified <paramref name="set"/>
    /// </summary>
    public TuningDescriptionCache(TuningDescriptionSet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        Set = set;
    }

    readonly ConcurrentDictionary<string, TuningDescriptionClass?> enums = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, TunableDescription>?> instanceMembers = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, TuningDescriptionInstance?> instances = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, TuningDescriptionModule?> modules = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, TunableDescription> fragments = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the set of tuning descriptions
    /// </summary>
    public TuningDescriptionSet Set { get; }

    /// <summary>
    /// Releases the documents the cache keeps
    /// </summary>
    public void Clear()
    {
        enums.Clear();
        instanceMembers.Clear();
        instances.Clear();
        modules.Clear();
        fragments.Clear();
    }

    internal void ClearInstances()
    {
        instanceMembers.Clear();
        instances.Clear();
    }

    internal TuningDescriptionClass? GetEnum(string staticEntries) =>
        enums.GetOrAdd(staticEntries, Set.GetEnum);

    internal TuningDescriptionInstance? GetInstance(string className) =>
        instances.GetOrAdd(className, Set.GetInstance);

    internal IReadOnlyDictionary<string, TunableDescription>? GetInstanceMembers(string className) =>
        instanceMembers.GetOrAdd(className, name => GetInstance(name) is { } instance
            ? instance.GetEffectiveMembers().Where(member => member.Name is not null).GroupBy(member => member.Name!, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal)
            : null);

    internal TuningDescriptionModule? GetModule(string name) =>
        modules.GetOrAdd(name, Set.GetModule);

    /// <summary>
    /// Gets the description the specified <paramref name="description"/> stands for: the description of the fragment it refers to if it is a <see cref="FragmentReferenceTunableDescription"/>, and otherwise itself
    /// </summary>
    /// <exception cref="InvalidDataException">The set does not have the fragment to which <paramref name="description"/> refers</exception>
    public TunableDescription Resolve(TunableDescription description)
    {
        ArgumentNullException.ThrowIfNull(description);
        return description is FragmentReferenceTunableDescription && description.Class is { } fragmentClass
            ? fragments.GetOrAdd(fragmentClass, _ => Set.Resolve(description))
            : description is FragmentReferenceTunableDescription
            ? Set.Resolve(description)
            : description;
    }
}