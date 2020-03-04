using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RamType0.JsonRpc.Internal
{
    class RpcMethodHandle<TParams,TResult>
    {
        string Name { get; }
        public ValueTask<TResult> InvokeAsync(TParams parameters)
        {

        }
    }
}
