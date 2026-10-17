namespace LlamaLogic.Architecture.CorpusTests;

[TestClass]
public class ArchitectureCorpusTests
{
    static void CheckArchitecture(string where, ReadOnlyMemory<byte> bundleData, ProblemLog problems, ref int sections, ref int undecoded)
    {
        try
        {
            var architecture = LotArchitecture.Decode(bundleData);
            sections += architecture.Sections.Count;
            undecoded += architecture.UndecodedRecords.Count;
            var encoded = architecture.Encode();
            if (!encoded.Span.SequenceEqual(bundleData.Span))
            {
                var original = RecordBundle.Decode(bundleData);
                var copy = RecordBundle.Decode(encoded);
                foreach (var record in original.Records)
                    if (!copy.TryGet(record.Key, out var data) || !data.Span.SequenceEqual(record.Data.Span))
                        problems.Add($"architecture section {(LotArchitectureSectionType)(uint)record.Key.Type}", where);
                if (original.Count != copy.Count)
                    problems.Add("architecture record count", $"{where}: {original.Count} became {copy.Count}");
            }
        }
        catch (Exception ex)
        {
            problems.Add("architecture", $"{where}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    static void CheckZoneObjectData(string where, ReadOnlyMemory<byte> zoneObjectData, ProblemLog problems, ref int sections, ref int undecoded)
    {
        try
        {
            var entries = ZoneArchitecture.GetArchitectureData(zoneObjectData);
            for (var i = 0; i < entries.Count; ++i)
                if (!entries[i].IsEmpty)
                    CheckArchitecture($"{where} entry {i}", entries[i], problems, ref sections, ref undecoded);
            if (!ZoneArchitecture.SetArchitectureData(zoneObjectData, entries).Span.SequenceEqual(zoneObjectData.Span))
                problems.Add("zone object data", $"{where}: replacing the architecture with itself changed the message");
        }
        catch (Exception ex)
        {
            problems.Add("zone object data", $"{where}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static string GetDisplayName(MethodInfo methodInfo, object[] data) =>
        Path.Combine(Path.GetFileName(((string)data[0]).TrimEnd(Path.DirectorySeparatorChar)), Path.GetRelativePath((string)data[0], (string)data[1]));

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [DynamicData(nameof(ArchitectureCorpus.Blueprints), typeof(ArchitectureCorpus), DynamicDataDisplayName = nameof(GetDisplayName))]
    public void BlueprintIsConsistent(string root, string path)
    {
        var problems = new ProblemLog();
        var sections = 0;
        var undecoded = 0;
        try
        {
            var data = File.ReadAllBytes(path);
            var blueprint = Blueprint.Decode(data);
            sections = blueprint.Architecture.Sections.Count;
            undecoded = blueprint.Architecture.UndecodedRecords.Count;
            if (!blueprint.Encode().Span.SequenceEqual(data))
                problems.Add("blueprint", "encoding changed the file");
            TestContext.WriteLine($"{blueprint.LotObjects.LotObjects.Count} objects, {blueprint.ObjectDefinitionMappings.Count} object definition mappings, {blueprint.Images.Count} images");
        }
        catch (Exception ex)
        {
            problems.Add("blueprint", $"{ex.GetType().Name}: {ex.Message}");
        }
        TestContext.WriteLine($"{sections} sections, {undecoded} undecoded records");
        if (problems.Count > 0)
            Assert.Fail(problems.ToString());
    }

    [TestMethod]
    [DynamicData(nameof(ArchitectureCorpus.Saves), typeof(ArchitectureCorpus), DynamicDataDisplayName = nameof(GetDisplayName))]
    public void SaveIsConsistent(string root, string path)
    {
        var problems = new ProblemLog();
        var sections = 0;
        var undecoded = 0;
        var zones = 0;
        try
        {
            using var save = new DataBasePackedFile(path, true);
            foreach (var key in save.Keys.Where(key => key.Type is ResourceType.ZoneObjectData))
            {
                ReadOnlyMemory<byte> zoneObjectData;
                try
                {
                    zoneObjectData = save.Get(key);
                }
                catch (Exception ex)
                {
                    problems.Add("read", $"{key}: {ex.GetType().Name}: {ex.Message}");
                    continue;
                }
                ++zones;
                CheckZoneObjectData(key.ToString(), zoneObjectData, problems, ref sections, ref undecoded);
            }
        }
        catch (Exception ex)
        {
            problems.Add("save", $"{ex.GetType().Name}: {ex.Message}");
        }
        TestContext.WriteLine($"{zones} zones, {sections} sections, {undecoded} undecoded records");
        if (problems.Count > 0)
            Assert.Fail(problems.ToString());
    }

    [TestMethod]
    [DynamicData(nameof(ArchitectureCorpus.Worlds), typeof(ArchitectureCorpus), DynamicDataDisplayName = nameof(GetDisplayName))]
    public void WorldIsConsistent(string root, string path)
    {
        var problems = new ProblemLog();
        var sections = 0;
        var undecoded = 0;
        var lotObjectResources = 0;
        try
        {
            using var world = new DataBasePackedFile(path, true);
            foreach (var key in world.Keys)
            {
                try
                {
                    switch (key.Type)
                    {
                        case ResourceType.WorldLotArchitecture:
                            CheckArchitecture(key.ToString(), world.GetModel<WorldLotArchitectureModel>(key).Bundle.Encode(), problems, ref sections, ref undecoded);
                            break;
                        case ResourceType.WorldLotObjects:
                            ++lotObjectResources;
                            var data = world.Get(key);
                            if (!WorldLotObjectsModel.Decode(data).Encode().Span.SequenceEqual(data.Span))
                                problems.Add("lot objects", $"{key}: encoding changed the resource");
                            break;
                        case ResourceType.ZoneObjectData:
                            CheckZoneObjectData(key.ToString(), world.Get(key), problems, ref sections, ref undecoded);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    problems.Add("resource", $"{key}: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            problems.Add("world", $"{ex.GetType().Name}: {ex.Message}");
        }
        TestContext.WriteLine($"{sections} sections, {undecoded} undecoded records, {lotObjectResources} lot object resources");
        if (problems.Count > 0)
            Assert.Fail(problems.ToString());
    }
}
