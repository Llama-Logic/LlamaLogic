namespace LlamaLogic.Packages.CorpusTests;

static class PackageCorpus
{
    const string rootsEnvironmentVariable = "LLAMALOGIC_PACKAGES_CORPUS_ROOTS";

    static readonly EnumerationOptions enumerationOptions = new()
    {
        IgnoreInaccessible = true,
        RecurseSubdirectories = true
    };

    public static IEnumerable<object[]> Packages =>
        GetRoots()
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.package", enumerationOptions)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => new object[] { root, path }));

    static IEnumerable<string> GetRoots() =>
        Environment.GetEnvironmentVariable(rootsEnvironmentVariable) is { Length: > 0 } roots
        ? roots.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        :
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EA Games", "The Sims 4"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Electronic Arts", "The Sims 4", "Mods")
        ];
}
