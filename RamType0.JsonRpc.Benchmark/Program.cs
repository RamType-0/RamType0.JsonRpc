using BenchmarkDotNet.Running;
using System.Threading.Tasks;

namespace RamType0.JsonRpc.Benchmark
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var rpc = new VSStreamJsonRpc();
            for (int i = 0; i < 1000000; i++)
            {
                await rpc.InvokeAsync();
            }
            
            //BenchmarkRunner.Run<VSStreamJsonRpc>();
        }
    }
}
