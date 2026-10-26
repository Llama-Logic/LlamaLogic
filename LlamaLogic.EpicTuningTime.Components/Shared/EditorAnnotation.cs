namespace LlamaLogic.EpicTuningTime.Components.Shared;

/// <summary>
/// Represents a span of an editor's text that has a hover, an inline hint, or a navigation target
/// </summary>
/// <param name="Line">The 1-based line of the span</param>
/// <param name="Start">The 1-based column at which the span starts</param>
/// <param name="End">The 1-based column at which the span ends (exclusive)</param>
/// <param name="Hover">Builds the Markdown to show when the span is hovered, if it has any (only when it is hovered)</param>
/// <param name="Hint">The text to show after the span, if any</param>
/// <param name="HintClass">The CSS class of the hint, if not the default</param>
/// <param name="Navigable">Whether activating the span navigates somewhere</param>
public sealed record EditorAnnotation(int Line, int Start, int End, [property: JsonIgnore] Func<string>? Hover, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Hint, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? HintClass, bool Navigable)
{
    /// <summary>
    /// Gets whether the span has a hover
    /// </summary>
    public bool HasHover =>
        Hover is not null;
}