namespace LlamaLogic.TuningArchive;

/// <summary>
/// Drafts a Python script mod that makes changes to tuning as the game loads it, without overriding any tuning
/// </summary>
/// <remarks>
/// The script changes each tunable of instance tuning on the tuning's class after the game loads the tuning's data and before the game invokes any tuning callback, so callbacks and everything that reads tuning see the changes.
/// The game loads module tuning when it imports the module, before script mods, so the script changes tunables of module tuning once the game has finished loading tuning (after it resolves the references module tuning defers), and leaves out tunables with a tuning callback, which has already run with the old value.
/// It is defensive: each change is made only when the tunable still holds what it held in the archive (so a game update or another mod that changed it is never clobbered), list items are matched by their content rather than their position, a change that is already in effect is not made again, and each change fails on its own without affecting the rest.
/// It writes what happened to each change to a log beside the mod.
/// Changes to tunables the game also exports to SimData are left out, because the engine and the UI read SimData, which Python cannot change.
/// </remarks>
public static class TuningPythonDraft
{
    const string script =
        """
        import importlib
        import os
        import re
        import sys
        import xml.etree.ElementTree as ElementTree
        import sims4.tuning.serialization as _serialization
        from sims4.tuning.instance_manager import InstanceManager, TuningCallbackHelper


        def _find_folder():
            path = os.path.abspath(__file__)
            while path and not os.path.isdir(path):
                parent = os.path.dirname(path)
                if parent == path:
                    return None
                path = parent
            return path


        def _read_game_version():
            try:
                folder = _find_folder()
                while folder and os.path.basename(folder).lower() != 'mods':
                    parent = os.path.dirname(folder)
                    if parent == folder:
                        return None
                    folder = parent
                if not folder:
                    return None
                with open(os.path.join(os.path.dirname(folder), 'GameVersion.txt'), 'rb') as file:
                    match = re.search(rb'\d+\.\d+\.\d+\.\d+', file.read())
                return match.group(0).decode('ascii') if match else None
            except Exception:
                return None


        def _write_log(lines, mode='a'):
            try:
                folder = _find_folder()
                if folder:
                    with open(os.path.join(folder, LOG_NAME), mode, encoding='utf-8') as file:
                        for line in lines:
                            file.write(line + '\n')
            except Exception:
                pass


        def _load(template, xml, source):
            return template.load_etree_node(ElementTree.fromstring(xml), source, False)


        def _set(manager, cls, name, template, value, source):
            reload_context = getattr(cls, '__reload_context__', None)
            if reload_context:
                with reload_context(cls, cls):
                    setattr(cls, name, value)
            else:
                setattr(cls, name, value)
            helpers = getattr(manager, '_callback_helper', None)
            if getattr(template, 'has_callback', False) and isinstance(helpers, dict):
                entries = list(helpers.get(cls, ()))
                for index, helper in enumerate(entries):
                    if helper.name == name:
                        entries[index] = helper._replace(value=value)
                        break
                else:
                    entries.append(TuningCallbackHelper(template, name, source, value))
                helpers[cls] = entries


        def _compute(template, name, current, kind, old, new, count, source):
            if kind == 'replace':
                expected = template.default if old is None else _load(template, old, source)
                wanted = template.default if new is None else _load(template, new, source)
                if current == wanted:
                    return 'already in effect', None
                if current != expected:
                    return 'skipped: {} is not what it was when this was drafted'.format(name), None
                return None, wanted
            item_template = getattr(template, '_template', None)
            if item_template is None or not isinstance(current, (tuple, frozenset)):
                return 'skipped: {} is not a list'.format(name), None
            old_item = None if old is None else _load(item_template, old, source)
            new_item = None if new is None else _load(item_template, new, source)
            items = list(current)
            if kind == 'append':
                present = items.count(new_item)
                if present >= count:
                    return 'already in effect', None
                items.extend([new_item] * (count - present))
            elif kind == 'remove':
                present = items.count(old_item)
                if present <= count:
                    return 'already in effect', None
                for _ in range(present - count):
                    items.remove(old_item)
            elif kind == 'replace_item':
                if old_item not in items:
                    if new_item in items:
                        return 'already in effect', None
                    return 'skipped: the item to change in {} is not there'.format(name), None
                items[items.index(old_item)] = new_item
            else:
                return 'skipped: {} is not a change this script knows'.format(kind), None
            return None, frozenset(items) if isinstance(current, frozenset) else tuple(items)


        def _change(manager, cls, kind, name, old, new, count, source):
            tunables = cls.get_tunables()
            template = tunables.get(name) if tunables else None
            if template is None:
                return 'skipped: {} is not tunable on {}'.format(name, cls.__name__)
            result, wanted = _compute(template, name, getattr(cls, name), kind, old, new, count, source)
            if result:
                return result
            _set(manager, cls, name, template, wanted, source)
            return 'applied'


        def _find_owner(module_name, owner_path):
            owner = sys.modules.get(module_name)
            if owner is None:
                owner = importlib.import_module(module_name)
            for part in owner_path.split('.') if owner_path else ():
                owner = vars(owner).get(part)
                if owner is None:
                    return None
            return owner


        def _change_module(owner, kind, name, old, new, count, source):
            tunables = vars(owner).get('MODULE_TUNABLES')
            template = tunables.get(name) if tunables else None
            if template is None:
                return 'skipped: {} is not tunable there'.format(name)
            if getattr(template, 'has_callback', False):
                return 'skipped: {} has a tuning callback, which already ran with the old value'.format(name)
            result, wanted = _compute(template, name, getattr(owner, name), kind, old, new, count, source)
            if result:
                return result
            reload_context = getattr(owner, '__reload_context__', None)
            if reload_context:
                with reload_context(owner, owner):
                    setattr(owner, name, wanted)
            else:
                setattr(owner, name, wanted)
            return 'applied'


        def _apply(manager):
            lines = []
            for type_id, instance_id, tuning_name, kind, name, old, new, count in CHANGES:
                if int(manager.TYPE) != type_id:
                    continue
                try:
                    cls = manager.get(instance_id)
                    if cls is None:
                        result = 'skipped: the tuning is not loaded'
                    else:
                        result = _change(manager, cls, kind, name, old, new, count, 'Epic Tuning Time draft for {}'.format(tuning_name))
                except Exception as exception:
                    result = 'failed: {!r}'.format(exception)
                lines.append('{} ({}) {} {}: {}'.format(tuning_name, instance_id, kind, name, result))
            if lines:
                _write_log(lines)


        def _apply_modules():
            lines = []
            for module_name, owner_path, kind, name, old, new, count in MODULE_CHANGES:
                try:
                    owner = _find_owner(module_name, owner_path)
                    if owner is None:
                        result = 'skipped: {} is not in {}'.format(owner_path, module_name)
                    else:
                        result = _change_module(owner, kind, name, old, new, count, 'Epic Tuning Time draft for {}'.format(module_name))
                except Exception as exception:
                    result = 'failed: {!r}'.format(exception)
                lines.append('{} {}: {}'.format('.'.join(part for part in (module_name, owner_path, name) if part), kind, result))
            if lines:
                _write_log(lines)


        _game_version = _read_game_version()
        _write_log(['Drafted for {}; the game is {}'.format(DRAFTED_FOR, _game_version or 'an unknown version')], 'w')
        _load_data_into_class_instances = InstanceManager.load_data_into_class_instances


        def _load_data_and_apply_changes(self, *args, **kwargs):
            result = _load_data_into_class_instances(self, *args, **kwargs)
            try:
                _apply(self)
            except Exception as exception:
                _write_log(['failed: {!r}'.format(exception)])
            return result


        InstanceManager.load_data_into_class_instances = _load_data_and_apply_changes
        _finalize_deferred_loads = _serialization.finalize_deferred_loads


        def _finalize_deferred_loads_and_apply_changes(*args, **kwargs):
            result = _finalize_deferred_loads(*args, **kwargs)
            try:
                _apply_modules()
            except Exception as exception:
                _write_log(['failed: {!r}'.format(exception)])
            return result


        if MODULE_CHANGES:
            _serialization.finalize_deferred_loads = _finalize_deferred_loads_and_apply_changes
        """;

