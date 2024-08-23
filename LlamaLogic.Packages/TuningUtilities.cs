namespace LlamaLogic.Packages;

static class TuningUtilities
{
    public const LoadOptions XDocumentLoadOptions = LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo;
    public static readonly XmlWriterSettings XmlWriterSettings = new()
    {
        Indent = true,
        OmitXmlDeclaration = false,
        Encoding = Encoding.UTF8
    };

    public static (string? name, bool isList) ReadTunableDetails(this XmlReader reader)
    {
        if (reader.NodeType is not XmlNodeType.Element)
            throw new XmlException("Expected a tunable element");
        return(reader.GetAttribute("n"), reader.Name == "L");
    }

    public static IEnumerable<string> ReadTunableList(this XmlReader reader)
    {
        if (reader.NodeType is not XmlNodeType.Element || reader.Name != "L")
            throw new XmlException("Expected a tunable list element");
        while (reader.Read() && reader.NodeType is not XmlNodeType.EndElement)
        {
            if (reader.NodeType is XmlNodeType.Element && reader.Name == "T")
                yield return reader.ReadElementContentAsString();
        }
        reader.ReadEndElement();
    }

    public static IEnumerable<TTuple> ReadTunableTupleList<TTuple>(this XmlReader reader)
        where TTuple : IXmlSerializable, new()
    {
        if (reader.NodeType is not XmlNodeType.Element || reader.Name != "L")
            throw new XmlException("Expected a tunable list element");
        while (reader.Read() && reader.NodeType is not XmlNodeType.EndElement)
            if (reader.NodeType is XmlNodeType.Element && reader.Name == "U")
            {
                var tuple = new TTuple();
                tuple.ReadXml(reader);
                yield return tuple;
            }
        reader.ReadEndElement();
    }

    public static byte[] ToByteArray(this string hex)
    {
        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have an even number of characters");
        return Enumerable
            .Range(0, hex.Length / 2)
            .Select(byteIndex => hex.Substring(byteIndex * 2, 2))
            .Select(byteHex => byte.Parse(byteHex, NumberStyles.HexNumber))
            .ToArray();
    }

    public static string ToHexString(this byte[] bytes) =>
        string.Join(string.Empty, bytes.Select(b => b.ToString("x2")));

    public static void WriteTunable(this XmlWriter writer, string? name, object? value)
    {
        var valueStr = value is byte[] hash
            ? string.Join(string.Empty, hash.Select(b => b.ToString("x2")))
            : value?.ToString();
        if (!string.IsNullOrWhiteSpace(valueStr))
        {
            writer.WriteStartElement("T");
            if (!string.IsNullOrWhiteSpace(name))
                writer.WriteAttributeString("n", name);
            writer.WriteString(valueStr);
            writer.WriteEndElement();
        }
    }

    public static void WriteTunableList(this XmlWriter writer, string name, IEnumerable<object?> enumerable)
    {
        if (enumerable.Any())
        {
            writer.WriteStartElement("L");
            if (!string.IsNullOrWhiteSpace(name))
                writer.WriteAttributeString("n", name);
            if (enumerable is IEnumerable<IXmlSerializable> tupleEnumerable)
                foreach (var tuple in tupleEnumerable)
                {
                    if (tuple is { } nonNullTuple)
                        nonNullTuple.WriteXml(writer);
                }
            else
                foreach (var tunable in enumerable)
                    WriteTunable(writer, null, tunable);
            writer.WriteEndElement();
        }
    }
}
