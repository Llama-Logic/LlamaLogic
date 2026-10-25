namespace LlamaLogic.Packages.Formats;

/// <summary>
/// Reads Maxis' binary packed XML documents (<c>DATA</c> files) and writes XML text the way Maxis writes it
/// </summary>
static class PackedXml
{
    const int nullOffset = BinaryUtilities.NullOffset;

    readonly record struct BinaryTable(int SchemaPosition, uint DataType, int RowSize, int RowPosition, int RowCount)
    {
        public bool Contains(long position) =>
            position >= RowPosition
            && position < RowPosition + (long)RowSize * RowCount
            && (position - RowPosition) % RowSize == 0;
    }

    record struct BinaryNodeFrame(XElement Element, int NodePosition, int NextChildReferencePosition);

    public static bool IsBinary(ReadOnlySpan<byte> data) =>
        data.StartsWith("DATA"u8);

    public static ReadOnlyMemory<byte> Encode(XElement root)
    {
        var builder = new StringBuilder();
        WriteNode(builder, root);
        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    public static XDocument DecodeBinary(ReadOnlySpan<byte> data)
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
        var stringReferenceTables = tables.Where(table => table.SchemaPosition < 0 && table.DataType is (uint)SimDataType.STRING8).ToList();
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
                throw new InvalidOperationException($"{node.NodeType} nodes cannot be encoded as Maxis XML");
        }
    }
}
