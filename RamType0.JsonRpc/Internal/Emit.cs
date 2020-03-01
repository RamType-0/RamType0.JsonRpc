using System;
using System.Collections.Generic;
using System.Text;
using static RamType0.JsonRpc.Emit;
namespace RamType0.JsonRpc.Internal
{
    class Emit
    {
        public RpcMethodEntry FromDelegate<T>(T d)
            where T: Delegate
        {
            if (d.Method.IsStatic)
            {

                return RpcMethodEntryFactoryDelegateTypeCache<T>.StaticMethodFactory.CreateEntry(d);
            }
            else
            {
                return RpcMethodEntryFactoryDelegateTypeCache<T>.InstanceMethodFactory.CreateEntry(d);
            }
            
        }
    }
}
