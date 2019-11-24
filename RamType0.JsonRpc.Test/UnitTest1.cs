using NUnit.Framework;
using RamType0.JsonRpc;
using System;
using Utf8Json;
using Utf8Json.Resolvers;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text;
using System.Threading;
using static RamType0.JsonRpc.Emit;

namespace RamType0.JsonRpc.Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            
        }


        [Test]
        public void Utf8JsonTest1()
        {
            JsonSerializer.Deserialize<string[]>("[\"\",\"\"]");

        }

        [Test]
        public void RpcDicTest()
        {

            var dic = new JsonRpcMethodDictionary();
            dic.Register("log", RpcEntry.FromDelegate<Action<string>>((str) => Debug.WriteLine(str)));
            dic.Register("none", RpcEntry.FromDelegate<Action>( () => { }));
            RequestReceiver receiver = CreateReceiver(dic);
            receiver.Resolve(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":{\"str\":\"Hello\"}," +
                "\"method\":\"log\"," +
                "\"id\":\"asd\"" +
                "}"
                );
            receiver.Resolve(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":{\"str\":\"World!\"}," +
                "\"method\":\"none\"," +
                "\"id\":\"3\"" +
                "}"
                );
        }

        private static RequestReceiver CreateReceiver(JsonRpcMethodDictionary dic)

        {
            
            var responser = new DefaultResponseOutput(Console.OpenStandardOutput());
            var receiver = new RequestReceiver(dic, responser, JsonSerializer.DefaultResolver);
            return receiver;
        }

        [Test]
        public void RpcDic10MReq()
        {
            var dic = new JsonRpcMethodDictionary();

            dic.Register("log1",RpcEntry.FromDelegate<Func<string,string>>((str) => { return str; }));
            //var tasks = new Task[10000000];
            var receiver = CreateReceiver(dic);
            var bytes = Encoding.UTF8.GetBytes(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":[\"10MegaShock!!!\"]," +
                "\"method\":\"log1\"," +
                "\"id\":1" +
                "}");
            for (int i = 0; i < 10_000_000; i++)
            {
                receiver.ResolveAsync(bytes);
            }
            //Task.WaitAll(tasks);
            
        }

      
        [Test]
        public void RpcDic10MNotification()
        {
            var dic = new JsonRpcMethodDictionary();

            dic.Register("log2",RpcEntry.FromDelegate<Func<string, string>>((str) => {  return str; }));
            var receiver = CreateReceiver(dic);
            //var tasks = new Task[10000000];
            var bytes = Encoding.UTF8.GetBytes(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":[\"10MegaShock!!!\"]," +
                "\"method\":\"log2\"" +
                "}");
            for (int i = 0; i < 10_000_000; i++)
            {
                receiver.ResolveAsync(bytes );
            }
            //Task.WaitAll(tasks);
        }

        

        [Test]
        public void RpcDic1MInvalidJson()
        {
            var dic = new JsonRpcMethodDictionary();

            dic.Register("log3",RpcEntry.FromDelegate<Func<string, string>>((str) => { return str; }));

            var receiver = CreateReceiver(dic);
            var bytes = Encoding.UTF8.GetBytes(
                //"{\"jsonrpc\":\"2.0\"," +
                "\"params\":[\"1MegaShock!!!\"]," +
                "\"method\":\"log3\"" +
                "}");
            for (int i = 0; i < 1000000; i++)
            {
                receiver.ResolveAsync(bytes);
            }
        }

        [Test]
        public void RpcDic1MMissingJsonRpc()
        {
            var dic = new JsonRpcMethodDictionary();

            dic.Register("log4", RpcEntry.FromDelegate<Func<string, string>>( (str) => { return str; }));

            var receiver = CreateReceiver(dic);
            var bytes = Encoding.UTF8.GetBytes(
                "{"+
                //"\"jsonrpc\":\"2.0\"," +
                "\"params\":[\"1MegaShock!!!\"]," +
                "\"method\":\"log4\"" +
                "}");
            for (int i = 0; i < 1000000; i++)
            {
                receiver.ResolveAsync(bytes);
            }

        }

        [Test]
        public void RpcDic1MInvalidJsonRpc()
        {
            var dic = new JsonRpcMethodDictionary();

            dic.Register("log5", RpcEntry.FromDelegate<Func<string, string>>( (str) => { return str; }));

            var receiver = CreateReceiver(dic);
    
            var bytes = Encoding.UTF8.GetBytes(
                "{" +
                "\"jsonrpc\":\"1.0\"," +
                "\"params\":[\"1MegaShock!!!\"]," +
                "\"method\":\"log5\"" +
                "}");
            for (int i = 0; i < 1000000; i++)
            {
                receiver.ResolveAsync(bytes);
            }

        }
        [Test]
        public void RpcDic10MLongNotification()
        {
            var dic = new JsonRpcMethodDictionary();

            dic.Register("log6", RpcEntry.FromDelegate<Func<string, string>>( (str) => { return str; }));

            var receiver = CreateReceiver(dic);
            //var tasks = new Task[10000000];
            var bytes = Encoding.UTF8.GetBytes(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":[\"10MegaShock!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\"]," +
                "\"method\":\"log6\"" +
                "}");
            for (int i = 0; i < 10_000_000; i++)
            {
                receiver.ResolveAsync(bytes);
            }
            //Task.WaitAll(tasks);
        }

        public void CancellableFunc(CancellationToken token)
        {
            return;
        }
        public delegate void TestCancellable([IDCancellation]CancellationToken token);
        [Test]
        public void RpcDic10MCancellableReq()
        {
            var dic = new JsonRpcMethodDictionary();

            dic.Register("cancellable", RpcEntry.FromDelegate<TestCancellable>(CancellableFunc));
            //var tasks = new Task[10000000];
            var receiver = CreateReceiver(dic);
            
            for (int i = 0; i < 10_000_000; i++)
            {
                var bytes = Encoding.UTF8.GetBytes(
                "{\"jsonrpc\":\"2.0\"," +
                //"\"params\":[\"10MegaShock!!!\"]," +
                "\"method\":\"cancellable\"," +
                $"\"id\":{i.ToString()}" +
                "}");
                receiver.ResolveAsync(bytes);
            }
            //Task.WaitAll(tasks);

        }
        public void RpcDic1MSingleThreadNotification()
        {
            var dic = new JsonRpcMethodDictionary();

            dic.Register("sT", RpcEntry.FromDelegate<Func<string, string>>( (str) => { return str; }));

            var receiver = CreateReceiver(dic);
            //var tasks = new Task[10000000];
            var bytes = Encoding.UTF8.GetBytes(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":[\"10MegaShock!!!\"]," +
                "\"method\":\"sT\"" +
                "}");
            for (int i = 0; i < 1_000_000; i++)
            {
                receiver.Resolve(bytes);
            }
            //Task.WaitAll(tasks);
        }
        [Test]

        public void RpcDic1MSingleThreadUnmanagedParams()
        {
            var dic = new JsonRpcMethodDictionary();

            dic.Register("mul2", RpcEntry.FromDelegate<Func<long,long>>( (number) => { return number * 2; }));

            var receiver = CreateReceiver(dic);
            //var tasks = new Task[10000000];
            var bytes = Encoding.UTF8.GetBytes(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":{\"number\":231}," +
                "\"method\":\"mul2\"" +
                "}");
            for (int i = 0; i < 1_000_000; i++)
            {
                receiver.Resolve(bytes);
            }
            //Task.WaitAll(tasks);
        }

        [Test]

        public void RpcDic10MUnmanagedParams()
        {
            var dic = new JsonRpcMethodDictionary();

            dic.Register("mul3", RpcEntry.FromDelegate<Func<long, long>>((number) => { return number * 3; }));

            var receiver = CreateReceiver(dic);
            //var tasks = new Task[10000000];
            var bytes = Encoding.UTF8.GetBytes(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":{\"number\":231}," +
                "\"method\":\"mul3\"" +
                "}");
            for (int i = 0; i < 10_000_000; i++)
            {
                receiver.ResolveAsync(bytes);
            }
            //Task.WaitAll(tasks);
        }

    }
}