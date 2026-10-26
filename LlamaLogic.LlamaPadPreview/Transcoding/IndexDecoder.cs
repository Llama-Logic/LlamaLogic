namespace LlamaLogic.LlamaPadPreview.Transcoding;

class IndexDecoder :
    IPyObjectDecoder
{
    public static IndexDecoder Instance { get; } = new();

    public bool CanDecode(PyType objectType, Type targetType) =>
        targetType == typeof(Index);

    public bool TryDecode<T>(PyObject pyObj, out T? value)
    {
        if (typeof(T) == typeof(Index))
        {
            if (PyInt.IsIntType(pyObj))
            {
                value = (T)(object)new Index(PyInt.AsInt(pyObj).ToInt32());
                return true;
            }
            if (PyTuple.IsTupleType(pyObj))
            {
                var elements = PyTuple.AsTuple(pyObj).ToImmutableArray();
                if (elements.Length is >= 1 and <= 2 && PyInt.IsIntType(elements[0]))
                {
                    var indexValue = PyInt.AsInt(elements[0]).ToInt32();
                    if (elements.Length == 1)
                    {
                        value = (T)(object)new Index(indexValue);
                        return true;
                    }
                    value = (T)(object)new Index(indexValue, elements[1].IsTrue());
                    return true;
                }
            }
        }
        value = default;
        return false;
    }
}
