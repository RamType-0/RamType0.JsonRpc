using System;
using System.Threading.Tasks;

namespace RamType0.JsonRpc.Server
{
    using Protocol;
    public interface IRpcExceptionHandler
    {
        public ValueTask OnException(Server server, ID? id, Exception exception);
    }

    public sealed class DefaultRpcExceptionHandler : IRpcExceptionHandler
    {
        public static DefaultRpcExceptionHandler Instance { get; } = new DefaultRpcExceptionHandler();
        public ValueTask OnException(Server server, ID? id, Exception exception)
        {
            if (exception is ManualResponseException explicitResponse)
            {
                return explicitResponse.Response(server, id);
            }
            else if (id is ID reqID)
            {
                return server.Output.ResponseException(server,ErrorResponse.Exception(reqID, ErrorCode.InternalError, exception));
            }
            else
            {
                return new ValueTask();
            }
        }
    }

}
