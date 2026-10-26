namespace LlamaLogic.TuningArchive;

/// <summary>
/// Merges a mod's changes to a tuning onto the game's new version of it: a three-way merge of the version the mod was made from (the base), the mod's version, and the game's new version
/// </summary>
/// <remarks>
/// <para>
/// Elements are matched by name (the element of a variant by the variant's option), and list items are aligned, so that a changed item is not mistaken for one removed and another added: items are the same item when their XML is the same, or, for items with members, when at least half of their values are.
/// </para>
/// <para>
/// Where only one side changed an element, the merge takes that side's version.
/// Where both changed it differently, or one removed an item the other changed, that is a conflict, resolved as the caller says (by default, the game's version).
/// Items both sides added at the same place are all kept, the game's first, and an item both added is kept once (where the mod put it, if they put it in different places).
/// Attributes merge the same way, so a change the mod made to the root element's attributes is kept.
/// </para>
/// </remarks>
public static class TuningMerge
{
    const long maximumAlignmentCells = 4_000_000;

    sealed class Context
    {
        public Context(Func<TuningMergeConflict, TuningMergeChoice>? resolve) =>
            this.resolve = resolve;

        readonly Func<TuningMergeConflict, TuningMergeChoice>? resolve;

        public List<TuningMergeConflict> Conflicts { get; } = [];

        public TuningMergeChoice Conflict(TuningMergeConflictKind kind, string path, string? baseValue, string? modValue, string? gameValue)
        {
            var conflict = new TuningMergeConflict(Conflicts.Count, kind, path, baseValue, modValue, gameValue);
            Conflicts.Add(conflict);
            return resolve?.Invoke(conflict) ?? TuningMergeChoice.Game;
        }
    }

    /// <summary>
    /// Merges the changes the mod made in <paramref name="modText"/> to <paramref name="baseText"/> onto <paramref name="gameText"/>, resolving each conflict as <paramref name="resolve"/> says (by default, the game's version)
    /// </summary>
    /// <param name="baseText">The version of the tuning the mod was made from</param>
    /// <param name="modText">The mod's version of the tuning</param>
    /// <param name="gameText">The game's new version of the tuning</param>
    /// <param name="resolve">Chooses a side for each conflict, or <see langword="null"/> to take the game's version every time</param>
    public static TuningMergeResult Merge(string baseText, string modText, string gameText, Func<TuningMergeConflict, TuningMergeChoice>? resolve = null)
    {
        ArgumentNullException.ThrowIfNull(baseText);
        ArgumentNullException.ThrowIfNull(modText);
        ArgumentNullException.ThrowIfNull(gameText);
        var baseRoot = TuningXml.Parse(baseText).Root ?? throw new ArgumentException("The base has no root element", nameof(baseText));
        var modRoot = TuningXml.Parse(modText).Root ?? throw new ArgumentException("The mod's version has no root element", nameof(modText));
        var gameRoot = TuningXml.Parse(gameText).Root ?? throw new ArgumentException("The game's version has no root element", nameof(gameText));
        var context = new Context(resolve);
        var merged = MergeElement(baseRoot, modRoot, gameRoot, string.Empty, context) ?? new XElement(gameRoot);
        return new(TuningXml.GetCanonicalText(merged), context.Conflicts);
    }

    static string? GetText(XElement? element) =>
        element is null ? null : TuningXml.GetFragmentText(element);

    static bool AreSame(XElement? a, XElement? b) =>
        a is null ? b is null : b is not null && TuningXml.HaveSameFragmentText(a, b);

    static XElement? Copy(XElement? element) =>
        element is null ? null : new XElement(element);

    static TuningMergeConflictKind GetConflictKind(object? baseValue, object? modValue, object? gameValue) =>
        baseValue is null ? TuningMergeConflictKind.BothChanged
        : modValue is null ? TuningMergeConflictKind.ModRemovedGameChanged
        : gameValue is null ? TuningMergeConflictKind.GameRemovedModChanged
        : TuningMergeConflictKind.BothChanged;

    static string GetChildName(XElement parent, XElement child) =>
        child.Attribute("n")?.Value ?? (parent.Name.LocalName == "V" ? parent.Attribute("t")?.Value ?? string.Empty : string.Empty);

