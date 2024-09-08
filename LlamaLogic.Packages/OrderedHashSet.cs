namespace LlamaLogic.Packages;

class OrderedHashSet<T> :
    ISet<T>
    where T : notnull
{
    public OrderedHashSet()
    {
        dictionary = new();
        linkedList = new();
    }

    public OrderedHashSet(IEqualityComparer<T> equalityComparer)
    {
        dictionary = new(equalityComparer);
        linkedList = new();
    }

    public OrderedHashSet(int capacity)
    {
        dictionary = new(capacity);
        linkedList = new();
    }

    public OrderedHashSet(int capacity, IEqualityComparer<T> equalityComparer)
    {
        dictionary = new(capacity, equalityComparer);
        linkedList = new();
    }

    public OrderedHashSet(IEnumerable<T> collection) :
        this()
    {
        foreach (var item in collection)
            if (!Add(item))
                throw new ArgumentException("Duplicate item in collection");
    }

    public OrderedHashSet(IEnumerable<T> collection, IEqualityComparer<T> equalityComparer) :
        this(equalityComparer)
    {
        foreach (var item in collection)
            if (!Add(item))
                throw new ArgumentException("Duplicate item in collection");
    }

    readonly Dictionary<T, LinkedListNode<T>> dictionary;
    readonly LinkedList<T> linkedList;

    public int Count =>
        dictionary.Count;

    public bool IsReadOnly =>
        false;

    public bool Add(T item)
    {
        ref var node = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, item, out var exists);
        if (exists)
            return false;
        node = linkedList.AddLast(item);
        return true;
    }

    void ICollection<T>.Add(T item) =>
        Add(item);

    public void Clear()
    {
        dictionary.Clear();
        linkedList.Clear();
    }

    public bool Contains(T item) =>
        dictionary.ContainsKey(item);

    public void CopyTo(T[] array, int arrayIndex) =>
        linkedList.CopyTo(array, arrayIndex);

    public void ExceptWith(IEnumerable<T> other)
    {
        foreach (var item in other)
            Remove(item);
    }

    public IEnumerator<T> GetEnumerator() =>
        linkedList.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    public void IntersectWith(IEnumerable<T> other)
    {
        foreach (var item in other)
            if (!Contains(item))
                Remove(item);
    }

    public bool IsProperSubsetOf(IEnumerable<T> other) =>
        IsSubsetOf(other) && Count < other.Count();

    public bool IsProperSupersetOf(IEnumerable<T> other) =>
        IsSupersetOf(other) && Count > other.Count();

    public bool IsSubsetOf(IEnumerable<T> other)
    {
        foreach (var item in this)
            if (!other.Contains(item))
                return false;
        return true;
    }

    public bool IsSupersetOf(IEnumerable<T> other)
    {
        foreach (var item in other)
            if (!Contains(item))
                return false;
        return true;
    }

    public bool Overlaps(IEnumerable<T> other)
    {
        foreach (var item in other)
            if (Contains(item))
                return true;
        return false;
    }

    public bool Remove(T item)
    {
        if (dictionary.Remove(item, out var node))
        {
            linkedList.Remove(node);
            return true;
        }
        return false;
    }

    public bool SetEquals(IEnumerable<T> other) =>
        Count == other.Count() && IsSubsetOf(other);

    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        foreach (var item in other)
            if (!Remove(item))
                Add(item);
    }

    public void UnionWith(IEnumerable<T> other)
    {
        foreach (var item in other)
            Add(item);
    }
}
