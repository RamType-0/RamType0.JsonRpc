#nullable enable
using NUnit.Framework;
using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using Nerdbank.Streams;
using StreamJsonRpc.Protocol;
namespace RamType0.JsonRpc.Tests
{
    using IO;
    using System.Threading;
    using JsonRpc = StreamJsonRpc.JsonRpc;
    
    public class StreamJsonRpcAPICompatibillity
    {
        private readonly JsonRpc SJRclient;
        private readonly JsonRpc SJRserver;
        private readonly PipeIOHeaderDelimitedRpcDomain serverDomain,clientDomain;
        private readonly RpcMethodHandle<Empty> noOpMethodHandle;
        private readonly RpcMethodHandle<MulParams, int> mulMethodHandle;
        private readonly RpcMethodHandle<Empty> cancelMethodHandle;
        public StreamJsonRpcAPICompatibillity()
        {
            var (sjrClientPipes,serverDomainPipes) = FullDuplexStream.CreatePipePair();
            this.SJRclient = new JsonRpc(new HeaderDelimitedMessageHandler(sjrClientPipes, new JsonMessageFormatter()));
            this.SJRclient.StartListening();
            serverDomain = new PipeIOHeaderDelimitedRpcDomain(serverDomainPipes.Input,serverDomainPipes.Output);
            _ = serverDomain.StartAsync();

            var server = new Server();
            var entry = RpcMethodEntry.FromDelegate<Action>(server.NoOp);
            serverDomain.AddMethod(nameof(Server.NoOp), entry);
            var mulEntry = RpcMethodEntry.FromDelegate<Mul>(server.Mul);
            serverDomain.AddMethod(nameof(Server.Mul), mulEntry);

            var cancelEntry = RpcMethodEntry.FromDelegate<CancelAsync>(server.CancelAsync);
            serverDomain.AddMethod(nameof(Server.CancelAsync), cancelEntry);
            var (sjrServerPipes,clientDomainPipes) = FullDuplexStream.CreatePipePair();
            this.SJRserver = new JsonRpc(new HeaderDelimitedMessageHandler(sjrServerPipes, new JsonMessageFormatter()));
            this.SJRserver.AddLocalRpcTarget(new Server());
            this.SJRserver.StartListening();
            clientDomain = new PipeIOHeaderDelimitedRpcDomain(clientDomainPipes.Input, clientDomainPipes.Output);
            _ = clientDomain.StartAsync();
            
            noOpMethodHandle = new RpcMethodHandle<Empty>(clientDomain, nameof(Server.NoOp));
            mulMethodHandle = new RpcMethodHandle<MulParams, int>(clientDomain, nameof(Server.Mul));
            cancelMethodHandle = new RpcMethodHandle<Empty>(clientDomain, nameof(Server.SJRCancelAsync));
        }


        [Test]
        public Task SJRInvokeNoOpAsync() => this.SJRclient.InvokeAsync(nameof(Server.NoOp), Array.Empty<object>());

        public struct Empty { }
        public struct MulParams
        {
            public int a,b;
        }

        public delegate int Mul(int a, int b);
        public delegate Task CancelAsync([RpcID] ID? id);
        [Test]
        public ValueTask InvokeNoOpAsync() => noOpMethodHandle.RequestAsync(new Empty());
        [Test]
        public async ValueTask SJRInvokeMulAsync()
        {
            Assert.AreEqual(114*514,await SJRclient.InvokeAsync<int>(nameof(Server.Mul), 114, 514));
        }

        [Test]
        public async ValueTask InvokeMulAsync()
        {
            Assert.AreEqual(114*514,await mulMethodHandle.RequestAsync(new MulParams() { a = 114, b = 514 }));
        }
        [Test]
        public void SJRCancel()
        {
            
            Assert.ThrowsAsync<TaskCanceledException>( async() =>
            {
                var cts = new CancellationTokenSource();

                var task = SJRclient.InvokeWithCancellationAsync(nameof(Server.CancelAsync), Array.Empty<object>(), cts.Token);
                cts.Cancel();
                await task;
            });
        }
        [Test]
        public void Cancel()
        {
            Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                var cts = new CancellationTokenSource();

                var task = cancelMethodHandle.RequestAsync(new Empty(), cts.Token);
                cts.Cancel();
                await task;
            });
        }

        private class Server
        {
            public void NoOp()
            {
            }

            public int Mul(int a,int b)
            {
                return a * b;
            }

            public Task SJRCancelAsync(CancellationToken cancellationToken)
            {
                return Task.Delay(114514, cancellationToken);
            }

            public Task CancelAsync(ID? id)
            {
                var cancellationToken = id.AsRpcCancellationToken();
                return Task.Delay(114514, cancellationToken);
            }

            
        }
    }

}