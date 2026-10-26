namespace LlamaLogic.TuningArchive;

/// <summary>
/// Drafts a Lot 51 Core Library tuning injector snippet (for Core Library 1.43) that makes changes to tuning without overriding it
/// </summary>
/// <remarks>
/// The snippet adds items to the lists Core Library's injectors can add to, for the tuning types they cover: object interactions, affordance lists, buff and trait loot, interactions, buffs and commodities, loot actions and random weighted loot, role state buffs, loot and interactions, situation venues and tags, relationship bit loot, interactions and buffs, venue buffs and zone modifiers, and zone modifier loot.
/// Core Library skips items a list already has.
/// Changes to the tests, autonomous tests and global tests of interactions, and to the tests of test set snippets, are made by adding tests where the change only adds new test lists or the same tests to every list, and otherwise by replacing the tests, which is reported as a warning.
/// Any other change, and any removal, is reported as an error and left out.
/// </remarks>
public static class TuningCoreLibraryDraft
{
    const string coreVersion = "1.43";
    const ulong objectSimInstance = 14965;

    enum Selection
    {
        Instance,
        Reference,
        References
    }

    sealed record Rule(string TypeName, string? ClassName, string ListPath, string Key, string SelectionField, Selection Selection, string ValueField, IReadOnlySet<string>? ItemMembers = null, bool ObjectSimOnly = false);

    sealed record TestRule(string TypeName, string? ClassName, string ListPath, string Key, string SelectionField, Selection Selection, string ModifyField, bool IsGlobal);

    static readonly IReadOnlySet<string> buffReference = new HashSet<string>(["buff_type", "buff_reason"], StringComparer.Ordinal);

    static readonly IReadOnlyList<Rule> rules =
    [
        new("object", null, "_super_affordances", "inject_by_object_tuning", "query", Selection.Instance, "affordances"),
        new("object", null, "_phone_affordances", "inject_by_object_tuning", "query", Selection.Instance, "phone_affordances", ObjectSimOnly: true),
        new("object", null, "_relation_panel_affordances", "inject_by_object_tuning", "query", Selection.Instance, "relation_panel_affordances", ObjectSimOnly: true),
        new("object", null, "_preroll_super_affordances", "inject_by_object_tuning", "query", Selection.Instance, "preroll_super_affordances"),
        new("snippet", "AffordanceList", "value", "inject_to_affordance_list", "affordance_list", Selection.Reference, "affordances"),
        new("buff", null, "_loot_on_addition", "inject_to_buffs", "buff", Selection.Reference, "loot_on_addition"),
        new("buff", null, "_loot_on_instance", "inject_to_buffs", "buff", Selection.Reference, "loot_on_instance"),
        new("buff", null, "_loot_on_removal", "inject_to_buffs", "buff", Selection.Reference, "loot_on_removal"),
        new("buff", null, "super_affordances", "inject_to_buffs", "buff", Selection.Reference, "super_affordances"),
        new("buff", null, "static_commodity_to_add", "inject_to_buffs", "buff", Selection.Reference, "static_commodity_to_add"),
        new("trait", null, "buffs", "inject_to_traits", "trait", Selection.Reference, "buffs", buffReference),
        new("trait", null, "buffs_proximity", "inject_to_traits", "trait", Selection.Reference, "buffs_proximity"),
        new("trait", null, "initial_commodities", "inject_to_traits", "trait", Selection.Reference, "initial_commodities"),
        new("trait", null, "initial_commodities_blacklist", "inject_to_traits", "trait", Selection.Reference, "initial_commodities_blacklist"),
        new("trait", null, "loot_on_trait_add", "inject_to_traits", "trait", Selection.Reference, "loot_on_trait_add"),
        new("trait", null, "restricted_ingredients", "inject_to_traits", "trait", Selection.Reference, "restricted_ingredients"),
        new("trait", null, "super_affordances", "inject_to_traits", "trait", Selection.Reference, "super_affordances"),
        new("action", "LootActions", "loot_actions", "inject_to_loot", "loot", Selection.Reference, "ops"),
        new("action", "RandomWeightedLoot", "random_loot_actions", "inject_to_random_weighted_loot", "loot", Selection.Reference, "random_loot_actions", new HashSet<string>(["action", "weight", "luck_option_config"], StringComparer.Ordinal)),
        new("role_state", null, "_buffs", "inject_to_role_states", "role_state", Selection.Reference, "buffs", buffReference),
        new("role_state", null, "loot_on_load", "inject_to_role_states", "role_state", Selection.Reference, "loot_on_load"),
        new("role_state", null, "role_affordances", "inject_to_role_states", "role_state", Selection.Reference, "role_affordances"),
        new("role_state", null, "role_target_affordances", "inject_to_role_states", "role_state", Selection.Reference, "role_target_affordances"),
        new("role_state", null, "preroll_affordances", "inject_to_role_states", "role_state", Selection.Reference, "preroll_affordances"),
        new("situation", null, "compatible_venues", "inject_to_situations", "situations", Selection.References, "compatible_venues"),
        new("situation", null, "tags", "inject_to_situations", "situations", Selection.References, "tags"),
        new("relbit", null, "bit_added_loot_list", "inject_to_relbits", "bits", Selection.References, "bit_added_loot_list"),
        new("relbit", null, "super_affordances", "inject_to_relbits", "bits", Selection.References, "super_affordances"),
        new("relbit", null, "buffs_to_add_if_on_active_lot", "inject_to_relbits", "bits", Selection.References, "buffs_to_add_if_on_active_lot", buffReference),
        new("venue", null, "venue_buffs", "inject_to_venues", "venues", Selection.References, "venue_buffs", buffReference),
        new("venue", null, "zone_modifiers", "inject_to_venues", "venues", Selection.References, "zone_modifiers"),
        new("zone_modifier", null, "enter_lot_loot", "inject_to_zone_modifiers", "zone_modifiers", Selection.References, "enter_lot_loot"),
        new("zone_modifier", null, "exit_lot_loot", "inject_to_zone_modifiers", "zone_modifiers", Selection.References, "exit_lot_loot")
    ];

