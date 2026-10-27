namespace LlamaLogic.TuningArchive;

partial class TuningArchiveFile
{
    const string animationStateMachineClass = "JAZZ";

    internal const string AnimationStateMachineCommentClass = animationStateMachineClass;
    const string animationStateMachineFolder = "AsmJazz";
    const string animationStateMachineInstanceType = "AsmJazz";
    const string fileIndexFileName = "XML File Index.txt";
    const string simDataFolder = "SimData";
    const string stringsReferenceFileName = "Reference - All Strings.txt";
    const string xmlCrossReferenceFileName = "XML Cross Reference.dat";
    const string xmlNamesReferenceFileName = "Reference - All XML Names.txt";

    static readonly ImmutableHashSet<string> exportGroupNoReferenceNames = ImmutableHashSet.Create(StringComparer.Ordinal, "BABY_BASSINET_DEFINITION_MAP", "ingredient_list", "ingredients", "ObjectDefinitions", "possible_fish", "resource_key", "ResourceIds");
    static readonly ImmutableHashSet<string> exportNoReferenceNames = ImmutableHashSet.Create(StringComparer.Ordinal, "_default_convergence_value", "_household_funds", "actual_object", "add_weight", "amount", "attractor point definition", "base_value", "bassinet_to_use", "cas_part", "CASPart", "CHEF_STATION_CUTTING_BOARD_OBJECT", "CHEF_STATION_PAN_OBJECT", "CHEF_STATION_POT_OBJECT", "CHEF_STATION_SERVING_PLATTER_OBJECT", "collectable_item", "cost", "decoration_resource", "default_object_to_create", "default_value", "DefaultPart", "definition", "destination_lot", "fallback_definition", "House Description", "jig", "jig_to_use", "LARGE_PORTRAIT_OBJ_DEF", "lower_bound", "magazine_collection", "MannequinPart", "MatchingPart", "max_duration", "max_value_tuning", "maximum_auto_satisfy_time", "MEDIUM_PORTRAIT_OBJ_DEF", "model", "multiplier", "object_definition", "object_reference", "object_to_be_mailed", "pet_crate_object_definition", "plant", "Region Description", "RegionDescription", "restaurant_menu_icon_definition", "sign_definition", "SMALL_LANDSCAPE_OBJ_DEF", "Spawn Point ID", "Street Description ID", "Template House Description", "TEMPLE_LOT_DESCRIPTION", "time_overhead", "treasure", "upper_bound", "wall_mounted_object", "weight", "WorldDescription");

