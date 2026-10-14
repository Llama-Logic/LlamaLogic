namespace LlamaLogic.TuningDescriptions.CorpusTests;

[TestClass]
public class TuningDescriptionCorpusTests
{
    public TestContext TestContext { get; set; } = null!;

    static void CheckDescription(TuningDescriptionSet set, TunableDescription description, string path, ProblemLog problems)
    {
        if (description.Attributes.ContainsKey("muid") && description.Muid is null)
            problems.Add("muid", $"{path}: {description.Attributes["muid"]}");
        switch (description)
        {
            case UnknownTunableDescription unknown:
                problems.Add("unknown element", $"{path}: {unknown.ElementName}");
                break;
            case FragmentReferenceTunableDescription reference:
                try
                {
                    set.Resolve(reference);
                }
                catch (Exception ex)
                {
                    problems.Add("fragment", $"{path}: {ex.GetType().Name}: {ex.Message}");
                }
                break;
            case EnumTunableDescription @enum when @enum.StaticEntries is { } staticEntries && set.GetEnum(staticEntries) is null:
                problems.Add("enum", $"{path}: {staticEntries}");
                break;
        }
        foreach (var member in description.Members)
            CheckDescription(set, member, $"{path}/{member.Name ?? "[]"}", problems);
    }

    public static string GetDisplayName(MethodInfo methodInfo, object[] data) =>
        Path.GetFileName((string)data[0]);

    [TestMethod]
    [DynamicData(nameof(TuningDescriptionCorpus.Archives), typeof(TuningDescriptionCorpus), DynamicDataDisplayName = nameof(GetDisplayName))]
    public void ArchiveIsConsistent(string path)
    {
        var problems = new ProblemLog();
        using var set = TuningDescriptionSet.FromZip(path);
        if (set.GameVersion is null)
            problems.Add("game version", Path.GetFileName(path));
        TestContext.WriteLine($"{set.InstanceClasses.Count} instances, {set.ModuleNames.Count} modules, {set.FragmentClasses.Count} fragments");
        var instanceTypes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var className in set.InstanceClasses.OrderBy(name => name, StringComparer.Ordinal))
            try
            {
                var instance = set.GetInstance(className)!;
                if (instance.InstanceType is { } instanceType)
                    instanceTypes.Add(instanceType);
                instance.GetEffectiveMembers();
                foreach (var member in instance.Members)
                    CheckDescription(set, member, $"{className}/{member.Name}", problems);
            }
            catch (Exception ex)
            {
                problems.Add("instance", $"{className}: {ex.GetType().Name}: {ex.Message}");
            }
        if (!instanceTypes.SetEquals(set.InstanceTypes))
            problems.Add("instance types", $"{string.Join(", ", set.InstanceTypes.Except(instanceTypes))} vs {string.Join(", ", instanceTypes.Except(set.InstanceTypes))}");
        foreach (var moduleName in set.ModuleNames.OrderBy(name => name, StringComparer.Ordinal))
            try
            {
                var classes = new Stack<TuningDescriptionClass>(set.GetModule(moduleName)!.Classes);
                while (classes.TryPop(out var @class))
                {
                    if (@class.IsEnum && set.GetEnum(@class.StaticEntriesName)?.StaticEntriesName != @class.StaticEntriesName)
                        problems.Add("enum lookup", @class.StaticEntriesName);
                    foreach (var member in @class.Members)
                        CheckDescription(set, member, $"{@class.StaticEntriesName}/{member.Name}", problems);
                    foreach (var nested in @class.Classes)
                        classes.Push(nested);
                }
            }
            catch (Exception ex)
            {
                problems.Add("module", $"{moduleName}: {ex.GetType().Name}: {ex.Message}");
            }
        foreach (var className in set.FragmentClasses.OrderBy(name => name, StringComparer.Ordinal))
            try
            {
                CheckDescription(set, set.GetFragment(className)!.Description, className, problems);
            }
            catch (Exception ex)
            {
                problems.Add("fragment document", $"{className}: {ex.GetType().Name}: {ex.Message}");
            }
        if (problems.Count > 0)
            Assert.Fail(problems.ToString());
    }
}
