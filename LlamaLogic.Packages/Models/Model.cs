namespace LlamaLogic.Packages.Models;

/// <summary>
/// Represents a model for a raw resource
/// </summary>
public abstract class Model :
    IModel
{
    /// <inheritdoc />
    public static ISet<ResourceType> SupportedTypes =>
        DataModel.SupportedTypes;

    /// <inheritdoc />
    public static string? GetName(Stream stream) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public static Task<string?> GetNameAsync(Stream stream, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    /// Gets the name of a resource from its raw data, if it has one
    /// </summary>
    public static string? GetName(ResourceType type, Stream stream) =>
        type switch
        {
            ResourceType dataResourceType when DataModel.SupportedTypes.Contains(dataResourceType) => DataModel.GetName(stream),
            ResourceType stringTableResourceType when StringTableModel.SupportedTypes.Contains(stringTableResourceType) => StringTableModel.GetName(stream),
            _ => throw new NotSupportedException($"Resource type {type} is not supported")
        };

    /// <summary>
    /// Gets the name of a resource from its raw data, if it has one
    /// </summary>
    public static Task<string?> GetNameAsync(ResourceType type, Stream stream, CancellationToken cancellationToken = default) =>
        type switch
        {
            ResourceType dataResourceType when DataModel.SupportedTypes.Contains(dataResourceType) => DataModel.GetNameAsync(stream, cancellationToken),
            ResourceType stringTableResourceType when StringTableModel.SupportedTypes.Contains(stringTableResourceType) => StringTableModel.GetNameAsync(stream),
            _ => throw new NotSupportedException($"Resource type {type} is not supported")
        };

    /// <inheritdoc />
    public abstract string? ResourceName { get; }

    /// <inheritdoc />
    public abstract ReadOnlyMemory<byte> Encode();
}