    static readonly XmlWriterSettings exportWriterSettings = new()
    {
        Encoding = new UTF8Encoding(true),
        Indent = true,
        IndentChars = "  ",
        NewLineChars = "\r\n",
        NewLineHandling = NewLineHandling.Replace
    };

#if IS_NET_7_0_OR_GREATER
    [GeneratedRegex(@"^(?<indent>\s+)(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<value>\d+)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex GetPackEnumMemberPattern();
#else
    static readonly Regex packEnumMemberPattern = new(@"^(?<indent>\s+)(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<value>\d+)\s*$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    static Regex GetPackEnumMemberPattern() =>
        packEnumMemberPattern;
#endif

    sealed record ExportFile(TuningArchiveEntry Entry, string RelativePath, string Name, string InstanceType);

    internal sealed class ExportCrossReferences
    {
        readonly Dictionary<ulong, (HashSet<ulong> by, HashSet<ulong> to)> references = [];

        public void Add(ulong instance)
        {
            lock (references)
                Get(instance);
        }

        public void Add(ulong source, ulong target)
        {
            lock (references)
            {
                Get(source).to.Add(target);
                Get(target).by.Add(source);
            }
        }

        (HashSet<ulong> by, HashSet<ulong> to) Get(ulong instance)
        {
            if (!references.TryGetValue(instance, out var entry))
            {
                entry = ([], []);
                references.Add(instance, entry);
            }
            return entry;
        }

        public void Write(Stream stream)
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(0x4DDC4353UL | ((1UL << 24) << 32));
            foreach (var (instance, (by, to)) in references.OrderBy(entry => entry.Key))
            {
                writer.Write(instance);
                writer.Write(by.Count);
                foreach (var source in by.OrderBy(value => value))
                    writer.Write(source);
                writer.Write(to.Count);
                foreach (var target in to.OrderBy(value => value))
                    writer.Write(target);
            }
        }
    }

    static string GetExportComment(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var character in text)
            if (XmlConvert.IsXmlChar(character) || char.IsSurrogate(character))
                builder.Append(character);
        var comment = builder.ToString();
        while (comment.Contains("--", StringComparison.Ordinal))
            comment = comment.Replace("--", "-", StringComparison.Ordinal);
        return comment.EndsWith('-') ? $"{comment} " : comment;
    }

    static string GetExportFileName(TuningArchiveExportNaming naming, ResourceType type, uint group, ulong instance, string name, string instanceType) =>
        naming switch
        {
            TuningArchiveExportNaming.TypeGroupInstance => string.Create(CultureInfo.InvariantCulture, $"S4_{(uint)type:X8}_{group:X8}_{instance:X16}.xml"),
            TuningArchiveExportNaming.TypeGroupInstanceName => string.Create(CultureInfo.InvariantCulture, $"S4_{(uint)type:X8}_{group:X8}_{instance:X16}_{name}.xml"),
            TuningArchiveExportNaming.Name => $"{name}.xml",
            TuningArchiveExportNaming.Native => string.Create(CultureInfo.InvariantCulture, $"0x{group:x8}!0x{instance:x16}.0x{(uint)type:x8}"),
            _ => string.Create(CultureInfo.InvariantCulture, $"{(uint)type:x8}!{group:x8}!{instance:x16}.{name}.{instanceType}.xml")
        };

    static string GetExportSafeName(string name)
    {
        var builder = new StringBuilder(name.Length);
        foreach (var character in name)
            builder.Append(character < ' ' || character is '<' or '>' or ':' or '"' or '/' or '\\' or '|' or '?' or '*' ? '_' : character);
        return builder.ToString();
    }

    static string GetExportStringText(string text) =>
        text.Replace("\r\n", "\\n", StringComparison.Ordinal);

    static bool TryGetExportStringKey(string value, out uint key)
    {
        key = 0;
        if (value.Length == 0 || value[0] is < '0' or > '9')
            return false;
        if (value.Length > 2 && value[0] == '0' && value[1] is 'x' or 'X')
            return uint.TryParse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out key);
        return uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out key);
    }

    static bool MoveAsideExistingExportFile(string path)
    {
        if (!File.Exists(path))
            return false;
        var extension = System.IO.Path.GetExtension(path);
        var stem = path[..^extension.Length];
        var number = 1;
        string copy;
        while (File.Exists(copy = $"{stem} (Copy {number}){extension}"))
            ++number;
        File.Move(path, copy);
        return true;
    }

    internal static void AddHeuristicReferenceComments(XElement element, ulong source, IReadOnlyDictionary<ulong, (string name, string? @class)> names, ExportCrossReferences? crossReferences)
    {
        foreach (var node in element.Nodes().ToList())
        {
            if (node is XElement child)
            {
                if (child.Attribute("n")?.Value is { } childName && exportGroupNoReferenceNames.Contains(childName))
                    continue;
                if (child.Nodes().Any())
                    AddHeuristicReferenceComments(child, source, names, crossReferences);
            }
            else if (node is XText text
                && !(element.Attribute("n")?.Value is { } name && exportNoReferenceNames.Contains(name))
                && ulong.TryParse(text.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var instance)
                && names.TryGetValue(instance, out var target))
            {
                element.Add(new XComment(GetExportComment(target.@class is null ? target.name : $"{target.@class}: {target.name}")));
                crossReferences?.Add(source, instance);
            }
        }
    }

    internal static void AddHeuristicStringComments(XElement root, IReadOnlyDictionary<uint, string> strings)
    {
        foreach (var text in root.DescendantNodes().OfType<XText>().ToList())
            if (TryGetExportStringKey(text.Value, out var key) && strings.TryGetValue(key, out var stringText))
                text.AddAfterSelf(new XComment(GetExportComment($"String: \"{GetExportStringText(stringText)}\"")));
    }

    internal sealed record CommentTables(IReadOnlyDictionary<uint, string> Strings, IReadOnlyDictionary<ulong, (string name, string? @class)> Names, IReadOnlyDictionary<(ResourceType type, ulong instance), TuningArchiveEntry> TuningsByKey, IReadOnlyDictionary<(ResourceType type, string name), TuningArchiveEntry> TuningsByName, IReadOnlyDictionary<ulong, TuningArchiveEntry> AnimationStateMachinesByInstance, TuningDescriptionCache? DescriptionCache, IReadOnlyDictionary<(ResourceType type, ulong instance), TuningArchiveUninstalledTuning> UninstalledTunings);

    internal static Dictionary<(ResourceType type, ulong instance), TuningArchiveUninstalledTuning> AddUninstalledTuningNames(IReadOnlyList<TuningArchiveUninstalledTuning> uninstalledTunings, Dictionary<ulong, (string name, string? @class)> names)
    {
        var byKey = new Dictionary<(ResourceType type, ulong instance), TuningArchiveUninstalledTuning>();
        foreach (var tuning in uninstalledTunings)
        {
            byKey.TryAdd((tuning.Type, tuning.Instance), tuning);
            names.TryAdd(tuning.Instance, (tuning.Name, tuning.Class ?? (tuning.Type is ResourceType.Tuning ? null : string.Empty)));
        }
        return byKey;
    }

    internal static void AddComments(XDocument document, string text, ulong instance, CommentTables tables, bool stringComments, bool referenceComments, ExportCrossReferences? crossReferences)
    {
        var documentRoot = document.Root!;
        var analyzed = false;
        if (tables.DescriptionCache is not null)
        {
            try
            {
                var analysis = TuningAnalysis.Analyze(text, tables.DescriptionCache);
                if (analysis.Nodes.Any(node => node.Description is not null))
                {
                    var elementsByLine = new Dictionary<int, XElement>();
                    foreach (var element in document.Descendants().Prepend(documentRoot))
                        if (element is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
                            elementsByLine.TryAdd(lineInfo.LineNumber, element);
                    if (stringComments)
                        foreach (var node in analysis.Nodes)
                            if (node.ValueKind is TuningValueKind.LocalizedString && node.StringKey is { } key and not 0 && tables.Strings.TryGetValue(key, out var stringText) && elementsByLine.TryGetValue(node.Line, out var element) && element.Nodes().OfType<XText>().FirstOrDefault() is { } textNode)
                                textNode.AddAfterSelf(new XComment(GetExportComment($"String: \"{GetExportStringText(stringText)}\"")));
                    foreach (var node in analysis.Nodes)
                    {
                        if (!elementsByLine.TryGetValue(node.Line, out var element))
                            continue;
                        string? comment = null;
                        ulong? target = null;
                        if (node.ValueKind is TuningValueKind.Reference && node.ReferenceType is { } referenceType)
                        {
                            var resolved = node.ReferenceInstance is { } referenceInstance ? tables.TuningsByKey.GetValueOrDefault((referenceType, referenceInstance)) : node.ReferenceName is { } referenceName ? tables.TuningsByName.GetValueOrDefault((referenceType, referenceName)) : null;
                            if (resolved is not null)
                            {
                                target = resolved.Instance;
                                if (node.ReferenceInstance is not null && resolved.Name is { } resolvedName)
                                    comment = resolved.Class is { } resolvedClass ? $"{resolvedClass}: {resolvedName}" : resolvedName;
                            }
                            else if (node.ReferenceInstance is { } uninstalledInstance && tables.UninstalledTunings.TryGetValue((referenceType, uninstalledInstance), out var uninstalled))
                            {
                                target = uninstalled.Instance;
                                comment = uninstalled.Class is { } uninstalledClass ? $"{uninstalledClass}: {uninstalled.Name}" : uninstalled.Name;
                            }
                        }
                        else if (node.ValueKind is TuningValueKind.ResourceKey && node.ResourceKey is { Type: ResourceType.AnimationStateMachine } resourceKey && tables.AnimationStateMachinesByInstance.TryGetValue(resourceKey.FullInstance, out var animationStateMachine))
                        {
                            target = animationStateMachine.Instance;
                            if (animationStateMachine.Name is { } animationStateMachineName)
                                comment = $"{animationStateMachineClass}: {animationStateMachineName}";
                        }
                        if (target is { } targetInstance)
                            crossReferences?.Add(instance, targetInstance);
                        if (comment is not null && referenceComments)
                            element.Add(new XComment(GetExportComment(comment)));
                    }
                    analyzed = true;
                }
            }
            catch (Exception ex) when (ex is XmlException or InvalidDataException)
            {
            }
        }
        if (!analyzed)
        {
            if (stringComments)
                AddHeuristicStringComments(documentRoot, tables.Strings);
            if (referenceComments || crossReferences is not null)
            {
                var commented = referenceComments ? documentRoot : new XElement(documentRoot);
                if (!(documentRoot.Attribute("n")?.Value is { } rootName && exportGroupNoReferenceNames.Contains(rootName)))
                    AddHeuristicReferenceComments(commented, instance, tables.Names, crossReferences);
            }
        }
    }

    static void ApplyExportAttributeOrder(XElement root, TuningArchiveExportAttributeOrder order)
    {
        if (order is not TuningArchiveExportAttributeOrder.TypeFirst)
            return;
        foreach (var element in root.DescendantsAndSelf())
            if (element.Attribute("n") is { } name && element.Attribute("t") is not null)
            {
                name.Remove();
                element.Add(name);
            }
    }

    Dictionary<string, uint> GetExportPackIdentifiers(TuningArchiveSnapshot snapshot, IEnumerable<string> packs, IReadOnlyList<TuningArchiveEntry> simData, ICollection<string> problems)
    {
        var fromScripts = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        if (GetPythonModule(snapshot, "sims4.common") is { } common)
        {
            var inPack = false;
            foreach (var line in GetText(common).Split('\n'))
            {
                var trimmed = line.TrimEnd('\r');
                if (trimmed.StartsWith("class ", StringComparison.Ordinal))
                {
                    inPack = trimmed.StartsWith("class Pack(", StringComparison.Ordinal);
                    continue;
                }
                if (!inPack)
                    continue;
                if (trimmed.Length > 0 && !char.IsWhiteSpace(trimmed[0]))
                {
                    inPack = false;
                    continue;
                }
                if (GetPackEnumMemberPattern().Match(trimmed) is { Success: true } match && match.Groups["indent"].Value.Length == 4 && uint.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                    fromScripts[match.Groups["name"].Value == "BASE_GAME" ? "BG" : match.Groups["name"].Value] = value;
            }
        }
        var simDataByPack = simData.GroupBy(entry => entry.Pack, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        var identifiers = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        foreach (var pack in packs.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (fromScripts.TryGetValue(pack, out var scripted))
            {
                identifiers[pack] = scripted;
                continue;
            }
            if (pack.Equals("BG", StringComparison.OrdinalIgnoreCase))
            {
                identifiers[pack] = 0;
                continue;
            }
            if (simDataByPack.TryGetValue(pack, out var packSimData))
            {
                var stamps = new Dictionary<uint, int>();
                foreach (var entry in packSimData.Take(16))
                {
                    var text = GetText(entry);
                    var start = text.IndexOf("<SimData u=\"0x", StringComparison.Ordinal);
                    if (start >= 0 && start + 22 <= text.Length && uint.TryParse(text.AsSpan(start + 14, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var stamp) && stamp != 0)
                        stamps[stamp] = stamps.GetValueOrDefault(stamp) + 1;
                }
                if (stamps.Count > 0)
                {
                    identifiers[pack] = stamps.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).First().Key;
                    continue;
                }
            }
            if (PackIdentifiers.Known.TryGetValue(pack, out var known))
            {
                identifiers[pack] = known;
                continue;
            }
            identifiers[pack] = 0;
            problems.Add($"{pack}: its identifier could not be found (the snapshot has no game scripts naming it and no SimData stamped with it), so 00000000 is used as the group in its file names");
        }
        return identifiers;
    }

    /// <summary>
    /// Exports the tuning of the specified <paramref name="snapshot"/> (and, optionally, its animation state machines and SimData) as files in the specified <paramref name="folder"/>, laid out and named the way Scumbumbo's XML Extractor extracts the game's tuning (💤)
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each resource is written to a folder for its pack (such as <c>EP01</c>) and, in it, a folder for its tuning type (such as <c>buff</c>) or <c>AsmJazz</c>, as indented UTF-8 XML with a byte order mark and CRLF line endings.
    /// SimData is written beside the tuning with its instance, so that both can be chosen together for Sims 4 Studio's batch import, or to a <c>SimData</c> folder in its pack's folder if there is no such tuning.
    /// The group in file names is the pack's identifier, as XML Extractor writes it: taken from the snapshot's game scripts when it has them, otherwise from the pack stamp of the pack's SimData, otherwise from XML Extractor's own table of packs.
    /// </para>
    /// <para>
    /// A tuning that more than one pack contains is written once, to the folder of the pack the snapshot attributes it to; XML Extractor writes it to the folder of each pack.
    /// The archive does not keep the order of the attributes the game ships, so elements' attributes are in canonical order (see <see cref="TuningXml"/>), with the <c>n</c> and <c>t</c> attributes ordered as <see cref="TuningArchiveExportOptions.AttributeOrder"/> specifies.
    /// References to tuning of packs that were not installed are commented with the names the packs' patches give it, as XML Extractor comments them (see <see cref="GetUninstalledTunings"/>), and those names are in the reference files, but the tuning itself is not exported.
    /// </para>
    /// </remarks>
    public async Task<TuningArchiveExportResult> ExportAsync(TuningArchiveSnapshot snapshot, string folder, TuningArchiveExportOptions? options = null, IProgress<TuningArchiveProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(folder);
        options ??= new();
        return await Task.Run(() =>
        {
            const int progressInterval = 100;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var problems = new List<string>();
            var root = System.IO.Path.GetFullPath(folder);
            Directory.CreateDirectory(root);
            progress?.Report(new("Reading the snapshot", 0, 0));
            var tunings = GetEntries(snapshot, TuningArchiveResourceKind.Tuning).OrderBy(entry => (uint)entry.Type).ThenBy(entry => entry.Instance).ToList();
            var animationStateMachines = options.IncludeAnimationStateMachines ? GetEntries(snapshot, TuningArchiveResourceKind.AnimationStateMachine).OrderBy(entry => entry.Instance).ToList() : [];
            var simData = GetEntries(snapshot, TuningArchiveResourceKind.SimData).OrderBy(entry => entry.Instance).ToList();
            var typeNames = GetTuningTypeNames();
            var strings = options.IncludeStringComments || options.WriteReferenceFiles ? GetStrings(snapshot) : ImmutableDictionary<uint, string>.Empty;
            var packIdentifiers = GetExportPackIdentifiers(snapshot, tunings.Concat(animationStateMachines).Concat(simData).Select(entry => entry.Pack), simData, problems);
            var names = new Dictionary<ulong, (string name, string? @class)>();
            foreach (var entry in tunings)
                if (entry.Name is { } name)
                    names.TryAdd(entry.Instance, (name, entry.Class ?? (entry.Type is ResourceType.Tuning ? null : string.Empty)));
            foreach (var entry in animationStateMachines)
                if (entry.Name is { } name)
                    names.TryAdd(entry.Instance, (name, animationStateMachineClass));
            var uninstalledTunings = AddUninstalledTuningNames(GetUninstalledTunings(snapshot), names);
            var tuningsByKey = new Dictionary<(ResourceType type, ulong instance), TuningArchiveEntry>();
            foreach (var entry in tunings)
                tuningsByKey.TryAdd((entry.Type, entry.Instance), entry);
            var tuningsByName = new Dictionary<(ResourceType type, string name), TuningArchiveEntry>();
            foreach (var entry in tunings)
                if (entry.Name is { } name)
                    tuningsByName.TryAdd((entry.Type, name), entry);
            var animationStateMachinesByInstance = new Dictionary<ulong, TuningArchiveEntry>();
            foreach (var entry in animationStateMachines)
                animationStateMachinesByInstance.TryAdd(entry.Instance, entry);
            var describe = options.IncludeStringComments || options.IncludeReferenceComments || options.WriteFileIndex;
            var descriptionCache = describe && options.UseTuningDescriptions && FindTuningDescriptions(snapshot) is { } descriptions ? GetTuningDescriptionCache(descriptions) : null;
            var crossReferences = options.WriteFileIndex ? new ExportCrossReferences() : null;
            var commentTables = new CommentTables(strings, names, tuningsByKey, tuningsByName, animationStateMachinesByInstance, descriptionCache, uninstalledTunings);
            progress?.Report(new("Naming the files", 0, 0));
            var planned = new List<ExportFile>();
            var claimedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tuningFolders = new Dictionary<ulong, string>();
            string plan(TuningArchiveEntry entry, string folder, string instanceType, uint group, string? fileNameStem = null)
            {
                var name = entry.Name ?? entry.Instance.ToString(CultureInfo.InvariantCulture);
                var fileName = GetExportFileName(options.Naming, entry.Type, group, entry.Instance, GetExportSafeName(fileNameStem ?? name), GetExportSafeName(instanceType));
                var relativePath = System.IO.Path.Combine(folder, fileName);
                if (!claimedPaths.Add(relativePath))
                {
                    var extension = System.IO.Path.GetExtension(fileName);
                    var stem = fileName[..^extension.Length];
                    var number = 2;
                    string candidate;
                    do
                        candidate = System.IO.Path.Combine(folder, $"{stem} ({number++}){extension}");
                    while (!claimedPaths.Add(candidate));
                    problems.Add($"{relativePath} would be written for more than one resource; {entry.Key} ({name}) was written as {candidate}");
                    relativePath = candidate;
                }
                planned.Add(new(entry, relativePath, name, instanceType));
                return relativePath;
            }
            foreach (var entry in tunings)
            {
                var typeName = typeNames.TryGetValue(entry.Type, out var knownTypeName) ? knownTypeName : string.Create(CultureInfo.InvariantCulture, $"0x{(uint)entry.Type:X8}");
                var relativePath = plan(entry, System.IO.Path.Combine(GetExportSafeName(entry.Pack), GetExportSafeName(typeName)), entry.Class is not null ? typeName : string.Empty, packIdentifiers[entry.Pack]);
                tuningFolders.TryAdd(entry.Instance, System.IO.Path.GetDirectoryName(relativePath)!);
            }
            foreach (var entry in animationStateMachines)
                plan(entry, System.IO.Path.Combine(GetExportSafeName(entry.Pack), animationStateMachineFolder), animationStateMachineInstanceType, 0);
            if (options.IncludeSimData)
                foreach (var entry in simData)
                    plan(entry, tuningFolders.TryGetValue(entry.Instance, out var tuningFolder) ? tuningFolder : System.IO.Path.Combine(GetExportSafeName(entry.Pack), simDataFolder), "SimData", entry.Group, options.Naming is TuningArchiveExportNaming.Name ? $"{entry.Name ?? entry.Instance.ToString(CultureInfo.InvariantCulture)}.SimData" : null);
            var written = 0;
            var renamed = 0;
            var skipped = 0;
            var completed = 0;
            var indexLines = new ConcurrentBag<(string path, string line)>();
            planned.AsParallel().WithCancellation(cancellationToken).ForAll(file =>
            {
                var done = Interlocked.Increment(ref completed);
                if (done % progressInterval == 0)
                    progress?.Report(new("Writing files", done, planned.Count));
                var entry = file.Entry;
                var path = System.IO.Path.Combine(root, file.RelativePath);
                try
                {
                    var skip = options.ExistingFiles is TuningArchiveExportExistingFiles.Skip && File.Exists(path);
                    var text = GetText(entry);
                    var document = TuningXml.Parse(text);
                    var documentRoot = document.Root!;
                    var instanceType = file.InstanceType;
                    if (entry.Kind is TuningArchiveResourceKind.Tuning)
                    {
                        crossReferences?.Add(entry.Instance);
                        AddComments(document, text, entry.Instance, commentTables, options.IncludeStringComments, options.IncludeReferenceComments, crossReferences);
                    }
                    if (skip)
                        Interlocked.Increment(ref skipped);
                    else
                    {
                        if (options.ExistingFiles is TuningArchiveExportExistingFiles.Rename && MoveAsideExistingExportFile(path))
                            Interlocked.Increment(ref renamed);
                        ApplyExportAttributeOrder(documentRoot, options.AttributeOrder);
                        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
                        using (var stream = File.Create(path))
                        using (var writer = XmlWriter.Create(stream, exportWriterSettings))
                        {
                            writer.WriteStartDocument();
                            documentRoot.WriteTo(writer);
                        }
                        Interlocked.Increment(ref written);
                    }
                    if (options.WriteFileIndex)
                    {
                        var (indexClass, indexInstanceType) = entry.Kind switch
                        {
                            TuningArchiveResourceKind.AnimationStateMachine => (animationStateMachineClass, "ASM"),
                            TuningArchiveResourceKind.SimData => ("SimData", "SimData"),
                            _ => (entry.Class ?? string.Empty, instanceType)
                        };
                        indexLines.Add((file.RelativePath, string.Create(CultureInfo.InvariantCulture, $"{entry.Instance}\t{file.Name}\t{indexClass}\t{indexInstanceType}\t{file.RelativePath}")));
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException or ArgumentException or InvalidDataException)
                {
                    lock (problems)
                        problems.Add($"{file.RelativePath} could not be written ({ex.Message})");
                }
            });
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new("Writing files", planned.Count, planned.Count));
            string? prepareReferenceFile(string fileName)
            {
                var path = System.IO.Path.Combine(root, fileName);
                if (File.Exists(path) && options.ExistingFiles is TuningArchiveExportExistingFiles.Skip)
                    return null;
                if (options.ExistingFiles is TuningArchiveExportExistingFiles.Rename)
                    MoveAsideExistingExportFile(path);
                return path;
            }
            void writeText(string fileName, IEnumerable<string> lines)
            {
                if (prepareReferenceFile(fileName) is not { } path)
                    return;
                using var writer = new StreamWriter(path, false, new UTF8Encoding(false)) { NewLine = "\r\n" };
                foreach (var line in lines)
                    writer.WriteLine(line);
            }
            try
            {
                if (options.WriteReferenceFiles)
                {
                    progress?.Report(new("Writing reference files", 0, 0));
                    if (strings.Count > 0)
                        writeText(stringsReferenceFileName, strings.OrderBy(pair => pair.Key).Select(pair => string.Create(CultureInfo.InvariantCulture, $"0x{pair.Key:X8}\t{GetExportStringText(pair.Value)}")));
                    writeText(xmlNamesReferenceFileName, names.OrderBy(pair => pair.Key).Select(pair => string.Create(CultureInfo.InvariantCulture, $"0x{pair.Key:X16}\t{pair.Value.@class}\t{pair.Value.name}")));
                }
                if (options.WriteFileIndex)
                {
                    progress?.Report(new("Writing the file index", 0, 0));
                    writeText(fileIndexFileName, indexLines.OrderBy(entry => entry.path, StringComparer.OrdinalIgnoreCase).Select(entry => entry.line));
                    if (prepareReferenceFile(xmlCrossReferenceFileName) is { } crossReferencePath)
                    {
                        using var stream = File.Create(crossReferencePath);
                        crossReferences!.Write(stream);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                problems.Add($"The reference files could not be written ({ex.Message})");
            }
            return new TuningArchiveExportResult(written, renamed, skipped, descriptionCache is not null, packIdentifiers, problems, stopwatch.Elapsed);
        }, cancellationToken).ConfigureAwait(false);
    }
}