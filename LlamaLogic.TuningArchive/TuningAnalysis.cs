namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents a tuning document understood through a set of tuning descriptions
/// </summary>
public sealed class TuningAnalysis
{
    static readonly IReadOnlyDictionary<string, ResourceType> nonTuningReferenceTypes = new Dictionary<string, ResourceType>(StringComparer.Ordinal)
    {
        ["block"] = (ResourceType)0x07936CE0,
        ["buildladder"] = (ResourceType)0xCC09004D,
        ["caspart"] = (ResourceType)0x034AEECB,
        ["ceilingrail"] = (ResourceType)0x3F0C529A,
        ["decotrim"] = (ResourceType)0x13CF0ED2,
        ["fence"] = (ResourceType)0x0418FE2A,
        ["floorpattern"] = (ResourceType)0xB4F762C9,
        ["floortrim"] = (ResourceType)0x84C23219,
        ["frieze"] = (ResourceType)0xA057811C,
        ["genericmtx"] = (ResourceType)0xAC03A936,
        ["halfwalltrim"] = (ResourceType)0xA9FADC5D,
        ["lotdescription"] = (ResourceType)0x01942E2C,
        ["magazinecollection"] = (ResourceType)0x74050B1F,
        ["mtxbundle"] = (ResourceType)0x8DB1DD26,
        ["object"] = ResourceType.ObjectDefinition,
        ["peltbrush"] = (ResourceType)0x9D7E7558,
        ["peltlayer"] = (ResourceType)0x26AF8338,
        ["pooltrim"] = (ResourceType)0xA5DFFCF3,
        ["railing"] = (ResourceType)0x1C1CF1F7,
        ["regiondescription"] = (ResourceType)0xD65DAFF9,
        ["roof"] = (ResourceType)0x91EDBD3E,
        ["roofpattern"] = (ResourceType)0xF1EDBD86,
        ["rooftrim"] = (ResourceType)0xB0311D0F,
        ["skintone"] = (ResourceType)0x0354796A,
        ["stair"] = (ResourceType)0x9A20CD1C,
        ["style"] = (ResourceType)0x9F5CFF10,
        ["wallpattern"] = (ResourceType)0xD5F0F921,
        ["worlddescription"] = (ResourceType)0xA680EA4B
    };

    /// <summary>
    /// Gets the resource type a tuning description's reference type name (such as <c>buff</c> or <c>caspart</c>) refers to
    /// </summary>
    public static ResourceType GetReferenceResourceType(string typeName)
    {
        ArgumentNullException.ThrowIfNull(typeName);
        return nonTuningReferenceTypes.TryGetValue(typeName, out var type) ? type : TuningXml.GetResourceType(typeName);
    }

    /// <summary>
    /// Gets whether a tuning description's reference type name refers to tuning (rather than another kind of resource)
    /// </summary>
    public static bool IsTuningReferenceType(string typeName)
    {
        ArgumentNullException.ThrowIfNull(typeName);
        return !nonTuningReferenceTypes.ContainsKey(typeName);
    }

    /// <summary>
    /// Analyzes the specified tuning document <paramref name="text"/> (canonical text, see <see cref="TuningXml"/>) with the specified <paramref name="descriptions"/>
    /// </summary>
    public static TuningAnalysis Analyze(string text, TuningDescriptionSet descriptions)
    {
        ArgumentNullException.ThrowIfNull(descriptions);
        return Analyze(text, new TuningDescriptionCache(descriptions));
    }

