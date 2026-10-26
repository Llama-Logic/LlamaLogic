using BlazorMonaco;
using BlazorMonaco.Editor;

namespace LlamaLogic.LlamaPad;

static class Extensions
{
    static List<Selection> ComputeCursorState(List<ValidEditOperation> inverseEditOperations)
    {
        // We assume a single operation for simplicity, handling multiple can be added if needed.
        var lastOperation = inverseEditOperations.LastOrDefault();

        if (lastOperation == null)
        {
            // No operations, return an empty list of selections
            return [];
        }

        // Create a new selection where the cursor should end up after the operation
        var newSelection = new Selection
        {
            StartLineNumber = lastOperation.Range.StartLineNumber,
            StartColumn = lastOperation.Range.StartColumn + lastOperation.Text.Length,
            EndLineNumber = lastOperation.Range.StartLineNumber,
            EndColumn = lastOperation.Range.StartColumn + lastOperation.Text.Length
        };

        return [newSelection];
    }

    public static Task ExecuteEditAsync(this StandaloneCodeEditor editor, string source, BlazorMonaco.Range range, string replacementText) =>
        editor.ExecuteEdits
        (
            source,
            [
                new()
                {
                    ForceMoveMarkers = true,
                    Range = range,
                    Text = replacementText
                }
            ],
            ComputeCursorState
        );

    public static async Task<(BlazorMonaco.Range? range, string? text)> GetEditorSelectionAsync(this StandaloneCodeEditor? editor)
    {
        if (editor is null)
            return (null, null);
        var selection = await editor.GetSelection();
        if (selection is null)
            return (null, null);
        var range = new BlazorMonaco.Range
        (
            selection.StartLineNumber,
            selection.StartColumn,
            selection.EndLineNumber,
            selection.EndColumn
        );
        var model = await editor.GetModel();
        if (model is null)
            return (null, null);
        return
        (
            range,
            await model.GetValueInRange(range, EndOfLinePreference.TextDefined)
        );
    }
}
