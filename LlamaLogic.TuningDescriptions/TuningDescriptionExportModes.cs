namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// The destinations to which the game's build exports a tunable (its <c>export_modes</c> attribute)
/// </summary>
[Flags]
public enum TuningDescriptionExportModes
{
    /// <summary>
    /// No destinations
    /// </summary>
    None = 0,

    /// <summary>
    /// The tunable is exported to <c>SimData</c> resources (<c>client_binary</c>)
    /// </summary>
    ClientBinary = 1,

    /// <summary>
    /// The tunable is exported to the binary combined tuning (<c>server_binary</c>)
    /// </summary>
    ServerBinary = 2,

    /// <summary>
    /// The tunable is exported to tuning XML (<c>server_xml</c>)
    /// </summary>
    ServerXml = 4
}
