namespace LlamaLogic.Packages;

static class SequenceUtilities
{
    public static void AddRange<T>(this Collection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
            collection.Add(item);
    }

    public static IEnumerable<bool> AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
    {
        foreach (var item in items)
            yield return hashSet.Add(item);
    }

    public static IReadOnlyList<bool> AddRangeImmediately<T>(this HashSet<T> hashSet, IEnumerable<T> items) =>
        [..AddRange(hashSet, items)];
}