    static readonly IReadOnlyList<TestRule> testRules =
    [
        new("interaction", null, "tests", "inject_to_affordances", "affordances", Selection.References, "modify_tests", false),
        new("interaction", null, "test_autonomous", "inject_to_affordances", "affordances", Selection.References, "modify_autonomous_tests", false),
        new("interaction", null, "test_globals", "inject_to_affordances", "affordances", Selection.References, "modify_global_tests", true),
        new("snippet", "TestSetInstance", "test", "inject_to_test_sets", "test_set", Selection.Reference, "modify_test", false)
    ];

    static readonly IReadOnlyList<string> keyOrder = [.. rules.Select(rule => rule.Key).Concat(testRules.Select(rule => rule.Key)).Distinct(StringComparer.Ordinal)];

    static XElement AddTests(string field, IReadOnlyList<XElement> and, bool prepend, IReadOnlyList<XElement> or)
    {
        var add = new XElement("U", new XAttribute("n", "add_tests"));
        if (and.Count > 0)
            add.Add(new XElement("L", new XAttribute("n", "AND"), and.Select(test => new XElement(test))));
        if (or.Count > 0)
            add.Add(new XElement("L", new XAttribute("n", "OR"), or.Select(tests => new XElement(tests))));
        if (prepend && and.Count > 0)
            add.Add(new XElement("T", new XAttribute("n", "prepend_and"), "True"));
        return new("V", new XAttribute("n", field), new XAttribute("t", "add_tests"), add);
    }

    static XElement ReplaceTests(string field, XElement tests) =>
        new("V", new XAttribute("n", field), new XAttribute("t", "replace_tests"),
            new XElement("U", new XAttribute("n", "replace_tests"),
                new XElement("L", new XAttribute("n", "tests"), tests.Elements().Select(test => new XElement(test)))));

    static bool SameItems(IReadOnlyList<XElement> a, IReadOnlyList<XElement> b) =>
        a.Count == b.Count && a.Zip(b).All(pair => TuningXml.GetFragmentText(pair.First) == TuningXml.GetFragmentText(pair.Second));

    static bool TryFindAddedTests(IReadOnlyList<XElement> original, IReadOnlyList<XElement> draft, out IReadOnlyList<XElement> added, out bool prepend)
    {
        added = [];
        prepend = false;
        if (draft.Count < original.Count)
            return false;
        var extra = draft.Count - original.Count;
        if (SameItems(original, [.. draft.Take(original.Count)]))
        {
            added = [.. draft.Skip(original.Count)];
            return true;
        }
        if (SameItems(original, [.. draft.Skip(extra)]))
        {
            added = [.. draft.Take(extra)];
            prepend = true;
            return true;
        }
        return false;
    }

