using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Columns;

namespace SSDI.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        // Run all benchmarks with detailed statistics
        var config = DefaultConfig.Instance
            .AddExporter(MarkdownExporter.GitHub)
            .WithOptions(ConfigOptions.JoinSummary)
            .AddJob(Job.Default.WithWarmupCount(2).WithIterationCount(10))
            // Add Min/Max columns
            .AddColumn(StatisticColumn.Min)
            .AddColumn(StatisticColumn.Max)
            // Add percentile columns
            .AddColumn(StatisticColumn.P90)
            .AddColumn(StatisticColumn.P95)
            // Add Median for reference
            .AddColumn(StatisticColumn.Median);

        // Run specific benchmark or all
        if (args.Length > 0)
        {
            switch (args[0].ToLower())
            {
                case "transient":
                    BenchmarkRunner.Run<TransientBenchmark>(config);
                    break;
                case "singleton":
                    BenchmarkRunner.Run<SingletonBenchmark>(config);
                    break;
                case "combined":
                    BenchmarkRunner.Run<CombinedBenchmark>(config);
                    break;
                case "complex":
                    BenchmarkRunner.Run<ComplexBenchmark>(config);
                    break;
                case "prepare":
                    BenchmarkRunner.Run<PrepareBenchmark>(config);
                    break;
                case "unregister":
                    BenchmarkRunner.Run<UnregisterBenchmark>(config);
                    break;
                default:
                    RunAll(config);
                    break;
            }
        }
        else
        {
            RunAll(config);
        }
    }

    static void RunAll(IConfig config)
    {
        BenchmarkRunner.Run<PrepareBenchmark>(config);
        BenchmarkRunner.Run<SingletonBenchmark>(config);
        BenchmarkRunner.Run<TransientBenchmark>(config);
        BenchmarkRunner.Run<CombinedBenchmark>(config);
        BenchmarkRunner.Run<ComplexBenchmark>(config);
        BenchmarkRunner.Run<UnregisterBenchmark>(config);
    }
}
