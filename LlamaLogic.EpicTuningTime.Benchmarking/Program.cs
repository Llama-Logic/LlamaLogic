namespace LlamaLogic.EpicTuningTime.Benchmarking;

class Program
{
    static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
