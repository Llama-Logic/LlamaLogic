namespace LlamaLogic.TuningArchive;

/// <summary>
/// Adds to tuning the comments XML Extractor adds, as a snapshot knows them: the text of each string after its key, and the class and name of the tuning or animation state machine each reference refers to
/// </summary>
/// <remarks>
/// The comments are the ones <see cref="TuningArchiveFile.ExportAsync"/> writes: found with the snapshot's tuning descriptions when it has them and they describe the tuning, and otherwise as XML Extractor finds them, as any number that is a string's key or a tuning's instance.
/// References to tuning of packs that were not installed when the snapshot was captured are commented with the names the packs' patches give it (see <see cref="TuningArchiveFile.GetUninstalledTunings"/>).
/// The game ignores comments, so tuning with them loads as it would without them.
/// </remarks>
public sealed class TuningCommenter
{
    const string declaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";

    TuningCommenter(TuningArchiveFile.CommentTables tables, bool stringComments, bool referenceComments)
    {
        this.tables = tables;
        StringComments = stringComments;
        ReferenceComments = referenceComments;
    }

    readonly TuningArchiveFile.CommentTables tables;

    /// <summary>
    /// Gets whether references are commented with the class and name of what they refer to
    /// </summary>
    public bool ReferenceComments { get; }

    /// <summary>
    /// Gets whether string keys are commented with the text of their strings
    /// </summary>
    public bool StringComments { get; }

    /// <summary>
    /// Creates a commenter with what the specified <paramref name="snapshot"/> of the specified <paramref name="archive"/> knows
    /// </summary>
    /// <param name="archive">The archive</param>
    /// <param name="snapshot">The snapshot whose strings, tuning names and tuning descriptions the comments come from</param>
    /// <param name="stringComments">Whether to comment string keys with the text of their strings</param>
    /// <param name="referenceComments">Whether to comment references with the class and name of what they refer to</param>
    public static TuningCommenter Create(TuningArchiveFile archive, TuningArchiveSnapshot snapshot, bool stringComments = true, bool referenceComments = true)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(snapshot);
        var tunings = archive.GetEntries(snapshot, TuningArchiveResourceKind.Tuning);
        var animationStateMachines = archive.GetEntries(snapshot, TuningArchiveResourceKind.AnimationStateMachine);
        var names = new Dictionary<ulong, (string name, string? @class)>();
        foreach (var entry in tunings)
            if (entry.Name is { } name)
                names.TryAdd(entry.Instance, (name, entry.Class ?? (entry.Type is ResourceType.Tuning ? null : string.Empty)));
        foreach (var entry in animationStateMachines)
            if (entry.Name is { } name)
                names.TryAdd(entry.Instance, (name, TuningArchiveFile.AnimationStateMachineCommentClass));
        var uninstalledTunings = TuningArchiveFile.AddUninstalledTuningNames(archive.GetUninstalledTunings(snapshot), names);
        var tuningsByKey = new Dictionary<(ResourceType type, ulong instance), TuningArchiveEntry>();
        var tuningsByName = new Dictionary<(ResourceType type, string name), TuningArchiveEntry>();
        foreach (var entry in tunings)
        {
            tuningsByKey.TryAdd((entry.Type, entry.Instance), entry);
            if (entry.Name is { } name)
                tuningsByName.TryAdd((entry.Type, name), entry);
        }
        var animationStateMachinesByInstance = new Dictionary<ulong, TuningArchiveEntry>();
        foreach (var entry in animationStateMachines)
            animationStateMachinesByInstance.TryAdd(entry.Instance, entry);
        IReadOnlyDictionary<uint, string> strings = stringComments ? archive.GetStrings(snapshot) : new Dictionary<uint, string>();
        var descriptionCache = archive.FindTuningDescriptions(snapshot) is { } descriptions ? archive.GetTuningDescriptionCache(descriptions) : null;
        return new(new(strings, names, tuningsByKey, tuningsByName, animationStateMachinesByInstance, descriptionCache, uninstalledTunings), stringComments, referenceComments);
    }

    /// <summary>
    /// Gets the specified tuning <paramref name="text"/> with comments, or the text as it is if it is not XML
    /// </summary>
    /// <remarks>
    /// Comments already in the text are kept. The result is indented with two spaces, with the line ends the text has, and starts with an XML declaration if the text does.
    /// </remarks>
    public string Comment(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        XDocument document;
        try
        {
            document = TuningXml.Parse(text);
        }
        catch (XmlException)
        {
            return text;
        }
        if (document.Root is not { } root)
            return text;
        TuningArchiveFile.AddComments(document, text, 0, tables, StringComments, ReferenceComments, null);
        var newLine = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var builder = new StringBuilder();
        if (text.TrimStart('﻿').StartsWith("<?xml", StringComparison.Ordinal))
            builder.Append(declaration).Append(newLine);
        using (var stringWriter = new StringWriter(builder, CultureInfo.InvariantCulture))
        using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true, IndentChars = "  ", NewLineChars = newLine, NewLineHandling = NewLineHandling.Replace, OmitXmlDeclaration = true }))
            root.WriteTo(xmlWriter);
        if (text.EndsWith('\n'))
            builder.Append(newLine);
        return builder.ToString();
    }

    /// <summary>
    /// Gets the specified fragment of tuning (one element, such as <see cref="TuningXml.GetFragmentText(XElement)"/> gets) with comments on one line, or the fragment as it is if it is not XML
    /// </summary>
    /// <remarks>
    /// A fragment has no tuning around it for its tuning descriptions to describe, so its comments are found as XML Extractor finds them.
    /// </remarks>
    public string CommentFragment(string fragment)
    {
        ArgumentNullException.ThrowIfNull(fragment);
        XElement element;
        try
        {
            element = XElement.Parse(fragment);
        }
        catch (XmlException)
        {
            return fragment;
        }
        TuningArchiveFile.AddComments(new XDocument(element), fragment, 0, tables with { DescriptionCache = null }, StringComments, ReferenceComments, null);
        var builder = new StringBuilder();
        using (var stringWriter = new StringWriter(builder, CultureInfo.InvariantCulture))
        using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = false, OmitXmlDeclaration = true }))
            element.WriteTo(xmlWriter);
        return builder.ToString();
    }
}