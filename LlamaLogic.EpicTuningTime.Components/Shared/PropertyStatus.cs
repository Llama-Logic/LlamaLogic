namespace LlamaLogic.EpicTuningTime.Components.Shared;

/// <summary>
/// Represents what one way of making a draft's changes does about the change to a tunable
/// </summary>
/// <param name="Target">The short name of the way, such as <c>Py</c></param>
/// <param name="Name">The name of the way, such as <c>Python</c></param>
/// <param name="Severity">Whether that way makes the change</param>
/// <param name="Message">What that way does, or why it cannot</param>
public sealed record PropertyStatus(string Target, string Name, TuningDraftSeverity Severity, string Message);