    static readonly ConditionalWeakTable<TuningAnalysis, Dictionary<string, TunableDescription?>> descriptionsByPath = new();

    static string Describe(TunableDescription? description) =>
        description?.Display ?? description?.Name ?? "the tunable";

    static string Describe(TuningDraftTarget target, TunableDescription description, string path)
    {
        if (path.EndsWith(']'))
        {
            var parent = path[..path.LastIndexOf('[')];
            return $"The items of {Describe(FindDescription(target, parent))}";
        }
        return $"The values of {Describe(description)}";
    }

    static string GetGeneralPath(string path)
    {
        var builder = new StringBuilder(path.Length);
        var inIndex = false;
        foreach (var character in path)
        {
            if (character == '[')
                inIndex = true;
            else if (character == ']')
                inIndex = false;
            else if (inIndex)
                continue;
            builder.Append(character);
        }
        return builder.ToString();
    }

    static IEnumerable<string> GetPrefixes(string path)
    {
        for (var index = 0; index < path.Length; ++index)
            if (path[index] is '.' or '[')
                yield return path[..index];
        yield return path;
    }

    static bool IsWithin(string path, string name) =>
        path == name || path.Length > name.Length && path.StartsWith(name, StringComparison.Ordinal) && path[name.Length] is '.' or '[';

