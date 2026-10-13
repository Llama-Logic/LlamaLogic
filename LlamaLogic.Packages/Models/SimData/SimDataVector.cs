namespace LlamaLogic.Packages.Models.SimData;

/// <summary>
/// Represents a <see cref="SimDataType.VECTOR"/> value: a sequence of elements of one <see cref="SimDataType"/> (🔓)
/// </summary>
/// <remarks>
/// Elements are of the .NET type <see cref="SimDataObject"/> describes for <see cref="ElementType"/>, except that elements of a vector of <see cref="SimDataType.OBJECT"/> cannot be <see langword="null"/> and must all have the same <see cref="SimDataSchema"/>.
/// </remarks>
public sealed class SimDataVector :
    Collection<object?>
{
    /// <summary>
    /// Initializes a new <see cref="SimDataVector"/> with the specified <paramref name="elementType"/> and optional initial <paramref name="elements"/>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="elementType"/> is not a defined <see cref="SimDataType"/></exception>
    /// <exception cref="ArgumentException">An element is not valid for <paramref name="elementType"/></exception>
    public SimDataVector(SimDataType elementType, IEnumerable<object?>? elements = null)
    {
        if (!elementType.IsDefined())
            throw new ArgumentOutOfRangeException(nameof(elementType), elementType, "Not a SimData type");
        ElementType = elementType;
        if (elements is not null)
            foreach (var element in elements)
                Add(element);
    }

    /// <summary>
    /// Gets the type of the vector's elements
    /// </summary>
    public SimDataType ElementType { get; }

    /// <inheritdoc/>
    protected override void InsertItem(int index, object? item) =>
        base.InsertItem(index, RequireElement(item, index, false));

    object? RequireElement(object? item, int index, bool replacing)
    {
        var element = ElementType.RequireValue(item, nameof(item));
        if (ElementType is SimDataType.OBJECT)
        {
            if (element is not SimDataObject @object)
                throw new ArgumentException("An element of a vector of objects cannot be null", nameof(item));
            var others = replacing ? Items.Where((_, i) => i != index) : Items;
            if (others.FirstOrDefault() is SimDataObject other && !other.Schema.DefinitionEquals(@object.Schema))
                throw new ArgumentException("All elements of a vector of objects must have the same schema", nameof(item));
        }
        return element;
    }

    /// <inheritdoc/>
    protected override void SetItem(int index, object? item) =>
        base.SetItem(index, RequireElement(item, index, true));
}
