namespace LlamaLogic.TuningDescriptions.CorpusTests;

static class TuningDescriptionCorpus
{
    const string rootsEnvironmentVariable = "LLAMALOGIC_TUNINGDESCRIPTIONS_CORPUS_ROOTS";

    public static IEnumerable<object[]> Archives =>
        GetRoots()
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*-TDESC.zip")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => new object[] { path }));

    static IEnumerable<string> GetRoots() =>
        Environment.GetEnvironmentVariable(rootsEnvironmentVariable) is { Length: > 0 } roots
        ? roots.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        : [Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Sims 4 Studio", "Tuning Descriptions")];
}
