namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents the base game or a pack in a <see cref="GameInstallation"/> (🔒)
/// </summary>
public sealed class GameInstallationPack
{
    internal GameInstallationPack(string code, string? version, IReadOnlyList<GameInstallationPackage> simulationPackages, IReadOnlyList<GameInstallationPackage> clientPackages, IReadOnlyDictionary<string, IReadOnlyList<GameInstallationPackage>> stringTablePackages)
    {
        Code = code;
        Version = version;
        SimulationPackages = simulationPackages;
        ClientPackages = clientPackages;
        this.stringTablePackages = stringTablePackages;
    }

    readonly IReadOnlyDictionary<string, IReadOnlyList<GameInstallationPackage>> stringTablePackages;

    /// <summary>
    /// Gets the packages of the pack containing client resources other than string tables (such as images, sounds and videos), in descending order of priority
    /// </summary>
    public IReadOnlyList<GameInstallationPackage> ClientPackages { get; }

    /// <summary>
    /// Gets the code of the pack (<c>BG</c> for the base game, otherwise the name of the pack's folder, such as <c>EP01</c>)
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the locales for which the pack has string tables (such as <c>ENG_US</c>)
    /// </summary>
    public IReadOnlyCollection<string> Locales =>
        [.. stringTablePackages.Keys];

    /// <summary>
    /// Gets the packages of the pack containing simulation resources (combined tuning and SimData), in descending order of priority
    /// </summary>
    public IReadOnlyList<GameInstallationPackage> SimulationPackages { get; }

    /// <summary>
    /// Gets the version of the pack, as its version file states it, if it has one
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// Gets the packages of the pack containing string tables for the specified <paramref name="locale"/>, in descending order of priority
    /// </summary>
    public IReadOnlyList<GameInstallationPackage> GetStringTablePackages(string locale)
    {
        ArgumentNullException.ThrowIfNull(locale);
        return stringTablePackages.TryGetValue(locale, out var packages) ? packages : [];
    }

    /// <inheritdoc/>
    public override string ToString() =>
        Code;
}