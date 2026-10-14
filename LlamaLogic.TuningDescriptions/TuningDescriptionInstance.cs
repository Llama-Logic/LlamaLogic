namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents the description of a class of instance tuning (an <c>Instance</c> element), such as <c>Buff</c> (🔒)
/// </summary>
/// <remarks>
/// <see cref="Members"/> holds only the tunables the class declares; <see cref="GetEffectiveMembers"/> adds those it inherits.
/// </remarks>
public sealed class TuningDescriptionInstance :
    TuningDescriptionDocument
{
    internal TuningDescriptionInstance(TuningDescriptionSet set, string path, XElement source, XElement? versionHistory) :
        base(set, path, source, versionHistory)
    {
        Class = Attributes.GetValueOrDefault("class") ?? string.Empty;
        Parents = [.. (Attributes.GetValueOrDefault("parents") ?? string.Empty).Split(',').Select(parent => parent.Trim()).Where(parent => parent.Length > 0)];
        Members = TunableDescription.CreateMembers(source);
        DeletedMembers = [.. source.Elements("Deleted").Select(deleted => deleted.Attribute("name")?.Value).OfType<string>()];
    }

    IReadOnlyList<TuningDescriptionInstance>? methodResolutionOrder;
    IReadOnlyList<(TunableDescription Member, TuningDescriptionInstance Declarer)>? effectiveMembers;

    /// <summary>
    /// Gets the Python class of the instance tuning (its <c>class</c> attribute)
    /// </summary>
    public string Class { get; }

    /// <summary>
    /// Gets the names of the inherited tunables the class removes (its <c>Deleted</c> elements)
    /// </summary>
    public IReadOnlyList<string> DeletedMembers { get; }

    /// <summary>
    /// Gets the description of the class for tuners (its <c>description</c> attribute)
    /// </summary>
    public string? Description =>
        Attributes.GetValueOrDefault("description");

    /// <summary>
    /// Gets the destinations to which the whole class is exported (its <c>export_modes</c> attribute), or <see langword="null"/> if the description does not specify them
    /// </summary>
    public TuningDescriptionExportModes? ExportModes =>
        TuningDescriptionUtilities.ParseExportModes(Attributes.GetValueOrDefault("export_modes"));

    /// <summary>
    /// Gets the type of instance tuning the class is (its <c>instance_type</c> attribute), such as <c>buff</c>
    /// </summary>
    public string? InstanceType =>
        Attributes.GetValueOrDefault("instance_type");

    /// <summary>
    /// Gets the tunables the class declares, in document order
    /// </summary>
    public IReadOnlyList<TunableDescription> Members { get; }

    /// <summary>
    /// Gets the Python module that defines the class (its <c>module</c> attribute)
    /// </summary>
    public string? Module =>
        Attributes.GetValueOrDefault("module");

    /// <summary>
    /// Gets the classes from which the class inherits (its <c>parents</c> attribute), in declaration order
    /// </summary>
    public IReadOnlyList<string> Parents { get; }

    /// <summary>
    /// Gets the tunables of the class, including those it inherits and excluding those it deletes
    /// </summary>
    /// <remarks>
    /// Each tunable is the description from the first class in <see cref="GetMethodResolutionOrder()"/> that declares or deletes it, as in Python.
    /// The tunables are in the order the game collects them: walking from the most basic class to this one, each class first removes the tunables it deletes and then adds or replaces those it declares, a newly added tunable going to the end.
    /// </remarks>
    /// <exception cref="InvalidDataException">The class's inheritance cannot be resolved (see <see cref="GetMethodResolutionOrder()"/>)</exception>
    public IReadOnlyList<TunableDescription> GetEffectiveMembers() =>
        [.. GetEffectiveMembersAndDeclarers().Select(memberAndDeclarer => memberAndDeclarer.Member)];

    internal IReadOnlyList<(TunableDescription Member, TuningDescriptionInstance Declarer)> GetEffectiveMembersAndDeclarers()
    {
        if (effectiveMembers is { } cached)
            return cached;
        var order = GetMethodResolutionOrder();
        var names = new List<string>();
        var members = new Dictionary<string, (TunableDescription Member, TuningDescriptionInstance Declarer)>(StringComparer.Ordinal);
        for (var i = order.Count - 1; i >= 0; --i)
        {
            var instance = order[i];
            foreach (var deleted in instance.DeletedMembers)
                if (members.Remove(deleted))
                    names.Remove(deleted);
            foreach (var member in instance.Members)
                if (member.Name is { } name)
                {
                    if (!members.ContainsKey(name))
                        names.Add(name);
                    members[name] = (member, instance);
                }
        }
        IReadOnlyList<(TunableDescription Member, TuningDescriptionInstance Declarer)> result = [.. names.Select(name => members[name])];
        effectiveMembers = result;
        return result;
    }

    /// <summary>
    /// Gets the tunable the class declares with the specified <paramref name="name"/>, or <see langword="null"/> if it declares none
    /// </summary>
    public TunableDescription? GetMember(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Members.FirstOrDefault(member => member.Name == name);
    }

    /// <summary>
    /// Gets this class followed by the classes from which it inherits, in the order Python searches them (the C3 linearization of <see cref="Parents"/>)
    /// </summary>
    /// <exception cref="InvalidDataException">A parent is not in the set, the inheritance is circular, or the parents cannot be linearized</exception>
    public IReadOnlyList<TuningDescriptionInstance> GetMethodResolutionOrder() =>
        GetMethodResolutionOrder([]);

    IReadOnlyList<TuningDescriptionInstance> GetMethodResolutionOrder(HashSet<string> visiting)
    {
        if (methodResolutionOrder is { } cached)
            return cached;
        if (!visiting.Add(Class))
            throw new InvalidDataException($"The inheritance of {Class} is circular");
        var parents = Parents.Select(parent => Set.GetInstance(parent) ?? throw new InvalidDataException($"{Class} inherits from {parent}, which is not in the set")).ToList();
        var sequences = parents.Select(parent => parent.GetMethodResolutionOrder(visiting).ToList()).Append(parents.ToList()).Where(sequence => sequence.Count > 0).ToList();
        var result = ImmutableArray.CreateBuilder<TuningDescriptionInstance>();
        result.Add(this);
        while (sequences.Count > 0)
        {
            var head = sequences.Select(sequence => sequence[0]).FirstOrDefault(candidate => !sequences.Any(sequence => sequence.Skip(1).Any(other => other.Class == candidate.Class)))
                ?? throw new InvalidDataException($"The parents of {Class} cannot be linearized");
            result.Add(head);
            foreach (var sequence in sequences)
                sequence.RemoveAll(other => other.Class == head.Class);
            sequences.RemoveAll(sequence => sequence.Count == 0);
        }
        visiting.Remove(Class);
        IReadOnlyList<TuningDescriptionInstance> linearization = result.ToImmutable();
        methodResolutionOrder = linearization;
        return linearization;
    }
}