    static (XElement modification, string description)? TryAddTests(TestRule rule, TuningDraftListChange list)
    {
        var original = list.Original.Elements().ToList();
        var draft = list.Draft.Elements().ToList();
        if (rule.IsGlobal)
            return TryFindAddedTests(original, draft, out var globalAnd, out var globalPrepend) && globalAnd.Count > 0
                ? (AddTests(rule.ModifyField, globalAnd, globalPrepend, []), globalAnd.Count == 1 ? "a test" : $"{globalAnd.Count} tests")
                : null;
        if (draft.Count < original.Count)
            return null;
        IReadOnlyList<XElement>? and = null;
        var prepend = false;
        for (var index = 0; index < original.Count; ++index)
        {
            if (!TryFindAddedTests([.. original[index].Elements()], [.. draft[index].Elements()], out var itemAnd, out var itemPrepend))
                return null;
            if (and is null)
            {
                and = itemAnd;
                prepend = itemPrepend;
            }
            else if (!SameItems(and, itemAnd) || and.Count > 0 && prepend != itemPrepend)
                return null;
        }
        and ??= [];
        IReadOnlyList<XElement> or = [.. draft.Skip(original.Count)];
        if (and.Count == 0 && or.Count == 0)
            return null;
        var parts = new List<string>();
        if (and.Count > 0)
            parts.Add($"{(and.Count == 1 ? "a test" : $"{and.Count} tests")} to {(prepend ? "the start" : "the end")} of every test list");
        if (or.Count > 0)
            parts.Add(or.Count == 1 ? "a test list" : $"{or.Count} test lists");
        return (AddTests(rule.ModifyField, and, prepend, or), string.Join(" and ", parts));
    }

    static string Describe(TuningDraftTarget target, string path)
    {
        var description = target.Draft.Nodes.FirstOrDefault(node => node.Path == path)?.Description ?? target.Original.Nodes.FirstOrDefault(node => node.Path == path)?.Description;
        return description?.Display ?? description?.Name ?? path;
    }

    static XElement Select(Selection selection, string field, ulong instance)
    {
        var id = instance.ToString(CultureInfo.InvariantCulture);
        return selection is Selection.References
            ? new XElement("L", new XAttribute("n", field), new XElement("T", id))
            : new XElement("T", new XAttribute("n", field), id);
    }

