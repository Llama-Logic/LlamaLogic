namespace LlamaLogic.Architecture.CorpusTests;

static class ArchitectureCorpus
{
    const string rootsEnvironmentVariable = "LLAMALOGIC_ARCHITECTURE_CORPUS_ROOTS";

    static readonly EnumerationOptions enumerationOptions = new()
    {
        IgnoreInaccessible = true,
        RecurseSubdirectories = true
    };

    public static IEnumerable<object[]> Blueprints =>
        GetFiles("*.blueprint");

    public static IEnumerable<object[]> Saves =>
        GetFiles("*.save");

    public static IEnumerable<object[]> Worlds =>
        GetFiles("*.world");

    static IEnumerable<object[]> GetFiles(string searchPattern) =>
        GetRoots()
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, searchPattern, enumerationOptions)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => new object[] { root, path }));

    static IEnumerable<string> GetRoots() =>
        Environment.GetEnvironmentVariable(rootsEnvironmentVariable) is { Length: > 0 } roots
        ? roots.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        :
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EA Games", "The Sims 4"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Electronic Arts", "The Sims 4", "saves"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Electronic Arts", "The Sims 4", "Tray")
        ];
}
