namespace LlamaLogic.Packages;

/// <summary>
/// Specifies the text encoding of a file type of a package resource independent of storage within a package
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class PackageResourceFileTypeEncodingAttribute(Type encodingType) :
    Attribute
{
    /// <summary>
    /// Gets the type of the text encoder to read and write values of the decorated package resource file type
    /// </summary>
    public Type EncodingType { get; } = encodingType;
}
