#nullable enable
using NUnit.Framework;
using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;

namespace RamType0.JsonRpc.Tests
{
    using IO;
    using System.Threading;
    public class Tests
    {
        RpcDomain RpcDomain { get; set; } = null!;
        PipeIOHeaderDelimitedRpcDomain DomainA { get; set; } = null!;
        PipeIOHeaderDelimitedRpcDomain DomainB { get; set; } = null!;

        [SetUp]
        public void SetUp()
        {
            RpcDomain = new RpcDomain();
            var a2b = new Pipe();
            var b2a = new Pipe();
            DomainA = new PipeIOHeaderDelimitedRpcDomain(b2a.Reader, a2b.Writer);
            DomainB = new PipeIOHeaderDelimitedRpcDomain(a2b.Reader, b2a.Writer);
            DomainA.StartAsync();
            DomainB.StartAsync();
        }


        public struct IntParam
        {
            public int i;
        }

        public RpcMethodHandle<TParams> NewMethodAtA<TParams>(string methodName, RpcAsyncMethodEntry methodEntry)
        {
            DomainA.AddMethod(methodName, methodEntry);
            return new RpcMethodHandle<TParams>(DomainB, methodName);
        }
        public RpcMethodHandle<TParams> NewMethodAtB<TParams>(string methodName, RpcAsyncMethodEntry methodEntry)
        {
            DomainB.AddMethod(methodName, methodEntry);
            return new RpcMethodHandle<TParams>(DomainA, methodName);
        }
        public RpcMethodHandle<TParams,TResult> NewMethodAtA<TParams,TResult>(string methodName, RpcAsyncMethodEntry methodEntry)
        {
            DomainA.AddMethod(methodName, methodEntry);
            return new RpcMethodHandle<TParams,TResult>(DomainB, methodName);
        }
        public RpcMethodHandle<TParams,TResult> NewMethodAtB<TParams,TResult>(string methodName, RpcAsyncMethodEntry methodEntry)
        {
            DomainB.AddMethod(methodName, methodEntry);
            return new RpcMethodHandle<TParams,TResult>(DomainA, methodName);
        }
        [Test]
        public Task PipeDuplex()
        {

            var methodEntry = RpcMethodEntry.ExplicitParams<IntParam, int>(p => p.i);
            var aMethodHandle = NewMethodAtB<IntParam,int>("getI", methodEntry);
            var bMethodHandle = NewMethodAtA<IntParam, int>("getI", methodEntry);

            var tasks = new Task[200000];
            var a2bTasks = tasks.AsSpan(100000);
            var b2aTasks = tasks.AsSpan(0, 100000);
            for (int i = 0; i < a2bTasks.Length; i++)
            {
                var taskLocalI = i;
                a2bTasks[i] = Task.Run(async () =>
                {
                    var task = aMethodHandle.RequestAsync(new IntParam() { i = taskLocalI });
                    var resultI = await task;
                    Assert.AreEqual(taskLocalI, resultI);
                });
            }
            for (int i = 0; i < b2aTasks.Length; i++)
            {
                var taskLocalI = i;
                b2aTasks[i] = Task.Run(async () =>
               {
                   var task = bMethodHandle.RequestAsync(new IntParam() { i = taskLocalI });
                   var resultI = await task;
                   Assert.AreEqual(taskLocalI, resultI);
               });
            }
            return Task.WhenAll(tasks);

        }

        [Test]
        public void ThrowArgumentException()
        {
            var entry = RpcMethodEntry.ExplicitParams<IntParam>(param =>
            {
                if (param.i < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return;
            });
            var handle = NewMethodAtA<IntParam>("VerifyIsNotNegative", entry);
            Assert.ThrowsAsync<ArgumentException>(() => handle.RequestAsync(new IntParam() { i = -1 }).AsTask());
        }

        delegate ValueTask InjectIDAsync([RpcID] ID? id);

        [Test]
        public void CancelByID()
        {
            var entry = RpcMethodEntry.FromDelegate<InjectIDAsync>(async id =>
            {
                var cancellationToken = id.AsRpcCancellationToken();
                await Task.Delay(1919810, cancellationToken);
            });

            var handle = NewMethodAtA<ValueTuple>("WaitForBeast", entry);

            var cts = new CancellationTokenSource();

            var req = handle.RequestAsync(new ValueTuple(),cts.Token);
            //await Task.Delay(810);
            
            cts.Cancel();
            Assert.ThrowsAsync<OperationCanceledException>(() => req.AsTask());

        }

  

    }
}