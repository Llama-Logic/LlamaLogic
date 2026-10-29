namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningArchiveFileTests
{
    static readonly string[] firstToSecond =
    [
        "Added Buff_Glad",
        "Modified Buff_Happy",
        "Moved Mood_Happy",
        "Removed Buff_Sad"
    ];

    static readonly string[] secondToFirst =
    [
        "Added Buff_Sad",
        "Modified Buff_Happy",
        "Moved Mood_Happy",
        "Removed Buff_Glad"
    ];

    static async Task<TuningArchiveSnapshot> ImportAsync(TuningArchiveFile archive, TemporaryDirectory directory, string folder, IReadOnlyDictionary<string, string> files, string gameVersion)
    {
        var result = await archive.ImportFolderAsync(directory.WriteFiles(folder, files), gameVersion);
        Assert.AreEqual(0, result.Problems.Count, string.Join(Environment.NewLine, result.Problems));
        return result.Snapshot;
    }

    [TestMethod]
    public async Task AnimationStateMachineReferencesAreIndexed()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await ImportAsync(archive, directory, "first", new Dictionary<string, string>
        {
            ["BG/Buff_Seated.xml"] =
                $"""
                <?xml version="1.0" encoding="utf-8"?>
                <I c="Buff" i="buff" m="buffs.buff" n="Buff_Seated" s="203">
                  <T n="animation">{new ResourceKey(ResourceType.AnimationStateMachine, 0, TuningArchiveFixtures.SitAnimationStateMachineInstance)}</T>
                </I>
                """,
            [$"BG/AsmJazz/{TuningArchiveFixtures.AnimationStateMachineFileName}"] = TuningArchiveFixtures.AnimationStateMachine("llamalogic_Sit", 1)
        }, "1.0.1.1000");
        var descriptions = new Dictionary<string, string>(TuningArchiveFixtures.Descriptions)
        {
            ["Buffs/Descriptions/Buff.tdesc"] =
                """
                <?xml version="1.0" encoding="iso-8859-1"?>

                <TuningRoot>
                    <Instance instance_type="buff" class="Buff" module="buffs.buff" muid="7F0C39A1D1E2B3C4" description="A buff." instance_needs_reference="True" instance_subclasses_only="False" path="Buffs\Descriptions" use_guid_for_reference="True">
                        <Tunable type="ResourceKey" name="animation" class="TunableResourceKey" default="None" display="Animation" muid="8888888888888888" resource_types="0x2d5df13" Deprecated="False" filter="0" group="General" />
                    </Instance>
                </TuningRoot>
                """
        };
        File.WriteAllBytes(directory.GetPath("descriptions.zip"), TuningArchiveFixtures.WriteDescriptionsZip(descriptions));
        await archive.ImportTuningDescriptionsAsync(directory.GetPath("descriptions.zip"), "1.0.1.1000");
        Assert.AreEqual(1, await archive.IndexReferencesAsync());
        var reference = archive.GetReferrers(snapshot, ResourceType.AnimationStateMachine, TuningArchiveFixtures.SitAnimationStateMachineInstance).Single();
        Assert.AreEqual("Buff_Seated", reference.Source.Name);
        Assert.AreEqual(0, archive.GetBrokenReferences(snapshot).Count);
    }

    [TestMethod]
    public async Task AnArchiveIsCreatedAndReopened()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.GetPath("Tuning.tuningarchive");
        using (var archive = TuningArchiveFile.Open(path))
            await ImportAsync(archive, directory, "first", TuningArchiveFixtures.First, "1.0.1.1000");
        using (var reopened = TuningArchiveFile.Open(path))
        {
            var snapshot = reopened.GetSnapshots().Single();
            Assert.AreEqual("1.0.1.1000", snapshot.GameVersion);
            Assert.AreEqual(TuningArchiveSnapshotSource.TuningFolder, snapshot.Source);
            Assert.AreEqual(3, reopened.GetEntries(snapshot).Count);
        }
    }

    [TestMethod]
    public async Task BrokenReferencesAreFound()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var first = await ImportAsync(archive, directory, "first", TuningArchiveFixtures.First, "1.0.1.1000");
        File.WriteAllBytes(directory.GetPath("descriptions.zip"), TuningArchiveFixtures.WriteDescriptionsZip());
        await archive.ImportTuningDescriptionsAsync(directory.GetPath("descriptions.zip"), "1.0.1.1000");
        await archive.IndexReferencesAsync();
        var broken = archive.GetBrokenReferences(first).Single();
        Assert.AreEqual("Mood_Happy", broken.Source.Name);
        Assert.AreEqual(ResourceType.BuffTuning, broken.TargetType);
        Assert.AreEqual(TuningArchiveFixtures.MissingBuffInstance, broken.TargetInstance);
    }

    [TestMethod]
    public async Task ComparingFindsEveryKindOfChange()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var first = await ImportAsync(archive, directory, "first", TuningArchiveFixtures.First, "1.0.1.1000");
        var second = await ImportAsync(archive, directory, "second", TuningArchiveFixtures.Second, "1.0.2.1000");
        CollectionAssert.AreEqual(firstToSecond, TuningArchiveFixtures.Describe(archive.Compare(first, second)).ToArray());
        CollectionAssert.AreEqual(secondToFirst, TuningArchiveFixtures.Describe(archive.Compare(second, first)).ToArray());
        Assert.AreEqual(0, archive.Compare(first, first).Count);
        var moved = archive.Compare(first, second).Single(change => change.Kind is TuningArchiveChangeKind.Moved);
        Assert.AreEqual("BG", moved.Old!.Pack);
        Assert.AreEqual("EP02", moved.New!.Pack);
        Assert.IsTrue(moved.Old.HasSameContentAs(moved.New));
    }

    [TestMethod]
    public async Task CapturedAnimationStateMachinesTakeTheDeltaOverInstalledFiles()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var result = await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithAnimationStateMachines(directory)));
        Assert.AreEqual(0, result.Problems.Count, string.Join(Environment.NewLine, result.Problems));
        Assert.AreEqual(2, result.AnimationStateMachines);
        Assert.IsTrue(result.Snapshot.Kinds.Contains(TuningArchiveResourceKind.AnimationStateMachine));
        CollectionAssert.AreEqual
        (
            new[] { "BG llamalogic_Sit", "EP01 llamalogic_Stand" },
            archive.GetEntries(result.Snapshot, TuningArchiveResourceKind.AnimationStateMachine).Select(entry => $"{entry.Pack} {entry.Name}").OrderBy(entry => entry, StringComparer.Ordinal).ToArray()
        );
        var stand = archive.GetEntry(result.Snapshot, TuningArchiveResourceKind.AnimationStateMachine, ResourceType.AnimationStateMachine, TuningArchiveFixtures.PatchedAnimationStateMachineInstance)!;
        Assert.AreEqual
        (
            """
            <?xml version="1.0" encoding="utf-8"?>
            <ASM name="llamalogic_Stand" type="Jazz">
              <State name="entry" type="entry" />
              <State name="exit" priority="2" type="exit" />
            </ASM>
            """.Replace("\r\n", "\n", StringComparison.Ordinal),
            archive.GetText(stand)
        );
    }

    [TestMethod]
    public async Task CapturedStringsMergeEveryStringTable()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var result = await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithStrings(directory)));
        Assert.AreEqual(0, result.Problems.Count, string.Join(Environment.NewLine, result.Problems));
        var strings = archive.GetStrings(result.Snapshot);
        Assert.AreEqual(4, strings.Count);
        Assert.AreEqual("Base game", strings[TuningArchiveFixtures.BaseGameStringKey]);
        Assert.AreEqual("New", strings[TuningArchiveFixtures.PatchedStringKey]);
        Assert.AreEqual("Installed only", strings[TuningArchiveFixtures.InstalledOnlyStringKey]);
        Assert.AreEqual("Delta only", strings[TuningArchiveFixtures.DeltaOnlyStringKey]);
    }

    [TestMethod]
    public async Task CapturingCanLeaveOutAnimationStateMachines()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var result = await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithAnimationStateMachines(directory)), new() { IncludeAnimationStateMachines = false });
        Assert.AreEqual(0, result.AnimationStateMachines);
        Assert.IsFalse(archive.HasKind(result.Snapshot, TuningArchiveResourceKind.AnimationStateMachine));
        Assert.AreEqual(0, archive.GetEntries(result.Snapshot, TuningArchiveResourceKind.AnimationStateMachine).Count);
    }

    [TestMethod]
    public async Task CapturingHonorsDeleteRecordsInTheDeltaOverInstalledFiles()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var result = await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithSimData(directory)));
        Assert.AreEqual(0, result.Problems.Count, string.Join(Environment.NewLine, result.Problems));
        Assert.AreEqual("BG", archive.GetEntries(result.Snapshot, TuningArchiveResourceKind.SimData).Single().Pack);
    }

    [TestMethod]
    public async Task ComparingIncludesUnchangedResourcesOnlyWhenAsked()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var first = await ImportAsync(archive, directory, "first", TuningArchiveFixtures.First, "1.0.1.1000");
        var second = await ImportAsync(archive, directory, "second", TuningArchiveFixtures.Second, "1.0.2.1000");
        CollectionAssert.AreEqual(firstToSecond, TuningArchiveFixtures.Describe(archive.Compare(first, second, includeUnchanged: true)).ToArray());
        CollectionAssert.AreEqual
        (
            new[]
            {
                "Unchanged Buff_Happy",
                "Unchanged Buff_Sad",
                "Unchanged Mood_Happy"
            },
            TuningArchiveFixtures.Describe(archive.Compare(first, first, includeUnchanged: true)).ToArray()
        );
        CollectionAssert.AreEqual
        (
            new[]
            {
                "Unchanged 0000ABCD Happy Happy",
                "Unchanged 0000ABCE Sad Sad"
            },
            TuningArchiveFixtures.Describe(archive.CompareStrings(first, first, includeUnchanged: true)).ToArray()
        );
    }

    [TestMethod]
    public async Task ComparingStringsFindsEveryKindOfChange()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var first = await ImportAsync(archive, directory, "first", TuningArchiveFixtures.First, "1.0.1.1000");
        var second = await ImportAsync(archive, directory, "second", TuningArchiveFixtures.Second, "1.0.2.1000");
        Assert.AreEqual("ENG_US", first.Locale);
        Assert.AreEqual("Happy", archive.GetStrings(first)[0xABCD]);
        CollectionAssert.AreEqual
        (
            new[]
            {
                "Added 0000ABCF  Glad",
                "Modified 0000ABCD Happy Happy!",
                "Removed 0000ABCE Sad "
            },
            TuningArchiveFixtures.Describe(archive.CompareStrings(first, second)).ToArray()
        );
    }

    [TestMethod]
    public async Task ComparingStringsWithExtractedStringsReportsNoMoves()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var extracted = await ImportAsync(archive, directory, "extracted", new Dictionary<string, string>
        {
            ["Reference - All Strings.txt"] = $"0x{TuningArchiveFixtures.BaseGameStringKey:X8}\tBase game\n0x{TuningArchiveFixtures.PatchedStringKey:X8}\tNew\n0x{TuningArchiveFixtures.InstalledOnlyStringKey:X8}\tInstalled only\n0x{TuningArchiveFixtures.DeltaOnlyStringKey:X8}\tDelta only\n"
        }, "1.0.0.1000");
        var captured = (await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithStrings(directory)))).Snapshot;
        Assert.AreEqual(0, archive.CompareStrings(extracted, captured).Count);
        Assert.AreEqual(4, archive.CompareStrings(extracted, captured, includeUnchanged: true).Count(change => change.Kind is TuningArchiveChangeKind.Unchanged));
    }

    [TestMethod]
    public async Task DeletingASnapshotRejoinsTheVersionsAroundIt()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var first = await ImportAsync(archive, directory, "first", TuningArchiveFixtures.First, "1.0.1.1000");
        var second = await ImportAsync(archive, directory, "second", TuningArchiveFixtures.Second, "1.0.2.1000");
        var third = await ImportAsync(archive, directory, "third", TuningArchiveFixtures.First, "1.0.3.1000");
        var buffHappy = archive.GetEntry(third, TuningArchiveResourceKind.Tuning, ResourceType.BuffTuning, TuningArchiveFixtures.BuffHappyInstance)!;
        Assert.AreEqual(3, archive.GetHistory(buffHappy).Count);
        await archive.DeleteSnapshotAsync(second);
        CollectionAssert.AreEqual(new[] { first.Id, third.Id }, archive.GetSnapshots().Select(snapshot => snapshot.Id).ToArray());
        first = archive.GetSnapshots()[0];
        third = archive.GetSnapshots()[1];
        Assert.AreEqual(0, archive.Compare(first, third).Count);
        Assert.AreEqual(0, archive.CompareStrings(first, third).Count);
        buffHappy = archive.GetEntry(third, TuningArchiveResourceKind.Tuning, ResourceType.BuffTuning, TuningArchiveFixtures.BuffHappyInstance)!;
        var history = archive.GetHistory(buffHappy).Single();
        CollectionAssert.AreEqual(new[] { first.Id, third.Id }, history.Snapshots.Select(snapshot => snapshot.Id).ToArray());
        var buffSad = archive.GetEntry(third, TuningArchiveResourceKind.Tuning, ResourceType.BuffTuning, TuningArchiveFixtures.BuffSadInstance)!;
        Assert.AreEqual(1, archive.GetHistory(buffSad).Count);
        Assert.IsNull(archive.GetEntry(third, TuningArchiveResourceKind.Tuning, ResourceType.BuffTuning, TuningArchiveFixtures.BuffGladInstance));
    }

    [TestMethod]
    public async Task ExactTuningDescriptionsAreOnlyThoseOfTheSnapshotsGameVersion()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var first = await ImportAsync(archive, directory, "first", TuningArchiveFixtures.First, "1.0.1.1000");
        var second = await ImportAsync(archive, directory, "second", TuningArchiveFixtures.Second, "1.0.2.1000");
        File.WriteAllBytes(directory.GetPath("1.0.1.1000-TDESC.zip"), TuningArchiveFixtures.WriteDescriptionsZip());
        await archive.ImportTuningDescriptionsAsync(directory.GetPath("1.0.1.1000-TDESC.zip"));
        Assert.AreEqual("1.0.1.1000", archive.FindTuningDescriptions(first, exactGameVersion: true)!.GameVersion);
        Assert.IsNull(archive.FindTuningDescriptions(second, exactGameVersion: true));
        Assert.AreEqual("1.0.1.1000", archive.FindTuningDescriptions(second, exactGameVersion: false)!.GameVersion);
        Assert.AreEqual("1.0.1.1000", archive.FindTuningDescriptions(second)!.GameVersion);
    }

    [TestMethod]
    public async Task ImportedAnimationStateMachinesAreKeyedByTheirFileNames()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var result = await archive.ImportFolderAsync(directory.WriteFiles("first", new Dictionary<string, string>(TuningArchiveFixtures.First)
        {
            [$"EP01/AsmJazz/{TuningArchiveFixtures.AnimationStateMachineFileName}"] = TuningArchiveFixtures.AnimationStateMachine("llamalogic_Sit", 1)
        }), "1.0.1.1000");
        Assert.AreEqual(0, result.Problems.Count, string.Join(Environment.NewLine, result.Problems));
        Assert.AreEqual(3, result.Tunings);
        Assert.AreEqual(1, result.AnimationStateMachines);
        var sit = archive.GetEntries(result.Snapshot, TuningArchiveResourceKind.AnimationStateMachine).Single();
        Assert.AreEqual(ResourceType.AnimationStateMachine, sit.Type);
        Assert.AreEqual(0u, sit.Group);
        Assert.AreEqual(TuningArchiveFixtures.SitAnimationStateMachineInstance, sit.Instance);
        Assert.AreEqual("llamalogic_Sit", sit.Name);
        Assert.AreEqual("EP01", sit.Pack);
    }

    [TestMethod]
    public async Task ImportedTuningIsCanonical()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var first = await ImportAsync(archive, directory, "first", TuningArchiveFixtures.First, "1.0.1.1000");
        var buffHappy = archive.GetEntry(first, TuningArchiveResourceKind.Tuning, ResourceType.BuffTuning, TuningArchiveFixtures.BuffHappyInstance)!;
        Assert.AreEqual("Buff_Happy", buffHappy.Name);
        Assert.AreEqual("BG", buffHappy.Pack);
        Assert.AreEqual("Buff", buffHappy.Class);
        Assert.AreEqual("buffs.buff", buffHappy.Module);
        Assert.AreEqual(0u, buffHappy.Group);
        Assert.AreEqual
        (
            """
            <?xml version="1.0" encoding="utf-8"?>
            <I c="Buff" i="buff" m="buffs.buff" n="Buff_Happy" s="200">
              <T n="buff_name">0x0000ABCD</T>
              <T n="mood_type">100</T>
              <T n="mood_weight">1</T>
            </I>
            """.Replace("\r\n", "\n", StringComparison.Ordinal),
            archive.GetText(buffHappy)
        );
        var buffSad = archive.GetEntry(first, TuningArchiveResourceKind.Tuning, ResourceType.BuffTuning, TuningArchiveFixtures.BuffSadInstance)!;
        Assert.AreEqual("EP01", buffSad.Pack);
        CollectionAssert.AreEqual(new[] { "BG", "EP01" }, first.Packs.Select(pack => pack.Code).ToArray());
        Assert.AreEqual("buff", archive.GetTuningTypeNames()[ResourceType.BuffTuning]);
    }

    [TestMethod]
    public async Task ImportingTheSameTuningAgainStoresNoNewContent()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var first = await archive.ImportFolderAsync(directory.WriteFiles("first", TuningArchiveFixtures.First), "1.0.1.1000");
        Assert.AreEqual(3, first.Tunings);
        Assert.AreEqual(2, first.Strings);
        Assert.IsTrue(first.NewContent > 0);
        var again = await archive.ImportFolderAsync(directory.GetPath("first"), "1.0.2.1000");
        Assert.AreEqual(3, again.Tunings);
        Assert.AreEqual(0, again.NewContent);
        Assert.AreEqual(0, archive.Compare(first.Snapshot, again.Snapshot).Count);
    }

    [TestMethod]
    public async Task ReferencesAreIndexed()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var first = await ImportAsync(archive, directory, "first", TuningArchiveFixtures.First, "1.0.1.1000");
        Assert.AreEqual((0, 3), archive.GetReferenceIndexStatus());
        Assert.AreEqual(0, await archive.IndexReferencesAsync());
        File.WriteAllBytes(directory.GetPath("1.0.1.1000-TDESC.zip"), TuningArchiveFixtures.WriteDescriptionsZip());
        var descriptions = await archive.ImportTuningDescriptionsAsync(directory.GetPath("1.0.1.1000-TDESC.zip"));
        Assert.AreEqual("1.0.1.1000", descriptions.GameVersion);
        Assert.AreEqual(3, await archive.IndexReferencesAsync());
        Assert.AreEqual((3, 3), archive.GetReferenceIndexStatus());
        Assert.AreEqual(0, await archive.IndexReferencesAsync());
        CollectionAssert.AreEqual
        (
            new[] { "Buff_Happy", "Buff_Sad" },
            archive.GetReferrers(first, ResourceType.MoodTuning, TuningArchiveFixtures.MoodHappyInstance).Select(reference => reference.Source.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray()
        );
        CollectionAssert.AreEqual
        (
            new[] { "Mood_Happy" },
            archive.GetReferrers(first, ResourceType.BuffTuning, TuningArchiveFixtures.BuffHappyInstance).Select(reference => reference.Source.Name).ToArray()
        );
        Assert.AreEqual(0, archive.GetReferrers(first, ResourceType.BuffTuning, TuningArchiveFixtures.BuffSadInstance).Count);
    }

    [TestMethod]
    public async Task StringsAreIndexedAsReferences()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var first = await ImportAsync(archive, directory, "first", TuningArchiveFixtures.First, "1.0.1.1000");
        File.WriteAllBytes(directory.GetPath("1.0.1.1000-TDESC.zip"), TuningArchiveFixtures.WriteDescriptionsZip());
        await archive.ImportTuningDescriptionsAsync(directory.GetPath("1.0.1.1000-TDESC.zip"));
        await archive.IndexReferencesAsync();
        var sad = archive.GetStringReferrers(first, [0x0000ABCE]).Single();
        Assert.AreEqual("Buff_Sad", sad.Source.Name);
        Assert.AreEqual(ResourceType.StringTable, sad.TargetType);
        Assert.AreEqual(0x0000ABCEUL, sad.TargetInstance);
        CollectionAssert.AreEqual
        (
            new[] { "Buff_Happy", "Buff_Sad" },
            archive.GetStringReferrers(first, [0x0000ABCD, 0x0000ABCE, 0x0000ABCF]).Select(reference => reference.Source.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray()
        );
        Assert.AreEqual(0, archive.GetStringReferrers(first, [0x0000ABCF]).Count);
        Assert.IsFalse(archive.GetBrokenReferences(first).Any(reference => reference.TargetType == ResourceType.StringTable));
    }

    [TestMethod]
    public async Task SimDataThatDiffersBetweenPacksIsAProblem()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var folder = directory.WriteFiles("simdata", new Dictionary<string, string>
        {
            [$"BG/{TuningArchiveFixtures.SimDataFileName}"] = TuningArchiveFixtures.SimData(0, 1),
            [$"GP12/{TuningArchiveFixtures.SimDataFileName}"] = TuningArchiveFixtures.SimData(63, 2)
        });
        var result = await archive.ImportFolderAsync(folder, "1.0.1.1000");
        Assert.AreEqual(1, result.SimData);
        StringAssert.StartsWith(result.Problems.Single(), "GP12 and BG both contain SimData 545ac67a:00000000:0000000000001234 (Thing) with different content");
        Assert.AreEqual("GP12", archive.GetEntries(result.Snapshot, TuningArchiveResourceKind.SimData).Single().Pack);
    }

    [TestMethod]
    public async Task SimDataThatDiffersOnlyInItsPackIsNotAProblem()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var folder = directory.WriteFiles("simdata", new Dictionary<string, string>
        {
            [$"BG/{TuningArchiveFixtures.SimDataFileName}"] = TuningArchiveFixtures.SimData(0, 1),
            [$"GP12/{TuningArchiveFixtures.SimDataFileName}"] = TuningArchiveFixtures.SimData(63, 1)
        });
        var result = await archive.ImportFolderAsync(folder, "1.0.1.1000");
        Assert.AreEqual(1, result.SimData);
        Assert.AreEqual(0, result.Problems.Count, string.Join(Environment.NewLine, result.Problems));
    }

    [TestMethod]
    public async Task SnapshotsAreOrderedByGameVersionWhateverTheOrderOfImport()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        await ImportAsync(archive, directory, "first", TuningArchiveFixtures.First, "1.0.1.1000");
        await ImportAsync(archive, directory, "third", TuningArchiveFixtures.First, "1.0.3.1000");
        await ImportAsync(archive, directory, "second", TuningArchiveFixtures.Second, "1.0.2.1000");
        var snapshots = archive.GetSnapshots();
        CollectionAssert.AreEqual(new[] { "1.0.1.1000", "1.0.2.1000", "1.0.3.1000" }, snapshots.Select(snapshot => snapshot.GameVersion).ToArray());
        CollectionAssert.AreEqual(firstToSecond, TuningArchiveFixtures.Describe(archive.Compare(snapshots[0], snapshots[1])).ToArray());
        CollectionAssert.AreEqual(secondToFirst, TuningArchiveFixtures.Describe(archive.Compare(snapshots[1], snapshots[2])).ToArray());
        Assert.AreEqual(0, archive.Compare(snapshots[0], snapshots[2]).Count);
        var buffHappy = archive.GetEntry(snapshots[2], TuningArchiveResourceKind.Tuning, ResourceType.BuffTuning, TuningArchiveFixtures.BuffHappyInstance)!;
        var history = archive.GetHistory(buffHappy);
        CollectionAssert.AreEqual
        (
            new[] { "1.0.1.1000", "1.0.2.1000", "1.0.3.1000" },
            history.Select(item => item.Snapshots.Single().GameVersion).ToArray()
        );
        CollectionAssert.AreEqual(new[] { 3, 3, 3 }, snapshots.Select(snapshot => archive.GetEntries(snapshot).Count).ToArray());
    }

    [TestMethod]
    public async Task SnapshotsAreUpdated()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var first = await ImportAsync(archive, directory, "first", TuningArchiveFixtures.First, "1.0.1.1000");
        var updated = await archive.UpdateSnapshotAsync(first, "Before the patch", "Captured for testing");
        Assert.AreEqual(first.Id, updated.Id);
        Assert.AreEqual("Before the patch", updated.Label);
        Assert.AreEqual("Captured for testing", archive.GetSnapshots().Single().Notes);
    }

    [TestMethod]
    public async Task SnapshotsKnowWhetherTheyAreCapturesOfAnInstallation()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var root = TuningArchiveFixtures.WriteInstallationWithSimData(directory);
        directory.WriteFile("The Sims 4/EP01/Version.ini", "[Version]\npackversion = 74.0\n");
        var captured = (await archive.CaptureAsync(GameInstallation.Open(root))).Snapshot;
        var imported = await ImportAsync(archive, directory, "first", TuningArchiveFixtures.First, "1.0.1.1000");
        Assert.IsTrue(captured.IsCaptureOf(GameInstallation.Open(root)));
        Assert.IsFalse(imported.IsCaptureOf(GameInstallation.Open(root)));
        directory.WriteFile("The Sims 4/EP01/Version.ini", "[Version]\npackversion = 75.0\n");
        Assert.IsFalse(captured.IsCaptureOf(GameInstallation.Open(root)));
        directory.WriteFile("The Sims 4/EP01/Version.ini", "[Version]\npackversion = 74.0\n");
        directory.WriteFile("The Sims 4/Game/Bin/Default.ini", "[Version]\ngameversion = 1.0.2.1000\n");
        Assert.IsFalse(captured.IsCaptureOf(GameInstallation.Open(root)));
        directory.WriteFile("The Sims 4/Game/Bin/Default.ini", "[Version]\ngameversion = 1.0.1.1000\n");
        Assert.IsTrue(captured.IsCaptureOf(GameInstallation.Open(root)));
        TuningArchiveFixtures.WritePackage(directory, "The Sims 4/EP02/SimulationFullBuild0.package");
        Assert.IsFalse(captured.IsCaptureOf(GameInstallation.Open(root)));
    }

    [TestMethod]
    public async Task UnreadableAnimationStateMachinesAreProblems()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var result = await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithAnimationStateMachines(directory, includeUnreadable: true)));
        Assert.AreEqual(2, result.AnimationStateMachines);
        StringAssert.StartsWith(result.Problems.Single(), $"BG: animation state machine {new ResourceKey(ResourceType.AnimationStateMachine, 0, TuningArchiveFixtures.UnreadableAnimationStateMachineInstance)} could not be read");
    }
}