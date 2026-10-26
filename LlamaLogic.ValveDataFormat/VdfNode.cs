namespace LlamaLogic.ValveDataFormat;

/// <summary>
/// Represents a node in a VDF document
/// </summary>
public class VdfNode :
    IEquatable<VdfNode>
{
    enum ParserMode
    {
        InNodeList,
        InKey,
        InKeyEscapeSequence,
        InBetweenKeyAndValue,
        InScalarValue,
        InScalarValueEscapeSequence,
        InCommentDelimiter,
        InKeyTrailingCommentDelimiter,
        InBeforeNodeListCommentDelimiter,
        InComment,
        InKeyTrailingComment,
        InBeforeNodeListComment,
        InLineBreak,
        InKeyTrailingLineBreak,
        InBeforeNodeListLineBreak
    }

    /// <summary>
    /// Deserializes a list of <see cref="VdfNode"/> from a specified <paramref name="reader"/>
    /// </summary>
    public static IReadOnlyList<VdfNode> Deserialize(StreamReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var result = new List<VdfNode>();
        var sectionStack = new Stack<VdfKeyValuePair>();
        var builder = new StringBuilder();
        Span<char> charSpan = stackalloc char[1];
        VdfNode? node = null;
        var mode = ParserMode.InNodeList;
        var beforeNodeList = false;
        while (reader.Read(charSpan) > 0)
            DeserializeChar(charSpan[0], result, sectionStack, builder, ref node, ref mode, ref beforeNodeList);
        if (node is not null)
            result.Add(node);
        return result.AsReadOnly();
    }

    /// <summary>
    /// Deserializes a list of <see cref="VdfNode"/> from a specified <paramref name="reader"/> asynchronously
    /// </summary>
    public static async Task<IReadOnlyList<VdfNode>> DeserializeAsync(StreamReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var result = new List<VdfNode>();
        var sectionStack = new Stack<VdfKeyValuePair>();
        var builder = new StringBuilder();
        Memory<char> charMemory = new char[1];
        VdfNode? node = null;
        var mode = ParserMode.InNodeList;
        var beforeNodeList = false;
        while (await reader.ReadAsync(charMemory).ConfigureAwait(false) > 0)
            DeserializeChar(charMemory.Span[0], result, sectionStack, builder, ref node, ref mode, ref beforeNodeList);
        if (node is not null)
            result.Add(node);
        return result.AsReadOnly();
    }

    static void DeserializeChar(char c, List<VdfNode> result, Stack<VdfKeyValuePair> sectionStack, StringBuilder builder, ref VdfNode? node, ref ParserMode mode, ref bool beforeNodeList)
    {
        switch (c)
        {
            case '\"' when mode is ParserMode.InNodeList or ParserMode.InLineBreak or ParserMode.InBeforeNodeListLineBreak:
                node = new VdfKeyValuePair();
                mode = ParserMode.InKey;
                return;
            case '\"' when mode is ParserMode.InKey:
                ((VdfKeyValuePair)node!).Key = builder.ToString();
                builder.Clear();
                mode = ParserMode.InBetweenKeyAndValue;
                return;
            case '\"' when mode is ParserMode.InBetweenKeyAndValue or ParserMode.InKeyTrailingLineBreak:
                mode = ParserMode.InScalarValue;
                return;
            case '\"' when mode is ParserMode.InScalarValue:
                ((VdfKeyValuePair)node!).Value = builder.ToString();
                builder.Clear();
                mode = ParserMode.InNodeList;
                return;
            case '\\' when mode is ParserMode.InKey:
                mode = ParserMode.InKeyEscapeSequence;
                return;
            case '\\' when mode is ParserMode.InScalarValue:
                mode = ParserMode.InScalarValueEscapeSequence;
                return;
            case '\\' when mode is ParserMode.InKeyEscapeSequence:
                builder.Append('\\');
                mode = ParserMode.InKey;
                return;
            case '\\' when mode is ParserMode.InScalarValueEscapeSequence:
                builder.Append('\\');
                mode = ParserMode.InScalarValue;
                return;
            case '\"' when mode is ParserMode.InKeyEscapeSequence:
                builder.Append('\"');
                mode = ParserMode.InKey;
                return;
            case '\"' when mode is ParserMode.InScalarValueEscapeSequence:
                builder.Append('\"');
                mode = ParserMode.InScalarValue;
                return;
            case not ('\\' or '\"') when mode is ParserMode.InKeyEscapeSequence or ParserMode.InScalarValueEscapeSequence:
                throw new FormatException("Unknown escape sequence");
            case not '\"' when mode is ParserMode.InKey or ParserMode.InScalarValue:
                builder.Append(c);
                return;
            case '/' when mode is not (ParserMode.InKey or ParserMode.InScalarValue or ParserMode.InComment or ParserMode.InKeyTrailingComment or ParserMode.InBeforeNodeListComment):
                if (beforeNodeList)
                    mode = ParserMode.InBeforeNodeListCommentDelimiter;
                else if (node is VdfKeyValuePair kvp && kvp.Value is null)
                    mode = ParserMode.InKeyTrailingCommentDelimiter;
                else
                    mode = ParserMode.InCommentDelimiter;
                return;
            case not ('/' or '\r' or '\n') when mode is ParserMode.InCommentDelimiter or ParserMode.InKeyTrailingCommentDelimiter or ParserMode.InBeforeNodeListCommentDelimiter:
                if (mode is ParserMode.InKeyTrailingCommentDelimiter)
                    mode = ParserMode.InKeyTrailingComment;
                else if (mode is ParserMode.InBeforeNodeListCommentDelimiter)
                    mode = ParserMode.InBeforeNodeListComment;
                else
                    mode = ParserMode.InComment;
                builder.Append(c);
                return;
            case '\r' or '\n' when mode is ParserMode.InBetweenKeyAndValue:
                mode = ParserMode.InKeyTrailingLineBreak;
                return;
            case '\r' or '\n' when mode is ParserMode.InNodeList:
                if (beforeNodeList)
                {
                    beforeNodeList = false;
                    mode = ParserMode.InBeforeNodeListLineBreak;
                }
                else
                {
                    node ??= new VdfNode();
                    DeserializeCharGetCurrentList(result, sectionStack).Add(node);
                    node = null;
                    mode = ParserMode.InLineBreak;
                }
                return;
            case '\r' or '\n' when mode is ParserMode.InCommentDelimiter or ParserMode.InKeyTrailingCommentDelimiter or ParserMode.InBeforeNodeListCommentDelimiter or ParserMode.InComment or ParserMode.InKeyTrailingComment or ParserMode.InBeforeNodeListComment:
                mode = ParserMode.InLineBreak;
                node ??= new VdfNode();
                if (mode is ParserMode.InKeyTrailingCommentDelimiter or ParserMode.InKeyTrailingComment)
                    ((VdfKeyValuePair)node!).KeyTrailingComment = builder.ToString().Trim();
                else if (mode is ParserMode.InBeforeNodeListCommentDelimiter or ParserMode.InBeforeNodeListComment)
                    ((VdfSection)sectionStack.Peek().Value).OpeningTrailingComment = builder.ToString().Trim();
                else
                    node.TrailingComment = builder.ToString().Trim();
                builder.Clear();
                DeserializeCharGetCurrentList(result, sectionStack).Add(node);
                node = null;
                return;
            case not ('\r' or '\n' or '\"' or '{' or '}') when mode is ParserMode.InLineBreak or ParserMode.InBeforeNodeListLineBreak:
                mode = ParserMode.InNodeList;
                return;
            case not ('\r' or '\n' or '\"' or '{' or '}') when mode is ParserMode.InKeyTrailingLineBreak:
                mode = ParserMode.InBetweenKeyAndValue;
                return;
            case '{' when mode is ParserMode.InBetweenKeyAndValue or ParserMode.InKeyTrailingLineBreak:
                var sectionKeyValuePair = (VdfKeyValuePair)node!;
                sectionKeyValuePair.Value = new VdfSection();
                sectionStack.Push(sectionKeyValuePair);
                mode = ParserMode.InNodeList;
                beforeNodeList = true;
                return;
            case '}' when mode is ParserMode.InNodeList or ParserMode.InLineBreak or ParserMode.InBeforeNodeListLineBreak:
                mode = ParserMode.InNodeList;
                node = sectionStack.Pop();
                return;
        }
    }

    static IList<VdfNode> DeserializeCharGetCurrentList(List<VdfNode> result, Stack<VdfKeyValuePair> sectionStack) =>
        sectionStack.TryPeek(out var sectionKeyValuePair)
        ? ((VdfSection)sectionKeyValuePair.Value).Nodes
        : result;

    /// <summary>
    /// Parses a list of <see cref="VdfNode"/> from a specified <see cref="string"/>
    /// </summary>
    /// <param name="s">The <see cref="string"/> to parse</param>
    public static IReadOnlyList<VdfNode> Parse(string s)
    {
        if (TryParse(s, out var key))
            return key;
        throw new FormatException($"Unable to parse '{s}' as {nameof(VdfNode)}.");
    }

    /// <summary>
    /// Tries to parse a list of <see cref="VdfNode"/> from a specified <see cref="string"/>
    /// </summary>
    /// <param name="s">The <see cref="string"/> to parse</param>
    /// <param name="result">The list of <see cref="VdfNode"/> if <paramref name="s"/> was successfully parsed</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> was successfully parsed; otherwise, <see langword="false"/></returns>
    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out IReadOnlyList<VdfNode> result)
    {
        if (!string.IsNullOrWhiteSpace(s))
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(s));
            using var r = new StreamReader(ms);
            result = Deserialize(r);
            return true;
        }
        result = default;
        return false;
    }

    /// <inheritdoc/>
    public static bool operator ==(VdfNode? left, VdfNode? right) =>
        left?.Equals(right) ?? left is null == right is null;

    /// <inheritdoc/>
    public static bool operator !=(VdfNode? left, VdfNode? right) =>
        !(left == right);

    /// <summary>
    /// Gets/sets the comment which will follow the <see cref="VdfNode"/>
    /// </summary>
    public string? TrailingComment { get; set; }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is VdfNode vdfNode
        && Equals(vdfNode);

    /// <inheritdoc/>
    public virtual bool Equals(VdfNode? other) =>
        other is not null
        && TrailingComment is null == other.TrailingComment is null
        && (TrailingComment ?? string.Empty).Equals(other.TrailingComment ?? string.Empty, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine
        (
            typeof(VdfNode),
            TrailingComment
        );

    /// <summary>
    /// Serializes this <see cref="VdfNode"/> to the specified <paramref name="writer"/>
    /// </summary>
    public void Serialize(StreamWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Serialize(writer, 0);
    }

    internal virtual void Serialize(StreamWriter writer, int depth)
    {
        for (var i = 0; i < depth; ++i)
            writer.Write('\t');
        if (TrailingComment is { } trailingComment)
            writer.WriteLine($"// {trailingComment}");
        else
            writer.WriteLine();
    }

    /// <summary>
    /// Serializes this <see cref="VdfNode"/> to the specified <paramref name="writer"/> asynchronously
    /// </summary>
    public async ValueTask SerializeAsync(StreamWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        await SerializeAsync(writer, 0).ConfigureAwait(false);
    }

    internal virtual async ValueTask SerializeAsync(StreamWriter writer, int depth)
    {
        for (var i = 0; i < depth; ++i)
            await writer.WriteAsync('\t').ConfigureAwait(false);
        if (TrailingComment is { } trailingComment)
            await writer.WriteLineAsync($"// {trailingComment}").ConfigureAwait(false);
        else
            await writer.WriteLineAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        Serialize(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }
}