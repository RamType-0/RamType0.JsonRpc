using System;
using RamType0.JsonRpc.Test;
namespace RamType0.JsonRpc.PerformanceTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new Tests();
            test.RpcDic10MUnmanagedParams();
        }
    }
}
