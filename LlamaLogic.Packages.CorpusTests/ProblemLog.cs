namespace LlamaLogic.Packages.CorpusTests;

sealed class ProblemLog
{
    const int maximumSamplesPerCategory = 10;

    readonly Dictionary<string, (int count, List<string> samples)> problemsByCategory = [];

    public int Count =>
        problemsByCategory.Values.Sum(problems => problems.count);

    public void Add(string category, string detail)
    {
        if (!problemsByCategory.TryGetValue(category, out var problems))
            problems = (0, []);
        if (problems.samples.Count < maximumSamplesPerCategory)
            problems.samples.Add(detail);
        problemsByCategory[category] = (problems.count + 1, problems.samples);
    }

    public override string ToString()
    {
        var report = new StringBuilder();
        foreach (var (category, (count, samples)) in problemsByCategory.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            report.AppendLine($"{category}: {count}");
            foreach (var sample in samples)
                report.AppendLine($"    {sample}");
        }
        return report.ToString();
    }
}
