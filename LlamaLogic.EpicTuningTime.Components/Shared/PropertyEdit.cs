namespace LlamaLogic.EpicTuningTime.Components.Shared;

/// <summary>
/// Represents an edit a creator asks for in the property tree
/// </summary>
/// <param name="Kind">What to do</param>
/// <param name="Node">The node to do it to</param>
/// <param name="Value">The new value, for <see cref="PropertyEditKind.SetValue"/>, or the option, for <see cref="PropertyEditKind.SetOption"/></param>
public sealed record PropertyEdit(PropertyEditKind Kind, PropertyNode Node, string? Value = null);