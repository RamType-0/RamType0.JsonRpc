using NUnit.Framework;
using RamType0.JsonRpc.Server;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;

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

            var dic = CreateServer();
            dic.Register("log", RpcEntry.FromDelegate<Action<string>>((str) => Debug.WriteLine(str)));
            dic.Register("none", RpcEntry.FromDelegate<Action>(() => { }));
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

            var output = new DummyResponseOutput();
            var server = new Server.Server(output, JsonSerializer.DefaultResolver);
            return server;
        }

        class DummyResponseOutput : IResponseOutput
        {
            ValueTask IResponseOutput.ResponseAsync<T>(Server.Server server,T response)
            {
                return new ValueTask();
            }
        }

        [Test]
        public void RpcDic10MReq()
        {
            var dic = CreateServer();

            dic.Register("log1", RpcEntry.FromDelegate<Func<string, string>>((str) => { return str; }));
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

            dic.Register("log2", RpcEntry.FromDelegate<Func<string, string>>((str) => { return str; }));
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

            dic.Register("log3", RpcEntry.FromDelegate<Func<string, string>>((str) => { return str; }));

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

            dic.Register("log4", RpcEntry.FromDelegate<Func<string, string>>((str) => { return str; }));

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

            dic.Register("log5", RpcEntry.FromDelegate<Func<string, string>>((str) => { return str; }));

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

            dic.Register("log6", RpcEntry.FromDelegate<Func<string, string>>((str) => { return str; }));
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

            dic.Register("idInject", RpcEntry.FromDelegate<TestInjectID>(IDInject));

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

            dic.Register("sT", RpcEntry.FromDelegate<Func<string, string>>((str) => { return str; }));

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

            dic.Register("mul2", RpcEntry.FromDelegate<Func<long, long>>((number) => { return number * 2; }));

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

            dic.Register("mul3", RpcEntry.FromDelegate<Func<long, long>>((number) => { return number * 3; }));

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

    }
}