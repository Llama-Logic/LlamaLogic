namespace LlamaLogic.Packages.Models;

/// <summary>
/// Represents a model for a raw resource
/// </summary>
public abstract class Model :
    IModel
{
    /// <inheritdoc />
    public static ISet<ResourceType> SupportedTypes =>
        SimDataModel.SupportedTypes;

    /// <summary>
    /// Decodes a resource of the specified <paramref name="type"/> with the model <see cref="GetModelType(ResourceType)"/> gives for it
    /// </summary>
    /// <exception cref="NotSupportedException">No model decodes resources of the specified <paramref name="type"/></exception>
    public static Model Decode(ResourceType type, ReadOnlyMemory<byte> data) =>
        type switch
        {
            ResourceType combinedTuningResourceType when CombinedTuningModel.SupportedTypes.Contains(combinedTuningResourceType) => CombinedTuningModel.Decode(data),
            ResourceType simDataResourceType when SimDataModel.SupportedTypes.Contains(simDataResourceType) => SimDataModel.Decode(data),
            ResourceType stringTableResourceType when StringTableModel.SupportedTypes.Contains(stringTableResourceType) => StringTableModel.Decode(data),
            _ => throw new NotSupportedException($"Resource type {type} is not supported")
        };

    /// <summary>
    /// Gets the model that decodes every resource of the specified <paramref name="type"/>, or <see langword="null"/> if there is none
    /// </summary>
    /// <remarks>
    /// Models that apply only to some resources of a type, such as <see cref="ModFileManifestModel"/> for <see cref="ResourceType.SnippetTuning"/>, are not given.
    /// </remarks>
    public static Type? GetModelType(ResourceType type) =>
        type switch
        {
            ResourceType combinedTuningResourceType when CombinedTuningModel.SupportedTypes.Contains(combinedTuningResourceType) => typeof(CombinedTuningModel),
            ResourceType simDataResourceType when SimDataModel.SupportedTypes.Contains(simDataResourceType) => typeof(SimDataModel),
            ResourceType stringTableResourceType when StringTableModel.SupportedTypes.Contains(stringTableResourceType) => typeof(StringTableModel),
            _ => null
        };

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
            ResourceType simDataResourceType when SimDataModel.SupportedTypes.Contains(simDataResourceType) => SimDataModel.GetName(stream),
            ResourceType stringTableResourceType when StringTableModel.SupportedTypes.Contains(stringTableResourceType) => StringTableModel.GetName(stream),
            _ => throw new NotSupportedException($"Resource type {type} is not supported")
        };

    /// <summary>
    /// Gets the name of a resource from its raw data, if it has one
    /// </summary>
    public static Task<string?> GetNameAsync(ResourceType type, Stream stream, CancellationToken cancellationToken = default) =>
        type switch
        {
            ResourceType simDataResourceType when SimDataModel.SupportedTypes.Contains(simDataResourceType) => SimDataModel.GetNameAsync(stream, cancellationToken),
            ResourceType stringTableResourceType when StringTableModel.SupportedTypes.Contains(stringTableResourceType) => StringTableModel.GetNameAsync(stream),
            _ => throw new NotSupportedException($"Resource type {type} is not supported")
        };

    /// <inheritdoc />
    public abstract string? ResourceName { get; }

    /// <inheritdoc />
    public abstract ReadOnlyMemory<byte> Encode();
}
