namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningCommenterTests
{
    static async Task<TuningArchiveSnapshot> ImportAsync(TuningArchiveFile archive, TemporaryDirectory directory, bool withDescriptions)
    {
        var result = await archive.ImportFolderAsync(directory.WriteFiles("extracted", TuningArchiveFixtures.First), "1.0.1.1000");
        Assert.AreEqual(0, result.Problems.Count, string.Join(Environment.NewLine, result.Problems));
        if (withDescriptions)
        {
            File.WriteAllBytes(directory.GetPath("descriptions.zip"), TuningArchiveFixtures.WriteDescriptionsZip());
            await archive.ImportTuningDescriptionsAsync(directory.GetPath("descriptions.zip"), "1.0.1.1000");
        }
        return result.Snapshot;
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CommentsAreTheOnesTheExportWrites(bool withDescriptions)
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await ImportAsync(archive, directory, withDescriptions);
        await archive.ExportAsync(snapshot, directory.GetPath("out"));
        var commenter = TuningCommenter.Create(archive, snapshot);
        foreach (var entry in archive.GetEntries(snapshot, TuningArchiveResourceKind.Tuning))
        {
            var exported = Encoding.UTF8.GetString(File.ReadAllBytes(Directory.EnumerateFiles(directory.GetPath("out"), $"*_{entry.Instance:X16}.xml", SearchOption.AllDirectories).Single())).TrimStart('﻿').Replace("\r\n", "\n", StringComparison.Ordinal);
            Assert.AreEqual(exported, commenter.Comment(archive.GetText(entry)), entry.Name);
        }
    }

    [TestMethod]
    public async Task CommentsCanBeLeftOut()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await ImportAsync(archive, directory, true);
        var commenter = TuningCommenter.Create(archive, snapshot, false, false);
        foreach (var entry in archive.GetEntries(snapshot, TuningArchiveResourceKind.Tuning))
            Assert.AreEqual(archive.GetText(entry), commenter.Comment(archive.GetText(entry)), entry.Name);
    }

    [TestMethod]
    public async Task FragmentsAreCommentedAsXmlExtractorCommentsThem()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var commenter = TuningCommenter.Create(archive, await ImportAsync(archive, directory, true));
        Assert.AreEqual("<T n=\"mood_type\">100<!--Mood: Mood_Happy--></T>", commenter.CommentFragment("<T n=\"mood_type\">100</T>"));
        Assert.AreEqual("<T n=\"buff_name\">0x0000ABCD<!--String: \"Happy\"--></T>", commenter.CommentFragment("<T n=\"buff_name\">0x0000ABCD</T>"));
        Assert.AreEqual("<T n=\"mood_weight\">2</T>", commenter.CommentFragment("<T n=\"mood_weight\">2</T>"));
    }

    [TestMethod]
    public async Task PythonDraftsCommentTheirTuning()
    {
        using var directory = new TemporaryDirectory();
        using var archive = TuningArchiveFile.Open(directory.GetPath("Tuning.tuningarchive"));
        var snapshot = await ImportAsync(archive, directory, false);
        using var set = TuningArchiveFixtures.OpenDescriptions();
        var entry = archive.GetEntries(snapshot, TuningArchiveResourceKind.Tuning).Single(candidate => candidate.Name == "Buff_Sad");
        var originalText = archive.GetText(entry);
        var draftText = TuningXml.GetCanonicalText(TuningXml.Parse(originalText.Replace(">2<", ">3<", StringComparison.Ordinal).Replace("0x0000ABCE", "0x0000ABCD", StringComparison.Ordinal)));
        var result = TuningPythonDraft.Generate([new(entry, TuningAnalysis.Analyze(originalText, set), TuningAnalysis.Analyze(draftText, set))], "1.0.1.1000", "draft.log", TuningCommenter.Create(archive, snapshot));
        StringAssert.Contains(result.Source, "'<T n=\"buff_name\">0x0000ABCE<!--String: \"Sad\"--></T>', '<T n=\"buff_name\">0x0000ABCD<!--String: \"Happy\"--></T>'");
        StringAssert.Contains(result.Source, "'<T n=\"mood_weight\">2</T>', '<T n=\"mood_weight\">3</T>'");
    }
}