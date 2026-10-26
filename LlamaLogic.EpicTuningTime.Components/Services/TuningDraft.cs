namespace LlamaLogic.EpicTuningTime.Components.Services;

/// <summary>
/// Represents changes a creator is drafting to a tuning: the tuning as the archive has it, and an edited copy
/// </summary>
sealed class TuningDraft
{
    public TuningDraft(TuningArchiveSnapshot snapshot, TuningArchiveEntry entry, string originalText, TuningDescriptionCache descriptions)
    {
        Snapshot = snapshot;
        Entry = entry;
        OriginalText = originalText;
        this.descriptions = descriptions;
        Original = TuningAnalysis.Analyze(originalText, descriptions);
        originalRoot = TuningXml.Parse(originalText).Root!;
        originalValues = GetValues(Original);
        document = TuningXml.Parse(originalText);
        Text = originalText;
        Draft = Original;
        ChangedPaths = new HashSet<string>(StringComparer.Ordinal);
    }

    readonly TuningDescriptionCache descriptions;
    readonly XElement originalRoot;
    readonly Dictionary<string, (string element, string? value)> originalValues;
    XDocument document;

    /// <summary>
    /// Gets the paths of the tunables whose values differ from the original, and of the tunables containing them
    /// </summary>
    public IReadOnlySet<string> ChangedPaths { get; private set; }

    /// <summary>
    /// Gets the analysis of the edited copy
    /// </summary>
    public TuningAnalysis Draft { get; private set; }

    /// <summary>
    /// Gets the tuning
    /// </summary>
    public TuningArchiveEntry Entry { get; }

    /// <summary>
    /// Gets the analysis of the tuning as the archive has it
    /// </summary>
    public TuningAnalysis Original { get; }

    /// <summary>
    /// Gets the canonical text of the tuning as the archive has it
    /// </summary>
    public string OriginalText { get; }

    /// <summary>
    /// Gets the snapshot the tuning is from
    /// </summary>
    public TuningArchiveSnapshot Snapshot { get; }

    /// <summary>
    /// Gets the canonical text of the edited copy
    /// </summary>
    public string Text { get; private set; }

    XElement Root =>
        document.Root!;

    static Dictionary<string, (string element, string? value)> GetValues(TuningAnalysis analysis)
    {
        var values = new Dictionary<string, (string element, string? value)>(StringComparer.Ordinal);
        foreach (var node in analysis.Nodes)
            values.TryAdd(node.Path, (node.ElementName, node.Value));
        return values;
    }

    static bool IsWithinAny(string path, HashSet<string> ancestors)
    {
        if (ancestors.Contains(path))
            return true;
        for (var index = 0; index < path.Length; ++index)
            if (path[index] is '.' or '[' && ancestors.Contains(path[..index]))
                return true;
        return false;
    }

    /// <summary>
    /// Adds a copy of the list item at the specified <paramref name="path"/> right after it
    /// </summary>
    public void Duplicate(string path)
    {
        if (TuningXml.FindElement(Root, path) is { } element)
        {
            element.AddAfterSelf(new XElement(element));
            Update();
        }
    }

    /// <summary>
    /// Removes the element at the specified <paramref name="path"/>: a list item, or a tunable, which then takes its default
    /// </summary>
    public void Remove(string path)
    {
        if (TuningXml.FindElement(Root, path) is { } element)
        {
            element.Remove();
            Update();
        }
    }

    /// <summary>
    /// Restores the element at the specified <paramref name="path"/> to what the original has there
    /// </summary>
    public void Revert(string path)
    {
        if (FindCounterparts(path) is not ({ } current, var original))
            return;
        if (original is not null)
            current.ReplaceWith(new XElement(original));
        else
            current.Remove();
        Update();
    }

