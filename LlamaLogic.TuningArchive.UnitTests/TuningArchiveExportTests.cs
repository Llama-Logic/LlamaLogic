namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningArchiveExportTests
{
    static readonly byte[] byteOrderMark = [0xEF, 0xBB, 0xBF];

    static async Task<TuningArchiveSnapshot> ImportAsync(TuningArchiveFile archive, TemporaryDirectory directory, IReadOnlyDictionary<string, string> files)
    {
        var result = await archive.ImportFolderAsync(directory.WriteFiles("extracted", files), "1.0.1.1000");
        Assert.AreEqual(0, result.Problems.Count, string.Join(Environment.NewLine, result.Problems));
        return result.Snapshot;
    }

    static string Read(TemporaryDirectory directory, string relativePath)
    {
        var bytes = File.ReadAllBytes(directory.GetPath(relativePath));
        CollectionAssert.AreEqual(byteOrderMark, bytes.Take(3).ToArray());
        return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
    }

    static string Crlf(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal);

    [TestMethod]
    public async Task AnimationStateMachinesAreWrittenToAsmJazzFolders()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await ImportAsync(archive, directory, new Dictionary<string, string>(TuningArchiveFixtures.First)
        {
            [$"EP01/AsmJazz/{TuningArchiveFixtures.AnimationStateMachineFileName}"] = TuningArchiveFixtures.AnimationStateMachine("llamalogic_Sit", 1)
        });
        var result = await archive.ExportAsync(snapshot, directory.GetPath("out"), new() { IncludeAnimationStateMachines = true, Naming = TuningArchiveExportNaming.Sims4Studio });
        Assert.AreEqual(4, result.Files);
        Assert.IsTrue(File.Exists(directory.GetPath("out/EP01/AsmJazz/02d5df13!00000000!8b12a7c35f0e4d61.llamalogic_Sit.AsmJazz.xml")));
        StringAssert.Contains(File.ReadAllText(directory.GetPath("out/XML File Index.txt")), $"{TuningArchiveFixtures.SitAnimationStateMachineInstance}\tllamalogic_Sit\tJAZZ\tASM\t{Path.Combine("EP01", "AsmJazz", "02d5df13!00000000!8b12a7c35f0e4d61.llamalogic_Sit.AsmJazz.xml")}\r\n");
        StringAssert.Contains(File.ReadAllText(directory.GetPath("out/Reference - All XML Names.txt")), "0x8B12A7C35F0E4D61\tJAZZ\tllamalogic_Sit\r\n");
    }

    [TestMethod]
    public async Task AttributeOrderCanPutTheTypeFirst()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await ImportAsync(archive, directory, new Dictionary<string, string>
        {
            ["BG/Buff_Typed.xml"] = """<I c="Buff" i="buff" m="buffs.buff" n="Buff_Typed" s="204"><V n="test" t="enabled"><T n="value">1</T></V></I>"""
        });
        await archive.ExportAsync(snapshot, directory.GetPath("out"), new() { AttributeOrder = TuningArchiveExportAttributeOrder.TypeFirst });
        StringAssert.Contains(Read(directory, "out/BG/buff/S4_6017E896_00000000_00000000000000CC.xml"), "<V t=\"enabled\" n=\"test\">");
    }

    [TestMethod]
    public async Task CommentsComeFromTheTuningDescriptionsWhenThereAreSome()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await ImportAsync(archive, directory, TuningArchiveFixtures.First);
        File.WriteAllBytes(directory.GetPath("descriptions.zip"), TuningArchiveFixtures.WriteDescriptionsZip());
        await archive.ImportTuningDescriptionsAsync(directory.GetPath("descriptions.zip"), "1.0.1.1000");
        var result = await archive.ExportAsync(snapshot, directory.GetPath("out"));
        Assert.IsTrue(result.UsedTuningDescriptions);
        Assert.AreEqual
        (
            Crlf
            (
                """
                <?xml version="1.0" encoding="utf-8"?>
                <I c="Mood" i="mood" m="buffs.mood" n="Mood_Happy" s="100">
                  <L n="buffs">
                    <U>
                      <T n="buff_type">200<!--Buff: Buff_Happy--></T>
                      <E n="age">ADULT</E>
                    </U>
                    <U>
                      <T n="buff_type">999</T>
                    </U>
                  </L>
                </I>
                """
            ),
            Read(directory, "out/BG/mood/S4_BA7B60B8_00000000_0000000000000064.xml")
        );
        Assert.AreEqual
        (
            Crlf
            (
                """
                <?xml version="1.0" encoding="utf-8"?>
                <I c="Buff" i="buff" m="buffs.buff" n="Buff_Sad" s="201">
                  <T n="buff_name">0x0000ABCE<!--String: "Sad"--></T>
                  <T n="mood_type">Mood_Happy</T>
                  <T n="mood_weight">2</T>
                </I>
                """
            ),
            Read(directory, "out/EP01/buff/S4_6017E896_00000003_00000000000000C9.xml")
        );
    }

    [TestMethod]
    public async Task CommentsTakeEveryMatchingNumberWithoutTuningDescriptions()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await ImportAsync(archive, directory, TuningArchiveFixtures.First);
        var result = await archive.ExportAsync(snapshot, directory.GetPath("out"));
        Assert.IsFalse(result.UsedTuningDescriptions);
        Assert.AreEqual(0, result.Problems.Count, string.Join(Environment.NewLine, result.Problems));
        Assert.AreEqual
        (
            Crlf
            (
                """
                <?xml version="1.0" encoding="utf-8"?>
                <I c="Buff" i="buff" m="buffs.buff" n="Buff_Happy" s="200">
                  <T n="buff_name">0x0000ABCD<!--String: "Happy"--></T>
                  <T n="mood_type">100<!--Mood: Mood_Happy--></T>
                  <T n="mood_weight">1</T>
                </I>
                """
            ),
            Read(directory, "out/BG/buff/S4_6017E896_00000000_00000000000000C8.xml")
        );
    }

    [TestMethod]
    public async Task ExistingFilesAreOverwrittenRenamedOrSkipped()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await ImportAsync(archive, directory, TuningArchiveFixtures.First);
        var path = directory.WriteFile("out/BG/buff/S4_6017E896_00000000_00000000000000C8.xml", "old");
        var skipped = await archive.ExportAsync(snapshot, directory.GetPath("out"), new() { ExistingFiles = TuningArchiveExportExistingFiles.Skip });
        Assert.AreEqual(2, skipped.Files);
        Assert.AreEqual(1, skipped.Skipped);
        Assert.AreEqual("old", File.ReadAllText(path));
        Assert.AreEqual(3, File.ReadAllLines(directory.GetPath("out/XML File Index.txt")).Length);
        var renamed = await archive.ExportAsync(snapshot, directory.GetPath("out"), new() { ExistingFiles = TuningArchiveExportExistingFiles.Rename });
        Assert.AreEqual(3, renamed.Files);
        Assert.AreEqual(3, renamed.Renamed);
        Assert.AreEqual("old", File.ReadAllText(directory.GetPath("out/BG/buff/S4_6017E896_00000000_00000000000000C8 (Copy 1).xml")));
        StringAssert.Contains(Read(directory, "out/BG/buff/S4_6017E896_00000000_00000000000000C8.xml"), "Buff_Happy");
        var overwritten = await archive.ExportAsync(snapshot, directory.GetPath("out"));
        Assert.AreEqual(3, overwritten.Files);
        Assert.AreEqual(0, overwritten.Renamed);
        Assert.IsFalse(File.Exists(directory.GetPath("out/BG/buff/S4_6017E896_00000000_00000000000000C8 (Copy 2).xml")));
        Assert.IsTrue(File.Exists(directory.GetPath("out/XML Cross Reference (Copy 1).dat")));
    }

    [TestMethod]
    [DataRow(TuningArchiveExportNaming.TypeGroupInstance, "S4_6017E896_00000003_00000000000000C9.xml")]
    [DataRow(TuningArchiveExportNaming.TypeGroupInstanceName, "S4_6017E896_00000003_00000000000000C9_Buff_Sad.xml")]
    [DataRow(TuningArchiveExportNaming.Name, "Buff_Sad.xml")]
    [DataRow(TuningArchiveExportNaming.Native, "0x00000003!0x00000000000000c9.0x6017e896")]
    [DataRow(TuningArchiveExportNaming.Sims4Studio, "6017e896!00000003!00000000000000c9.Buff_Sad.buff.xml")]
    public async Task FilesAreNamedTheWayXmlExtractorNamesThem(TuningArchiveExportNaming naming, string fileName)
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await ImportAsync(archive, directory, TuningArchiveFixtures.First);
        var result = await archive.ExportAsync(snapshot, directory.GetPath("out"), new() { Naming = naming });
        Assert.AreEqual(3, result.Files);
        Assert.AreEqual(3u, result.PackIdentifiers["EP01"]);
        Assert.IsTrue(File.Exists(directory.GetPath($"out/EP01/buff/{fileName}")), string.Join(Environment.NewLine, Directory.EnumerateFiles(directory.GetPath("out"), "*", SearchOption.AllDirectories)));
    }

    [TestMethod]
    public async Task ModuleTuningIsNamedLikeXmlExtractorNamesIt()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await ImportAsync(archive, directory, new Dictionary<string, string>
        {
            ["BG/some_module.xml"] = """<M n="some.module" s="300"><T n="X">100</T></M>"""
        });
        await archive.ExportAsync(snapshot, directory.GetPath("out"), new() { Naming = TuningArchiveExportNaming.Sims4Studio });
        Assert.IsTrue(File.Exists(directory.GetPath("out/BG/tun/03b33ddf!00000000!000000000000012c.some.module..xml")));
        StringAssert.Contains(File.ReadAllText(directory.GetPath("out/Reference - All XML Names.txt")), "0x000000000000012C\t\tsome.module\r\n");
    }

    [TestMethod]
    public async Task PackIdentifiersComeFromTheStampOfThePacksSimData()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await ImportAsync(archive, directory, new Dictionary<string, string>
        {
            ["GP12/Buff_Glad.xml"] = """<I c="Buff" i="buff" m="buffs.buff" n="Buff_Glad" s="202" />""",
            [$"GP12/{TuningArchiveFixtures.SimDataFileName}"] = TuningArchiveFixtures.SimData(0x40, 1),
            ["SP120/Buff_Sad.xml"] = """<I c="Buff" i="buff" m="buffs.buff" n="Buff_Sad" s="201" />"""
        });
        var result = await archive.ExportAsync(snapshot, directory.GetPath("out"));
        Assert.AreEqual(0x40u, result.PackIdentifiers["GP12"]);
        Assert.AreEqual(0u, result.PackIdentifiers["SP120"]);
        StringAssert.StartsWith(result.Problems.Single(), "SP120: its identifier could not be found");
        Assert.IsTrue(File.Exists(directory.GetPath("out/GP12/buff/S4_6017E896_00000040_00000000000000CA.xml")));
    }

    [TestMethod]
    [DataRow(TuningArchiveExportNaming.Sims4Studio, "545ac67a!00000000!00000000000000c8.Thing.SimData.xml", "545ac67a!00000000!0000000000001234.Thing.SimData.xml")]
    [DataRow(TuningArchiveExportNaming.Name, "Thing.SimData.xml", "Thing.SimData.xml")]
    public async Task SimDataIsWrittenBesideItsTuning(TuningArchiveExportNaming naming, string besideTuning, string withoutTuning)
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await ImportAsync(archive, directory, new Dictionary<string, string>(TuningArchiveFixtures.First)
        {
            ["BG/545AC67A!00000000!00000000000000C8.Buff_Happy.SimData.xml"] = TuningArchiveFixtures.SimData(0, 1),
            [$"BG/{TuningArchiveFixtures.SimDataFileName}"] = TuningArchiveFixtures.SimData(0, 2)
        });
        var result = await archive.ExportAsync(snapshot, directory.GetPath("out"), new() { IncludeSimData = true, Naming = naming });
        Assert.AreEqual(5, result.Files);
        Assert.AreEqual(0, result.Problems.Count, string.Join(Environment.NewLine, result.Problems));
        Assert.IsTrue(File.Exists(directory.GetPath($"out/BG/buff/{besideTuning}")), string.Join(Environment.NewLine, Directory.EnumerateFiles(directory.GetPath("out"), "*", SearchOption.AllDirectories)));
        Assert.IsTrue(File.Exists(directory.GetPath($"out/BG/SimData/{withoutTuning}")));
    }

    [TestMethod]
    public async Task ReferenceFilesAndTheFileIndexAreWritten()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await ImportAsync(archive, directory, TuningArchiveFixtures.First);
        await archive.ExportAsync(snapshot, directory.GetPath("out"));
        Assert.AreEqual("0x0000ABCD\tHappy\r\n0x0000ABCE\tSad\r\n", File.ReadAllText(directory.GetPath("out/Reference - All Strings.txt")));
        Assert.AreEqual("0x0000000000000064\tMood\tMood_Happy\r\n0x00000000000000C8\tBuff\tBuff_Happy\r\n0x00000000000000C9\tBuff\tBuff_Sad\r\n", File.ReadAllText(directory.GetPath("out/Reference - All XML Names.txt")));
        Assert.AreEqual
        (
            $"200\tBuff_Happy\tBuff\tbuff\t{Path.Combine("BG", "buff", "S4_6017E896_00000000_00000000000000C8.xml")}\r\n"
            + $"100\tMood_Happy\tMood\tmood\t{Path.Combine("BG", "mood", "S4_BA7B60B8_00000000_0000000000000064.xml")}\r\n"
            + $"201\tBuff_Sad\tBuff\tbuff\t{Path.Combine("EP01", "buff", "S4_6017E896_00000003_00000000000000C9.xml")}\r\n",
            File.ReadAllText(directory.GetPath("out/XML File Index.txt"))
        );
        using var crossReferences = new BinaryReader(File.OpenRead(directory.GetPath("out/XML Cross Reference.dat")));
        var magic = crossReferences.ReadUInt64();
        Assert.AreEqual(0x4DDC4353UL, magic & 0xFFFFFFFFUL);
        Assert.AreEqual(1UL, magic >> 56);
        var references = new Dictionary<ulong, (ulong[] by, ulong[] to)>();
        while (crossReferences.BaseStream.Position < crossReferences.BaseStream.Length)
        {
            var instance = crossReferences.ReadUInt64();
            var by = Enumerable.Range(0, crossReferences.ReadInt32()).Select(_ => crossReferences.ReadUInt64()).ToArray();
            var to = Enumerable.Range(0, crossReferences.ReadInt32()).Select(_ => crossReferences.ReadUInt64()).ToArray();
            references.Add(instance, (by, to));
        }
        CollectionAssert.AreEqual(new ulong[] { TuningArchiveFixtures.MoodHappyInstance, TuningArchiveFixtures.BuffHappyInstance, TuningArchiveFixtures.BuffSadInstance }, references.Keys.ToArray());
        CollectionAssert.AreEqual(new ulong[] { TuningArchiveFixtures.MoodHappyInstance }, references[TuningArchiveFixtures.BuffHappyInstance].to);
        CollectionAssert.AreEqual(new ulong[] { TuningArchiveFixtures.MoodHappyInstance }, references[TuningArchiveFixtures.BuffHappyInstance].by);
        CollectionAssert.AreEqual(new ulong[] { TuningArchiveFixtures.BuffHappyInstance }, references[TuningArchiveFixtures.MoodHappyInstance].to);
        CollectionAssert.AreEqual(new ulong[] { TuningArchiveFixtures.BuffHappyInstance }, references[TuningArchiveFixtures.MoodHappyInstance].by);
        Assert.AreEqual(0, references[TuningArchiveFixtures.BuffSadInstance].by.Length + references[TuningArchiveFixtures.BuffSadInstance].to.Length);
    }
}