    static string Quote(string? text)
    {
        if (text is null)
            return "None";
        var builder = new StringBuilder("'");
        foreach (var character in text)
            builder.Append(character switch
            {
                '\\' => "\\\\",
                '\'' => "\\'",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                < ' ' => $"\\x{(int)character:x2}",
                _ => character.ToString()
            });
        return builder.Append('\'').ToString();
    }

    /// <summary>
    /// Drafts a script that makes the changes in the specified <paramref name="targets"/>, for the specified <paramref name="gameVersion"/>, logging to a file with the specified <paramref name="logName"/> beside the mod
    /// </summary>
    public static TuningDraftResult Generate(IReadOnlyList<TuningDraftTarget> targets, string gameVersion, string logName)
    {
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(gameVersion);
        ArgumentNullException.ThrowIfNull(logName);
        var diagnostics = new List<TuningDraftDiagnostic>();
        var changes = new List<string>();
        var moduleChanges = new List<string>();
        foreach (var target in targets)
        {
            var entry = target.Entry;
            var originalRoot = target.Original.Root;
            var draftRoot = target.Draft.Root;
            var originalTunables = GetTunables(originalRoot);
            var draftTunables = GetTunables(draftRoot);
            var changedPaths = draftTunables.Keys.Concat(originalTunables.Keys).Distinct(StringComparer.Ordinal)
                .Where(path => !(originalTunables.TryGetValue(path, out var before) && draftTunables.TryGetValue(path, out var after) && TuningXml.HaveSameFragmentText(before.element, after.element)))
                .ToList();
            if (changedPaths.Count == 0)
                continue;
            var isModule = originalRoot.Name.LocalName == "M";
            if (isModule ? target.Draft.Module is null : originalRoot.Name.LocalName != "I" || target.Draft.Instance is null)
            {
                diagnostics.Add(new(TuningDraftSeverity.Error, entry, string.Empty, isModule ? "The tuning descriptions don't describe this module, so Python can't change its tuning." : "The tuning descriptions don't describe this tuning's class, so Python can't change it."));
                continue;
            }
            var typeId = (uint)entry.Type;
            var tuningName = entry.Name ?? entry.Instance.ToString(CultureInfo.InvariantCulture);
            var moduleName = target.Draft.PythonModule ?? string.Empty;
            foreach (var path in changedPaths)
            {
                var (owner, name) = originalTunables.TryGetValue(path, out var located) ? (located.owner, located.name) : (draftTunables[path].owner, draftTunables[path].name);
                void change(string kind, string? old, string? @new, int? count = null)
                {
                    var wanted = count is { } value ? value.ToString(CultureInfo.InvariantCulture) : "None";
                    if (isModule)
                        moduleChanges.Add($"    ({Quote(moduleName)}, {Quote(owner)}, {Quote(kind)}, {Quote(name)}, {Quote(old)}, {Quote(@new)}, {wanted}),");
                    else
                        changes.Add($"    (0x{typeId:X8}, {entry.Instance.ToString(CultureInfo.InvariantCulture)}, {Quote(tuningName)}, {Quote(kind)}, {Quote(name)}, {Quote(old)}, {Quote(@new)}, {wanted}),");
                }
                var original = originalTunables.TryGetValue(path, out var originalTunable) ? originalTunable.element : null;
                var draft = draftTunables.TryGetValue(path, out var draftTunable) ? draftTunable.element : null;
                var description = FindDescription(target, path);
                var blockers = GetSimDataBlockers(target, path).ToList();
                if (blockers.Count > 0)
                {
                    diagnostics.AddRange(blockers.Select(blocker => new TuningDraftDiagnostic(TuningDraftSeverity.Error, entry, blocker.path, blocker.message)));
                    continue;
                }
                if (description is ListTunableDescription { IsMapping: false } && original is { Name.LocalName: "L" } && draft is { Name.LocalName: "L" })
                {
                    var originalElements = original.Elements().ToList();
                    var draftElements = draft.Elements().ToList();
                    if (originalElements.Count == draftElements.Count)
                    {
                        for (var index = 0; index < originalElements.Count; ++index)
                            if (!TuningXml.HaveSameFragmentText(originalElements[index], draftElements[index]))
                            {
                                change("replace_item", TuningXml.GetFragmentText(originalElements[index]), TuningXml.GetFragmentText(draftElements[index]));
                                diagnostics.Add(new(TuningDraftSeverity.Information, entry, $"{path}[{index}]", $"Changes an item of {Describe(description)}, found by its content."));
                            }
                    }
                    else
                    {
                        var originalItems = originalElements.Select(TuningXml.GetFragmentText).ToList();
                        var draftItems = draftElements.Select(TuningXml.GetFragmentText).ToList();
                        var originalCounts = originalItems.GroupBy(item => item, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
                        var draftCounts = draftItems.GroupBy(item => item, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
                        foreach (var item in originalItems.Distinct(StringComparer.Ordinal))
                            if (draftCounts.GetValueOrDefault(item) is var kept && kept < originalCounts[item])
                            {
                                change("remove", item, null, kept);
                                diagnostics.Add(new(TuningDraftSeverity.Information, entry, path, $"Removes an item from {Describe(description)}, found by its content."));
                            }
                        foreach (var item in draftItems.Distinct(StringComparer.Ordinal))
                            if (draftCounts[item] is var wanted && wanted > originalCounts.GetValueOrDefault(item))
                            {
                                change("append", null, item, wanted);
                                diagnostics.Add(new(TuningDraftSeverity.Information, entry, path, $"Adds an item to the end of {Describe(description)}, keeping the items other mods added."));
                            }
                    }
                    continue;
                }
                change("replace", original is null ? null : TuningXml.GetFragmentText(original), draft is null ? null : TuningXml.GetFragmentText(draft));
                var callbackCaveat = isModule ? " It's skipped if the tunable has a tuning callback, which already ran with the old value." : string.Empty;
                if (description is TupleTunableDescription or ListTunableDescription or VariantTunableDescription)
                    diagnostics.Add(new(TuningDraftSeverity.Warning, entry, path, $"Replaces all of {Describe(description)}, so the change is skipped if a game update or another mod has changed any part of it.{callbackCaveat}"));
                else
                    diagnostics.Add(new(TuningDraftSeverity.Information, entry, path, draft is null ? $"Resets {Describe(description)} to its default.{callbackCaveat}" : $"Sets {Describe(description)}.{callbackCaveat}"));
            }
        }
        var source = new StringBuilder();
        source.Append("\"\"\"Drafted by Epic Tuning Time for The Sims 4 ").Append(gameVersion).Append(".\n\n");
        source.Append("Changes tuning as the game loads it, without overriding any tuning. Each change is made only if the tunable still\n");
        source.Append("holds what it held when this was drafted, and what happened to each change is written to ").Append(logName).Append(" beside the mod.\n\"\"\"\n");
        source.Append("DRAFTED_FOR = ").Append(Quote(gameVersion)).Append('\n');
        source.Append("LOG_NAME = ").Append(Quote(logName)).Append('\n');
        source.Append("CHANGES = (\n");
        foreach (var line in changes)
            source.Append(line).Append('\n');
        source.Append(")\n");
        source.Append("MODULE_CHANGES = (\n");
        foreach (var line in moduleChanges)
            source.Append(line).Append('\n');
        source.Append(")\n");
        source.Append(script.Replace("\r\n", "\n", StringComparison.Ordinal)).Append('\n');
        return new(source.ToString(), diagnostics, changes.Count + moduleChanges.Count);
    }

    static Dictionary<string, TunableDescription?> GetDescriptionsByPath(TuningAnalysis analysis) =>
        descriptionsByPath.GetValue(analysis, analyzed =>
        {
            var byPath = new Dictionary<string, TunableDescription?>(StringComparer.Ordinal);
            foreach (var node in analyzed.Nodes)
                byPath.TryAdd(node.Path, node.Description);
            return byPath;
        });

    static TunableDescription? FindDescription(TuningDraftTarget target, string path) =>
        GetDescriptionsByPath(target.Draft).GetValueOrDefault(path)
        ?? GetDescriptionsByPath(target.Original).GetValueOrDefault(path)
        ?? (path.IndexOfAny(['.', '[']) < 0 ? target.Draft.InstanceMembers.FirstOrDefault(member => member.Name == path) : null);

    static Dictionary<string, (string owner, string name, XElement element)> GetTunables(XElement root)
    {
        var tunables = new Dictionary<string, (string owner, string name, XElement element)>(StringComparer.Ordinal);
        void visit(XElement parent, string owner)
        {
            foreach (var element in parent.Elements())
            {
                if (element.Attribute("n")?.Value is not { } name)
                    continue;
                var path = owner.Length == 0 ? name : $"{owner}.{name}";
                if (root.Name.LocalName == "M" && element.Name.LocalName == "C")
                    visit(element, path);
                else
                    tunables.TryAdd(path, (owner, name, element));
            }
        }
        visit(root, string.Empty);
        return tunables;
    }

    static IEnumerable<(string path, string message)> GetSimDataBlockers(TuningDraftTarget target, string name)
    {
        static Dictionary<string, (string element, string? value)> values(TuningAnalysis analysis, string name)
        {
            var found = new Dictionary<string, (string element, string? value)>(StringComparer.Ordinal);
            foreach (var node in analysis.Nodes)
                if (IsWithin(node.Path, name))
                    found.TryAdd(node.Path, (node.ElementName, node.Value));
            return found;
        }
        var original = values(target.Original, name);
        var draft = values(target.Draft, name);
        var changed = original.Keys.Concat(draft.Keys).Distinct(StringComparer.Ordinal)
            .Where(path => !original.TryGetValue(path, out var before) || !draft.TryGetValue(path, out var after) || before != after)
            .DefaultIfEmpty(name);
        var reported = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in changed)
            foreach (var prefix in GetPrefixes(path))
                if (FindDescription(target, prefix) is { ExportModes: { } modes } description && modes.HasFlag(TuningDescriptionExportModes.ClientBinary))
                {
                    var general = GetGeneralPath(prefix);
                    if (reported.Add(general))
                    {
                        var what = Describe(target, description, prefix);
                        yield return (general, (modes & (TuningDescriptionExportModes.ServerBinary | TuningDescriptionExportModes.ServerXml)) == TuningDescriptionExportModes.None
                            ? $"{what} only exist in SimData, which the game's engine and UI read and Python cannot change; only an override with its SimData can change them."
                            : $"{what} are also in SimData, which the game's engine and UI read and Python cannot change, so Python would change only the gameplay side and the two would disagree; only an override with its SimData can change both.");
                    }
                    break;
                }
    }
}