    static XElement? MergeElement(XElement? baseElement, XElement? modElement, XElement? gameElement, string path, Context context)
    {
        if (AreSame(modElement, baseElement))
            return Copy(gameElement);
        if (AreSame(gameElement, baseElement) || AreSame(modElement, gameElement))
            return Copy(modElement);
        if (baseElement is null || modElement is null || gameElement is null
            || baseElement.Name != modElement.Name || baseElement.Name != gameElement.Name
            || (baseElement.Name.LocalName == "V" && (baseElement.Attribute("t")?.Value != modElement.Attribute("t")?.Value || baseElement.Attribute("t")?.Value != gameElement.Attribute("t")?.Value))
            || (!baseElement.HasElements && !modElement.HasElements && !gameElement.HasElements))
            return context.Conflict(GetConflictKind(baseElement, modElement, gameElement), path, GetText(baseElement), GetText(modElement), GetText(gameElement)) is TuningMergeChoice.Mod
                ? Copy(modElement)
                : Copy(gameElement);
        if (baseElement.Name.LocalName == "L")
            return MergeList(baseElement, modElement, gameElement, path, context);
        var result = new XElement(gameElement.Name, MergeAttributes(baseElement, modElement, gameElement, path, context));
        var names = new List<string>();
        foreach (var child in gameElement.Elements())
        {
            var name = GetChildName(gameElement, child);
            if (!names.Contains(name))
                names.Add(name);
        }
        string? previous = null;
        foreach (var child in modElement.Elements())
        {
            var name = GetChildName(modElement, child);
            if (!names.Contains(name))
                names.Insert(previous is null ? 0 : names.IndexOf(previous) + 1, name);
            previous = name;
        }
        foreach (var name in names)
        {
            XElement? pick(XElement parent) =>
                parent.Elements().FirstOrDefault(child => GetChildName(parent, child) == name);
            if (MergeElement(pick(baseElement), pick(modElement), pick(gameElement), path.Length == 0 ? name : $"{path}.{name}", context) is { } merged)
                result.Add(merged);
        }
        return result;
    }

    static List<XAttribute> MergeAttributes(XElement baseElement, XElement modElement, XElement gameElement, string path, Context context)
    {
        var names = gameElement.Attributes().Select(attribute => attribute.Name).ToList();
        names.AddRange(modElement.Attributes().Select(attribute => attribute.Name).Where(name => !names.Contains(name)));
        var attributes = new List<XAttribute>();
        foreach (var name in names)
        {
            var baseValue = baseElement.Attribute(name)?.Value;
            var modValue = modElement.Attribute(name)?.Value;
            var gameValue = gameElement.Attribute(name)?.Value;
            var value = modValue == baseValue ? gameValue
                : gameValue == baseValue || modValue == gameValue ? modValue
                : context.Conflict(GetConflictKind(baseValue, modValue, gameValue), $"{path}@{name.LocalName}", baseValue, modValue, gameValue) is TuningMergeChoice.Mod ? modValue : gameValue;
            if (value is not null)
                attributes.Add(new XAttribute(name, value));
        }
        return attributes;
    }

    sealed class ListSide
    {
        public ListSide(XElement list)
        {
            Items = [.. list.Elements()];
            Texts = [.. Items.Select(TuningXml.GetFragmentText)];
            leaves = new HashSet<string>?[Items.Length];
        }

        readonly HashSet<string>?[] leaves;

        public XElement[] Items { get; }

        public string[] Texts { get; }

        public HashSet<string> GetLeaves(int index) =>
            leaves[index] ??= Items[index].DescendantsAndSelf().Where(element => !element.HasElements).Select(element => $"{element.Attribute("n")?.Value}={element.Value}").ToHashSet(StringComparer.Ordinal);
    }

    static bool AreSimilar(ListSide a, int aIndex, ListSide b, int bIndex)
    {
        var aItem = a.Items[aIndex];
        var bItem = b.Items[bIndex];
        if (!aItem.HasElements || !bItem.HasElements || aItem.Name != bItem.Name)
            return false;
        var aLeaves = a.GetLeaves(aIndex);
        var bLeaves = b.GetLeaves(bIndex);
        return aLeaves.Count(bLeaves.Contains) * 2 >= Math.Max(aLeaves.Count, bLeaves.Count);
    }

    static int[]? Align(ListSide baseSide, ListSide otherSide)
    {
        var baseLength = baseSide.Texts.Length;
        var otherLength = otherSide.Texts.Length;
        var map = Enumerable.Repeat(-1, baseLength).ToArray();
        var prefix = 0;
        while (prefix < baseLength && prefix < otherLength && baseSide.Texts[prefix] == otherSide.Texts[prefix])
        {
            map[prefix] = prefix;
            ++prefix;
        }
        var suffix = 0;
        while (suffix < baseLength - prefix && suffix < otherLength - prefix && baseSide.Texts[baseLength - 1 - suffix] == otherSide.Texts[otherLength - 1 - suffix])
        {
            map[baseLength - 1 - suffix] = otherLength - 1 - suffix;
            ++suffix;
        }
        var b = baseLength - prefix - suffix;
        var x = otherLength - prefix - suffix;
        if ((long)(b + 1) * (x + 1) > maximumAlignmentCells)
            return null;
        var width = x + 1;
        var cost = new int[(b + 1) * width];
        for (var i = b; i >= 0; --i)
            for (var j = x; j >= 0; --j)
            {
                if (i == b)
                {
                    cost[i * width + j] = x - j;
                    continue;
                }
                if (j == x)
                {
                    cost[i * width + j] = b - i;
                    continue;
                }
                var best = Math.Min(cost[(i + 1) * width + j], cost[i * width + j + 1]) + 1;
                if (baseSide.Texts[prefix + i] == otherSide.Texts[prefix + j])
                    best = Math.Min(best, cost[(i + 1) * width + j + 1]);
                else if (AreSimilar(baseSide, prefix + i, otherSide, prefix + j))
                    best = Math.Min(best, cost[(i + 1) * width + j + 1] + 1);
                cost[i * width + j] = best;
            }
        int p = 0, q = 0;
        while (p < b && q < x)
        {
            var same = baseSide.Texts[prefix + p] == otherSide.Texts[prefix + q];
            if (same && cost[p * width + q] == cost[(p + 1) * width + q + 1])
                map[prefix + p++] = prefix + q++;
            else if (!same && cost[p * width + q] == cost[(p + 1) * width + q + 1] + 1 && AreSimilar(baseSide, prefix + p, otherSide, prefix + q))
                map[prefix + p++] = prefix + q++;
            else if (cost[p * width + q] == cost[(p + 1) * width + q] + 1)
                ++p;
            else
                ++q;
        }
        return map;
    }