    /// <summary>
    /// Analyzes the specified tuning document <paramref name="text"/> (canonical text, see <see cref="TuningXml"/>) with the tuning descriptions of the specified <paramref name="cache"/>, which keeps the descriptions it reads for later analyses
    /// </summary>
    public static TuningAnalysis Analyze(string text, TuningDescriptionCache cache)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(cache);
        var descriptions = cache;
        var lines = text.Split('\n');
        var document = TuningXml.Parse(text);
        var nodes = new List<TuningNode>();
        var root = document.Root!;
        TuningDescriptionInstance? instance = null;
        TuningDescriptionModule? module = null;
        IReadOnlyList<TunableDescription> instanceMembers = [];
        var missing = new List<string>();
        void visitMembers(XElement parent, Func<string, TunableDescription?> getMember, string parentPath)
        {
            foreach (var child in parent.Elements())
            {
                var name = child.Attribute("n")?.Value;
                var path = parentPath.Length == 0 ? name ?? string.Empty : $"{parentPath}.{name}";
                var description = name is null ? null : getMember(name);
                if (description is null && name is not null)
                    missing.Add(path);
                Visit(child, description, path, descriptions, lines, nodes, missing);
            }
        }
        if (root.Name.LocalName == "I" && root.Attribute("c")?.Value is { } className)
        {
            instance = descriptions.GetInstance(className);
            if (descriptions.GetInstanceMembers(className) is { } members)
            {
                instanceMembers = ResolveMembers(members.Values, descriptions);
                visitMembers(root, name => members.GetValueOrDefault(name), string.Empty);
            }
            else
                visitMembers(root, _ => null, string.Empty);
        }
        else if (root.Name.LocalName == "M" && root.Attribute("n")?.Value is { } moduleName)
        {
            module = descriptions.GetModule(moduleName);
            void visitClass(XElement classElement, TuningDescriptionClass? descriptionClass, string classPath)
            {
                foreach (var child in classElement.Elements())
                {
                    var name = child.Attribute("n")?.Value ?? string.Empty;
                    var path = classPath.Length == 0 ? name : $"{classPath}.{name}";
                    if (child.Name.LocalName == "C")
                    {
                        nodes.Add(CreateNode(child, path, null, TuningValueKind.None, lines));
                        visitClass(child, descriptionClass?.GetClass(name), path);
                        continue;
                    }
                    var description = descriptionClass?.Members.FirstOrDefault(member => member.Name == name);
                    if (description is null)
                        missing.Add(path);
                    Visit(child, description, path, descriptions, lines, nodes, missing);
                }
            }
            foreach (var classElement in root.Elements())
            {
                var name = classElement.Attribute("n")?.Value ?? string.Empty;
                if (classElement.Name.LocalName == "C")
                {
                    nodes.Add(CreateNode(classElement, name, null, TuningValueKind.None, lines));
                    visitClass(classElement, module?.GetClass(name), name);
                }
                else
                    Visit(classElement, null, name, descriptions, lines, nodes, missing);
            }
        }
        return new(root, instance, module, instanceMembers, nodes, missing);
    }

    static IReadOnlyList<TunableDescription> ResolveMembers(IEnumerable<TunableDescription> members, TuningDescriptionCache descriptions) =>
        [.. members.Select(member =>
        {
            try
            {
                return descriptions.Resolve(member);
            }
            catch (InvalidDataException)
            {
                return member;
            }
        })];

    static TuningNode CreateNode(XElement element, string path, TunableDescription? description, TuningValueKind valueKind, string[] lines, string? value = null)
    {
        var lineInfo = (IXmlLineInfo)element;
        var line = lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0;
        var start = 0;
        var end = 0;
        if (value is not null && line > 0 && line <= lines.Length)
        {
            var lineText = lines[line - 1];
            var tagStart = lineInfo.LinePosition - 1;
            var inQuote = false;
            var position = tagStart;
            while (position < lineText.Length && (inQuote || lineText[position] != '>'))
            {
                if (lineText[position] == '"')
                    inQuote = !inQuote;
                ++position;
            }
            var closing = lineText.IndexOf("</", position, StringComparison.Ordinal);
            if (position < lineText.Length && closing > position)
            {
                start = position + 2;
                end = closing + 1;
            }
        }
        return new(line, start, end, path, element.Name.LocalName, value, description, valueKind);
    }

    static void Visit(XElement element, TunableDescription? description, string path, TuningDescriptionCache descriptions, string[] lines, List<TuningNode> nodes, List<string> missing)
    {
        if (description is not null)
            description = descriptions.Resolve(description);
        var value = element.HasElements ? null : element.Value is { Length: > 0 } text ? text : null;
        switch (description)
        {
            case ScalarTunableDescription scalar when value is not null:
                nodes.Add(ClassifyScalar(element, path, scalar, value, lines));
                return;
            case EnumTunableDescription enumDescription:
                {
                    var enumClass = enumDescription.StaticEntries is { } staticEntries ? descriptions.GetEnum(staticEntries) : null;
                    var memberName = value?.Split('.')[^1];
                    long? enumValue = memberName is not null && enumClass is not null ? enumClass.GetEnumValue(memberName) : null;
                    if (enumValue is null && value is not null && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
                        enumValue = numeric;
                    nodes.Add(CreateNode(element, path, description, value is null ? TuningValueKind.None : TuningValueKind.Enum, lines, value).With(enumClass: enumClass, enumValue: enumValue));
                    return;
                }
            case VariantTunableDescription variant:
                {
                    var option = element.Attribute("t")?.Value;
                    nodes.Add(CreateNode(element, path, description, TuningValueKind.None, lines).With(option: option));
                    foreach (var child in element.Elements())
                    {
                        var childName = child.Attribute("n")?.Value ?? option;
                        var childDescription = childName is null ? null : variant.GetOption(childName);
                        if (childDescription is null && childName is not null)
                            missing.Add($"{path}.{childName}");
                        Visit(child, childDescription, $"{path}.{childName}", descriptions, lines, nodes, missing);
                    }
                    return;
                }
            case ListTunableDescription list:
                {
                    nodes.Add(CreateNode(element, path, description, TuningValueKind.None, lines));
                    var index = 0;
                    foreach (var child in element.Elements())
                        Visit(child, list.Item, $"{path}[{index++}]", descriptions, lines, nodes, missing);
                    return;
                }
            case TupleTunableDescription or null or UnknownTunableDescription or ScalarTunableDescription:
                {
                    var node = CreateNode(element, path, description, value is null ? TuningValueKind.None : TuningValueKind.Scalar, lines, value);
                    nodes.Add(description is TupleTunableDescription tuple ? node.With(members: ResolveMembers(tuple.Members, descriptions)) : node);
                    var index = 0;
                    foreach (var child in element.Elements())
                    {
                        var childName = child.Attribute("n")?.Value;
                        var childDescription = childName is not null ? description?.GetMember(childName) : null;
                        if (description is not null && childDescription is null && childName is not null)
                            missing.Add($"{path}.{childName}");
                        Visit(child, childDescription, childName is null ? $"{path}[{index++}]" : $"{path}.{childName}", descriptions, lines, nodes, missing);
                    }
                    return;
                }
            default:
                nodes.Add(CreateNode(element, path, description, value is null ? TuningValueKind.None : TuningValueKind.Scalar, lines, value));
                return;
        }
    }

    static TuningNode ClassifyScalar(XElement element, string path, ScalarTunableDescription scalar, string value, string[] lines)
    {
        if (scalar.Class == "TunableReference" && scalar.Type is { } typeName)
        {
            var referenceType = GetReferenceResourceType(typeName);
            var node = CreateNode(element, path, scalar, TuningValueKind.Reference, lines, value);
            return ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var instance)
                ? node.With(referenceTypeName: typeName, referenceType: referenceType, referenceInstance: instance)
                : node.With(referenceTypeName: typeName, referenceType: referenceType, referenceName: value);
        }
        if (scalar.Class == "TunableLocalizedString" && TryParseStringKey(value, out var stringKey))
            return CreateNode(element, path, scalar, TuningValueKind.LocalizedString, lines, value).With(stringKey: stringKey);
        if (scalar.Type == "ResourceKey" && LlamaLogic.Packages.ResourceKey.TryParse(value, out var resourceKey))
            return CreateNode(element, path, scalar, TuningValueKind.ResourceKey, lines, value).With(resourceKey: resourceKey);
        return CreateNode(element, path, scalar, TuningValueKind.Scalar, lines, value);
    }

    static bool TryParseStringKey(string value, out uint key)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return uint.TryParse(value.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out key);
        return uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out key);
    }

    TuningAnalysis(XElement root, TuningDescriptionInstance? instance, TuningDescriptionModule? module, IReadOnlyList<TunableDescription> instanceMembers, IReadOnlyList<TuningNode> nodes, IReadOnlyList<string> unrecognizedPaths)
    {
        Root = root;
        Instance = instance;
        InstanceMembers = instanceMembers;
        Module = module;
        Nodes = nodes;
        UnrecognizedPaths = unrecognizedPaths;
    }

    /// <summary>
    /// Gets the instance description of the document, if it is instance tuning and its class was found
    /// </summary>
    public TuningDescriptionInstance? Instance { get; }

    /// <summary>
    /// Gets the tunables the document's class has, including those it inherits, in the order the game collects them, if it is instance tuning and its class was found; otherwise, an empty list
    /// </summary>
    public IReadOnlyList<TunableDescription> InstanceMembers { get; }

    /// <summary>
    /// Gets the module description of the document, if it is module tuning and its module was found
    /// </summary>
    public TuningDescriptionModule? Module { get; }

    /// <summary>
    /// Gets the elements of the document, in document order
    /// </summary>
    public IReadOnlyList<TuningNode> Nodes { get; }

    /// <summary>
    /// Gets the Python module the document's class is in (its <c>m</c> attribute), or the module of module tuning
    /// </summary>
    public string? PythonModule =>
        Root.Name.LocalName == "M" ? Root.Attribute("n")?.Value : Root.Attribute("m")?.Value;

    /// <summary>
    /// Gets the elements whose names the tuning descriptions do not recognize, by path (a large number suggests the descriptions are for another version of the game)
    /// </summary>
    public IReadOnlyList<string> UnrecognizedPaths { get; }

    internal XElement Root { get; }

    /// <summary>
    /// Gets the references in the document
    /// </summary>
    public IEnumerable<TuningNode> References =>
        Nodes.Where(node => node.ValueKind is TuningValueKind.Reference);
}