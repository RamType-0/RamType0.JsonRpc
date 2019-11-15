using NUnit.Framework;
using RamType0.JsonRpc;
using System;
using RamType0.JsonRpc.Emit;
using static RamType0.JsonRpc.Emit.MethodInvokerClassBuilder;
using Utf8Json;
using Utf8Json.Resolvers;

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
            var paramsType = MethodInvokerClassBuilder.CreateType<Func<object,object,bool>>(ReferenceEquals,"t1");
            using (dynamic paramsObj = Activator.CreateInstance(paramsType))
            {
                paramsObj.objA = paramsObj.objB = null;
                Assert.AreEqual(true, paramsObj.Invoke());
            }
        }

        [Test]
        public void Test2()
        {
            var paramsType = MethodInvokerClassBuilder.CreateType<Func<int,int>>(1.CompareTo,"t2");
            using (var paramsObj = (IMethodParamsObject<int>)Activator.CreateInstance(paramsType))
            {
                ((dynamic)paramsObj).value = 2;
                Assert.AreEqual(-1, paramsObj.Invoke());
                Assert.IsFalse(paramsObj is IEmptyParamsObject);
            }
        }

        [Test]
        public void Test3()
        {
            var paramsType = MethodInvokerClassBuilder.CreateType<Func<string>>("".ToString,"");
            IMethodParamsObject<string> paramsObj = (IMethodParamsObject<string>)Activator.CreateInstance(paramsType);
            Assert.AreEqual("", paramsObj.Invoke());
            Assert.IsTrue(paramsObj is IEmptyParamsObject);
            paramsObj.Dispose();
            Assert.IsNull( paramsObj.Invoke());
            
            
        }
        [Test]
        public void Test4()
        {
            var paramsType = MethodInvokerClassBuilder.CreateType<Func<int, int>>(1.CompareTo,"t4");
            var paramsObj = (IMethodParamsObject<int>)Activator.CreateInstance(paramsType);

            ((dynamic)paramsObj).value = 0;
            paramsObj.Dispose();
            Assert.AreEqual(0, paramsObj.Invoke());
            Assert.IsFalse(paramsObj is IEmptyParamsObject);


        }
        [Test]
        public void Utf8JsonTest1()
        {
            var s = JsonSerializer.Deserialize<string[]>("[\"\",\"\"]");

        }
            [Test]
        public void Utf8JsonTest2()
        {
            
            var s = JsonSerializer.Deserialize<RequestReceiver.RequestReceiverObject>("{\"jsonrpc\":\"2.0\"}");

        }

    }
}