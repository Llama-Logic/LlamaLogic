namespace LlamaLogic.TuningDescriptions.CorpusTests;

static class GameCorpus
{
    const string rootEnvironmentVariable = "LLAMALOGIC_TUNINGDESCRIPTIONS_GAME_ROOT";

    public static string BasePackagePath =>
        Path.Combine(Root, "Data", "Simulation", "SimulationDeltaBuild0.package");

    public static IEnumerable<object[]> Packages
    {
        get
        {
            if (!File.Exists(BasePackagePath) || TuningDescriptionsPath is null)
                yield break;
            yield return ["BASE_GAME", BasePackagePath];
            var deltaRoot = Path.Combine(Root, "Delta");
            if (!Directory.Exists(deltaRoot))
                yield break;
            foreach (var packRoot in Directory.EnumerateDirectories(deltaRoot).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                if (Path.Combine(packRoot, "SimulationDeltaBuild0.package") is { } packagePath && File.Exists(packagePath))
                    yield return [Path.GetFileName(packRoot), packagePath];
        }
    }

    public static string Root =>
        Environment.GetEnvironmentVariable(rootEnvironmentVariable) is { Length: > 0 } root
        ? root
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EA Games", "The Sims 4");

    public static string? TuningDescriptionsPath
    {
        get
        {
            var defaultIniPath = Path.Combine(Root, "Game", "Bin", "Default.ini");
            if (!File.Exists(defaultIniPath))
                return null;
            var gameVersion = File.ReadLines(defaultIniPath)
                .Select(line => line.Split('=', 2))
                .Where(parts => parts.Length == 2 && parts[0].Trim().Equals("gameversion", StringComparison.OrdinalIgnoreCase))
                .Select(parts => parts[1].Trim())
                .FirstOrDefault();
            return gameVersion is null
                ? null
                : TuningDescriptionCorpus.Archives.Select(archive => (string)archive[0]).FirstOrDefault(path => Path.GetFileName(path).Equals($"{gameVersion}-TDESC.zip", StringComparison.OrdinalIgnoreCase));
        }
    }
}
