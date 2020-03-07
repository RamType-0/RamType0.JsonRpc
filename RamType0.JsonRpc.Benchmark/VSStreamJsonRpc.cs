using System;
using System.Collections.Generic;
using System.Text;

namespace RamType0.JsonRpc.Benchmark
{
    using BenchmarkDotNet.Attributes;
    using Nerdbank.Streams;
    using StreamJsonRpc;
    using System.IO.Pipelines;
    using System.Threading.Tasks;
    using Marshaling;
    using RamType0.JsonRpc.Internal;
    using Utf8Json;
    //[ShortRunJob]
    public class VSStreamJsonRpc
    {
        private readonly JsonRpc SJRclientRpc;
        private readonly JsonRpc SJRserverRpc;

        private readonly RpcMethodHandle rpcMethodHandle;
        
        public VSStreamJsonRpc()
        {
            var duplex = FullDuplexStream.CreatePipePair();
            this.SJRclientRpc = new JsonRpc(new HeaderDelimitedMessageHandler(duplex.Item1, new JsonMessageFormatter()));
            this.SJRclientRpc.StartListening();
            this.SJRserverRpc = new JsonRpc(new HeaderDelimitedMessageHandler(duplex.Item2, new JsonMessageFormatter()));
            this.SJRserverRpc.AddLocalRpcTarget(new Server());
            this.SJRserverRpc.StartListening();
            
            var (aPipes,bPipes) = FullDuplexStream.CreatePipePair();
            var domainA = new Marshaling.PipeIOHeaderDelimitedRpcDomain(aPipes.Input,aPipes.Output);
            var domainB = new Marshaling.PipeIOHeaderDelimitedRpcDomain(bPipes.Input,bPipes.Output);
            _ = domainA.StartAsync();
            _ = domainB.StartAsync();
            var entry = RpcMethodEntry.FromDelegate<Action>(new Server().NoOp);
            domainA.AddMethod(nameof(Server.NoOp), entry);
            rpcMethodHandle = new RpcMethodHandle(domainB, nameof(Server.NoOp), JsonSerializer.DefaultResolver);


        }

        /// <summary>
        /// Workaround https://github.com/dotnet/BenchmarkDotNet/issues/837.
        /// </summary>
        [GlobalSetup]
        public void Workaround() => this.SJRInvokeAsync();

        [Benchmark]
        public Task SJRInvokeAsync() => this.SJRclientRpc.InvokeAsync(nameof(Server.NoOp), Array.Empty<object>());

        public struct Empty { }
        [Benchmark]
        public ValueTask InvokeAsync() => rpcMethodHandle.RequestAsync(new Empty());
        private class Server
        {
            public void NoOp()
            {
            }
        }
    }
}
