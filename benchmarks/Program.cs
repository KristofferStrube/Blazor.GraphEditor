using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using benchmarks;

var config = DefaultConfig.Instance;
var summary = BenchmarkRunner.Run<Benchmarks>(config, args);