    /// <summary>
    /// Finds the element at the specified <paramref name="path"/> in the edited copy and the element of the original it stands for (none if it was added), matching list items by their content rather than their position, since adding, copying and removing items moves the rest
    /// </summary>
    (XElement current, XElement? original)? FindCounterparts(string path)
    {
        XElement? current = Root;
        XElement? original = originalRoot;
        var position = 0;
        while (position < path.Length && current is not null)
        {
            if (path[position] == '[')
            {
                var close = path.IndexOf(']', position);
                if (close < 0 || !int.TryParse(path.AsSpan(position + 1, close - position - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var index))
                    return null;
                var currentItems = current.Elements().ToList();
                if (index >= currentItems.Count)
                    return null;
                original = original is null ? null : PairItems(currentItems, original.Elements().ToList())[index];
                current = currentItems[index];
                position = close + 1;
                continue;
            }
            if (path[position] == '.')
                ++position;
            var end = path.IndexOfAny(['.', '['], position);
            if (end < 0)
                end = path.Length;
            var name = path[position..end];
            XElement? child(XElement parent) =>
                parent.Elements().FirstOrDefault(element => element.Attribute("n")?.Value == name)
                ?? (parent.Name.LocalName == "V" && parent.Attribute("t")?.Value == name ? parent.Elements().FirstOrDefault(element => element.Attribute("n") is null) : null);
            current = child(current);
            original = original is null || original.Name.LocalName == "V" && original.Attribute("t")?.Value != current?.Parent?.Attribute("t")?.Value && current?.Parent?.Name.LocalName == "V" ? null : child(original);
            position = end;
        }
        return current is null ? null : (current, original);
    }

    static IReadOnlyList<XElement?> PairItems(IReadOnlyList<XElement> currentItems, IReadOnlyList<XElement> originalItems)
    {
        var pairs = new XElement?[currentItems.Count];
        var unused = originalItems.ToList();
        for (var index = 0; index < currentItems.Count; ++index)
        {
            var current = currentItems[index];
            if (unused.FirstOrDefault(item => TuningXml.HaveSameFragmentText(item, current)) is { } match)
            {
                pairs[index] = match;
                unused.Remove(match);
            }
        }
        for (var index = 0; index < currentItems.Count && unused.Count > 0; ++index)
            if (pairs[index] is null && currentItems[index] is var current && !originalItems.Any(item => TuningXml.HaveSameFragmentText(item, current)))
            {
                pairs[index] = unused[0];
                unused.RemoveAt(0);
            }
        return pairs;
    }

    /// <summary>
    /// Discards every change
    /// </summary>
    public void RevertAll()
    {
        document = TuningXml.Parse(OriginalText);
        Update();
    }

    /// <summary>
    /// Adds a new item, with its tunables' defaults, to the end of the list at the specified <paramref name="path"/>
    /// </summary>
    public void AddItem(string path, ListTunableDescription description)
    {
        if (TuningXml.FindElement(Root, path) is not { } list || description.Item is not { } item)
            return;
        list.Add(CreateElement(Resolve(item), null));
        Update();
    }

    XElement CreateElement(TunableDescription description, string? name)
    {
        var element = new XElement(description switch
        {
            EnumTunableDescription => "E",
            ListTunableDescription => "L",
            TupleTunableDescription => "U",
            VariantTunableDescription => "V",
            _ => "T"
        });
        if (name is not null)
            element.Add(new XAttribute("n", name));
        if (description is VariantTunableDescription variant)
        {
            if ((variant.Default is { Length: > 0 } defaultOption && defaultOption != "None" ? defaultOption : variant.Options.FirstOrDefault()?.Name) is { } option)
                element.Add(new XAttribute("t", option));
        }
        else if (description is ScalarTunableDescription or EnumTunableDescription && description.Default is { Length: > 0 } defaultValue && defaultValue != "None")
            element.Value = defaultValue;
        return element;
    }

    TunableDescription Resolve(TunableDescription description)
    {
        try
        {
            return descriptions.Resolve(description);
        }
        catch (InvalidDataException)
        {
            return description;
        }
    }

    /// <summary>
    /// Sets a tunable the tuning does not set, starting from its default, before the specified <paramref name="nextSiblingPath"/> if there is one
    /// </summary>
    public void SetUnset(string? parentPath, string name, TunableDescription description, string? nextSiblingPath)
    {
        var parent = parentPath is null ? Root : TuningXml.FindElement(Root, parentPath);
        if (parent is null)
            return;
        var element = CreateElement(Resolve(description), name);
        if (nextSiblingPath is not null && TuningXml.FindElement(Root, nextSiblingPath) is { } next && next.Parent == parent)
            next.AddBeforeSelf(element);
        else
            parent.Add(element);
        Update();
    }

    /// <summary>
    /// Chooses the specified <paramref name="option"/> of the variant at the specified <paramref name="path"/>, which then takes that option's defaults
    /// </summary>
    public void SetOption(string path, VariantTunableDescription description, string option)
    {
        if (TuningXml.FindElement(Root, path) is not { Name.LocalName: "V" } variant || variant.Attribute("t")?.Value == option)
            return;
        variant.RemoveNodes();
        variant.SetAttributeValue("t", option);
        if (description.GetOption(option) is { } optionDescription)
            variant.Add(CreateElement(Resolve(optionDescription), option));
        Update();
    }

    /// <summary>
    /// Sets the value of the element at the specified <paramref name="path"/>
    /// </summary>
    public void SetValue(string path, string value)
    {
        if (TuningXml.FindElement(Root, path) is { HasElements: false } element && element.Value != value)
        {
            element.Value = value;
            Update();
        }
    }

    void Update()
    {
        Text = TuningXml.GetCanonicalText(document);
        document = TuningXml.Parse(Text);
        Draft = TuningAnalysis.Analyze(Text, descriptions);
        var original = originalValues;
        var draft = GetValues(Draft);
        var changed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in draft.Keys)
            if (!original.TryGetValue(path, out var before) || before != draft[path])
                for (var prefix = path; ; )
                {
                    changed.Add(prefix);
                    var separator = prefix.LastIndexOfAny(['.', '[']);
                    if (separator < 0)
                        break;
                    prefix = prefix[..separator];
                }
        foreach (var path in original.Keys.Where(path => !draft.ContainsKey(path)))
        {
            var separator = path.LastIndexOfAny(['.', '[']);
            for (var prefix = separator < 0 ? null : path[..separator]; prefix is not null; )
            {
                changed.Add(prefix);
                separator = prefix.LastIndexOfAny(['.', '[']);
                prefix = separator < 0 ? null : prefix[..separator];
            }
        }
        var matchedItems = new HashSet<string>(StringComparer.Ordinal);
        foreach (var list in Draft.Nodes.Where(node => node.Description is ListTunableDescription && changed.Contains(node.Path) && !IsWithinAny(node.Path, matchedItems)))
        {
            if (TuningXml.FindElement(originalRoot, list.Path) is not { } originalList || TuningXml.FindElement(Root, list.Path) is not { } draftList)
                continue;
            var unmatched = originalList.Elements().ToList();
            var index = 0;
            foreach (var item in draftList.Elements())
            {
                var itemPath = $"{list.Path}[{index++}]";
                var match = unmatched.FindIndex(candidate => TuningXml.HaveSameFragmentText(candidate, item));
                if (match >= 0)
                {
                    unmatched.RemoveAt(match);
                    matchedItems.Add(itemPath);
                }
            }
        }
        changed.RemoveWhere(path => IsWithinAny(path, matchedItems));
        ChangedPaths = changed;
    }

    /// <summary>
    /// Gets whether the edited copy differs from the original
    /// </summary>
    public bool HasChanges =>
        !string.Equals(Text, OriginalText, StringComparison.Ordinal);
}