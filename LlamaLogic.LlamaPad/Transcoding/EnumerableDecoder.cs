namespace LlamaLogic.LlamaPad.Transcoding;

class EnumerableDecoder :
    IPyObjectDecoder
{
    public static EnumerableDecoder Instance { get; } = new();

    public bool CanDecode(PyType objectType, Type targetType) =>
        targetType == typeof(IEnumerable)
        || targetType.IsGenericType
        && targetType.GetGenericTypeDefinition() is { } gtd
        && gtd == typeof(IEnumerable<>)
        && targetType.GetGenericArguments() is { Length: 1 } ga
        && ga[0] == typeof(object);

    public bool TryDecode<T>(PyObject pyObj, out T? value)
    {
        if (typeof(T) == typeof(IEnumerable)
            || typeof(T).IsGenericType
            && typeof(T).GetGenericTypeDefinition() is { } gtd
            && gtd == typeof(IEnumerable<>)
            && typeof(T).GetGenericArguments() is { Length: 1 } ga
            && ga[0] == typeof(object)
            && PyList.IsListType(pyObj))
        {
            value = (T)(IEnumerable<object?>)[..PyList.AsList(pyObj)];
            return true;
        }
        value = default;
        return false;
    }
}
