using NUnit.Framework;
using RamType0.JsonRpc.Server;
using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;

namespace RamType0.JsonRpc.Tests
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

            var dic = CreateServer();
            dic.Register("log", RpcMethodEntry.FromDelegate<Action<string>>((str) => Debug.WriteLine(str)));
            dic.Register("none", RpcMethodEntry.FromDelegate<Action>(() => { }));
            dic.ResolveAsync(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":{\"str\":\"Hello\"}," +
                "\"method\":\"log\"," +
                "\"id\":\"asd\"" +
                "}"
                );
            dic.ResolveAsync(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":{\"str\":\"World!\"}," +
                "\"method\":\"none\"," +
                "\"id\":\"3\"" +
                "}"
                );
        }

        private static Server.Server CreateServer()

        {
            var output = new PipeMessageOutput<PassThroughWriter>(new PassThroughWriter(), PipeWriter.Create(Console.OpenStandardOutput()));
            _ = output.StartOutputAsync();
            var server = new Server.Server(output);
            return server;
        }

        [Test]
        public void RpcDic10MReq()
        {
            var dic = CreateServer();

            dic.Register("log1", RpcMethodEntry.FromDelegate<Func<string, string>>((str) => { return str; }));
            var bytes = Encoding.UTF8.GetBytes(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":[\"10MegaShock!!!\"]," +
                "\"method\":\"log1\"," +
                "\"id\":1" +
                "}");
            for (int i = 0; i < 10_000_000; i++)
            {
                dic.ResolveAsync(bytes);
            }
            //Task.WaitAll(tasks);

        }


        [Test]
        public void RpcDic10MNotification()
        {
            var dic = CreateServer();

            dic.Register("log2", RpcMethodEntry.FromDelegate<Func<string, string>>((str) => { return str; }));
            //var tasks = new Task[10000000];
            var bytes = Encoding.UTF8.GetBytes(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":[\"10MegaShock!!!\"]," +
                "\"method\":\"log2\"" +
                "}");
            for (int i = 0; i < 10_000_000; i++)
            {
                dic.ResolveAsync(bytes);
            }
            //Task.WaitAll(tasks);
        }



        [Test]
        public void RpcDic1MInvalidJson()
        {
            var dic = CreateServer();

            dic.Register("log3", RpcMethodEntry.FromDelegate<Func<string, string>>((str) => { return str; }));

            var bytes = Encoding.UTF8.GetBytes(
                //"{\"jsonrpc\":\"2.0\"," +
                "\"params\":[\"1MegaShock!!!\"]," +
                "\"method\":\"log3\"" +
                "}");
            for (int i = 0; i < 1000000; i++)
            {
                dic.ResolveAsync(bytes);
            }
        }

        [Test]
        public void RpcDic1MMissingJsonRpc()
        {
            var dic = CreateServer();

            dic.Register("log4", RpcMethodEntry.FromDelegate<Func<string, string>>((str) => { return str; }));

            var bytes = Encoding.UTF8.GetBytes(
                "{" +
                //"\"jsonrpc\":\"2.0\"," +
                "\"params\":[\"1MegaShock!!!\"]," +
                "\"method\":\"log4\"" +
                "}");
            for (int i = 0; i < 1000000; i++)
            {
                dic.ResolveAsync(bytes);
            }

        }

        [Test]
        public void RpcDic1MInvalidJsonRpc()
        {
            var dic = CreateServer();

            dic.Register("log5", RpcMethodEntry.FromDelegate<Func<string, string>>((str) => { return str; }));

            var bytes = Encoding.UTF8.GetBytes(
                "{" +
                "\"jsonrpc\":\"1.0\"," +
                "\"params\":[\"1MegaShock!!!\"]," +
                "\"method\":\"log5\"" +
                "}");
            for (int i = 0; i < 1000000; i++)
            {
                dic.ResolveAsync(bytes);
            }

        }
        [Test]
        public void RpcDic10MLongNotification()
        {
            var dic = CreateServer();

            dic.Register("log6", RpcMethodEntry.FromDelegate<Func<string, string>>((str) => { return str; }));
            //var tasks = new Task[10000000];
            var bytes = Encoding.UTF8.GetBytes(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":[\"10MegaShock!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\"]," +
                "\"method\":\"log6\"" +
                "}");
            for (int i = 0; i < 10_000_000; i++)
            {
                dic.ResolveAsync(bytes);
            }
            //Task.WaitAll(tasks);
        }

        public ID? IDInject(ID? id)
        {
            return id;
        }
        public delegate ID? TestInjectID([RpcID]ID? id);
        [Test]
        public void RpcDic10MIDInjectReq()
        {
            var dic = CreateServer();

            dic.Register("idInject", RpcMethodEntry.FromDelegate<TestInjectID>(IDInject));

            for (int i = 0; i < 10_000_000; i++)
            {
                var bytes = Encoding.UTF8.GetBytes(
                "{\"jsonrpc\":\"2.0\"," +
                //"\"params\":[\"10MegaShock!!!\"]," +
                "\"method\":\"idInject\"," +
                $"\"id\":{i.ToString()}" +
                "}");
                dic.ResolveAsync(bytes);
            }
            //Task.WaitAll(tasks);

        }
        public void RpcDic1MSingleThreadNotification()
        {
            var dic = CreateServer();

            dic.Register("sT", RpcMethodEntry.FromDelegate<Func<string, string>>((str) => { return str; }));

            //var tasks = new Task[10000000];
            var bytes = Encoding.UTF8.GetBytes(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":[\"10MegaShock!!!\"]," +
                "\"method\":\"sT\"" +
                "}");
            for (int i = 0; i < 1_000_000; i++)
            {
                dic.ResolveAsync(bytes);
            }
            //Task.WaitAll(tasks);
        }
        [Test]

        public void RpcDic1MSingleThreadUnmanagedParams()
        {
            var dic = CreateServer();

            dic.Register("mul2", RpcMethodEntry.FromDelegate<Func<long, long>>((number) => { return number * 2; }));

            //var tasks = new Task[10000000];
            var bytes = Encoding.UTF8.GetBytes(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":{\"number\":231}," +
                "\"method\":\"mul2\"" +
                "}");
            for (int i = 0; i < 1_000_000; i++)
            {
                dic.ResolveAsync(bytes);
            }
            //Task.WaitAll(tasks);
        }

        [Test]

        public void RpcDic10MUnmanagedParams()
        {
            var dic = CreateServer();

            dic.Register("mul3", RpcMethodEntry.FromDelegate<Func<long, long>>((number) => { return number * 3; }));

            //var tasks = new Task[10000000];
            var bytes = Encoding.UTF8.GetBytes(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":{\"number\":231}," +
                "\"method\":\"mul3\"" +
                "}");
            for (int i = 0; i < 10_000_000; i++)
            {
                dic.ResolveAsync(bytes);
            }
            //Task.WaitAll(tasks);
        }

        [Test]
        public void CalliStandardEntry()
        {
            var entry = Internal.RpcMethodEntry.FromDelegate<Func<int,int,int>>(Math.Max);
            var arg = Encoding.UTF8.GetBytes("[2,1]");
            var json = entry.ResolveRequest(arg,new ID(1), JsonSerializer.DefaultResolver);
            var str = Encoding.UTF8.GetString(json);
        }

        [Test]
        public void CalliHasThisEntry()
        {
            var entry = Internal.RpcMethodEntry.FromDelegate<Func<string>>("114514".ToString);
            var arg = Encoding.UTF8.GetBytes("[]");
            var json = entry.ResolveRequest(arg, new ID(1), JsonSerializer.DefaultResolver);
            var str = Encoding.UTF8.GetString(json);
        }

        [Test]
        public void CalliHasThisEntryEmptyParams()
        {
            var entry = Internal.RpcMethodEntry.FromDelegate<Func<string>>("114514".ToString);
            var json = entry.ResolveRequest(default, new ID(1), JsonSerializer.DefaultResolver);
            var str = Encoding.UTF8.GetString(json);
        }

        [Test]
        public void CalliEmptyParamsInjectID()
        {
            var entry = Internal.RpcMethodEntry.FromDelegate<InjectID>(id => id.ToString());
            var json = entry.ResolveRequest(default, new ID(1145141919810364364), JsonSerializer.DefaultResolver);
            var str = Encoding.UTF8.GetString(json);
        }

        delegate string InjectID([RpcID] ID? id);


        [Test]
        public void CalliEmptyParamsInjectIDResolveReq()
        {
            var entry = Internal.RpcMethodEntry.FromDelegate<InjectID>(id => id.ToString());
            var resolver = new Internal.RequestResolver();
            resolver.TryRegister("공격전이다", entry);
            var request = $"{{\"jsonrpc\":\"2.0\",\"params\":[],\"method\":\"공격전이다\",\"id\":114514}}";
            var json = resolver.Resolve(Encoding.UTF8.GetBytes(request));
            var str = Encoding.UTF8.GetString(json);
            
        }

        [Test]
        public void Calli()
        {
            var entry = Internal.RpcMethodEntry.FromDelegate<Action>(()=> { });
            var resolver = new Internal.RequestResolver();
            resolver.TryRegister("Hello", entry);
            var request = "{\"jsonrpc\":\"2.0\",\"method\":\"Hello\",\"id\":1}";
            var json = resolver.Resolve(Encoding.UTF8.GetBytes(request));
            var str = Encoding.UTF8.GetString(json);
        }
    }
}