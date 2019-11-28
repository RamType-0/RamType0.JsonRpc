using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RamType0.JsonRpc.Server
{
    public interface IRpcExceptionHandler
    {
        public ValueTask OnException(JsonRpcServer server,ID? id, Exception exception);
    }

    public sealed class DefaultRpcExceptionHandler : IRpcExceptionHandler
    {
        public static DefaultRpcExceptionHandler Instance { get; } = new DefaultRpcExceptionHandler();
        public ValueTask OnException(JsonRpcServer server,ID? id,Exception exception)
        {
            if(exception is ManualResponseException explicitResponse)
            {
                return explicitResponse.Response(server, id);
            }
            else if (id is ID reqID)
            {
                return server.Output.ResponseException(ErrorResponse.Exception(reqID, ErrorCode.InternalError, exception));
            }
            else
            {
                return new ValueTask();
            }
        }
    }

}
