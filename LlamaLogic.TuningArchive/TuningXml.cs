namespace LlamaLogic.TuningArchive;

/// <summary>
/// Provides the canonical text form in which a tuning archive stores tuning and SimData documents
/// </summary>
/// <remarks>
/// The canonical form is UTF-8 without a byte order mark, an XML declaration, two-space indentation and line feeds, with comments removed.
/// The root element's attributes are in ordinal order (<c>c</c>, <c>i</c>, <c>m</c>, <c>n</c>, <c>s</c>), and every other element's are <c>n</c>, then <c>t</c>, then the rest in ordinal order.
/// Documents with the same content therefore have the same canonical text however they were obtained (from the game's combined tuning or from files extracted by another tool).
/// </remarks>
public static class TuningXml
{
    const string declaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";

    static readonly XmlWriterSettings writerSettings = new()
    {
        Indent = true,
        IndentChars = "  ",
        NewLineChars = "\n",
        NewLineHandling = NewLineHandling.Replace,
        OmitXmlDeclaration = true
    };

    static int GetAttributeRank(string name) =>
        name switch
        {
            "n" => 0,
            "t" => 1,
            _ => 2
        };

    static XElement Canonicalize(XElement element, bool isRoot)
    {
        var attributes = isRoot
            ? element.Attributes().OrderBy(attribute => attribute.Name.LocalName, StringComparer.Ordinal)
            : element.Attributes().OrderBy(attribute => GetAttributeRank(attribute.Name.LocalName)).ThenBy(attribute => attribute.Name.LocalName, StringComparer.Ordinal);
        var canonical = new XElement(element.Name, attributes.Select(attribute => new XAttribute(attribute.Name, attribute.Value)));
        if (element.HasElements)
            canonical.Add(element.Elements().Select(child => Canonicalize(child, false)));
        else if (string.Concat(element.Nodes().OfType<XText>().Select(text => text.Value)) is { Length: > 0 } value)
            canonical.Add(new XText(value));
        return canonical;
    }

    /// <summary>
    /// Gets the canonical text of the specified <paramref name="document"/>
    /// </summary>
    public static string GetCanonicalText(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Root is not { } root)
            throw new ArgumentException("The document has no root element", nameof(document));
        return GetCanonicalText(root);
    }

    /// <summary>
    /// Gets the canonical text of a document whose root is the specified <paramref name="root"/>
    /// </summary>
    public static string GetCanonicalText(XElement root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var builder = new StringBuilder(declaration);
        using (var stringWriter = new StringWriter(builder, CultureInfo.InvariantCulture))
        using (var xmlWriter = XmlWriter.Create(stringWriter, writerSettings))
            Canonicalize(root, true).WriteTo(xmlWriter);
        return builder.ToString();
    }

    static readonly XmlWriterSettings fragmentSettings = new()
    {
        Indent = false,
        OmitXmlDeclaration = true
    };

    /// <summary>
    /// Gets the canonical text of the specified <paramref name="element"/> on one line, without an XML declaration
    /// </summary>
    public static string GetFragmentText(XElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        var builder = new StringBuilder();
        using (var stringWriter = new StringWriter(builder, CultureInfo.InvariantCulture))
        using (var xmlWriter = XmlWriter.Create(stringWriter, fragmentSettings))
            Canonicalize(element, false).WriteTo(xmlWriter);
        return builder.ToString();
    }

    /// <summary>
    /// Gets whether the specified elements have the same canonical text on one line (what <see cref="GetFragmentText(XElement)"/> gets for each), without writing either
    /// </summary>
    public static bool HaveSameFragmentText(XElement a, XElement b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return HaveSameContent(a, b);
    }

    static bool HaveSameContent(XElement a, XElement b)
    {
        if (a.Name != b.Name)
            return false;
        var attributeCount = 0;
        foreach (var attribute in a.Attributes())
        {
            ++attributeCount;
            if (b.Attribute(attribute.Name)?.Value != attribute.Value)
                return false;
        }
        if (attributeCount != b.Attributes().Count())
            return false;
        if (a.HasElements || b.HasElements)
        {
            if (!a.HasElements || !b.HasElements)
                return false;
            using var aChildren = a.Elements().GetEnumerator();
            using var bChildren = b.Elements().GetEnumerator();
            while (true)
            {
                var aMoved = aChildren.MoveNext();
                if (aMoved != bChildren.MoveNext())
                    return false;
                if (!aMoved)
                    return true;
                if (!HaveSameContent(aChildren.Current, bChildren.Current))
                    return false;
            }
        }
        return NormalizeNewLines(a.Value) == NormalizeNewLines(b.Value);
    }

    static string NormalizeNewLines(string text) =>
        text.Contains('\r', StringComparison.Ordinal) ? text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n') : text;

    /// <summary>
    /// Finds the element at the specified <paramref name="path"/> (as <see cref="TuningNode.Path"/> spells it, such as <c>loot_list[2].stat</c>) below the specified <paramref name="root"/>, or returns <see langword="null"/> if there is none
    /// </summary>
    public static XElement? FindElement(XElement root, string path)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(path);
        var current = root;
        var position = 0;
        while (position < path.Length)
        {
            if (path[position] == '[')
            {
                var close = path.IndexOf(']', position);
                if (close < 0 || !int.TryParse(path.AsSpan(position + 1, close - position - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var index))
                    return null;
                current = current.Elements().ElementAtOrDefault(index);
                position = close + 1;
            }
            else
            {
                if (path[position] == '.')
                    ++position;
                var end = path.IndexOfAny(['.', '['], position);
                if (end < 0)
                    end = path.Length;
                var name = path[position..end];
                current = current.Elements().FirstOrDefault(child => child.Attribute("n")?.Value == name)
                    ?? (current.Name.LocalName == "V" && current.Attribute("t")?.Value == name ? current.Elements().FirstOrDefault(child => child.Attribute("n") is null) : null);
                position = end;
            }
            if (current is null)
                return null;
        }
        return current;
    }

    /// <summary>
    /// Gets the type of the tuning resource for the specified tuning type name (the <c>i</c> attribute of an instance, or the name of a group in combined tuning), the way the game derives it
    /// </summary>
    public static ResourceType GetResourceType(string typeName)
    {
        ArgumentNullException.ThrowIfNull(typeName);
        return typeName switch
        {
            "notebook_entry" => ResourceType.NotebookEntryTuning,
            "relbit" => ResourceType.RelationshipBitTuning,
            "tun" or "module" => ResourceType.Tuning,
            _ => (ResourceType)Fnv.Hash32(typeName)
        };
    }

    /// <summary>
    /// Parses canonical text (or any tuning XML) into a document with line information
    /// </summary>
    public static XDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return XDocument.Parse(text, LoadOptions.SetLineInfo);
    }
}