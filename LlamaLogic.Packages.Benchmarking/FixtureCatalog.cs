namespace LlamaLogic.Packages.Benchmarking;

static class FixtureCatalog
{
    public static IEnumerable<Fixture> All
    {
        get
        {
            var mods = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Electronic Arts", "The Sims 4", "Mods");
            var game = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EA Games", "The Sims 4");
            return new Fixture[]
            {
                new("TS3", FindRepositoryFile(Path.Combine("LlamaLogic.Packages.UnitTests", "TS3.package")) ?? string.Empty),
                new("MaxisSP84Client", Path.Combine(game, "SP84", "ClientFullBuild0.package")),
                new("BasementalDrugs", Path.Combine(mods, "Basemental - Drugs", "Basemental Drugs.package")),
                new("WickedWhimsTuning", Path.Combine(mods, "TURBODRIVER - WickedWhims", "TURBODRIVER_WickedWhims_Tuning.package"))
            }.Where(fixture => File.Exists(fixture.SourcePath));
        }
    }

    static string FindRepositoryFile(string relativePath)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }

    public static (string temporaryDirectory, string pristinePath) Stage(Fixture fixture)
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), "LlamaLogic.Packages.Benchmarking", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(temporaryDirectory);
        var pristinePath = Path.Combine(temporaryDirectory, "pristine.package");
        File.Copy(fixture.SourcePath, pristinePath);
        return (temporaryDirectory, pristinePath);
    }

    public static void Unstage(string temporaryDirectory)
    {
        if (Directory.Exists(temporaryDirectory))
            Directory.Delete(temporaryDirectory, true);
    }
}
