namespace LlamaLogic.Packages;

static class IndexUtilities
{
    public static IEnumerable<int> GetOffsets(this int length, Range range)
    {
        ThrowIfIndexOutOfRange(length, range);
        var startOffset = range.Start.GetOffset(length);
        var endOffset = range.End.GetOffset(length);
        if (endOffset < startOffset)
            throw new ArgumentOutOfRangeException(nameof(range), range, "Range end is less than start");
        for (var offset = startOffset; offset <= endOffset; ++offset)
            yield return offset;
    }

    public static void ThrowIfIndexOutOfRange(this int length, Index index, [CallerArgumentExpression(nameof(index))] string? paramName = null)
    {
        var offset = index.GetOffset(length);
        if (offset < 0 || offset >= length)
            throw new ArgumentOutOfRangeException(paramName, index, $"Index {index} is out of range [0..{length}]");
    }

    public static void ThrowIfIndexOutOfRange(this int length, Range range, [CallerArgumentExpression(nameof(range))] string? paramName = null)
    {
        var startOffset = range.Start.GetOffset(length);
        var endOffset = range.End.GetOffset(length);
        if (startOffset < 0 || startOffset >= length || endOffset < 0 || endOffset >= length)
            throw new ArgumentOutOfRangeException(paramName, range, $"Range {range} is out of range [0..{length}]");
    }

    public static void ThrowIfIndexOutOfRangeForInsertion(this int length, Index index, [CallerArgumentExpression(nameof(index))] string? paramName = null)
    {
        var offset = index.GetOffset(length);
        if (offset < 0 || offset > length)
            throw new ArgumentOutOfRangeException(paramName, index, $"Index {index} is out of range [0..{length + 1}]");
    }
}
