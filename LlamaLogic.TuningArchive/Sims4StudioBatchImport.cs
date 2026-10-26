namespace LlamaLogic.TuningArchive;

/// <summary>
/// Names files the way Sims 4 Studio's batch import reads them
/// </summary>
/// <remarks>
/// Batch import reads a resource's key from the start of its file name, up to the first period: the type, group and instance in hexadecimal, separated by exclamation points.
/// Sims 4 Studio's batch export follows the key with the resource's name and type, for people, as in <c>6017E896!00000000!0000000000012345.buff_Example.BuffTuning.xml</c>, and so do these names.
/// Characters Windows doesn't allow in file names become underscores, as they do in Sims 4 Studio's export.
/// </remarks>
public static class Sims4StudioBatchImport
{
    static readonly IReadOnlySet<char> invalidFileNameCharacters = new HashSet<char>("\"<>|:*?\\/".Concat(Enumerable.Range(0, 32).Select(code => (char)code)));

    /// <summary>
    /// Gets the name of a file of XML for the resource with the specified <paramref name="key"/> and <paramref name="name"/>
    /// </summary>
    public static string GetFileName(ResourceKey key, string? name)
    {
        var builder = new StringBuilder(string.Format(CultureInfo.InvariantCulture, "{0:X8}!{1:X8}!{2:X16}", (uint)key.Type, key.Group, key.FullInstance));
        if (!string.IsNullOrWhiteSpace(name))
            builder.Append('.').Append(name.Trim());
        if (Enum.IsDefined(key.Type))
            builder.Append('.').Append(key.Type.ToString());
        builder.Append(".xml");
        for (var index = 0; index < builder.Length; ++index)
            if (invalidFileNameCharacters.Contains(builder[index]))
                builder[index] = '_';
        return builder.ToString();
    }

    /// <summary>
    /// Gets the name of a file of the specified tuning document <paramref name="text"/>, whose key comes from its root element: the type from its <c>i</c> attribute, the instance from its <c>s</c> attribute, and the group 0
    /// </summary>
    /// <exception cref="ArgumentException">The root element does not have both attributes, or its instance is not a number</exception>
    public static string GetFileName(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var root = TuningXml.Parse(text).Root;
        if (root?.Attribute("i")?.Value is not { } typeName || !ulong.TryParse(root.Attribute("s")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var instance))
            throw new ArgumentException("The tuning's root element must have an i attribute and a numeric s attribute", nameof(text));
        return GetFileName(new ResourceKey(TuningXml.GetResourceType(typeName), 0, instance), root.Attribute("n")?.Value);
    }
}