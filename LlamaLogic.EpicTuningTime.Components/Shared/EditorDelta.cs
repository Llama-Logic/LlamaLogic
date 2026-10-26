namespace LlamaLogic.EpicTuningTime.Components.Shared;

/// <summary>
/// Finds what changed between two versions of an editor's text and of its annotations, so that only the change has to reach the editor
/// </summary>
static class EditorDelta
{
    /// <summary>
    /// Gets the span of <paramref name="before"/> that differs from <paramref name="after"/> and the text that replaces it, or <see langword="null"/> if either has carriage returns, whose offsets the editor counts differently
    /// </summary>
    public static (int start, int end, string text)? GetTextEdit(string before, string after)
    {
        if (before.Contains('\r', StringComparison.Ordinal) || after.Contains('\r', StringComparison.Ordinal))
            return null;
        var prefix = before.AsSpan().CommonPrefixLength(after);
        var limit = Math.Min(before.Length, after.Length) - prefix;
        var suffix = 0;
        while (suffix < limit && before[before.Length - 1 - suffix] == after[after.Length - 1 - suffix])
            ++suffix;
        return (prefix, before.Length - suffix, after.Substring(prefix, after.Length - suffix - prefix));
    }

    /// <summary>
    /// Gets which of the <paramref name="before"/> annotations to replace, starting at index <c>start</c> and numbering <c>removed</c>, with which of the <paramref name="after"/> annotations, given that the annotations following them move by <paramref name="lineShift"/> lines
    /// </summary>
    public static (int start, int removed, IReadOnlyList<EditorAnnotation> inserted) GetAnnotationDelta(IReadOnlyList<EditorAnnotation> before, IReadOnlyList<EditorAnnotation> after, int lineShift)
    {
        var limit = Math.Min(before.Count, after.Count);
        var prefix = 0;
        while (prefix < limit && AreSame(before[prefix], after[prefix], 0))
            ++prefix;
        limit -= prefix;
        var suffix = 0;
        while (suffix < limit && AreSame(before[before.Count - 1 - suffix], after[after.Count - 1 - suffix], lineShift))
            ++suffix;
        var inserted = new EditorAnnotation[after.Count - suffix - prefix];
        for (var index = 0; index < inserted.Length; ++index)
            inserted[index] = after[prefix + index];
        return (prefix, before.Count - suffix - prefix, inserted);
    }

    static bool AreSame(EditorAnnotation before, EditorAnnotation after, int lineShift) =>
        before.Line + lineShift == after.Line
        && before.Start == after.Start
        && before.End == after.End
        && before.Navigable == after.Navigable
        && before.HasHover == after.HasHover
        && string.Equals(before.Hint, after.Hint, StringComparison.Ordinal)
        && string.Equals(before.HintClass, after.HintClass, StringComparison.Ordinal);

    /// <summary>
    /// Gets the number of lines in the specified <paramref name="text"/>
    /// </summary>
    public static int CountLines(string text) =>
        text.AsSpan().Count('\n') + 1;
}