namespace LlamaLogic.TuningArchive;

/// <summary>
/// Compares two versions of a tuning document element by element, matching elements by name and aligning list items, rather than line by line
/// </summary>
public static class TuningSemanticDiff
{
    const long maximumAlignmentCells = 4_000_000;

    /// <summary>
    /// Compares the <paramref name="older"/> and <paramref name="newer"/> versions of a tuning document (canonical text, see <see cref="TuningXml"/>)
    /// </summary>
    public static IReadOnlyList<TuningSemanticChange> Compare(string older, string newer)
    {
        ArgumentNullException.ThrowIfNull(older);
        ArgumentNullException.ThrowIfNull(newer);
        var changes = new List<TuningSemanticChange>();
        var olderRoot = TuningXml.Parse(older).Root!;
        var newerRoot = TuningXml.Parse(newer).Root!;
        foreach (var attribute in olderRoot.Attributes().Select(attribute => attribute.Name).Union(newerRoot.Attributes().Select(attribute => attribute.Name)))
        {
            var olderValue = olderRoot.Attribute(attribute)?.Value;
            var newerValue = newerRoot.Attribute(attribute)?.Value;
            if (olderValue != newerValue)
                changes.Add(new(olderValue is null ? TuningSemanticChangeKind.Added : newerValue is null ? TuningSemanticChangeKind.Removed : TuningSemanticChangeKind.Changed, $"@{attribute.LocalName}", olderValue, newerValue, GetLine(olderRoot), GetLine(newerRoot)));
        }
        CompareChildren(olderRoot, newerRoot, string.Empty, changes);
        return changes;
    }

    static string Join(string parent, string child) =>
        parent.Length == 0 ? child : child.StartsWith('[') ? $"{parent}{child}" : $"{parent}.{child}";

    static int GetLine(XElement element) =>
        element is IXmlLineInfo lineInfo && lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0;

    static string Render(XElement element) =>
        element.HasElements ? element.ToString(SaveOptions.DisableFormatting) : element.Value;

    static void CompareElements(XElement older, XElement newer, string path, List<TuningSemanticChange> changes)
    {
        if (older.Name != newer.Name || older.Attribute("t")?.Value != newer.Attribute("t")?.Value || older.HasElements != newer.HasElements)
        {
            changes.Add(new(TuningSemanticChangeKind.Changed, path, Render(older), Render(newer), GetLine(older), GetLine(newer)));
            return;
        }
        if (!older.HasElements)
        {
            if (older.Value != newer.Value)
                changes.Add(new(TuningSemanticChangeKind.Changed, path, older.Value, newer.Value, GetLine(older), GetLine(newer)));
            return;
        }
        CompareChildren(older, newer, path, changes);
    }

    static void CompareChildren(XElement older, XElement newer, string path, List<TuningSemanticChange> changes)
    {
        var olderKeyed = older.Elements().Where(element => element.Attribute("n") is not null).GroupBy(element => element.Attribute("n")!.Value, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        foreach (var newerChild in newer.Elements().Where(element => element.Attribute("n") is not null))
        {
            var name = newerChild.Attribute("n")!.Value;
            if (olderKeyed.Remove(name, out var olderChild))
                CompareElements(olderChild, newerChild, Join(path, name), changes);
            else
                changes.Add(new(TuningSemanticChangeKind.Added, Join(path, name), null, Render(newerChild), 0, GetLine(newerChild)));
        }
        foreach (var (name, olderChild) in olderKeyed)
            changes.Add(new(TuningSemanticChangeKind.Removed, Join(path, name), Render(olderChild), null, GetLine(olderChild), 0));
        var olderItems = older.Elements().Where(element => element.Attribute("n") is null).ToList();
        var newerItems = newer.Elements().Where(element => element.Attribute("n") is null).ToList();
        if (olderItems.Count > 0 || newerItems.Count > 0)
            CompareItems(olderItems, newerItems, path, changes);
    }

    static string? GetMappingKey(XElement item) =>
        item.Elements().FirstOrDefault(child => child.Attribute("n")?.Value == "key") is { } key ? Render(key) : null;

    static void CompareItems(List<XElement> olderItems, List<XElement> newerItems, string path, List<TuningSemanticChange> changes)
    {
        if (olderItems.Count > 0 && newerItems.Count > 0 && olderItems.Concat(newerItems).All(item => GetMappingKey(item) is not null))
        {
            var olderByKey = olderItems.Select((item, index) => (item, index)).GroupBy(entry => GetMappingKey(entry.item)!, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            if (olderByKey.Count == olderItems.Count)
            {
                for (var newerIndex = 0; newerIndex < newerItems.Count; ++newerIndex)
                {
                    var newerItem = newerItems[newerIndex];
                    if (olderByKey.Remove(GetMappingKey(newerItem)!, out var olderEntry))
                        CompareElements(olderEntry.item, newerItem, $"{path}[{newerIndex}]", changes);
                    else
                        changes.Add(new(TuningSemanticChangeKind.Added, $"{path}[{newerIndex}]", null, Render(newerItem), 0, GetLine(newerItem)));
                }
                foreach (var (_, (item, index)) in olderByKey)
                    changes.Add(new(TuningSemanticChangeKind.Removed, $"{path}[{index}]", Render(item), null, GetLine(item), 0));
                return;
            }
        }
        var olderRendered = olderItems.Select(Render).ToList();
        var newerRendered = newerItems.Select(Render).ToList();
        var matches = (long)olderItems.Count * newerItems.Count <= maximumAlignmentCells
            ? Align(olderRendered, newerRendered)
            : [];
        var olderPosition = 0;
        var newerPosition = 0;
        foreach (var (olderMatch, newerMatch) in matches.Append((olderItems.Count, newerItems.Count)))
        {
            var removed = olderMatch - olderPosition;
            var added = newerMatch - newerPosition;
            var paired = Math.Min(removed, added);
            for (var i = 0; i < paired; ++i)
                CompareElements(olderItems[olderPosition + i], newerItems[newerPosition + i], $"{path}[{newerPosition + i}]", changes);
            for (var i = paired; i < removed; ++i)
                changes.Add(new(TuningSemanticChangeKind.Removed, $"{path}[{olderPosition + i}]", olderRendered[olderPosition + i], null, GetLine(olderItems[olderPosition + i]), 0));
            for (var i = paired; i < added; ++i)
                changes.Add(new(TuningSemanticChangeKind.Added, $"{path}[{newerPosition + i}]", null, newerRendered[newerPosition + i], 0, GetLine(newerItems[newerPosition + i])));
            olderPosition = olderMatch + 1;
            newerPosition = newerMatch + 1;
        }
    }

    static List<(int older, int newer)> Align(List<string> older, List<string> newer)
    {
        var width = newer.Count + 1;
        var lengths = new int[(older.Count + 1) * width];
        for (var i = older.Count - 1; i >= 0; --i)
            for (var j = newer.Count - 1; j >= 0; --j)
                lengths[i * width + j] = string.Equals(older[i], newer[j], StringComparison.Ordinal) ? lengths[(i + 1) * width + j + 1] + 1 : Math.Max(lengths[(i + 1) * width + j], lengths[i * width + j + 1]);
        var matches = new List<(int older, int newer)>();
        var x = 0;
        var y = 0;
        while (x < older.Count && y < newer.Count)
        {
            if (string.Equals(older[x], newer[y], StringComparison.Ordinal))
            {
                matches.Add((x, y));
                ++x;
                ++y;
            }
            else if (lengths[(x + 1) * width + y] >= lengths[x * width + y + 1])
                ++x;
            else
                ++y;
        }
        return matches;
    }
}