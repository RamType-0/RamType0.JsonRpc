using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RamType0.JsonRpc.Internal
{
    using Client;
    using Protocol;
    using Utf8Json;

    
    public class RpcMethodHandle
    {
        public RpcMethodHandle(RpcDomain domain, string name, IJsonFormatterResolver jsonFormatterResolver)
        {
            Name = name;
            EscapedUTF8Name = EscapedUTF8String.FromUnEscaped(name);
            Domain = domain;
            JsonFormatterResolver = jsonFormatterResolver;
        }
        public RpcDomain Domain { get; }
        public string Name { get; }
        EscapedUTF8String EscapedUTF8Name { get; }
        IJsonFormatterResolver JsonFormatterResolver { get; }

        public ValueTask<TResult> RequestAsync<TParams,TResult>(TParams parameters)
        {
            return Domain.RequestAsync<TParams,TResult>(EscapedUTF8Name, parameters, JsonFormatterResolver);
        }
        public ValueTask RequestAsync<TParams>(TParams parameters)
        {
            return Domain.RequestAsync(EscapedUTF8Name, parameters, JsonFormatterResolver);
        }
        public ValueTask NotifyAsync<TParams>(TParams parameters)
        {
            return Domain.NotifyAsync(EscapedUTF8Name, parameters, JsonFormatterResolver);
        }
    }

    public class RpcMethodHandle<TParams, TResult>
    {

    }
}
