namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningArchiveMediaTests
{
    static readonly TuningArchiveCaptureOptions everything = new() { IncludeSounds = true, IncludeVideos = true };

    static string Describe(TuningArchiveFile archive, TuningArchiveEntry entry) =>
        $"{entry.Type} {entry.Instance:X} {entry.Name} {Encoding.UTF8.GetString(archive.GetContent(entry))}";

    static string[] DescribeAll(TuningArchiveFile archive, TuningArchiveSnapshot snapshot, TuningArchiveResourceKind kind) =>
        [.. archive.GetEntries(snapshot, kind).Where(entry => entry.Type is not ResourceType.AudioConfiguration).Select(entry => Describe(archive, entry)).OrderBy(description => description, StringComparer.Ordinal)];

    [TestMethod]
    public async Task CapturingCanLeaveOutImages()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var result = await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithMedia(directory, "install")), new() { IncludeImages = false });
        Assert.AreEqual(0, result.Images);
        Assert.IsFalse(archive.HasKind(result.Snapshot, TuningArchiveResourceKind.Image));
        Assert.AreEqual(0, archive.GetEntries(result.Snapshot, TuningArchiveResourceKind.Image).Count);
    }

    [TestMethod]
    public async Task CapturingLeavesOutSoundsAndVideosByDefault()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var result = await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithMedia(directory, "install")));
        Assert.AreEqual(0, result.Problems.Count, string.Join(Environment.NewLine, result.Problems));
        Assert.AreEqual(3, result.Images);
        Assert.AreEqual(0, result.Sounds);
        Assert.AreEqual(0, result.Videos);
        Assert.IsTrue(archive.HasKind(result.Snapshot, TuningArchiveResourceKind.Image));
        Assert.IsFalse(archive.HasKind(result.Snapshot, TuningArchiveResourceKind.Sound));
        Assert.IsFalse(archive.HasKind(result.Snapshot, TuningArchiveResourceKind.Video));
        Assert.AreEqual(0, archive.GetEntries(result.Snapshot, TuningArchiveResourceKind.Sound).Count);
    }

    [TestMethod]
    public async Task CapturingStoresReferencedMediaAsTheGameResolvesThem()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var result = await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithMedia(directory, "install")), everything);
        Assert.AreEqual(0, result.Problems.Count, string.Join(Environment.NewLine, result.Problems));
        CollectionAssert.AreEqual
        (
            new[]
            {
                @"DSTImage A1 InGame\UI\Icons\buff_media.png icon, version 1",
                "DSTImage A3  SimData icon",
                "PNGImage A4  PNG icon"
            },
            DescribeAll(archive, result.Snapshot, TuningArchiveResourceKind.Image)
        );
        CollectionAssert.AreEqual
        (
            new[]
            {
                "AudioEffects D2  stream body",
                "AudioVocals D1  sample",
                "AudioVocals D2  stream header"
            },
            DescribeAll(archive, result.Snapshot, TuningArchiveResourceKind.Sound)
        );
        CollectionAssert.AreEqual
        (
            new[] { $"{ResourceType.AudioConfiguration} B1 " + @"InGame\Audio\buff_media.propx", $"{ResourceType.AudioConfiguration} B2 " },
            archive.GetEntries(result.Snapshot, TuningArchiveResourceKind.Sound).Where(entry => entry.Type is ResourceType.AudioConfiguration).Select(entry => $"{entry.Type} {entry.Instance:X} {entry.Name}").OrderBy(description => description, StringComparer.Ordinal).ToArray()
        );
        CollectionAssert.AreEqual(new[] { "AVI C1  video" }, DescribeAll(archive, result.Snapshot, TuningArchiveResourceKind.Video));
        Assert.AreEqual(3, result.Images);
        Assert.AreEqual(5, result.Sounds);
        Assert.AreEqual(1, result.Videos);
    }

    [TestMethod]
    public async Task MediaEntriesResolveTheReferencesTuningUses()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = (await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithMedia(directory, "install")), everything)).Snapshot;
        Assert.AreEqual(ResourceType.DSTImage, archive.GetMediaEntry(snapshot, new ResourceKey(ResourceType.PNGImage, 0, TuningArchiveFixtures.IconInstance))!.Type);
        Assert.AreEqual(ResourceType.PNGImage, archive.GetMediaEntry(snapshot, new ResourceKey(ResourceType.PNGImage, 0, TuningArchiveFixtures.PngIconInstance))!.Type);
        Assert.IsNull(archive.GetMediaEntry(snapshot, new ResourceKey(ResourceType.PNGImage2, 0, TuningArchiveFixtures.DeletedIconInstance)));
        var sound = archive.GetMediaEntry(snapshot, new ResourceKey(ResourceType.AudioConfigurationReference, 0, TuningArchiveFixtures.SoundInstance))!;
        Assert.AreEqual(ResourceType.AudioConfiguration, sound.Type);
        CollectionAssert.AreEqual
        (
            new[] { $"{ResourceType.AudioVocals} D1", $"{ResourceType.AudioVocals} D2", $"{ResourceType.AudioEffects} D2" },
            archive.GetSoundSamples(snapshot, sound).Select(entry => $"{entry.Type} {entry.Instance:X}").ToArray()
        );
        Assert.AreEqual(ResourceType.AVI, archive.GetMediaEntry(snapshot, new ResourceKey(ResourceType.AVI, 0, TuningArchiveFixtures.VideoInstance))!.Type);
        Assert.IsNull(archive.GetMediaEntry(snapshot, new ResourceKey(ResourceType.BuffTuning, 0, 300)));
        Assert.AreEqual(TuningArchiveResourceKind.Image, TuningArchiveFile.GetMediaKind(ResourceType.PNGImage2));
        Assert.AreEqual(TuningArchiveResourceKind.Sound, TuningArchiveFile.GetMediaKind(ResourceType.AudioConfigurationReference));
        Assert.IsNull(TuningArchiveFile.GetMediaKind(ResourceType.AnimationStateMachine));
    }

    [TestMethod]
    public async Task MediaHistoryKeepsEveryVersionAndStoresUnchangedMediaOnce()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var first = await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithMedia(directory, "first")), new() { GameVersion = "1.0.1.1000" });
        var second = await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithMedia(directory, "second", iconVersion: 2)), new() { GameVersion = "1.0.2.1000" });
        Assert.AreEqual(1, second.NewContent);
        var changes = archive.Compare(first.Snapshot, second.Snapshot, TuningArchiveResourceKind.Image);
        Assert.AreEqual($"{TuningArchiveChangeKind.Modified} A1", $"{changes.Single().Kind} {changes.Single().New!.Instance:X}");
        var icon = archive.GetMediaEntry(second.Snapshot, new ResourceKey(ResourceType.PNGImage, 0, TuningArchiveFixtures.IconInstance))!;
        Assert.AreEqual(2, archive.GetHistory(icon).Count);
        Assert.AreEqual("icon, version 1", Encoding.UTF8.GetString(archive.GetContent(archive.GetMediaEntry(first.Snapshot, new ResourceKey(ResourceType.PNGImage, 0, TuningArchiveFixtures.IconInstance))!)));
        Assert.AreEqual("icon, version 2", Encoding.UTF8.GetString(archive.GetContent(icon)));
    }

    [TestMethod]
    public async Task MediaReferencesAreIndexed()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = (await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithMedia(directory, "install")), everything)).Snapshot;
        File.WriteAllBytes(directory.GetPath("1.0.1.1000-TDESC.zip"), TuningArchiveFixtures.WriteDescriptionsZip(TuningArchiveFixtures.MediaDescriptions));
        await archive.ImportTuningDescriptionsAsync(directory.GetPath("1.0.1.1000-TDESC.zip"));
        Assert.AreEqual(1, await archive.IndexReferencesAsync());
        var icon = archive.GetMediaEntry(snapshot, new ResourceKey(ResourceType.PNGImage, 0, TuningArchiveFixtures.IconInstance))!;
        var referrer = archive.GetMediaReferrers(snapshot, icon).Single();
        Assert.AreEqual("Buff_Media", referrer.Source.Name);
        Assert.AreEqual(ResourceType.PNGImage, referrer.TargetType);
        var sound = archive.GetMediaEntry(snapshot, new ResourceKey(ResourceType.AudioConfigurationReference, 0, TuningArchiveFixtures.SoundInstance))!;
        Assert.AreEqual("Buff_Media", archive.GetMediaReferrers(snapshot, sound).Single().Source.Name);
        var video = archive.GetMediaEntry(snapshot, new ResourceKey(ResourceType.AVI, 0, TuningArchiveFixtures.VideoInstance))!;
        Assert.AreEqual("Buff_Media", archive.GetMediaReferrers(snapshot, video).Single().Source.Name);
        var broken = archive.GetBrokenMediaReferences(snapshot).Single();
        Assert.AreEqual(ResourceType.PNGImage2, broken.TargetType);
        Assert.AreEqual(TuningArchiveFixtures.DeletedIconInstance, broken.TargetInstance);
        Assert.AreEqual(0, archive.GetBrokenReferences(snapshot).Count);
    }

    [TestMethod]
    public async Task SnapshotsWithoutMediaDoNotEndMediaHistory()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var first = await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithMedia(directory, "first")), new() { GameVersion = "1.0.1.1000" });
        var second = await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithMedia(directory, "second", iconVersion: 2)), new() { GameVersion = "1.0.2.1000", IncludeImages = false });
        var third = await archive.CaptureAsync(GameInstallation.Open(TuningArchiveFixtures.WriteInstallationWithMedia(directory, "third")), new() { GameVersion = "1.0.3.1000" });
        Assert.IsFalse(archive.HasKind(second.Snapshot, TuningArchiveResourceKind.Image));
        Assert.AreEqual(3, archive.GetEntries(first.Snapshot, TuningArchiveResourceKind.Image).Count);
        Assert.AreEqual(0, archive.Compare(first.Snapshot, third.Snapshot, TuningArchiveResourceKind.Image).Count);
        Assert.AreEqual(0, archive.GetBrokenMediaReferences(second.Snapshot).Count);
    }
}