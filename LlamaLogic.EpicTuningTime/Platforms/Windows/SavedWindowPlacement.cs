namespace LlamaLogic.EpicTuningTime;

/// <summary>
/// Where a window was, in its restored state, on a particular arrangement of displays, and whether it was maximized
/// </summary>
sealed record SavedWindowPlacement
(
    string DisplayConfiguration,
    int Left,
    int Top,
    int Right,
    int Bottom,
    bool Maximized
);