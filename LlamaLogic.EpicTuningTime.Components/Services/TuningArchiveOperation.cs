namespace LlamaLogic.EpicTuningTime.Components.Services;

/// <summary>
/// Represents a long-running operation the session is performing
/// </summary>
sealed class TuningArchiveOperation
{
    static readonly TimeSpan minimumEstimationTime = TimeSpan.FromSeconds(5);

    internal TuningArchiveOperation(string name, CancellationTokenSource cancellation)
    {
        Name = name;
        Cancellation = cancellation;
        StartedAt = DateTimeOffset.UtcNow;
        stageStartedAt = StartedAt;
    }

    int stageStartCompleted;
    DateTimeOffset stageStartedAt;

    internal CancellationTokenSource Cancellation { get; }

    /// <summary>
    /// Gets how long the operation has been running
    /// </summary>
    public TimeSpan Elapsed =>
        DateTimeOffset.UtcNow - StartedAt;

    /// <summary>
    /// Gets the estimated time until the operation reaches the total it is counting toward, if enough has been done to estimate
    /// </summary>
    public TimeSpan? EstimatedRemaining
    {
        get
        {
            if (Progress is not { Total: > 0 } progress)
                return null;
            var stageElapsed = DateTimeOffset.UtcNow - stageStartedAt;
            var stageCompleted = progress.Completed - stageStartCompleted;
            if (stageElapsed < minimumEstimationTime || stageCompleted <= 0 || stageCompleted * 100L < progress.Total)
                return null;
            return TimeSpan.FromTicks((long)(stageElapsed.Ticks * ((double)(progress.Total - progress.Completed) / stageCompleted)));
        }
    }

    /// <summary>
    /// Gets the name of the operation
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the most recent progress the operation reported
    /// </summary>
    public TuningArchiveProgress? Progress { get; private set; }

    /// <summary>
    /// Gets when the operation started
    /// </summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>
    /// Requests that the operation stop
    /// </summary>
    public void Cancel() =>
        Cancellation.Cancel();

    internal void Report(TuningArchiveProgress progress)
    {
        if (Progress is not { } previous || previous.Total != progress.Total || progress.Completed < previous.Completed)
        {
            stageStartedAt = DateTimeOffset.UtcNow;
            stageStartCompleted = progress.Completed;
        }
        Progress = progress;
    }
}