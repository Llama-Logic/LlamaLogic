namespace LlamaLogic.EpicTuningTime.Components.Shared;

/// <summary>
/// Builds the editor annotations of tuning text as a snapshot understands it, for showing beside other text such as in a diff
/// </summary>
static class TuningAnnotations
{
    /// <summary>
    /// Builds the annotations and navigation targets of the specified tuning <paramref name="text"/> with the tuning descriptions, tuning and strings of the specified <paramref name="snapshot"/>, or returns <see langword="null"/> if the archive has no tuning descriptions or the text can't be analyzed
    /// </summary>
    public static async Task<(IReadOnlyList<EditorAnnotation> annotations, IReadOnlyList<(ResourceType type, ulong instance)?> targets)?> BuildAsync(TuningArchiveSession session, TuningArchiveSnapshot snapshot, string text)
    {
        if (text.Length == 0 || session.Archive is not { } archive || archive.FindTuningDescriptions(snapshot) is not { } descriptions)
            return null;
        var entries = await session.GetEntriesAsync(snapshot);
        var strings = await session.GetStringsAsync(snapshot);
        var animationStateMachines = await session.GetEntriesAsync(snapshot, TuningArchiveResourceKind.AnimationStateMachine);
        var media = await session.GetMediaAsync(snapshot);
        try
        {
            return await Task.Run(() =>
            {
                var analysis = TuningAnalysis.Analyze(text, archive.GetTuningDescriptionCache(descriptions));
                return AnnotationBuilder.Build(text, analysis, entries, strings, session.GetTypeName, animationStateMachines, media);
            });
        }
        catch (Exception ex) when (ex is XmlException or InvalidDataException)
        {
            return null;
        }
    }
}