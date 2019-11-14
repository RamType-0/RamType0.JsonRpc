using NUnit.Framework;
using RamType0.JsonRpc;
using System;
using RamType0.JsonRpc.Emit;
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
            var paramsType = MethodInvokerClassBuilder.CreateType<Func<object,object,bool>>(ReferenceEquals);
            dynamic paramsObj = Activator.CreateInstance(paramsType);
            paramsObj.objA = paramsObj.objB = null;
            Assert.AreEqual(true, paramsObj.Invoke());
        }

        [Test]
        public void Test2()
        {
            var paramsType = MethodInvokerClassBuilder.CreateType<Func<int,int>>(1.CompareTo);
            dynamic paramsObj = Activator.CreateInstance(paramsType);
            paramsObj.value = 2;
            Assert.AreEqual(-1, paramsObj.Invoke());
        }

        [Test]
        public void Test3()
        {
            var paramsType = MethodInvokerClassBuilder.CreateType<Func<string>>("".ToString);
            dynamic paramsObj = Activator.CreateInstance(paramsType);
            Assert.AreEqual("", paramsObj.Invoke());
        }
    }
}