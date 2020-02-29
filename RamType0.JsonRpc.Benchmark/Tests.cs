using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using System.IO.Pipelines;
using RamType0.JsonRpc.Server;
using System.Threading.Tasks;

namespace RamType0.JsonRpc.Benchmark
{
    public class Tests
    {
        Server.Server server;
        public Tests()
        {
            var output = new PipeResponseOutput<PassThroughWriter>(new PassThroughWriter(), PipeWriter.Create(Console.OpenStandardError()));
            _ = output.StartOutputAsync();
            server = new Server.Server(output);
            server.Register("Hello", RpcMethodEntry.FromDelegate(new Func<string>(() => "World!")));
            
        }
        [Benchmark]
        public void ResolveRequestSingleThread()
        {
            for (int i = 0; i < 1000; i++)
            {
                server.ResolveAsync("{\"jsonrpc\":\"2.0\"," +
                "\"params\":[]," +
                "\"method\":\"Hello\"," +
                "\"id\":1" +
                "}");
            }
           
        }
        [Benchmark]
        public void ResolveRequestParallel()
        {
            Parallel.For(0, 1000, 
                (_) => 
            {
                server.ResolveAsync("{\"jsonrpc\":\"2.0\"," +
"\"params\":[]," +
"\"method\":\"Hello\"," +
"\"id\":1" +
"}");
            });
        }
    }
}
