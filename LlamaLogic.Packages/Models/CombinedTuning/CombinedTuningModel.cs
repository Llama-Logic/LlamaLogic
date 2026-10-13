namespace LlamaLogic.Packages.Models.CombinedTuning;

/// <summary>
/// Represents a <see cref="ResourceType.CombinedTuning"/> resource (🔓)
/// </summary>
/// <remarks>
/// Maxis ships the game's tuning merged into resources of this type, encoded either as a binary packed XML document (a <c>DATA</c> file) or as XML text.
/// <see cref="Decode(ReadOnlyMemory{byte})"/> accepts both and presents the merged document as <see cref="Document"/>.
/// <see cref="Encode"/> always produces XML text, written the way Maxis writes it.
/// <see cref="GetTunings"/> produces the individual tuning documents the merged document contains.
/// </remarks>
public sealed class CombinedTuningModel :
    Model,
    IModel<CombinedTuningModel>
{
    const int nullOffset = BinaryUtilities.NullOffset;
    static readonly ImmutableHashSet<ResourceType> supportedTypes =
    [
        ResourceType.CombinedTuning
    ];

    readonly record struct BinaryTable(int SchemaPosition, uint DataType, int RowSize, int RowPosition, int RowCount)
    {
        public bool Contains(long position) =>
            position >= RowPosition
            && position < RowPosition + (long)RowSize * RowCount
            && (position - RowPosition) % RowSize == 0;
    }

    record struct BinaryNodeFrame(XElement Element, int NodePosition, int NextChildReferencePosition);

    /// <inheritdoc/>
    public static new ISet<ResourceType> SupportedTypes =>
        supportedTypes;

    /// <inheritdoc/>
    /// <exception cref="InvalidDataException">The data is neither a well-formed binary nor a well-formed XML combined tuning document</exception>
    public static CombinedTuningModel Decode(ReadOnlyMemory<byte> data)
    {
        if (data.Span.StartsWith("DATA"u8))
            return new CombinedTuningModel(DecodeBinary(data.Span));
        XDocument document;
        try
        {
            using var stream = new ReadOnlyMemoryOfByteStream(data);
            document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException("The data is neither a binary nor an XML combined tuning document", ex);
        }
        if (document.Root is not { Name.LocalName: "combined" })
            throw new InvalidDataException($"The root element of an XML combined tuning document must be <combined>, not <{document.Root?.Name}>");
        return new CombinedTuningModel(document);
    }

    static XDocument DecodeBinary(ReadOnlySpan<byte> data)
    {
        var version = ReadUInt32(data, 4);
        if (version is < 0x100 or > 0x101)
            throw new InvalidDataException($"Version {version:x} is not supported");
        var tableHeaderPosition = ReadRequiredOffset(data, 8);
        var tableCount = ReadCount(data, 12, 28);
        var schemaPosition = ReadRequiredOffset(data, 16);
        var schemaCount = ReadCount(data, 20, 24);
        var schemaNames = new Dictionary<int, string?>();
        var columnOffsets = new Dictionary<(int schemaPosition, string columnName), int>();
        for (var s = 0; s < schemaCount; ++s)
        {
            var position = checked(schemaPosition + s * 24);
            schemaNames[position] = ReadOptionalString(data, position);
            var columnPosition = ReadRequiredOffset(data, position + 16);
            var columnCount = ReadCount(data, position + 20, 20);
            for (var c = 0; c < columnCount; ++c)
            {
                var columnInfoPosition = checked(columnPosition + c * 20);
                if (ReadOptionalString(data, columnInfoPosition) is { } columnName)
                    columnOffsets[(position, columnName)] = (int)ReadUInt32(data, columnInfoPosition + 12);
            }
        }
        var tables = new List<BinaryTable>(tableCount);
        for (var t = 0; t < tableCount; ++t)
        {
            var position = checked(tableHeaderPosition + t * 28);
            var rowSize = (int)Math.Min(ReadUInt32(data, position + 16), int.MaxValue);
            var rowPosition = ReadRequiredOffset(data, position + 20);
            var rowCount = (int)Math.Min(ReadUInt32(data, position + 24), int.MaxValue);
            if (rowSize <= 0 || rowPosition + (long)rowSize * rowCount > data.Length)
                throw new InvalidDataException($"Table {t} does not fit in the data");
            tables.Add(new(ReadOffset(data, position + 8), ReadUInt32(data, position + 12), rowSize, rowPosition, rowCount));
        }
        BinaryTable getSchemaTable(string schemaName)
        {
            var matches = tables.Where(table => table.SchemaPosition >= 0 && schemaNames.TryGetValue(table.SchemaPosition, out var name) && name == schemaName).ToList();
            if (matches.Count != 1)
                throw new InvalidDataException($"Expected exactly one table of {schemaName}, found {matches.Count}");
            return matches[0];
        }
        int getColumnOffset(BinaryTable table, string columnName) =>
            columnOffsets.TryGetValue((table.SchemaPosition, columnName), out var offset)
            ? offset
            : throw new InvalidDataException($"The schema of {schemaNames[table.SchemaPosition]} has no {columnName} column");
        var documentTable = getSchemaTable("PackedXmlDocument");
        var nodeTable = getSchemaTable("PackedXmlNode");
        var attributeTable = getSchemaTable("PackedXmlAttribute");
        var stringReferenceTables = tables.Where(table => table.SchemaPosition < 0 && table.DataType is (uint)DataModelType.STRING8).ToList();
        if (stringReferenceTables.Count != 1 || documentTable.RowCount < 1)
            throw new InvalidDataException("The data is not a packed XML document");
        var stringReferenceTable = stringReferenceTables[0];
        var nodeText = getColumnOffset(nodeTable, "text");
        var nodeAttributes = getColumnOffset(nodeTable, "attrs");
        var nodeChildren = getColumnOffset(nodeTable, "children");
        var attributeName = getColumnOffset(attributeTable, "name");
        var attributeValue = getColumnOffset(attributeTable, "value");
        var firstElement = ReadRequiredOffset(data, documentTable.RowPosition + getColumnOffset(documentTable, "first_element"));
        var topElement = ReadRequiredOffset(data, documentTable.RowPosition + getColumnOffset(documentTable, "top_element"));
        var strings = new string?[stringReferenceTable.RowCount];
        string getString(ReadOnlySpan<byte> data, uint index)
        {
            if (index >= strings.Length)
                throw new InvalidDataException($"String index {index} is out of range");
            return strings[index] ??= RequireXmlCharacters(ReadOptionalString(data, stringReferenceTable.RowPosition + (int)index * 4)
                ?? throw new InvalidDataException($"String {index} is null"));
        }
        int requireNode(int position) =>
            nodeTable.Contains(position)
            ? position
            : throw new InvalidDataException($"Position {position} is not a node");
        XNode createNode(ReadOnlySpan<byte> data, int position, out int childReferencePosition)
        {
            var text = getString(data, ReadUInt32(data, position + nodeText));
            var attributeReferencePosition = ReadOffset(data, position + nodeAttributes);
            childReferencePosition = ReadOffset(data, position + nodeChildren);
            if (attributeReferencePosition < 0 && childReferencePosition < 0)
                return position >= firstElement
                    ? new XElement(GetXmlName(text))
                    : new XText(text);
            var element = new XElement(GetXmlName(text));
            if (attributeReferencePosition >= 0)
                for (var referencePosition = attributeReferencePosition; ReadOffset(data, referencePosition) is var attribute and >= 0; referencePosition = checked(referencePosition + 4))
                {
                    if (!attributeTable.Contains(attribute))
                        throw new InvalidDataException($"Position {attribute} is not an attribute");
                    element.SetAttributeValue(GetXmlName(getString(data, ReadUInt32(data, attribute + attributeName))), getString(data, ReadUInt32(data, attribute + attributeValue)));
                }
            return element;
        }
        if (createNode(data, requireNode(topElement), out var topChildReferencePosition) is not XElement root)
            throw new InvalidDataException("The top node is not an element");
        var nodesBeingRead = new HashSet<int> { topElement };
        var frames = new List<BinaryNodeFrame> { new(root, topElement, topChildReferencePosition) };
        while (frames.Count > 0)
        {
            var frame = frames[^1];
            var child = frame.NextChildReferencePosition < 0 ? -1 : ReadOffset(data, frame.NextChildReferencePosition);
            if (child < 0)
            {
                nodesBeingRead.Remove(frame.NodePosition);
                frames.RemoveAt(frames.Count - 1);
                continue;
            }
            frames[^1] = frame with { NextChildReferencePosition = checked(frame.NextChildReferencePosition + 4) };
            if (!nodesBeingRead.Add(requireNode(child)))
                throw new InvalidDataException($"The node at position {child} contains itself");
            var childNode = createNode(data, child, out var childReferencePosition);
            frame.Element.Add(childNode);
            if (childReferencePosition >= 0 && childNode is XElement childElement)
                frames.Add(new(childElement, child, childReferencePosition));
            else
                nodesBeingRead.Remove(child);
        }
        return new XDocument(root);
    }

    static XName GetXmlName(string name)
    {
        try
        {
            return XNamespace.None.GetName(name);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException($"\"{name}\" is not a valid XML name", ex);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException($"\"{name}\" is not a valid XML name", ex);
        }
    }

    /// <inheritdoc/>
    public static new string? GetName(Stream stream) =>
        null;

    /// <inheritdoc/>
    public static new Task<string?> GetNameAsync(Stream stream, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    static int ReadCount(ReadOnlySpan<byte> data, int position, int entrySize)
    {
        var count = ReadUInt32(data, position);
        if (count > (uint)(data.Length / entrySize))
            throw new InvalidDataException($"The count at position {position} exceeds the data");
        return (int)count;
    }

    static int ReadOffset(ReadOnlySpan<byte> data, int position)
    {
        var offset = (int)ReadUInt32(data, position);
        if (offset is nullOffset)
            return -1;
        var target = (long)position + offset;
        if (target < 0 || target >= data.Length)
            throw new InvalidDataException($"The offset at position {position} points outside the data");
        return (int)target;
    }

    static string? ReadOptionalString(ReadOnlySpan<byte> data, int position)
    {
        var stringPosition = ReadOffset(data, position);
        if (stringPosition < 0)
            return null;
        var length = data[stringPosition..].IndexOf((byte)0);
        if (length < 0)
            throw new InvalidDataException($"The string at position {stringPosition} is not terminated");
        return Encoding.UTF8.GetString(data.Slice(stringPosition, length));
    }

    static string RequireXmlCharacters(string value)
    {
        try
        {
            return XmlConvert.VerifyXmlChars(value);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException("A string contains characters XML cannot express", ex);
        }
    }

    static int ReadRequiredOffset(ReadOnlySpan<byte> data, int position) =>
        ReadOffset(data, position) is var target and >= 0
        ? target
        : throw new InvalidDataException($"The offset at position {position} is null");

    static uint ReadUInt32(ReadOnlySpan<byte> data, int position) =>
        position >= 0 && position <= data.Length - sizeof(uint)
        ? BinaryPrimitives.ReadUInt32LittleEndian(data[position..])
        : throw new InvalidDataException($"Position {position} is outside the data");

    static void WriteEscaped(StringBuilder builder, string value, bool isAttribute)
    {
        for (var i = 0; i < value.Length; ++i)
        {
            var c = value[i];
            switch (c)
            {
                case '&':
                    builder.Append("&amp;");
                    break;
                case '<':
                    builder.Append("&lt;");
                    break;
                case '>':
                    builder.Append("&gt;");
                    break;
                case '"' when isAttribute:
                    builder.Append("&quot;");
                    break;
                case '\'' when isAttribute:
                    builder.Append("&apos;");
                    break;
                case < (char)0x80:
                    builder.Append(c);
                    break;
                default:
                    var codePoint = char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1])
                        ? char.ConvertToUtf32(c, value[++i])
                        : c;
                    builder.Append("&#").Append(codePoint.ToString(CultureInfo.InvariantCulture)).Append(';');
                    break;
            }
        }
    }

    static void WriteNode(StringBuilder builder, XNode node)
    {
        switch (node)
        {
            case XElement element:
                builder.Append('<').Append(element.Name.LocalName);
                foreach (var attribute in element.Attributes())
                {
                    builder.Append(' ').Append(attribute.Name.LocalName).Append("=\"");
                    WriteEscaped(builder, attribute.Value, true);
                    builder.Append('"');
                }
                if (element.IsEmpty)
                {
                    builder.Append(" />");
                    break;
                }
                builder.Append('>');
                foreach (var child in element.Nodes())
                    WriteNode(builder, child);
                builder.Append("</").Append(element.Name.LocalName).Append('>');
                break;
            case XText text:
                WriteEscaped(builder, text.Value, false);
                break;
            case XComment comment:
                builder.Append("<!--").Append(comment.Value).Append("-->");
                break;
            default:
                throw new InvalidOperationException($"{node.NodeType} nodes cannot be encoded in combined tuning");
        }
    }

    /// <summary>
    /// Initializes a new <see cref="CombinedTuningModel"/> for the specified <paramref name="document"/>
    /// </summary>
    /// <param name="document">The merged document, the root element of which is <c>&lt;combined&gt;</c></param>
    public CombinedTuningModel(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Document = document;
    }

    /// <summary>
    /// Gets the merged document, the root element of which is <c>&lt;combined&gt;</c>
    /// </summary>
    public XDocument Document { get; }

    /// <inheritdoc/>
    public override string? ResourceName =>
        null;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException"><see cref="Document"/> has no root element or contains a node that XML combined tuning cannot express</exception>
    public override ReadOnlyMemory<byte> Encode()
    {
        if (Document.Root is not { } root)
            throw new InvalidOperationException("The document has no root element");
        var builder = new StringBuilder();
        WriteNode(builder, root);
        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    /// <summary>
    /// Gets the individual tuning documents in <see cref="Document"/>, in document order, with each reference to a shared node (<c>&lt;r x="…"&gt;</c>) replaced by a copy of that node (💤)
    /// </summary>
    /// <remarks>
    /// The documents are produced as the sequence is enumerated, and are independent of <see cref="Document"/> and of each other.
    /// Modifying <see cref="Document"/> while enumerating has undefined results.
    /// </remarks>
    /// <exception cref="InvalidDataException"><see cref="Document"/> is not a well-formed merged document: it lacks the shared node group, a reference names a node that does not exist or contains itself, or a tuning document lacks a valid instance</exception>
    public IEnumerable<CombinedTuningModelTuning> GetTunings()
    {
        var root = Document.Root;
        if (root is not { Name.LocalName: "combined" })
            throw new InvalidDataException("The root element of a combined tuning document must be <combined>");
        var sharedNodeGroup = root.Element("g")
            ?? throw new InvalidDataException("The combined tuning document has no shared node group (<g>)");
        var sharedNodes = new Dictionary<string, XElement>();
        foreach (var sharedNode in sharedNodeGroup.Elements())
            if (sharedNode.Attribute("x")?.Value is { } x)
                sharedNodes[x] = sharedNode;
        var resolvedSharedNodes = new Dictionary<string, XElement>();
        var sharedNodesBeingResolved = new HashSet<string>();
        XElement resolve(XElement element)
        {
            var resolved = new XElement(element.Name, element.Attributes().Where(attribute => attribute.Name.LocalName is not "x"));
            foreach (var node in element.Nodes())
            {
                if (node is not XElement childElement)
                {
                    resolved.Add(node);
                    continue;
                }
                if (childElement.Name.LocalName is not "r")
                {
                    resolved.Add(resolve(childElement));
                    continue;
                }
                var x = childElement.Attribute("x")?.Value
                    ?? throw new InvalidDataException("A reference (<r>) has no x attribute");
                if (!resolvedSharedNodes.TryGetValue(x, out var resolvedSharedNode))
                {
                    if (!sharedNodes.TryGetValue(x, out var sharedNode))
                        throw new InvalidDataException($"A reference (<r>) names shared node {x}, which does not exist");
                    if (!sharedNodesBeingResolved.Add(x))
                        throw new InvalidDataException($"Shared node {x} contains itself");
                    resolvedSharedNode = resolve(sharedNode);
                    sharedNodesBeingResolved.Remove(x);
                    resolvedSharedNodes.Add(x, resolvedSharedNode);
                }
                var copy = new XElement(resolvedSharedNode);
                if (childElement.Attribute("n") is { } name)
                    copy.ReplaceAttributes(new[] { name }.Concat(copy.Attributes().Where(attribute => attribute.Name.LocalName is not "n")).Select(attribute => new XAttribute(attribute)).ToList());
                resolved.Add(copy);
            }
            return resolved;
        }
        foreach (var group in root.Elements("R"))
        {
            var typeName = group.Attribute("n")?.Value
                ?? throw new InvalidDataException("A resource type group (<R>) has no n attribute");
            foreach (var tuning in group.Elements())
            {
                if (!ulong.TryParse(tuning.Attribute("s")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var instance))
                    throw new InvalidDataException($"A tuning document in {typeName} has no valid instance (s attribute)");
                yield return new CombinedTuningModelTuning(typeName, instance, tuning.Attribute("n")?.Value, new XDocument(resolve(tuning)));
            }
        }
    }
}
