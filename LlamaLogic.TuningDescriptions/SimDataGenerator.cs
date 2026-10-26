namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Generates <see cref="ResourceType.SimData"/> resources from tuning the way Maxis' build does, using the tuning descriptions of the game version for which they are generated (🔒)
/// </summary>
/// <remarks>
/// <para>Which tunables become columns, the columns' types, and the values written for tuning that omits a tunable all come from <see cref="Descriptions"/>.</para>
/// <para>Some of what Maxis' build knows is not in tuning descriptions, so the generator learns it:</para>
/// <list type="bullet">
/// <item><see cref="AddTuning(XDocument)"/> and <see cref="AddTuning(CombinedTuningModel)"/> learn the entries of dynamic enumerations (such as tags defined by module tuning) and the instances of tuning that is referenced by name</item>
/// <item><see cref="AddLayouts(SimDataModel)"/> learns the column order of schemas the game reads with a layout of its own (such as <c>Client_*</c> and <c>Renderer.*</c> schemas), whose columns are otherwise ordered by name</item>
/// </list>
/// <para>Maxis' build also converts some resource keys: a <see cref="ResourceType.PNGImage"/> key in tuning is written to SimData as a <see cref="ResourceType.DSTImage"/> key, and an <see cref="ResourceType.AudioConfigurationReference"/> key as an <see cref="ResourceType.AudioConfiguration"/> key in group <c>0x001407EC</c>. The generator does the same unless <see cref="RemoveResourceKeyConversion(ResourceType, uint)"/> says otherwise, and <see cref="AddResourceKeyConversion(ResourceType, uint, ResourceType, uint)"/> adds conversions.</para>
/// <para>For the game's own SimData, the game's combined tuning and SimData are the sources of what the generator needs to learn.</para>
/// </remarks>
public sealed class SimDataGenerator
{
    static readonly IReadOnlyDictionary<string, SimDataType> vectorTypes = new Dictionary<string, SimDataType>(StringComparer.Ordinal)
    {
        { "TunableVector2", SimDataType.FLOAT2 },
        { "TunableVector3", SimDataType.FLOAT3 },
        { "TunableVector4", SimDataType.FLOAT4 }
    };

    static string? GetAttribute(XElement element, string name) =>
        element.Attribute(name)?.Value;

    static string GetColumnNamesKey(IEnumerable<string> columnNames) =>
        string.Join("\0", columnNames.OrderBy(columnName => columnName, StringComparer.Ordinal));

    [SuppressMessage("Globalization", "CA1308: Normalize strings to uppercase", Justification = "Maxis hashes lowercase strings.")]
    static uint GetHash32(string text)
    {
        var hash = 0x811C9DC5U;
        foreach (var b in Encoding.UTF8.GetBytes(text.ToLowerInvariant()))
        {
            hash *= 0x01000193U;
            hash ^= b;
        }
        return hash;
    }

    [SuppressMessage("Globalization", "CA1308: Normalize strings to uppercase", Justification = "Maxis hashes lowercase strings.")]
    static ulong GetHash64(string text)
    {
        var hash = 0xCBF29CE484222325UL;
        foreach (var b in Encoding.UTF8.GetBytes(text.ToLowerInvariant()))
        {
            hash *= 0x100000001B3UL;
            hash ^= b;
        }
        return hash;
    }

    static string? GetText(XElement? element)
    {
        if (element is null)
            return null;
        string? text = null;
        foreach (var node in element.Nodes())
        {
            if (node is XElement)
                break;
            if (node is XText xText)
                text = (text ?? string.Empty) + xText.Value;
        }
        return text;
    }

    static Dictionary<string, XElement> GetValuesByName(XElement? element)
    {
        var valuesByName = new Dictionary<string, XElement>(StringComparer.Ordinal);
        if (element is not null)
            foreach (var child in element.Elements())
                if (GetAttribute(child, "n") is { } name)
                    valuesByName[name] = child;
        return valuesByName;
    }

    static bool IsClientBinary(XElement element) =>
        GetAttribute(element, "export_modes")?.Contains("client_binary", StringComparison.Ordinal) ?? false;

