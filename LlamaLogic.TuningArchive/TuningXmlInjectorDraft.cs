namespace LlamaLogic.TuningArchive;

/// <summary>
/// Drafts an XML Injector snippet (for Scumbumbo's XML Injector, version 4) that makes changes to tuning without overriding it
/// </summary>
/// <remarks>
/// XML Injector can only add: interactions to objects, to the Sim object, to phones and to the relationship panel; interactions to affordance lists; loot actions to loot actions and random weighted loot; buffs to traits; states and state triggers to objects; name and object relationship components to objects that have none; and purchase list options and picker categories to purchase picker interactions.
/// Any other change, and any removal, is reported as an error and left out.
/// </remarks>
public static class TuningXmlInjectorDraft
{
    const ulong objectSimInstance = 14965;

    static readonly string[] keyOrder =
    [
        "add_interactions_to_objects",
        "add_interactions_to_sims",
        "add_interactions_to_phones",
        "add_interactions_to_relationship_panel",
        "add_mixer_interactions",
        "add_to_loot_actions",
        "add_to_random_loot_actions",
        "add_states_to_objects",
        "add_name_component_to_objects",
        "add_object_relationships_to_objects",
        "add_buffs_to_trait",
        "add_purchase_list_options_to_interactions",
        "add_picker_dialog_categories_to_interactions"
    ];

    static readonly HashSet<string> purchaseListOptions = new(["all_items", "specific_items", "inventory_items", "random_items", "tested_items"], StringComparer.Ordinal);

    static readonly HashSet<string> pickerCategoryMembers = new(["tag", "icon", "tooltip"], StringComparer.Ordinal);

    static string Describe(TuningDraftTarget target, string path)
    {
        var description = target.Draft.Nodes.FirstOrDefault(node => node.Path == path)?.Description ?? target.Original.Nodes.FirstOrDefault(node => node.Path == path)?.Description;
        return description?.Display ?? description?.Name ?? path;
    }

    static XElement? IsAddedComponent(TuningDraftTarget target, string path)
    {
        if (path is not ("_components.name" or "_components.object_relationships"))
            return null;
        if (TuningXml.FindElement(target.Draft.Root, path) is not { } draft || draft.Attribute("t")?.Value != "enabled")
            return null;
        return TuningXml.FindElement(target.Original.Root, path) is not { } original || original.Attribute("t")?.Value == "disabled" ? draft : null;
    }

    static XElement ObjectSelection(ulong instance) =>
        new("V", new XAttribute("n", "object_selection"), new XAttribute("t", "object_list"),
            new XElement("U", new XAttribute("n", "object_list"),
                new XElement("L", new XAttribute("n", "object_list"),
                    new XElement("T", instance.ToString(CultureInfo.InvariantCulture)))));

    static XElement Items(string name, IEnumerable<XElement> items) =>
        new("L", new XAttribute("n", name), items.Select(item => new XElement(item)));

    static XElement Reference(string name, ulong instance) =>
        new("T", new XAttribute("n", name), instance.ToString(CultureInfo.InvariantCulture));

    static XElement References(string name, ulong instance) =>
        new("L", new XAttribute("n", name), new XElement("T", instance.ToString(CultureInfo.InvariantCulture)));

