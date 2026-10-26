namespace LlamaLogic.EpicTuningTime.Components.Shared;

/// <summary>
/// Turns a tuning analysis into editor annotations: hovers from the tuning descriptions, hints naming what references, string keys and animation state machine keys point to (as XML comments, the way extracted tuning has long shown them), hints flagging media the snapshot captured but lacks, and navigation targets
/// </summary>
static class AnnotationBuilder
{
    static string Escape(string text) =>
        text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("*", "\\*", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace("`", "\\`", StringComparison.Ordinal);

    static string Truncate(string text, int length)
    {
        var singleLine = text.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", " ⏎ ", StringComparison.Ordinal);
        return singleLine.Length <= length ? singleLine : $"{singleLine[..length]}…";
    }

    static string BuildHover(TuningNode node, bool isUnrecognized, TuningArchiveEntry? resolved, string? typeName, bool isTuning, string? stringText, int voiceVersions)
    {
        var hover = new StringBuilder();
        if (node.Description is { } description)
        {
            hover.Append(CultureInfo.InvariantCulture, $"**{Escape(description.Display ?? description.Name ?? node.Path)}** · `{node.Path}`");
            if (!string.IsNullOrWhiteSpace(description.Description))
                hover.Append("\n\n").Append(Escape(description.Description));
            var facts = new List<string>();
            if (description.Class is { } tunableClass)
                facts.Add($"*{Escape(tunableClass)}*");
            if (description is ScalarTunableDescription { Type: { } scalarType })
                facts.Add($"type `{scalarType}`");
            if (!string.IsNullOrEmpty(description.Default))
                facts.Add($"default `{Truncate(description.Default, 60)}`");
            if (description.IsDeprecated)
                facts.Add("**deprecated**");
            if (facts.Count > 0)
                hover.Append("\n\n").AppendJoin(" · ", facts);
        }
        else if (isUnrecognized)
            hover.Append(CultureInfo.InvariantCulture, $"`{node.Path}` · *not in these tuning descriptions*");
        switch (node.ValueKind)
        {
            case TuningValueKind.Reference when node.ReferenceType is not null:
                if (resolved is not null)
                    hover.Append(CultureInfo.InvariantCulture, $"\n\n---\n\n→ **{Escape(resolved.Name ?? string.Empty)}** ({typeName}, {resolved.Pack})\n\n*Ctrl+click or F12 to open*");
                else if (isTuning)
                    hover.Append(CultureInfo.InvariantCulture, $"\n\n---\n\n→ {typeName} `{node.Value}` is not in this snapshot");
                else
                    hover.Append(CultureInfo.InvariantCulture, $"\n\n---\n\n→ {typeName} `{node.Value}` (not tuning, so not in the archive)");
                break;
            case TuningValueKind.LocalizedString when stringText is not null:
                hover.Append(CultureInfo.InvariantCulture, $"\n\n---\n\n> {Escape(stringText).Replace("\n", "\n> ", StringComparison.Ordinal)}");
                break;
            case TuningValueKind.Enum when node.Enum is { } enumClass:
                hover.Append(CultureInfo.InvariantCulture, $"\n\n---\n\n`{enumClass.StaticEntriesName}`{(node.EnumValue is { } enumValue ? $" = {enumValue}" : " (value not found)")}");
                break;
            case TuningValueKind.ResourceKey when node.ResourceKey is { } resourceKey:
                hover.Append(CultureInfo.InvariantCulture, $"\n\n---\n\n{(Enum.IsDefined(resourceKey.Type) ? resourceKey.Type.ToString() : $"type 0x{(uint)resourceKey.Type:X8}")}");
                if (resolved is not null && voiceVersions > 0)
                    hover.Append(CultureInfo.InvariantCulture, $"\n\n→ {voiceVersions} voice versions, starting with **{Escape(resolved.Name ?? resolved.Key.ToString())}** ({resolved.Pack})\n\n*Ctrl+click or F12 to open it*");
                else if (resolved is not null)
                    hover.Append(CultureInfo.InvariantCulture, $"\n\n→ **{Escape(resolved.Name ?? resolved.Key.ToString())}** ({resolved.Pack})\n\n*Ctrl+click or F12 to open*");
                else if (isTuning)
                    hover.Append(CultureInfo.InvariantCulture, $"\n\n→ not in this snapshot");
                break;
        }
        return hover.ToString();
    }

    public static (IReadOnlyList<EditorAnnotation> annotations, IReadOnlyList<(ResourceType type, ulong instance)?> targets) Build(string text, TuningAnalysis analysis, SnapshotEntries entries, IReadOnlyDictionary<uint, string> strings, Func<ResourceType, string> getTypeName, SnapshotEntries? animationStateMachines = null, MediaEntries? media = null)
    {
        var lines = text.Split('\n');
        var annotations = new List<EditorAnnotation>();
        var targets = new List<(ResourceType type, ulong instance)?>();
        var unrecognized = analysis.UnrecognizedPaths.ToHashSet(StringComparer.Ordinal);
        foreach (var node in analysis.Nodes)
        {
            if (node.Line < 1 || node.Line > lines.Length)
                continue;
            var lineText = lines[node.Line - 1];
            var start = node.ValueStartColumn;
            var end = node.ValueEndColumn;
            if (start <= 0)
            {
                start = lineText.Length - lineText.TrimStart().Length + 1;
                end = lineText.Length + 1;
            }
            var isUnrecognized = node.Description is null && unrecognized.Contains(node.Path);
            var hasHover = node.Description is not null || isUnrecognized;
            string? hint = null;
            string? hintClass = null;
            (ResourceType type, ulong instance)? target = null;
            TuningArchiveEntry? resolved = null;
            string? typeName = null;
            var isTuning = false;
            string? stringText = null;
            var voiceVersions = 0;
            switch (node.ValueKind)
            {
                case TuningValueKind.Reference when node.ReferenceType is { } referenceType:
                    {
                        hasHover = true;
                        typeName = node.ReferenceTypeName ?? getTypeName(referenceType);
                        resolved = node.ReferenceInstance is { } instance ? entries.Find(referenceType, instance) : node.ReferenceName is { } name ? entries.All.FirstOrDefault(entry => entry.Type == referenceType && entry.Name == name) : null;
                        isTuning = TuningAnalysis.IsTuningReferenceType(typeName);
                        if (resolved is not null)
                        {
                            hint = $"<!--{resolved.Name}-->";
                            target = (resolved.Type, resolved.Instance);
                        }
                        else if (isTuning)
                        {
                            hint = $"<!--{typeName} not in this snapshot-->";
                            hintClass = "ta-hint-missing";
                        }
                        break;
                    }
                case TuningValueKind.LocalizedString when node.StringKey is { } key:
                    if (key == 0)
                        break;
                    if (strings.TryGetValue(key, out var foundText))
                    {
                        stringText = foundText;
                        hasHover = true;
                        hint = $"<!--{Truncate(foundText, 80)}-->";
                    }
                    else if (strings.Count > 0)
                    {
                        hint = "<!--string not found-->";
                        hintClass = "ta-hint-missing";
                    }
                    break;
                case TuningValueKind.Enum when node.Enum is not null:
                    hasHover = true;
                    break;
                case TuningValueKind.ResourceKey when node.ResourceKey is { } resourceKey:
                    hasHover = true;
                    if (TuningArchiveFile.GetMediaKind(resourceKey.Type) is { } mediaKind)
                    {
                        if (media is null || !media.Has(mediaKind) || resourceKey.FullInstance is 0)
                            break;
                        isTuning = true;
                        resolved = media.Resolve(resourceKey);
                        if (resolved is not null)
                            target = (resolved.Type, resolved.Instance);
                        else if (media.ResolveVoiceVersions(resourceKey) is { Count: > 0 } versions)
                        {
                            resolved = versions[0].entry;
                            target = (resolved.Type, resolved.Instance);
                            voiceVersions = versions.Count;
                            var childVersions = versions.Count(version => version.voiceCode.StartsWith('c'));
                            hint = childVersions > 0 ? $"<!--{versions.Count - childVersions} voice versions + {childVersions} child-->" : $"<!--{versions.Count} voice versions-->";
                        }
                        else
                        {
                            hint = $"<!--{MediaContent.GetSingularName(mediaKind)} not in this snapshot-->";
                            hintClass = "ta-hint-missing";
                        }
                        break;
                    }
                    if (resourceKey.Type is not ResourceType.AnimationStateMachine || animationStateMachines is not { All.Count: > 0 })
                        break;
                    isTuning = true;
                    resolved = animationStateMachines.Find(resourceKey.Type, resourceKey.FullInstance);
                    if (resolved is not null)
                    {
                        hint = $"<!--{resolved.Name ?? resolved.Key.ToString()}-->";
                        target = (resolved.Type, resolved.Instance);
                    }
                    else
                    {
                        hint = "<!--animation state machine not in this snapshot-->";
                        hintClass = "ta-hint-missing";
                    }
                    break;
            }
            if (!hasHover && hint is null && target is null)
                continue;
            annotations.Add(new(node.Line, start, end, hasHover ? () => BuildHover(node, isUnrecognized, resolved, typeName, isTuning, stringText, voiceVersions) : null, hint, hintClass, target is not null));
            targets.Add(target);
        }
        return (annotations, targets);
    }
}