using BenchmarkDotNet.Running;
using DddDotNet.CrossCuttingConcerns.Benchmarks;

_ = BenchmarkRunner.Run<UriPathBenchmarks>();