    /// <summary>
    /// Drafts a snippet with the specified <paramref name="name"/> that makes the changes in the specified <paramref name="targets"/>
    /// </summary>
    public static TuningDraftResult Generate(IReadOnlyList<TuningDraftTarget> targets, string name)
    {
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(name);
        var diagnostics = new List<TuningDraftDiagnostic>();
        var entries = new Dictionary<string, List<XElement>>(StringComparer.Ordinal);
        var version = 1;
        var changes = 0;
        void add(string key, int keyVersion, IEnumerable<XElement> elements)
        {
            if (!entries.TryGetValue(key, out var list))
            {
                list = [];
                entries.Add(key, list);
            }
            list.AddRange(elements);
            version = Math.Max(version, keyVersion);
            ++changes;
        }
        foreach (var target in targets)
        {
            var entry = target.Entry;
            var root = target.Original.Root;
            var typeName = root.Attribute("i")?.Value;
            var className = root.Attribute("c")?.Value;
            var instance = entry.Instance;
            var found = TuningDraftListChanges.Find(target);
            foreach (var path in found.OtherPaths)
            {
                if (typeName == "object" && IsAddedComponent(target, path) is { } draftComponent)
                {
                    var isName = path == "_components.name";
                    var values = draftComponent.Elements().FirstOrDefault()?.Elements().Select(element => new XElement(element)) ?? [];
                    add(isName ? "add_name_component_to_objects" : "add_object_relationships_to_objects", 2,
                        [new XElement("U", ObjectSelection(instance), new XElement("U", new XAttribute("n", isName ? "name_component" : "object_relationships_component"), values))]);
                    diagnostics.Add(new(TuningDraftSeverity.Information, entry, path, $"Adds {Describe(target, path)} to the object (XML Injector won't if it already has one)."));
                    continue;
                }
                diagnostics.Add(new(TuningDraftSeverity.Error, entry, path, $"XML Injector can't change {Describe(target, path)}; it can only add items to certain lists."));
            }
            foreach (var list in found.Lists)
            {
                var what = Describe(target, list.Path);
                if (list.Removed.Count > 0)
                    diagnostics.Add(new(TuningDraftSeverity.Error, entry, list.Path, $"XML Injector can't remove items from {what}; it can only add."));
                if (list.Added.Count == 0)
                    continue;
                var added = list.Added;
                var dedupes = (typeName, list.Path) is ("object", "_super_affordances" or "_phone_affordances" or "_relation_panel_affordances") or ("snippet", "value") || typeName == "interaction" && list.Path.StartsWith("picker_dialog.", StringComparison.Ordinal);
                if (dedupes)
                {
                    var existing = list.Original.Elements().Select(TuningXml.GetFragmentText).ToHashSet(StringComparer.Ordinal);
                    added = [.. added.Where(item => !existing.Contains(TuningXml.GetFragmentText(item)))];
                    if (added.Count < list.Added.Count)
                        diagnostics.Add(new(TuningDraftSeverity.Error, entry, list.Path, $"{what} already has an item like the one added, and XML Injector skips items it already has there, so it can't add another."));
                    if (added.Count == 0)
                        continue;
                }
                var count = added.Count == 1 ? "an item" : $"{added.Count} items";
                switch (typeName, list.Path)
                {
                    case ("object", "_super_affordances") when instance == objectSimInstance:
                        add("add_interactions_to_sims", 1, added);
                        diagnostics.Add(new(TuningDraftSeverity.Information, entry, list.Path, $"Adds {count} to the interactions on Sims (XML Injector skips ones already there)."));
                        break;
                    case ("object", "_phone_affordances") when instance == objectSimInstance:
                        add("add_interactions_to_phones", 1, added);
                        diagnostics.Add(new(TuningDraftSeverity.Information, entry, list.Path, $"Adds {count} to the phone's interactions (XML Injector skips ones already there)."));
                        break;
                    case ("object", "_relation_panel_affordances") when instance == objectSimInstance:
                        add("add_interactions_to_relationship_panel", 1, added);
                        diagnostics.Add(new(TuningDraftSeverity.Information, entry, list.Path, $"Adds {count} to the relationship panel's interactions (XML Injector skips ones already there)."));
                        break;
                    case ("object", "_super_affordances"):
                        add("add_interactions_to_objects", 1, [new XElement("U", ObjectSelection(instance), Items("_super_affordances", added))]);
                        diagnostics.Add(new(TuningDraftSeverity.Information, entry, list.Path, $"Adds {count} to {what} (XML Injector skips ones already there)."));
                        break;
                    case ("object", "_components.state.enabled.states"):
                        add("add_states_to_objects", 2, [new XElement("U", ObjectSelection(instance), new XElement("U", new XAttribute("n", "state_component"), Items("states", added)))]);
                        diagnostics.Add(new(TuningDraftSeverity.Information, entry, list.Path, $"Adds {count} to the object's states."));
                        break;
                    case ("object", "_components.state.enabled.state_triggers"):
                        add("add_states_to_objects", 2, [new XElement("U", ObjectSelection(instance), new XElement("U", new XAttribute("n", "state_component"), Items("state_triggers", added)))]);
                        diagnostics.Add(new(TuningDraftSeverity.Information, entry, list.Path, $"Adds {count} to the object's state triggers."));
                        break;
                    case ("snippet", "value") when className == "AffordanceList":
                        add("add_mixer_interactions", 1, [new XElement("U", References("mixer_snippets", instance), Items("affordances", added))]);
                        diagnostics.Add(new(TuningDraftSeverity.Information, entry, list.Path, $"Adds {count} to the affordance list (XML Injector skips ones already there)."));
                        break;
                    case ("action", "loot_actions") when className == "LootActions":
                        add("add_to_loot_actions", 2, [new XElement("U", Reference("loot_actions_ref", instance), Items("loot_actions_to_add", added))]);
                        diagnostics.Add(new(TuningDraftSeverity.Information, entry, list.Path, $"Adds {count} to the end of the loot actions."));
                        break;
                    case ("action", "random_loot_actions") when className == "RandomWeightedLoot":
                        add("add_to_random_loot_actions", 2, [new XElement("U", Reference("random_weighted_loot_ref", instance), Items("random_loot_actions_to_add", added))]);
                        diagnostics.Add(new(TuningDraftSeverity.Information, entry, list.Path, $"Adds {count} to the random loot actions."));
                        break;
                    case ("trait", "buffs"):
                        add("add_buffs_to_trait", 2, [new XElement("U", Reference("trait", instance), Items("buffs", added))]);
                        diagnostics.Add(new(TuningDraftSeverity.Information, entry, list.Path, $"Adds {count} to the trait's buffs."));
                        break;
                    case ("interaction", "purchase_list_option"):
                        if (added.FirstOrDefault(item => item.Name.LocalName != "V" || !purchaseListOptions.Contains(item.Attribute("t")?.Value ?? string.Empty)) is { } unsupported)
                        {
                            diagnostics.Add(new(TuningDraftSeverity.Error, entry, list.Path, $"XML Injector can only add purchase list options of the kinds {string.Join(", ", purchaseListOptions.OrderBy(option => option, StringComparer.Ordinal))}, not {unsupported.Attribute("t")?.Value ?? "this one"}."));
                            break;
                        }
                        add("add_purchase_list_options_to_interactions", 4, [new XElement("U", References("interactions_to_add_to", instance), Items("purchase_list_options", added))]);
                        diagnostics.Add(new(TuningDraftSeverity.Information, entry, list.Path, $"Adds {count} to the purchase list options."));
                        break;
                    case ("interaction", var categories) when categories.StartsWith("picker_dialog.", StringComparison.Ordinal) && categories.EndsWith(".categories", StringComparison.Ordinal) && categories.Count(character => character == '.') == 2:
                        if (added.Any(item => item.Elements().Any(member => !pickerCategoryMembers.Contains(member.Attribute("n")?.Value ?? string.Empty))))
                        {
                            diagnostics.Add(new(TuningDraftSeverity.Error, entry, list.Path, "XML Injector can only add picker categories that set nothing but a tag, an icon and a tooltip."));
                            break;
                        }
                        add("add_picker_dialog_categories_to_interactions", 4, [new XElement("U", References("interactions_to_add_to", instance), Items("picker_dialog_categories", added))]);
                        diagnostics.Add(new(TuningDraftSeverity.Information, entry, list.Path, $"Adds {count} to the picker's categories (XML Injector skips ones already there)."));
                        break;
                    case ("action", "loot_actions"):
                        diagnostics.Add(new(TuningDraftSeverity.Error, entry, list.Path, $"XML Injector's add_to_loot_actions only adds to {what} of LootActions, and this is {className}."));
                        break;
                    case ("action", "random_loot_actions"):
                        diagnostics.Add(new(TuningDraftSeverity.Error, entry, list.Path, $"XML Injector's add_to_random_loot_actions only adds to {what} of RandomWeightedLoot, and this is {className}."));
                        break;
                    default:
                        diagnostics.Add(new(TuningDraftSeverity.Error, entry, list.Path, $"XML Injector has no way to add to {what}."));
                        break;
                }
            }
        }
        var snippet = new XElement("I",
            new XAttribute("c", "XmlInjector"),
            new XAttribute("i", "snippet"),
            new XAttribute("m", "xml_injector.snippet"),
            new XAttribute("n", name),
            new XAttribute("s", Fnv64.SetHighBit(Fnv64.GetHash(name)).ToString(CultureInfo.InvariantCulture)),
            new XElement("T", new XAttribute("n", "xml_injector_minimum_version"), version.ToString(CultureInfo.InvariantCulture)));
        foreach (var key in keyOrder)
            if (entries.TryGetValue(key, out var elements))
                snippet.Add(new XElement("L", new XAttribute("n", key), elements));
        return new(TuningXml.GetCanonicalText(snippet), diagnostics, changes);
    }
}