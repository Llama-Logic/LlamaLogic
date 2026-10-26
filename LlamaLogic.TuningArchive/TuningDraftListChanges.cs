namespace LlamaLogic.TuningArchive;

/// <summary>
/// Finds what a draft changed in a tuning in terms of lists: the items it added to and removed from each list, and the changes that are not to list items
/// </summary>
sealed class TuningDraftListChanges
{
    TuningDraftListChanges(IReadOnlyList<TuningDraftListChange> lists, IReadOnlyList<string> otherPaths)
    {
        Lists = lists;
        OtherPaths = otherPaths;
    }

    /// <summary>
    /// Gets the lists the draft changed, with the items it added (from the draft) and removed (from the original)
    /// </summary>
    public IReadOnlyList<TuningDraftListChange> Lists { get; }

    /// <summary>
    /// Gets the paths of the changes that are not additions to or removals from a list
    /// </summary>
    public IReadOnlyList<string> OtherPaths { get; }

    static void Compare(XElement? original, XElement? draft, string path, Func<string, bool> defaultsToEmpty, List<TuningDraftListChange> lists, List<string> otherPaths)
    {
        if (original is not null && draft is not null && TuningXml.HaveSameFragmentText(original, draft))
            return;
        if (original is null && draft is { Name.LocalName: "L" } && defaultsToEmpty(path))
            original = new XElement("L", draft.Attributes("n"));
        if (original is null || draft is null || original.Name != draft.Name || original.Attribute("t")?.Value != draft.Attribute("t")?.Value)
        {
            otherPaths.Add(path);
            return;
        }
        if (original.Name.LocalName == "L")
        {
            var remaining = original.Elements().ToList();
            var added = new List<XElement>();
            foreach (var item in draft.Elements())
            {
                if (remaining.FirstOrDefault(candidate => TuningXml.HaveSameFragmentText(candidate, item)) is { } match)
                    remaining.Remove(match);
                else
                    added.Add(item);
            }
            lists.Add(new(path, original, draft, added, remaining));
            return;
        }
        if (!original.HasElements && !draft.HasElements)
        {
            otherPaths.Add(path);
            return;
        }
        var option = original.Name.LocalName == "V" ? original.Attribute("t")?.Value : null;
        string childName(XElement child) =>
            child.Attribute("n")?.Value ?? option ?? string.Empty;
        var names = original.Elements().Concat(draft.Elements()).Select(childName).Distinct(StringComparer.Ordinal).ToList();
        foreach (var name in names)
            Compare(original.Elements().FirstOrDefault(child => childName(child) == name), draft.Elements().FirstOrDefault(child => childName(child) == name), path.Length == 0 ? name : $"{path}.{name}", defaultsToEmpty, lists, otherPaths);
    }

    /// <summary>
    /// Finds the changes in the specified <paramref name="target"/>, treating a list the original does not set, and whose tuning description gives it no default, as an empty list
    /// </summary>
    public static TuningDraftListChanges Find(TuningDraftTarget target)
    {
        var lists = new List<TuningDraftListChange>();
        var otherPaths = new List<string>();
        var emptyByDefault = target.Draft.Nodes.Where(node => node.Description is ListTunableDescription { Default: null or "" }).Select(node => node.Path).ToHashSet(StringComparer.Ordinal);
        Compare(target.Original.Root, target.Draft.Root, string.Empty, emptyByDefault.Contains, lists, otherPaths);
        return new(lists, otherPaths);
    }
}