    /// <summary>
    /// Drafts a snippet with the specified <paramref name="name"/> (by convention <c>creator:mod</c>, which also names the creator and the mod for Core Library's messages) that makes the changes in the specified <paramref name="targets"/>
    /// </summary>
    public static TuningDraftResult Generate(IReadOnlyList<TuningDraftTarget> targets, string name)
    {
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(name);
        var diagnostics = new List<TuningDraftDiagnostic>();
        var entries = new Dictionary<(string key, ulong instance), XElement>();
        var changes = 0;
        foreach (var target in targets)
        {
            var entry = target.Entry;
            var root = target.Original.Root;
            var typeName = root.Attribute("i")?.Value;
            var className = root.Attribute("c")?.Value;
            var instance = entry.Instance;
            var found = TuningDraftListChanges.Find(target);
            foreach (var path in found.OtherPaths)
                diagnostics.Add(new(TuningDraftSeverity.Error, entry, path, $"Core Library can't change {Describe(target, path)}; this draft only adds items to the lists Core Library's injectors add to."));
            foreach (var list in found.Lists)
            {
                var what = Describe(target, list.Path);
                if (testRules.FirstOrDefault(candidate => candidate.TypeName == typeName && candidate.ListPath == list.Path && (candidate.ClassName is null || candidate.ClassName == className)) is { } testRule)
                {
                    if (!entries.TryGetValue((testRule.Key, instance), out var testInjection))
                    {
                        testInjection = new XElement("U", Select(testRule.Selection, testRule.SelectionField, instance));
                        entries.Add((testRule.Key, instance), testInjection);
                    }
                    if (TryAddTests(testRule, list) is { } addition)
                    {
                        testInjection.Add(addition.modification);
                        diagnostics.Add(new(TuningDraftSeverity.Information, entry, list.Path, $"Adds {addition.description} to {what} with {testRule.Key}'s {testRule.ModifyField}."));
                    }
                    else
                    {
                        testInjection.Add(ReplaceTests(testRule.ModifyField, list.Draft));
                        diagnostics.Add(new(TuningDraftSeverity.Warning, entry, list.Path, $"Replaces all of {what} with {testRule.Key}'s {testRule.ModifyField}, because the change isn't only new test lists or the same tests added to every list; this replaces the game's tests and wins over other mods' injections to them."));
                    }
                    ++changes;
                    continue;
                }
                if (list.Removed.Count > 0)
                    diagnostics.Add(new(TuningDraftSeverity.Error, entry, list.Path, $"Core Library can't remove items from {what}; it can only add."));
                if (list.Added.Count == 0)
                    continue;
                var rule = rules.FirstOrDefault(candidate => candidate.TypeName == typeName && candidate.ListPath == list.Path && (candidate.ClassName is null || candidate.ClassName == className) && (!candidate.ObjectSimOnly || instance == objectSimInstance));
                if (rule is null)
                {
                    diagnostics.Add(new(TuningDraftSeverity.Error, entry, list.Path, rules.FirstOrDefault(candidate => candidate.TypeName == typeName && candidate.ListPath == list.Path && candidate.ClassName is not null) is { } other
                        ? $"Core Library's {other.Key} only adds to {what} of {other.ClassName}, and this is {className}, whose items it can't express."
                        : $"Core Library has no injector that adds to {what}."));
                    continue;
                }
                var existing = list.Original.Elements().Select(TuningXml.GetFragmentText).ToHashSet(StringComparer.Ordinal);
                var added = list.Added.Where(item => !existing.Contains(TuningXml.GetFragmentText(item))).ToList();
                if (added.Count < list.Added.Count)
                    diagnostics.Add(new(TuningDraftSeverity.Error, entry, list.Path, $"{what} already has an item like the one added, and Core Library skips items a list already has, so it can't add another."));
                if (added.Count == 0)
                    continue;
                if (rule.ItemMembers is { } members && added.FirstOrDefault(item => item.Elements().Any(member => !members.Contains(member.Attribute("n")?.Value ?? string.Empty))) is not null)
                {
                    diagnostics.Add(new(TuningDraftSeverity.Error, entry, list.Path, $"Core Library can only add items to {what} that set nothing but {string.Join(", ", members.OrderBy(member => member, StringComparer.Ordinal))}."));
                    continue;
                }
                if (!entries.TryGetValue((rule.Key, instance), out var injection))
                {
                    injection = new XElement("U", Select(rule.Selection, rule.SelectionField, instance));
                    entries.Add((rule.Key, instance), injection);
                }
                injection.Add(new XElement("L", new XAttribute("n", rule.ValueField), added.Select(item => new XElement(item))));
                ++changes;
                var count = added.Count == 1 ? "an item" : $"{added.Count} items";
                diagnostics.Add(new(TuningDraftSeverity.Information, entry, list.Path, $"Adds {count} to {what} with {rule.Key} (Core Library skips ones already there)."));
            }
        }
        var separator = name.IndexOf(':', StringComparison.Ordinal);
        var snippet = new XElement("I",
            new XAttribute("c", "TuningInjector"),
            new XAttribute("i", "snippet"),
            new XAttribute("m", "lot51_core.snippets.injector"),
            new XAttribute("n", name),
            new XAttribute("s", Fnv64.SetHighBit(Fnv64.GetHash(name)).ToString(CultureInfo.InvariantCulture)),
            new XElement("T", new XAttribute("n", "creator_name"), separator > 0 ? name[..separator] : name),
            new XElement("T", new XAttribute("n", "minimum_core_version"), coreVersion),
            new XElement("T", new XAttribute("n", "mod_name"), separator > 0 ? name[(separator + 1)..] : name));
        foreach (var key in keyOrder)
        {
            var injections = entries.Where(pair => pair.Key.key == key).Select(pair => pair.Value).ToList();
            if (injections.Count > 0)
                snippet.Add(new XElement("L", new XAttribute("n", key), injections));
        }
        return new(TuningXml.GetCanonicalText(snippet), diagnostics, changes);
    }
}