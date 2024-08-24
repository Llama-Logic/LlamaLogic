namespace LlamaLogic.LlamaPad.Transcoding;

class IndexDecoder :
    IPyObjectDecoder
{
    public bool CanDecode(PyType objectType, Type targetType) =>
        targetType == typeof(Index);

    public bool TryDecode<T>(PyObject pyObj, out T? value)
    {
        if (typeof(T) == typeof(Index) && PyInt.IsIntType(pyObj))
        {
            value = (T)(object)new Index(PyInt.AsInt(pyObj).ToInt32());
            return true;
        }
        value = default;
        return false;
    }
}
