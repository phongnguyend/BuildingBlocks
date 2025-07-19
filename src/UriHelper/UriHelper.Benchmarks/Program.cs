using BenchmarkDotNet.Running;
using UriHelper.Benchmarks;

_ = BenchmarkRunner.Run<UriPathBenchmarks>();