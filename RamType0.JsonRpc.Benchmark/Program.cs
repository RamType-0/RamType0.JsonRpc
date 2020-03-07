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
            /*
            var tests =new Tests();
            for (int i = 0; i < 1000; i++)
            {
                tests.ResolveRequestSingleThreadNew();
            }

            for (int i = 0; i < 1000; i++)
            {
                tests.ResolveRequestParallelNew();
            }

            for (int i = 0; i < 1000; i++)
            {
                tests.ResolveRequestSingleThreadServer();
            }

            for (int i = 0; i < 1000; i++)
            {
                tests.ResolveRequestParallelServer();
            }
            */
            BenchmarkRunner.Run<VSStreamJsonRpc>();
        }
    }
}
