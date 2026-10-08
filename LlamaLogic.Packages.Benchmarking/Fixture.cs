namespace LlamaLogic.Packages.Benchmarking;

public sealed record Fixture(string Name, string SourcePath)
{
    public override string ToString() =>
        Name;
}
