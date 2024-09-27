namespace LlamaLogic.Packages.Models.ModFileManifest;

/// <summary>
/// Represents a strategy for generating hashes for package mod files
/// </summary>
[Flags]
public enum ModFileManifestResourceHashStrategy :
    int
{
    /// <summary>
    /// Inverts the typical behavior of <see cref="ModFileManifestModel.HashResourceKeys"/>, causing it to become a list of keys for the only resources to be used in generating the hash
    /// </summary>
    None = 0,

    /// <summary>
    /// <see cref="ResourceType.SimData"/> and <see cref="ResourceFileType.TuningMarkup"/> resources will be used to generate hashes for mod package files
    /// </summary>
    UseTuningAndSimData = 0x1,

    /// <summary>
    /// Non <see cref="ResourceFileType.DirectDrawSurface"/>, <see cref="ResourceFileType.PortableNetworkGraphic"/>, <see cref="ResourceType.SimData"/>, <see cref="ResourceType.StringTable"/> and <see cref="ResourceFileType.TuningMarkup"/> resources will be used to generate hashes for mod package files
    /// </summary>
    UseNonTuningSimDataStringTablesAndImages = 0x2,

    /// <summary>
    /// <see cref="ResourceType.StringTable"/> resources will be used to generate hashes for mod package files
    /// </summary>
    UseStringTables = 0x4,

    /// <summary>
    /// <see cref="ResourceFileType.DirectDrawSurface"/> and <see cref="ResourceFileType.PortableNetworkGraphic"/> resources will be used to generate hashes for mod package files
    /// </summary>
    UseImages = 0x8,

    /// <summary>
    /// The strategy most tolerant of player customization of mod package files, permitting any change so long as it does not impact <see cref="ResourceType.SimData"/> or <see cref="ResourceFileType.TuningMarkup"/>
    /// </summary>
    PlayerCustomizationTolerancePermissive = UseTuningAndSimData,

    /// <summary>
    /// The strategy with lenient tolerance for player customization of mod package files, permitting changes to <see cref="ResourceFileType.DirectDrawSurface"/>, <see cref="ResourceFileType.PortableNetworkGraphic"/>, and <see cref="ResourceType.StringTable"/> resources
    /// </summary>
    PlayerCustomizationToleranceLenient = PlayerCustomizationTolerancePermissive | UseNonTuningSimDataStringTablesAndImages,

    /// <summary>
    /// The strategy with moderate tolerance for player customization of mod package files, permitting changes to <see cref="ResourceFileType.DirectDrawSurface"/> and <see cref="ResourceFileType.PortableNetworkGraphic"/> resources
    /// </summary>
    PlayerCustomizationToleranceModerate = PlayerCustomizationToleranceLenient | UseStringTables,

    /// <summary>
    /// The strategy least tolerant of player customization of mod package files, permitting no changes of any kind whatever
    /// </summary>
    PlayerCustomizationToleranceStrict = PlayerCustomizationToleranceModerate | UseImages,

    /// <summary>
    /// The strategy selected by default by creator manifest tooling
    /// </summary>
    [SuppressMessage("Design", "CA1069: Enums values should not be duplicated", Justification = "Except when they may change, dufus.")]
    Default = PlayerCustomizationTolerancePermissive
}
