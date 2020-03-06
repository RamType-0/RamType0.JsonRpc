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
        public RpcMethodHandle(RpcDomain messageSender, string name, IJsonFormatterResolver jsonFormatterResolver)
        {
            Name = name;
            EscapedUTF8Name = EscapedUTF8String.FromUnEscaped(name);
            MessageSender = messageSender;
            JsonFormatterResolver = jsonFormatterResolver;
        }
        public RpcDomain MessageSender { get; }
        public string Name { get; }
        EscapedUTF8String EscapedUTF8Name { get; }
        IJsonFormatterResolver JsonFormatterResolver { get; }

        public ValueTask<TResult> RequestAsync<TParams,TResult>(TParams parameters)
        {
            return MessageSender.RequestAsync<TParams,TResult>(EscapedUTF8Name, parameters, JsonFormatterResolver);
        }
        public ValueTask RequestAsync<TParams>(TParams parameters)
        {
            return MessageSender.RequestAsync(EscapedUTF8Name, parameters, JsonFormatterResolver);
        }
        public ValueTask NotifyAsync<TParams>(TParams parameters)
        {
            return MessageSender.NotifyAsync(EscapedUTF8Name, parameters, JsonFormatterResolver);
        }
    }
}
