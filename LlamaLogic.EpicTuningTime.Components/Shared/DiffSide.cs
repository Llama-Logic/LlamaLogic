namespace LlamaLogic.EpicTuningTime.Components.Shared;

/// <summary>
/// Receives what the creator does with the annotations of one side of a diff
/// </summary>
sealed class DiffSide(Func<int, Task> annotationActivated, Func<int, string?> getHover)
{
    /// <summary>
    /// Gets the Markdown to show when the creator hovers the annotation at the specified <paramref name="index"/>
    /// </summary>
    [JSInvokable]
    public string? GetHover(int index) =>
        getHover(index);

    /// <summary>
    /// Called when the creator activates the annotation at the specified <paramref name="index"/>
    /// </summary>
    [JSInvokable]
    public Task OnAnnotationActivated(int index) =>
        annotationActivated(index);

    /// <summary>
    /// Called when the creator moves the cursor to another line
    /// </summary>
    [JSInvokable]
    public Task OnCursorLineChanged(int line) =>
        Task.CompletedTask;
}