using NUnit.Framework;
using RamType0.JsonRpc;
using System;
using RamType0.JsonRpc.Emit;
using static RamType0.JsonRpc.Emit.MethodParamsTypeBuilder;
using Utf8Json;
using Utf8Json.Resolvers;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text;
using System.Threading;

namespace RamType0.JsonRpc.Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            
        }

        [Test]
        public void Test1()
        {
            var paramsType = MethodParamsTypeBuilder.CreateParamsType<Func<object,object,bool>>(ReferenceEquals,"t1");
            using (dynamic paramsObj = Activator.CreateInstance(paramsType))
            {
                paramsObj.objA = paramsObj.objB = null;
                Assert.AreEqual(true, paramsObj.Invoke());
            }
        }

        [Test]
        public void Test2()
        {
            var paramsType = MethodParamsTypeBuilder.CreateParamsType<Func<int,int>>(1.CompareTo,"t2");
            using (var paramsObj = (IFunctionParamsObject<int>)Activator.CreateInstance(paramsType))
            {
                ((dynamic)paramsObj).value = 2;
                Assert.AreEqual(-1, paramsObj.Invoke());
                Assert.IsFalse(paramsObj is IEmptyParamsObject);
            }
        }

        [Test]
        public void Test3()
        {
            var paramsType = MethodParamsTypeBuilder.CreateParamsType<Func<string>>("".ToString,"");
            IFunctionParamsObject<string> paramsObj = (IFunctionParamsObject<string>)Activator.CreateInstance(paramsType);
            Assert.AreEqual("", paramsObj.Invoke());
            Assert.IsTrue(paramsObj is IEmptyParamsObject);
            paramsObj.Dispose();
            Assert.Throws<NullReferenceException>(()=>paramsObj.Invoke());
            
            
        }
        [Test]
        public void Test4()
        {
            var paramsType = MethodParamsTypeBuilder.CreateParamsType<Func<int, int>>(1.CompareTo,"t4");
            var paramsObj = (IFunctionParamsObject<int>)Activator.CreateInstance(paramsType);

            ((dynamic)paramsObj).value = 0;
            paramsObj.Dispose();
            Assert.Throws<NullReferenceException>(() => paramsObj.Invoke());
            Assert.IsFalse(paramsObj is IEmptyParamsObject);


        }
        [Test]
        public void Utf8JsonTest1()
        {
            var s = JsonSerializer.Deserialize<string[]>("[\"\",\"\"]");

        }

        [Test]
        public void RpcDicTest()
        {

            var dic = new JsonRpcMethodDictionary();
            dic.Register<Action<string>>("log", (str) => Debug.WriteLine(str));
            dic.Register<Action>("none", () => { });
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
            
            var responser = new DefaultResponser(Console.OpenStandardOutput());
            var receiver = new RequestReceiver(dic, responser, JsonSerializer.DefaultResolver);
            return receiver;
        }

        [Test]
        public void RpcDic10MReq()
        {
            var dic = new JsonRpcMethodDictionary();

            dic.Register<Func<string,string>>("log1", (str) => { return str; });
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

            dic.Register<Func<string, string>>("log2", (str) => {  return str; });

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

            dic.Register<Func<string, string>>("log3", (str) => { return str; });

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

            dic.Register<Func<string, string>>("log4", (str) => { return str; });

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

            dic.Register<Func<string, string>>("log5", (str) => { return str; });

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

            dic.Register<Func<string, string>>("log6", (str) => { return str; });

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

        public string CancellableFunc(string ret, [CancelledByID] CancellationToken token)
        {
            return ret;
        }

        [Test]
        public void RpcDic10MCancellableReq()
        {
            var dic = new JsonRpcMethodDictionary();

            dic.Register<Func<string,CancellationToken, string>>("cancellable",CancellableFunc );
            //var tasks = new Task[10000000];
            var receiver = CreateReceiver(dic);
            
            for (int i = 0; i < 10_000_000; i++)
            {
                var bytes = Encoding.UTF8.GetBytes(
                "{\"jsonrpc\":\"2.0\"," +
                "\"params\":[\"10MegaShock!!!\"]," +
                "\"method\":\"cancellable\"," +
                $"\"id\":{i.ToString()}" +
                "}");
                receiver.ResolveAsync(bytes);
            }
            //Task.WaitAll(tasks);

        }

    }
}