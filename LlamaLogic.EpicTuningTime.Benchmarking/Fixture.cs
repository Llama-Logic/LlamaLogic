namespace LlamaLogic.EpicTuningTime.Benchmarking;

public sealed record Fixture(string Name, string TuningName)
{
    public override string ToString() =>
        Name;
}