    static List<XElement>[] GetInsertions(int[] map, XElement[] items)
    {
        var insertions = Enumerable.Range(0, map.Length + 1).Select(_ => new List<XElement>()).ToArray();
        var mapped = map.Where(index => index >= 0).ToHashSet();
        var anchor = 0;
        var next = 0;
        for (var j = 0; j < items.Length; ++j)
        {
            if (mapped.Contains(j))
            {
                while (next < map.Length && map[next] != j)
                    ++next;
                anchor = next + 1;
                ++next;
                continue;
            }
            insertions[anchor].Add(items[j]);
        }
        return insertions;
    }

    static XElement MergeList(XElement baseList, XElement modList, XElement gameList, string path, Context context)
    {
        var baseSide = new ListSide(baseList);
        var modSide = new ListSide(modList);
        var gameSide = new ListSide(gameList);
        var result = new XElement(gameList.Name, MergeAttributes(baseList, modList, gameList, path, context));
        if (Align(baseSide, modSide) is not { } modMap || Align(baseSide, gameSide) is not { } gameMap)
        {
            var chosen = context.Conflict(TuningMergeConflictKind.TooLarge, path, GetText(baseList), GetText(modList), GetText(gameList)) is TuningMergeChoice.Mod ? modSide : gameSide;
            result.Add(chosen.Items.Select(item => new XElement(item)));
            return result;
        }
        var modInsertions = GetInsertions(modMap, modSide.Items);
        var gameInsertions = GetInsertions(gameMap, gameSide.Items);
        for (var anchor = 0; anchor < modInsertions.Length; ++anchor)
        {
            var gameTexts = gameInsertions[anchor].Select(TuningXml.GetFragmentText).ToList();
            modInsertions[anchor] = [.. modInsertions[anchor].Where(item => !gameTexts.Remove(TuningXml.GetFragmentText(item)))];
        }
        var modElsewhere = modInsertions.SelectMany(insertions => insertions).GroupBy(TuningXml.GetFragmentText, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        if (modElsewhere.Count > 0)
            for (var anchor = 0; anchor < gameInsertions.Length; ++anchor)
                gameInsertions[anchor] = [.. gameInsertions[anchor].Where(item =>
                {
                    var text = TuningXml.GetFragmentText(item);
                    if (!modElsewhere.TryGetValue(text, out var count) || count == 0)
                        return true;
                    modElsewhere[text] = count - 1;
                    return false;
                })];
        var items = new List<XElement>();
        void insert(int anchor)
        {
            items.AddRange(gameInsertions[anchor].Select(item => new XElement(item)));
            items.AddRange(modInsertions[anchor].Select(item => new XElement(item)));
        }
        insert(0);
        for (var k = 0; k < baseSide.Items.Length; ++k)
        {
            var itemPath = $"{path}[{items.Count}]";
            var inMod = modMap[k] >= 0;
            var inGame = gameMap[k] >= 0;
            if (inMod && inGame)
            {
                if (MergeElement(baseSide.Items[k], modSide.Items[modMap[k]], gameSide.Items[gameMap[k]], itemPath, context) is { } merged)
                    items.Add(merged);
            }
            else if (inGame)
            {
                if (gameSide.Texts[gameMap[k]] != baseSide.Texts[k] && context.Conflict(TuningMergeConflictKind.ModRemovedGameChanged, itemPath, baseSide.Texts[k], null, gameSide.Texts[gameMap[k]]) is TuningMergeChoice.Game)
                    items.Add(new XElement(gameSide.Items[gameMap[k]]));
            }
            else if (inMod)
            {
                if (modSide.Texts[modMap[k]] != baseSide.Texts[k] && context.Conflict(TuningMergeConflictKind.GameRemovedModChanged, itemPath, baseSide.Texts[k], modSide.Texts[modMap[k]], null) is TuningMergeChoice.Mod)
                    items.Add(new XElement(modSide.Items[modMap[k]]));
            }
            insert(k + 1);
        }
        result.Add(items);
        return result;
    }
}