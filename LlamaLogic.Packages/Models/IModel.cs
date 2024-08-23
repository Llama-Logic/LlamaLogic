namespace LlamaLogic.Packages.Models;

/// <summary>
/// Represents a model for a resource
/// </summary>
public interface IModel
{
    /// <summary>
    /// Gets a list of resource types that this model supports
    /// </summary>
    static abstract ISet<ResourceType> SupportedTypes { get; }

    /// <summary>
    /// Gets the name of a resource from its raw data, if it has one (🔄️💤)
    /// </summary>
    static abstract string? GetName(Stream stream);

    /// <summary>
    /// Gets the name of a resource from its raw data, if it has one, asynchronously (🔄️💤)
    /// </summary>
    static abstract Task<string?> GetNameAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the name of this resource if it has one
    /// </summary>
    string? ResourceName { get; }

    /// <summary>
    /// Encodes the resource model into raw format (🔄️🏃)
    /// </summary>
    ReadOnlyMemory<byte> Encode();
}

/// <summary>
/// Represents a model for a raw resource
/// </summary>
public interface IModel<TSelf> :
    IModel
    where TSelf : IModel<TSelf>
{
    /// <summary>
    /// Decodes the resource in raw format to produce an operable model (🔄️🏃)
    /// </summary>
    static abstract TSelf Decode(ReadOnlyMemory<byte> data);
}
