namespace LlamaLogic.LlamaPadPreview.Transcoding;

class RangeDecoder :
    IPyObjectDecoder
{
    public static RangeDecoder Instance { get; } = new();

    public bool CanDecode(PyType objectType, Type targetType) =>
        targetType == typeof(Range);

    public bool TryDecode<T>(PyObject pyObj, out T? value)
    {
        if (typeof(T) == typeof(Range) && PyTuple.IsTupleType(pyObj))
        {
            var elements = PyTuple.AsTuple(pyObj).ToImmutableArray();
            if (elements.Length == 2)
            {
                var indexDecoder = IndexDecoder.Instance;
                if (!indexDecoder.TryDecode<Index>(elements[0], out var lowerBound))
                    lowerBound = 0;
                if (!indexDecoder.TryDecode<Index>(elements[1], out var upperBound))
                    upperBound = ^0;
                value = (T)(object)new Range(lowerBound, upperBound);
                return true;
            }
        }
        value = default;
        return false;
    }
}