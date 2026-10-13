namespace LlamaLogic.Packages.UnitTests;

static class PackedXmlBuilder
{
    const int nullOffset = int.MinValue;

    sealed class Node(XNode source)
    {
        public List<(int name, int value)> Attributes { get; } = [];
        public List<Node> Children { get; } = [];
        public XNode Source { get; } = source;
        public int Text { get; set; }
    }

    public static byte[] Build(XElement root, bool rootContainsItself = false)
    {
        var strings = new List<string>();
        var stringIndices = new Dictionary<string, int>();
        int intern(string value)
        {
            if (!stringIndices.TryGetValue(value, out var index))
            {
                index = strings.Count;
                strings.Add(value);
                stringIndices.Add(value, index);
            }
            return index;
        }
        var textNodes = new List<Node>();
        var elementNodes = new List<Node>();
        Node add(XNode source)
        {
            var node = new Node(source);
            if (source is XElement element)
            {
                elementNodes.Add(node);
                node.Text = intern(element.Name.LocalName);
                foreach (var attribute in element.Attributes())
                    node.Attributes.Add((intern(attribute.Name.LocalName), intern(attribute.Value)));
                foreach (var child in element.Nodes())
                    node.Children.Add(add(child));
            }
            else
            {
                textNodes.Add(node);
                node.Text = intern(((XText)source).Value);
            }
            return node;
        }
        var rootNode = add(root);
        if (rootContainsItself)
            rootNode.Children.Insert(0, rootNode);
        var nodes = textNodes.Concat(elementNodes).ToList();
        var nodeIndices = nodes.Select((node, index) => (node, index)).ToDictionary(t => t.node, t => t.index);
        var nodeReferences = new List<int>();
        var nodeChildrenStarts = new Dictionary<Node, int>();
        var attributes = new List<(int name, int value)>();
        var attributeReferences = new List<int>();
        var nodeAttributesStarts = new Dictionary<Node, int>();
        foreach (var node in nodes)
        {
            if (node.Children.Count > 0)
            {
                nodeChildrenStarts.Add(node, nodeReferences.Count);
                nodeReferences.AddRange(node.Children.Select(child => nodeIndices[child]));
                nodeReferences.Add(-1);
            }
            if (node.Attributes.Count > 0)
            {
                nodeAttributesStarts.Add(node, attributeReferences.Count);
                foreach (var attribute in node.Attributes)
                {
                    attributeReferences.Add(attributes.Count);
                    attributes.Add(attribute);
                }
                attributeReferences.Add(-1);
            }
        }
        var characters = new List<byte>();
        var stringStarts = new List<int>();
        foreach (var value in strings)
        {
            stringStarts.Add(characters.Count);
            characters.AddRange(Encoding.UTF8.GetBytes(value));
            characters.Add(0);
        }
        static int align(int position) =>
            (position + 15) & ~15;
        var tableInfoPosition = 32;
        var documentPosition = align(tableInfoPosition + (7 * 28));
        var nodesPosition = align(documentPosition + 20);
        var attributesPosition = align(nodesPosition + (nodes.Count * 12));
        var nodeReferencesPosition = align(attributesPosition + (attributes.Count * 8));
        var attributeReferencesPosition = align(nodeReferencesPosition + (nodeReferences.Count * 4));
        var stringReferencesPosition = align(attributeReferencesPosition + (attributeReferences.Count * 4));
        var charactersPosition = align(stringReferencesPosition + (strings.Count * 4));
        var schemaPosition = align(charactersPosition + characters.Count);
        (string name, (string name, SimDataType type, int offset)[] columns)[] schemas =
        [
            ("PackedXmlDocument", [("element_count", SimDataType.UINT32, 8), ("first_element", SimDataType.OBJECT, 0), ("top_element", SimDataType.OBJECT, 4), ("string_table", SimDataType.VECTOR, 12)]),
            ("PackedXmlNode", [("children", SimDataType.OBJECT, 8), ("text", SimDataType.UINT32, 0), ("attrs", SimDataType.OBJECT, 4)]),
            ("PackedXmlAttribute", [("value", SimDataType.UINT32, 4), ("name", SimDataType.UINT32, 0)])
        ];
        var columnPosition = schemaPosition + (schemas.Length * 24);
        var namesPosition = columnPosition + (schemas.Sum(schema => schema.columns.Length) * 20);
        var data = new byte[namesPosition + schemas.Sum(schema => Encoding.ASCII.GetByteCount(schema.name) + 1 + schema.columns.Sum(column => Encoding.ASCII.GetByteCount(column.name) + 1)) + Encoding.ASCII.GetByteCount("documents") + 1];
        void writeInt32(int position, int value) =>
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(position), value);
        void writeOffset(int position, int target) =>
            writeInt32(position, target < 0 ? nullOffset : target - position);
        var namePosition = namesPosition;
        int writeName(string name)
        {
            var position = namePosition;
            namePosition += Encoding.ASCII.GetBytes(name, data.AsSpan(namePosition)) + 1;
            return position;
        }
        "DATA"u8.CopyTo(data);
        writeInt32(4, 0x101);
        writeOffset(8, tableInfoPosition);
        writeInt32(12, 7);
        writeOffset(16, schemaPosition);
        writeInt32(20, schemas.Length);
        var schemaPositions = new int[schemas.Length];
        for (var s = 0; s < schemas.Length; ++s)
        {
            var position = schemaPosition + (s * 24);
            schemaPositions[s] = position;
            var (schemaName, columns) = schemas[s];
            writeOffset(position, writeName(schemaName));
            writeInt32(position + 4, (int)Fnv32.GetHash(schemaName));
            writeInt32(position + 8, s + 1);
            writeInt32(position + 12, s switch { 0 => 20, 1 => 12, _ => 8 });
            writeOffset(position + 16, columnPosition);
            writeInt32(position + 20, columns.Length);
            foreach (var (columnName, columnType, columnOffset) in columns)
            {
                writeOffset(columnPosition, writeName(columnName));
                writeInt32(columnPosition + 4, (int)Fnv32.GetHash(columnName));
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(columnPosition + 8), (ushort)columnType);
                writeInt32(columnPosition + 12, columnOffset);
                writeInt32(columnPosition + 16, nullOffset);
                columnPosition += 20;
            }
        }
        (int schema, SimDataType type, int rowSize, int position, int count)[] tables =
        [
            (0, SimDataType.OBJECT, 20, documentPosition, 1),
            (1, SimDataType.OBJECT, 12, nodesPosition, nodes.Count),
            (2, SimDataType.OBJECT, 8, attributesPosition, attributes.Count),
            (-1, SimDataType.OBJECT, 4, nodeReferencesPosition, nodeReferences.Count),
            (-1, SimDataType.OBJECT, 4, attributeReferencesPosition, attributeReferences.Count),
            (-1, SimDataType.STRING8, 4, stringReferencesPosition, strings.Count),
            (-1, SimDataType.CHAR8, 1, charactersPosition, characters.Count)
        ];
        for (var t = 0; t < tables.Length; ++t)
        {
            var position = tableInfoPosition + (t * 28);
            var (schema, type, rowSize, rowPosition, count) = tables[t];
            writeOffset(position, t is 0 ? writeName("documents") : -1);
            writeOffset(position + 8, schema < 0 ? -1 : schemaPositions[schema]);
            writeInt32(position + 12, (int)type);
            writeInt32(position + 16, rowSize);
            writeOffset(position + 20, rowPosition);
            writeInt32(position + 24, count);
        }
        var nodePosition = (Node node) => nodesPosition + (nodeIndices[node] * 12);
        writeOffset(documentPosition, textNodes.Count < nodes.Count ? nodesPosition + (textNodes.Count * 12) : -1);
        writeOffset(documentPosition + 4, nodePosition(rootNode));
        writeInt32(documentPosition + 8, elementNodes.Count);
        writeOffset(documentPosition + 12, stringReferencesPosition);
        writeInt32(documentPosition + 16, strings.Count);
        foreach (var node in nodes)
        {
            var position = nodePosition(node);
            writeInt32(position, node.Text);
            writeOffset(position + 4, nodeAttributesStarts.TryGetValue(node, out var attributesStart) ? attributeReferencesPosition + (attributesStart * 4) : -1);
            writeOffset(position + 8, nodeChildrenStarts.TryGetValue(node, out var childrenStart) ? nodeReferencesPosition + (childrenStart * 4) : -1);
        }
        for (var a = 0; a < attributes.Count; ++a)
        {
            writeInt32(attributesPosition + (a * 8), attributes[a].name);
            writeInt32(attributesPosition + (a * 8) + 4, attributes[a].value);
        }
        for (var r = 0; r < nodeReferences.Count; ++r)
            writeOffset(nodeReferencesPosition + (r * 4), nodeReferences[r] < 0 ? -1 : nodesPosition + (nodeReferences[r] * 12));
        for (var r = 0; r < attributeReferences.Count; ++r)
            writeOffset(attributeReferencesPosition + (r * 4), attributeReferences[r] < 0 ? -1 : attributesPosition + (attributeReferences[r] * 8));
        for (var s = 0; s < strings.Count; ++s)
            writeOffset(stringReferencesPosition + (s * 4), charactersPosition + stringStarts[s]);
        characters.CopyTo(data, charactersPosition);
        return data;
    }
}
