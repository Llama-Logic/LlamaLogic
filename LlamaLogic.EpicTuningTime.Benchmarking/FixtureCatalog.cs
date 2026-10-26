namespace LlamaLogic.EpicTuningTime.Benchmarking;

static class FixtureCatalog
{
    public static IEnumerable<Fixture> All
    {
        get
        {
            if (FindArchive() is null)
                return [];
            return new Fixture[]
            {
                new("SimSpawner", "sims.sim_spawner"),
                new("TeaSetPourTeaPottery", "teaSet_PourTea_Pottery"),
                new("UiTuning", "ui.ui_tuning"),
                new("GroupSingHappyBirthday", "groupSing_LeaderSingInteraction_HappyBirthday")
            };
        }
    }

    static string FindArchive()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Directory.Exists(documents)
            ? new DirectoryInfo(documents).GetFiles("*.tuningarchive").OrderByDescending(file => file.LastWriteTimeUtc).FirstOrDefault()?.FullName
            : null;
    }

    public static OpenedFixture Open(Fixture fixture)
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), "LlamaLogic.EpicTuningTime.Benchmarking", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(temporaryDirectory);
        var archivePath = Path.Combine(temporaryDirectory, "pristine.tuningarchive");
        File.Copy(FindArchive() ?? throw new FileNotFoundException("There is no tuning archive in the documents folder"), archivePath);
        return new OpenedFixture(temporaryDirectory, TuningArchiveFile.Open(archivePath), fixture.TuningName);
    }

    public static void Unstage(string temporaryDirectory)
    {
        if (Directory.Exists(temporaryDirectory))
            Directory.Delete(temporaryDirectory, true);
    }
}
