using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace SemanticTypeModel.Benchmarks;

internal static class Program
{
    private static void Main()
    {
        _ = BenchmarkRunner.Run<BaselineBenchmark>();
    }
}

/// <summary>
/// Provides a minimal benchmark to verify benchmark infrastructure is wired correctly.
/// </summary>
[MemoryDiagnoser]
public class BaselineBenchmark
{
    private readonly int baselineValue = 42;

    /// <summary>
    /// Returns the baseline value.
    /// </summary>
    [Benchmark]
    public int Baseline()
    {
        return baselineValue;
    }
}
