namespace LlamaLogic.EpicTuningTime.Components.Shared;

/// <summary>
/// Turns a tuning analysis into a tree of the tuning's tunables, optionally with the tunables its tuning descriptions declare but the tuning does not set
/// </summary>
static class PropertyTree
{
    public static IReadOnlyList<PropertyNode> Build(TuningAnalysis analysis, SnapshotEntries entries, IReadOnlyDictionary<uint, string> strings, bool includeUnset, IReadOnlySet<string>? includeUnsetWithin = null)
    {
        var roots = new List<PropertyNode>();
        var byPath = new Dictionary<string, PropertyNode>(StringComparer.Ordinal);
        foreach (var node in analysis.Nodes)
        {
            var parentPath = GetParentPath(node.Path);
            var parent = parentPath.Length > 0 ? byPath.GetValueOrDefault(parentPath) : null;
            var propertyNode = Create(node, entries, strings);
            propertyNode.Parent = parent;
            (parent?.ChildList ?? roots).Add(propertyNode);
            byPath[node.Path] = propertyNode;
        }
        if (includeUnset)
            AddUnset(roots, analysis.InstanceMembers, null, 0);
        foreach (var propertyNode in byPath.Values)
            if (propertyNode.Source is { Members.Count: > 0 } source && (includeUnset || includeUnsetWithin?.Contains(propertyNode.Path) is true))
                AddUnset(propertyNode.ChildList, source.Members, propertyNode, propertyNode.Line);
        var index = 0;
        void number(IEnumerable<PropertyNode> nodes, int depth)
        {
            foreach (var propertyNode in nodes)
            {
                propertyNode.Depth = depth;
                propertyNode.Index = index++;
                propertyNode.Summary = propertyNode.Description switch
                {
                    ListTunableDescription when propertyNode.IsSet => propertyNode.Children.Count switch
                    {
                        0 => "empty",
                        1 => "1 item",
                        var count => string.Format(CultureInfo.CurrentCulture, "{0:N0} items", count)
                    },
                    VariantTunableDescription => propertyNode.Source?.Option ?? propertyNode.Children.FirstOrDefault()?.Label,
                    _ => null
                };
                if (propertyNode.Children.Count > 0)
                    number(propertyNode.Children, depth + 1);
            }
        }
        number(roots, 0);
        return roots;
    }

    static void AddUnset(List<PropertyNode> children, IReadOnlyList<TunableDescription> members, PropertyNode? parent, int line)
    {
        if (members.Count == 0)
            return;
        var order = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var member in members)
            if (member.Name is { } name)
                order.TryAdd(name, order.Count);
        var set = children.Select(child => child.Label).ToHashSet(StringComparer.Ordinal);
        foreach (var member in members)
            if (member.Name is { } name && !member.IsDeprecated && !set.Contains(name))
                children.Add(new()
                {
                    Description = member,
                    IsSet = false,
                    Label = name,
                    Line = line,
                    Parent = parent,
                    Path = parent is null ? name : $"{parent.Path}.{name}",
                    Value = member.Default
                });
        var sorted = children.OrderBy(child => order.TryGetValue(child.Label, out var position) ? position : int.MaxValue).ToList();
        children.Clear();
        children.AddRange(sorted);
    }

    static PropertyNode Create(TuningNode node, SnapshotEntries entries, IReadOnlyDictionary<uint, string> strings)
    {
        var label = GetLastSegment(node.Path);
        string? detail = null;
        var isMissing = false;
        (ResourceType type, ulong instance)? target = null;
        switch (node.ValueKind)
        {
            case TuningValueKind.Reference when node.ReferenceType is { } referenceType:
                {
                    var resolved = node.ReferenceInstance is { } instance ? entries.Find(referenceType, instance) : node.ReferenceName is { } name ? entries.All.FirstOrDefault(entry => entry.Type == referenceType && entry.Name == name) : null;
                    if (resolved is not null)
                    {
                        detail = resolved.Name;
                        target = (resolved.Type, resolved.Instance);
                    }
                    else if (TuningAnalysis.IsTuningReferenceType(node.ReferenceTypeName ?? string.Empty))
                    {
                        detail = "not in this snapshot";
                        isMissing = true;
                    }
                    break;
                }
            case TuningValueKind.LocalizedString when node.StringKey is { } key and not 0:
                if (strings.TryGetValue(key, out var text))
                    detail = Truncate(text, 120);
                else if (strings.Count > 0)
                {
                    detail = "string not found";
                    isMissing = true;
                }
                break;
        }
        return new()
        {
            Description = node.Description,
            Detail = detail,
            IsMissing = isMissing,
            Label = label,
            Line = node.Line,
            Path = node.Path,
            Source = node,
            Target = target,
            Value = node.Value
        };
    }

    static string GetLastSegment(string path)
    {
        var separator = path.LastIndexOfAny(['.', '[']);
        return separator < 0 ? path : path[separator] == '[' ? path[separator..] : path[(separator + 1)..];
    }

    static string GetParentPath(string path)
    {
        var separator = path.LastIndexOfAny(['.', '[']);
        return separator < 0 ? string.Empty : path[..separator];
    }

    static string Truncate(string text, int length)
    {
        var singleLine = text.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", " ⏎ ", StringComparison.Ordinal);
        return singleLine.Length <= length ? singleLine : $"{singleLine[..length]}…";
    }
}