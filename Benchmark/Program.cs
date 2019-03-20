using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Benchmark {
    class Program {
        static void Main(string[] args) => BenchmarkRunner.Run<Benchmark>();
    }
}
