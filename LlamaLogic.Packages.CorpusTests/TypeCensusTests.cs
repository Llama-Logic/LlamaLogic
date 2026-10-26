namespace LlamaLogic.Packages.CorpusTests;

[TestClass]
public class TypeCensusTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void TypeCensus()
    {
        var counts = new Dictionary<(string root, ResourceType type), (int resources, int packages)>();
        var packagesByRoot = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var unopened = 0;
        foreach (var data in PackageCorpus.Packages)
        {
            var root = (string)data[0];
            var path = (string)data[1];
            packagesByRoot[root] = packagesByRoot.TryGetValue(root, out var packageCount) ? packageCount + 1 : 1;
            try
            {
                using var package = new DataBasePackedFile(path);
                foreach (var group in package.Keys.GroupBy(key => key.Type))
                    counts[(root, group.Key)] = counts.TryGetValue((root, group.Key), out var existing)
                        ? (existing.resources + group.Count(), existing.packages + 1)
                        : (group.Count(), 1);
            }
            catch (Exception ex)
            {
                ++unopened;
                TestContext.WriteLine($"could not open {path}: {ex.GetType().Name}: {ex.Message}");
            }
        }
        var roots = packagesByRoot.Keys.ToList();
        foreach (var (root, packageCount) in packagesByRoot)
            TestContext.WriteLine($"{root}: {packageCount} packages");
        TestContext.WriteLine($"{unopened} packages could not be opened");
        TestContext.WriteLine($"type\tname\t{string.Join("\t", roots.Select(root => $"{Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar))} resources\t{Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar))} packages"))}");
        foreach (var type in counts.Keys.Select(key => key.type).Distinct().OrderBy(type => (uint)type))
            TestContext.WriteLine($"0x{(uint)type:x8}\t{(Enum.IsDefined(type) ? type.ToString() : "(undefined)")}\t{string.Join("\t", roots.Select(root => counts.TryGetValue((root, type), out var count) ? $"{count.resources}\t{count.packages}" : "0\t0"))}");
    }
}
