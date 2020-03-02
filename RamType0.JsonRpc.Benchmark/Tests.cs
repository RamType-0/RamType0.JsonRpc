using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using System.IO.Pipelines;
using RamType0.JsonRpc.Server;
using System.Threading.Tasks;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using StreamJsonRpc.Reflection;
using System.IO.Pipes;
using System.IO;
using Utf8Json;

namespace RamType0.JsonRpc.Benchmark
{
    public class Tests
    {
        sealed class EmptyOutput : IResponseOutput
        {
            ValueTask IResponseOutput.ResponseAsync<T>(Server.Server server, T response)
            {
                JsonSerializer.SerializeUnsafe(response);
                return default;
            }
        }

        Server.Server server;
        Internal.RequestResolver resolver;
        byte[] requestJson;
        public Tests()
        {



            var output = new EmptyOutput(); //new PipeMessageOutput<PassThroughWriter>(new PassThroughWriter(), PipeWriter.Create(Console.OpenStandardError()));
            //_ = output.StartOutputAsync();
            server = new Server.Server(output);
            server.Register("Hello", RpcMethodEntry.FromDelegate(new Action(EmptyAction)));

            resolver = new Internal.RequestResolver();
            resolver.TryRegister("Hello", Internal.RpcEntry.FromDelegate(new Action(EmptyAction)));
            requestJson = Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\",\"method\":\"Hello\",\"id\":1}");
            //var stream = new NamedPipeClientStream("StreamJsonRpc");
            //stream.Connect();
            //StreamJsonRpc.JsonRpc.Attach(stream, this);
            
            
            
        }

        static void EmptyAction() { }

        [Benchmark]
        public void ResolveRequestSingleThreadServer()
        {
            for (int i = 0; i < 1000; i++)
            {
                server.ResolveAsync(requestJson);
            }
           
        }
        [Benchmark]
        public void ResolveRequestParallelServer()
        {
            Parallel.For(0, 1000, 
                (_) => 
            {
                server.ResolveAsync(requestJson);
            });
        }

        [Benchmark]
        public void ResolveRequestSingleThreadNew()
        {
            for (int i = 0; i < 1000; i++)
            {
                resolver.Resolve(requestJson);
            }

        }
        [Benchmark]
        public void ResolveRequestParallelNew()
        {
            Parallel.For(0, 1000,
                (_) =>
                {
                    resolver.Resolve(requestJson);
                });
        }

    }
}
