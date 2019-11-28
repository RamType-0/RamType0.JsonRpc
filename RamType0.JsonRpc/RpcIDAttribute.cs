using System;

namespace RamType0.JsonRpc
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class RpcIDAttribute : Attribute
    {

    }
}
