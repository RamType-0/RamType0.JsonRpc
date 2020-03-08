using System.Threading.Tasks;
using Utf8Json.Resolvers;
namespace RamType0.JsonRpc
{
    using System.Threading;
    using Utf8Json;

    public abstract class RpcMethodHandle
    {
        public RpcMethodHandle(RpcDomain domain, string name, IJsonFormatterResolver jsonFormatterResolver, IErrorHandler errorHandler)
        {
            Name = name;
            EscapedUTF8Name = EscapedUTF8String.FromUnEscaped(name);
            Domain = domain;
            JsonFormatterResolver = jsonFormatterResolver;
            ErrorHandler = errorHandler;
        }

        public RpcMethodHandle(RpcDomain domain, string name) : this(domain, name, StandardResolver.CamelCase, DefaultErrorHandler.Instance)
        {

        }

        public RpcDomain Domain { get; }
        public string Name { get; }
        EscapedUTF8String EscapedUTF8Name { get; }
        IJsonFormatterResolver JsonFormatterResolver { get; }
        IErrorHandler ErrorHandler { get; }

        protected ValueTask<TResult> RequestAsync<TParams, TResult>(TParams parameters, CancellationToken cancellationToken = default)
        {
            return Domain.RequestAsync<TParams, TResult>(EscapedUTF8Name, parameters, JsonFormatterResolver, ErrorHandler,cancellationToken);
        }
        protected ValueTask RequestAsync<TParams>(TParams parameters, CancellationToken cancellationToken = default)
        {
            return Domain.RequestAsync(EscapedUTF8Name, parameters, JsonFormatterResolver, ErrorHandler,cancellationToken);
        }
        protected ValueTask NotifyAsync<TParams>(TParams parameters)
        {
            return Domain.NotifyAsync(EscapedUTF8Name, parameters, JsonFormatterResolver);
        }
    }

    public class RpcMethodHandle<TParams, TResult> : RpcMethodHandle
    {
        public RpcMethodHandle(RpcDomain domain, string name) : base(domain, name)
        {
        }

        public RpcMethodHandle(RpcDomain domain, string name, IJsonFormatterResolver jsonFormatterResolver, IErrorHandler errorHandler) : base(domain, name, jsonFormatterResolver, errorHandler)
        {
        }

        public ValueTask<TResult> RequestAsync(TParams parameters,CancellationToken cancellationToken = default) => RequestAsync<TParams, TResult>(parameters, cancellationToken);

        public ValueTask NotifyAsync(TParams parameters) => NotifyAsync<TParams>(parameters);

    }

    public class RpcMethodHandle<TParams> : RpcMethodHandle
    {
        public RpcMethodHandle(RpcDomain domain, string name) : base(domain, name)
        {
        }

        public RpcMethodHandle(RpcDomain domain, string name, IJsonFormatterResolver jsonFormatterResolver, IErrorHandler errorHandler) : base(domain, name, jsonFormatterResolver, errorHandler)
        {
        }

        public ValueTask RequestAsync(TParams parameters, CancellationToken cancellationToken = default) => RequestAsync<TParams>(parameters, cancellationToken);

        public ValueTask NotifyAsync(TParams parameters) => NotifyAsync<TParams>(parameters);

    }

}