    static bool IsExcluded(XElement element) =>
        GetAttribute(element, "_force") != "1" && GetAttribute(element, "export_modes") is not null && !IsClientBinary(element);

    static bool IsNone(string? text) =>
        text is null or "None";

    static uint ParseHex32(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[2..];
        return uint.Parse(trimmed, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
    }

    static int ParseInt32(string text)
    {
        if (text.Contains('.', StringComparison.Ordinal))
            return checked((int)double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture));
        var trimmed = text.Trim();
        var negative = trimmed.StartsWith('-');
        if (negative || trimmed.StartsWith('+'))
            trimmed = trimmed[1..];
        var magnitude = trimmed.Length > 2 && trimmed[0] == '0' && char.ToLowerInvariant(trimmed[1]) is 'x' or 'o' or 'b'
            ? Convert.ToInt64(trimmed[2..], char.ToLowerInvariant(trimmed[1]) switch { 'x' => 16, 'o' => 8, _ => 2 })
            : long.Parse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture);
        return checked((int)(negative ? -magnitude : magnitude));
    }

    static float ParseSingle(string? text) =>
        IsNone(text) ? 0F : (float)double.Parse(text!, NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <summary>
    /// Initializes a new <see cref="SimDataGenerator"/>
    /// </summary>
    /// <param name="descriptions">The tuning descriptions of the game version for which SimData will be generated</param>
    public SimDataGenerator(TuningDescriptionSet descriptions)
    {
        ArgumentNullException.ThrowIfNull(descriptions);
        Descriptions = descriptions;
        resourceKeyConversions[(ResourceType.PNGImage, 0)] = (ResourceType.DSTImage, 0);
        resourceKeyConversions[(ResourceType.AudioConfigurationReference, 0)] = (ResourceType.AudioConfiguration, 0x001407EC);
    }

    readonly ConcurrentDictionary<string, ConcurrentDictionary<string, long>> dynamicEnumEntries = new(StringComparer.Ordinal);
    readonly ConditionalWeakTable<XElement, StrongBox<bool>> exportedDescriptions = new();
    readonly ConcurrentDictionary<string, XElement> fragments = new(StringComparer.Ordinal);
    readonly ConditionalWeakTable<XElement, IReadOnlyList<(string Name, XElement Description)>> fullTupleColumns = new();
    readonly ConcurrentDictionary<string, TuningDescriptionInstance> instances = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, ulong> instancesByName = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<(string SchemaName, string ColumnNames), ImmutableArray<string>> layouts = new();
    readonly ConditionalWeakTable<XElement, XElement> mappingItems = new();
    readonly ConditionalWeakTable<XElement, IReadOnlyList<(string Name, XElement Description)>> partialTupleColumns = new();
    readonly ConcurrentDictionary<(ResourceType Type, uint Group), (ResourceType Type, uint Group)> resourceKeyConversions = new();
    readonly ConcurrentDictionary<(string Name, string Columns), SimDataSchema> schemas = new();
    readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, long>?> staticEnumEntries = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the tuning descriptions of the game version for which SimData is generated
    /// </summary>
    public TuningDescriptionSet Descriptions { get; }

    /// <summary>
    /// Learns the column order of each schema in the specified <paramref name="simData"/> whose columns are not ordered by name
    /// </summary>
    /// <remarks>
    /// When generating an object of a schema with the same name and the same columns, the generator orders the columns as they are ordered in <paramref name="simData"/>.
    /// </remarks>
    public void AddLayouts(SimDataModel simData)
    {
        ArgumentNullException.ThrowIfNull(simData);
        foreach (var schema in simData.Schemas)
        {
            var columnNames = schema.Columns.Select(column => column.Name).ToImmutableArray();
            if (!columnNames.SequenceEqual(columnNames.OrderBy(columnName => columnName, StringComparer.Ordinal)))
                layouts[(schema.Name, GetColumnNamesKey(columnNames))] = columnNames;
        }
    }

    /// <summary>
    /// Learns that resource keys of the specified <paramref name="fromType"/> and <paramref name="fromGroup"/> in tuning are written to SimData with the specified <paramref name="toType"/> and <paramref name="toGroup"/>
    /// </summary>
    public void AddResourceKeyConversion(ResourceType fromType, uint fromGroup, ResourceType toType, uint toGroup) =>
        resourceKeyConversions[(fromType, fromGroup)] = (toType, toGroup);

    /// <summary>
    /// Learns the entries of dynamic enumerations and the names of instances from each tuning in the specified <paramref name="combinedTuning"/>
    /// </summary>
    public void AddTuning(CombinedTuningModel combinedTuning)
    {
        ArgumentNullException.ThrowIfNull(combinedTuning);
        foreach (var tuning in combinedTuning.GetTunings())
            AddTuning(tuning.Document);
    }

    /// <summary>
    /// Learns the entries of dynamic enumerations the specified <paramref name="tuning"/> defines (if it is module tuning) and the name of its instance
    /// </summary>
    /// <remarks>
    /// An instance or enumeration value that is not a decimal number (such as one a modding tool wrote in hexadecimal) is not learned.
    /// </remarks>
    public void AddTuning(XDocument tuning)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        if (tuning.Root is not { } root)
            return;
        var name = GetAttribute(root, "n");
        if (name is not null && ulong.TryParse(GetAttribute(root, "s"), NumberStyles.None, CultureInfo.InvariantCulture, out var instance))
            instancesByName[name] = instance;
        if (root.Name.LocalName != "M" || name is null)
            return;
        void addClasses(XElement parent, string path)
        {
            foreach (var @class in parent.Elements("C"))
            {
                if (GetAttribute(@class, "n") is not { } className)
                    continue;
                var classPath = $"{path}.{className}";
                foreach (var elements in @class.Elements("L").Where(list => GetAttribute(list, "n") == "_elements"))
                {
                    var entries = dynamicEnumEntries.GetOrAdd($"{classPath}._elements", _ => new(StringComparer.Ordinal));
                    foreach (var entry in elements.Elements())
                        if (long.TryParse(GetAttribute(entry, "ev"), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) && GetText(entry) is { } entryName)
                            entries[entryName] = value;
                }
                addClasses(@class, classPath);
            }
        }
        addClasses(root, name.Replace('.', '-'));
    }

    IReadOnlyList<(string Name, XElement Description)> Arrange(string? schemaName, IReadOnlyList<(string Name, XElement Description)> columns)
    {
        if (schemaName is null || !layouts.TryGetValue((schemaName, GetColumnNamesKey(columns.Select(column => column.Name))), out var layout))
            return columns;
        var columnsByName = columns.ToDictionary(column => column.Name, column => column.Description, StringComparer.Ordinal);
        return [.. layout.Select(name => (name, columnsByName[name]))];
    }

    SimDataObject CreateObject(string schemaName, IReadOnlyList<(string Name, XElement Description)> columns, XElement? value, bool full, (string Name, SimDataType Type, object? Value)? appendedColumn = null)
    {
        var columnTypes = columns.Select(column => (column.Name, Type: GetSimDataType(column.Description) ?? throw new SimDataGenerationException($"The tuning descriptions do not give a SimData type for {column.Description.Name.LocalName} {GetAttribute(column.Description, "class")} {column.Name} of {schemaName}"))).ToList();
        if (appendedColumn is { } appended)
            columnTypes.Add((appended.Name, appended.Type));
        var schema = GetSchema(schemaName, columnTypes);
        var @object = new SimDataObject(schema);
        var valuesByName = GetValuesByName(value);
        for (var i = 0; i < columns.Count; ++i)
            @object[i] = GenerateValue(columns[i].Description, valuesByName.GetValueOrDefault(columns[i].Name), full).Value;
        if (appendedColumn is { } appendedValue)
            @object[columns.Count] = appendedValue.Value;
        return @object;
    }

    /// <summary>
    /// Generates the SimData for the specified <paramref name="tuning"/>
    /// </summary>
    /// <param name="tuning">Instance tuning (an <c>I</c> element) or module tuning (an <c>M</c> element)</param>
    /// <returns>The SimData, with one instance for instance tuning or one instance for each class of module tuning that has exported tunables</returns>
    /// <remarks>
    /// The <see cref="SimDataModel.Pack"/> of the SimData is <c>0</c>, the base game's; set it for SimData that belongs to a pack.
    /// </remarks>
    /// <exception cref="SimDataGenerationException"><paramref name="tuning"/> cannot be generated with <see cref="Descriptions"/> and what the generator has learned</exception>
    public SimDataModel Generate(XDocument tuning)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        var root = tuning.Root ?? throw new SimDataGenerationException("The tuning has no root element");
        var model = new SimDataModel();
        try
        {
            switch (root.Name.LocalName)
            {
                case "I":
                    model.Instances.Add(GenerateInstance(root));
                    break;
                case "M":
                    foreach (var instance in GenerateModule(root))
                        model.Instances.Add(instance);
                    if (model.Instances.Count == 0)
                        throw new SimDataGenerationException($"Module {GetAttribute(root, "n")} has no exported tunables");
                    break;
                default:
                    throw new SimDataGenerationException($"The root element of the tuning is {root.Name.LocalName}, not I or M");
            }
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw new SimDataGenerationException($"A value in the tuning cannot be read: {ex.Message}", ex);
        }
        return model;
    }

    (SimDataType Type, object? Value) GenerateEnum(XElement description, XElement? value, SimDataType type)
    {
        var text = GetText(value);
        if (string.IsNullOrEmpty(text))
            text = GetAttribute(description, "default");
        var staticEntries = GetAttribute(description, "static_entries") ?? throw new SimDataGenerationException($"Enum {GetAttribute(description, "name")} has no static entries");
        var staticValues = staticEnumEntries.GetOrAdd(staticEntries, _ => Descriptions.GetEnum(staticEntries) is { } @enum ? @enum.EnumItems.GroupBy(enumItem => enumItem.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal) : null);
        var dynamicEntries = GetAttribute(description, "dynamic_entries") is { } dynamicEntriesName ? dynamicEnumEntries.GetValueOrDefault(dynamicEntriesName) : null;
        long? lookUp(string candidate) =>
            staticValues is not null && staticValues.TryGetValue(candidate, out var staticValue) ? staticValue : dynamicEntries is not null && dynamicEntries.TryGetValue(candidate, out var dynamicValue) ? dynamicValue : null;
        if (text is not null && text.Contains('.', StringComparison.Ordinal) && lookUp(text) is null)
            text = text[(text.LastIndexOf('.') + 1)..];
        var enumValue = text is null
            ? null
            : lookUp(text) ?? (text.TrimStart('-') is { Length: > 0 } digits && digits.All(char.IsDigit) ? long.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture) : null);
        if (enumValue is not { } resolved)
            throw new SimDataGenerationException($"{text ?? "None"} is not a value of {staticEntries}");
        return (type, type is SimDataType.UINT32 ? unchecked((uint)resolved) : (object)resolved);
    }

    SimDataInstance GenerateInstance(XElement root)
    {
        var className = GetAttribute(root, "c") ?? throw new SimDataGenerationException("The instance tuning has no class");
        var instance = instances.GetOrAdd(className, _ => Descriptions.GetInstance(className) ?? throw new SimDataGenerationException($"The tuning descriptions do not describe {className}"));
        var full = IsClientBinary(instance.Source);
        var columns = instance.GetEffectiveMembersAndDeclarers()
            .Where(memberAndDeclarer => IsInstanceColumn(full, memberAndDeclarer.Member.Source, memberAndDeclarer.Declarer.Source))
            .Select(memberAndDeclarer => (Name: memberAndDeclarer.Member.Name!, Description: Resolve(memberAndDeclarer.Member.Source)))
            .OrderBy(column => column.Name, StringComparer.Ordinal)
            .ToList();
        return new SimDataInstance(GetAttribute(root, "n") ?? string.Empty, CreateObject(className, Arrange(className, columns), root, full));
    }

    IEnumerable<SimDataInstance> GenerateModule(XElement root)
    {
        var moduleName = GetAttribute(root, "n") ?? throw new SimDataGenerationException("The module tuning has no name");
        var module = Descriptions.GetModule(moduleName) ?? throw new SimDataGenerationException($"The tuning descriptions do not describe module {moduleName}");
        var classValues = new Dictionary<string, XElement>(StringComparer.Ordinal);
        void addClassValues(XElement parent, string path)
        {
            foreach (var @class in parent.Elements("C"))
                if (GetAttribute(@class, "n") is { } className)
                {
                    classValues[$"{path}.{className}"] = @class;
                    addClassValues(@class, $"{path}.{className}");
                }
        }
        addClassValues(root, moduleName);
        IEnumerable<(string Path, TuningDescriptionClass Class)> getClasses(IEnumerable<TuningDescriptionClass> classes, string path)
        {
            foreach (var @class in classes)
            {
                var classPath = $"{path}.{@class.Name}";
                yield return (classPath, @class);
                foreach (var nested in getClasses(@class.Classes, classPath))
                    yield return nested;
            }
        }
        foreach (var (path, @class) in getClasses(module.Classes, moduleName))
        {
            var columns = @class.Members
                .Where(member => member.Name is not null && IsExported(member.Source))
                .Select(member => (Name: member.Name!, Description: Resolve(member.Source)))
                .OrderBy(column => column.Name, StringComparer.Ordinal)
                .ToList();
            if (columns.Count == 0)
                continue;
            var classValue = classValues.GetValueOrDefault(path);
            (string Name, SimDataType Type, object? Value)? enumNameValuePairs = null;
            if (@class.IsEnum && columns.Any(column => column.Name == "_elements"))
            {
                var pairSchema = GetSchema("EnumNameValuePair", [("Name", SimDataType.STRING8), ("Value", SimDataType.INT64)]);
                SimDataObject createPair(string name, long value)
                {
                    var pair = new SimDataObject(pairSchema);
                    pair[0] = name;
                    pair[1] = value;
                    return pair;
                }
                var dynamicPairs = classValue?.Elements("L").Where(list => GetAttribute(list, "n") == "_elements").SelectMany(list => list.Elements()).Where(entry => GetAttribute(entry, "ev") is not null).Select(entry => createPair(GetText(entry) ?? string.Empty, long.Parse(GetAttribute(entry, "ev")!, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture))) ?? [];
                enumNameValuePairs = ("EnumNameValuePairs", SimDataType.VECTOR, new SimDataVector(SimDataType.OBJECT, @class.EnumItems.Select(enumItem => createPair(enumItem.Name, enumItem.Value)).Concat(dynamicPairs)));
            }
            yield return new SimDataInstance(path, CreateObject(path, Arrange(path, columns), classValue, false, enumNameValuePairs));
        }
    }

    (SimDataType Type, object? Value) GeneratePrimitive(XElement description, XElement? value, SimDataType type)
    {
        var text = GetText(value);
        if (string.IsNullOrEmpty(text))
            text = GetAttribute(description, "default");
        switch (type)
        {
            case SimDataType.BOOL:
                return (type, string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) || text == "1");
            case SimDataType.INT32:
                return (type, IsNone(text) ? 0 : ParseInt32(text!));
            case SimDataType.FLOAT:
                return (type, ParseSingle(text));
            case SimDataType.STRING8:
                if (value is not null && string.IsNullOrEmpty(GetText(value)))
                    text = string.Empty;
                if (text is null || (text == "None" && description.Attribute("choice_query") is not null && (value is null || string.IsNullOrEmpty(GetText(value)))))
                    text = string.Empty;
                return (type, text);
            case SimDataType.LOCKEY:
                return (type, IsNone(text) ? 0U : ParseHex32(text!));
            case SimDataType.TABLESETREFERENCE:
                return (type, GetReference(text));
            case SimDataType.UINT32 when description.Name.LocalName == "Tunable" && GetAttribute(description, "class") == "TunableReference":
                return (type, unchecked((uint)GetReference(text)));
            case SimDataType.UINT64:
                return (type, IsNone(text) ? 0UL : GetHash64(text!));
            case SimDataType.UINT32:
                return (type, IsNone(text) || text!.Length == 0 ? 0U : GetHash32(text));
            case SimDataType.RESOURCEKEY:
                if (IsNone(text) || text!.Length == 0)
                    return (type, new ResourceKey(0, 0, 0));
                var parts = text.Split(':');
                if (parts.Length != 3)
                    throw new SimDataGenerationException($"{text} is not a resource key");
                var resourceType = (ResourceType)ParseHex32(parts[0]);
                var group = ParseHex32(parts[1]);
                var instancePart = parts[2].Trim();
                var fullInstance = ulong.Parse(instancePart.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? instancePart[2..] : instancePart, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                if (resourceKeyConversions.TryGetValue((resourceType, group), out var conversion))
                    (resourceType, group) = conversion;
                return (type, new ResourceKey(resourceType, group, fullInstance));
            default:
                throw new SimDataGenerationException($"Generating {type} values from {description.Name.LocalName} {GetAttribute(description, "class")} is not supported");
        }
    }

    (SimDataType Type, object? Value) GenerateValue(XElement description, XElement? value, bool full)
    {
        description = Resolve(description);
        full = IsClientBinary(description) || (full && description.Attribute("export_modes") is null);
        var type = GetSimDataType(description) ?? throw new SimDataGenerationException($"The tuning descriptions do not give a SimData type for {description.Name.LocalName} {GetAttribute(description, "class")} {GetAttribute(description, "name")}");
        if (description.Name.LocalName == "TunableEnum")
            return GenerateEnum(description, value, type);
        switch (type)
        {
            case SimDataType.VECTOR:
                var item = description.Elements().FirstOrDefault(child => child.Name.LocalName != "Deleted") is { } firstChild
                    ? Resolve(firstChild)
                    : throw new SimDataGenerationException($"List {GetAttribute(description, "name")} has no item description");
                if (description.Attribute("mapping_key") is not null && item.Name.LocalName == "TunableTuple")
                    item = mappingItems.GetValue(description, list => GetMappingItem(list, item));
                var itemType = GetSimDataType(item) ?? throw new SimDataGenerationException($"The tuning descriptions do not give a SimData type for the items of list {GetAttribute(description, "name")}");
                return (type, new SimDataVector(itemType, value?.Elements().Select(element => GenerateValue(item, element, full).Value)));
            case SimDataType.OBJECT:
                return (type, CreateObject(GetAttribute(description, "class") ?? string.Empty, GetTupleColumns(description, full), value, full));
            case SimDataType.FLOAT2 or SimDataType.FLOAT3 or SimDataType.FLOAT4:
                var componentValues = GetValuesByName(value);
                var componentDescriptions = new Dictionary<string, XElement>(StringComparer.Ordinal);
                foreach (var child in description.Elements())
                    if (GetAttribute(child, "name") is { } childName)
                        componentDescriptions[childName] = child;
                float component(string name) =>
                    ParseSingle(componentValues.TryGetValue(name, out var componentValue) ? GetText(componentValue) : componentDescriptions.TryGetValue(name, out var componentDescription) ? GetAttribute(componentDescription, "default") : "0");
                return type switch
                {
                    SimDataType.FLOAT2 => (type, new Vector2(component("x"), component("y"))),
                    SimDataType.FLOAT3 => (type, new Vector3(component("x"), component("y"), component("z"))),
                    _ => (type, new Vector4(component("x"), component("y"), component("z"), component("w")))
                };
            case SimDataType.VARIANT:
                var option = value is null ? null : GetAttribute(value, "t");
                if (string.IsNullOrEmpty(option))
                    option = GetAttribute(description, "default");
                if (option is null)
                    throw new SimDataGenerationException($"Variant {GetAttribute(description, "name")} has no option and no default");
                var optionDescription = description.Elements().FirstOrDefault(child => GetAttribute(child, "name") == option)
                    ?? throw new SimDataGenerationException($"Variant {GetAttribute(description, "name")} has no option {option}");
                var typeHash = GetHash32(option);
                if (!IsVariantOptionWritten(optionDescription, full))
                    return (type, new SimDataVariant(typeHash));
                var optionValue = GenerateValue(optionDescription, value?.Elements().FirstOrDefault(child => GetAttribute(child, "n") == option), full);
                return (type, new SimDataVariant(typeHash, optionValue.Type, optionValue.Value));
            default:
                return GeneratePrimitive(description, value, type);
        }
    }

    /// <summary>
    /// Gets whether the tunable at the end of the specified <paramref name="path"/> in instance tuning of the specified <paramref name="instance"/> class is written to the SimData <see cref="Generate"/> generates, so that changing it changes the SimData
    /// </summary>
    /// <param name="instance">The class of the instance tuning</param>
    /// <param name="path">The descriptions from one of the class's effective members (see <see cref="TuningDescriptionInstance.GetEffectiveMembers"/>) down to the tunable: after a list, its item; after a variant, the option; after a tuple, the member</param>
    /// <remarks>
    /// The same rules decide this as decide what <see cref="Generate"/> writes, including those that export a whole class (its <c>export_modes</c> has <c>client_binary</c>), a member through the class that declares it, and every member of a tuple none of whose members is exported.
    /// A path that ends at a variant's option stands for choosing that option, which changes the variant's hash, so it is written whenever the variant is, even when the option's own tunables are not.
    /// </remarks>
    public bool IsWrittenToSimData(TuningDescriptionInstance instance, IReadOnlyList<TunableDescription> path)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(path);
        if (path.Count is 0 || path[0].Name is not { } topName)
            return false;
        var full = IsClientBinary(instance.Source);
        var top = instance.GetEffectiveMembersAndDeclarers().FirstOrDefault(memberAndDeclarer => memberAndDeclarer.Member.Name == topName);
        if (top.Member is null || !IsInstanceColumn(full, top.Member.Source, top.Declarer.Source))
            return false;
        var description = Resolve(top.Member.Source);
        for (var i = 1; i < path.Count; ++i)
        {
            full = IsClientBinary(description) || (full && description.Attribute("export_modes") is null);
            var name = GetAttribute(path[i].Source, "name");
            switch (GetSimDataType(description))
            {
                case SimDataType.VECTOR:
                    if (description.Elements().FirstOrDefault(child => child.Name.LocalName != "Deleted") is not { } firstChild)
                        return false;
                    var item = Resolve(firstChild);
                    if (description.Attribute("mapping_key") is not null && item.Name.LocalName == "TunableTuple")
                        item = mappingItems.GetValue(description, list => GetMappingItem(list, item));
                    description = item;
                    break;
                case SimDataType.VARIANT:
                    if (i == path.Count - 1)
                        return true;
                    if (description.Elements().FirstOrDefault(child => GetAttribute(child, "name") == name) is not { } option || !IsVariantOptionWritten(option, full))
                        return false;
                    description = Resolve(option);
                    break;
                case SimDataType.OBJECT:
                    if (GetTupleColumns(description, full).FirstOrDefault(column => column.Name == name).Description is not { } column)
                        return false;
                    description = column;
                    break;
                default:
                    return true;
            }
        }
        return true;
    }

    bool IsInstanceColumn(bool full, XElement member, XElement declarer) =>
        full ? !IsExcluded(member) : IsClientBinary(declarer) || IsExported(member);

    bool IsVariantOptionWritten(XElement option, bool full) =>
        GetAttribute(option, "class") != "TunableExistance" && !IsExcluded(option) && (full || IsExported(option));

    XElement GetMappingItem(XElement list, XElement item)
    {
        var mappingNames = new HashSet<string>(new[] { GetAttribute(list, "mapping_key"), GetAttribute(list, "mapping_value") }.OfType<string>(), StringComparer.Ordinal);
        var mappingItem = new XElement(item.Name, item.Attributes().Where(attribute => attribute.Name.LocalName != "class"), new XAttribute("class", GetAttribute(list, "mapping_class") ?? GetAttribute(item, "class") ?? string.Empty));
        foreach (var child in item.Elements())
            mappingItem.Add(GetAttribute(child, "name") is { } childName && mappingNames.Contains(childName) && !IsClientBinary(child)
                ? new XElement(child.Name, child.Attributes(), new XAttribute("_force", "1"), child.Elements())
                : child);
        return mappingItem;
    }

    ulong GetReference(string? text)
    {
        if (IsNone(text) || text!.Length == 0)
            return 0;
        if (ulong.TryParse(text, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out var instance))
            return instance;
        if (instancesByName.TryGetValue(text, out instance))
            return instance;
        throw new SimDataGenerationException($"No tuning named {text} has been added to the generator");
    }

    SimDataSchema GetSchema(string name, IReadOnlyList<(string Name, SimDataType Type)> columnTypes) =>
        columnTypes.Count == 0
        ? throw new SimDataGenerationException($"{name} has no exported tunables")
        : schemas.GetOrAdd((name, string.Join("\0", columnTypes.Select(columnType => $"{columnType.Name}\0{(int)columnType.Type}"))), _ => new SimDataSchema(name, columnTypes.Select(columnType => new SimDataColumn(columnType.Name, columnType.Type))));

    SimDataType? GetSimDataType(XElement description)
    {
        var className = GetAttribute(description, "class");
        var type = GetAttribute(description, "type");
        var binaryType = GetAttribute(description, "binary_type")?.ToUpperInvariant();
        return description.Name.LocalName switch
        {
            "TunableList" => SimDataType.VECTOR,
            "TunableVariant" => SimDataType.VARIANT,
            "TunableTuple" => className is not null && vectorTypes.TryGetValue(className, out var vectorType) ? vectorType : SimDataType.OBJECT,
            "TunableEnum" => binaryType == "UINT32" ? SimDataType.UINT32 : SimDataType.INT64,
            _ when className == "TunableLocalizedString" => SimDataType.LOCKEY,
            _ when type == "ResourceKey" => SimDataType.RESOURCEKEY,
            _ when className == "TunableReference" => type == "object" || type is null || !Descriptions.InstanceTypes.Contains(type) ? SimDataType.UINT32 : SimDataType.TABLESETREFERENCE,
            _ => type switch
            {
                "bool" => SimDataType.BOOL,
                "float" => SimDataType.FLOAT,
                "int" => SimDataType.INT32,
                "str" => binaryType switch
                {
                    "HASHCODE64" => SimDataType.UINT64,
                    "HASHCODE32" => SimDataType.UINT32,
                    _ => SimDataType.STRING8
                },
                _ => null
            }
        };
    }

    IReadOnlyList<(string Name, XElement Description)> GetTupleColumns(XElement description, bool full) =>
        Arrange(GetAttribute(description, "class"), (full ? fullTupleColumns : partialTupleColumns).GetValue(description, tuple =>
        {
            var members = tuple.Elements().Where(child => child.Name.LocalName != "Deleted" && GetAttribute(child, "name") is not null).ToList();
            if (full)
                members = [.. members.Where(member => !IsExcluded(member))];
            else if (members.Any(IsExported))
                members = [.. members.Where(IsExported)];
            return [.. members.Select(member => (Name: GetAttribute(member, "name")!, Description: Resolve(member))).OrderBy(column => column.Name, StringComparer.Ordinal)];
        }));

    bool IsExported(XElement description) =>
        GetAttribute(description, "_force") == "1" || exportedDescriptions.GetValue(description, element => new(element.DescendantsAndSelf().Any(IsClientBinary))).Value!;

    /// <summary>
    /// Stops converting resource keys of the specified <paramref name="fromType"/> and <paramref name="fromGroup"/> in tuning, so that they are written to SimData as they are
    /// </summary>
    public void RemoveResourceKeyConversion(ResourceType fromType, uint fromGroup) =>
        resourceKeyConversions.TryRemove((fromType, fromGroup), out _);

    XElement Resolve(XElement description)
    {
        if (description.Name.LocalName != "TdescFragTag")
            return description;
        var className = GetAttribute(description, "class") ?? throw new SimDataGenerationException("A fragment reference has no class");
        return fragments.GetOrAdd(className, _ => (Descriptions.GetFragment(className) ?? throw new SimDataGenerationException($"The tuning descriptions do not have fragment {className}")).Description.Source);
    }
}
