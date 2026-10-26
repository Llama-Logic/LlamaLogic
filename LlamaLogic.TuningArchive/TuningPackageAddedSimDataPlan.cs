namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents what a set of fixes does to the SimData of tuning a package adds, and to the tuning
/// </summary>
/// <param name="Fixed">The problems the fixes correct</param>
/// <param name="Remaining">The problems left after the fixes</param>
/// <param name="Message">What the fixes do, or why they do nothing, for the creator</param>
public sealed record TuningPackageAddedSimDataPlan(TuningPackageAddedSimDataProblems Fixed, TuningPackageAddedSimDataProblems Remaining, string Message)
{
    internal SimDataModel? SimData { get; init; }

    internal ReadOnlyMemory<byte>? Tuning { get; init; }

    /// <summary>
    /// Gets whether the fixes change the SimData
    /// </summary>
    public bool ChangesSimData =>
        SimData is not null;

    /// <summary>
    /// Gets whether the fixes change the tuning
    /// </summary>
    public bool ChangesTuning =>
        Tuning is not null;
}