using RamType0.JsonRpc.Server;
using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using BenchmarkDotNet;
using BenchmarkDotNet.Running;

namespace RamType0.JsonRpc.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Tests>();
        }
    }
}
