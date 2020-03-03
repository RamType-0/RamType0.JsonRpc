using RamType0.JsonRpc.Server;
using System;
using System.Collections.Generic;
using System.Text;
using Utf8Json;

namespace RamType0.JsonRpc.Internal
{

    [Serializable]
    public abstract class RpcExplicitResponseException : Exception
    {
        public RpcResponse Response { get; }
        public RpcExplicitResponseException(RpcResponse response) 
        {
            this.Response = response;
        }

    }

    public delegate ArraySegment<byte> RpcResponse(ArraySegment<byte> parametersJson, ID? id, IJsonFormatterResolver readFormatterResolver, IJsonFormatterResolver writeFormatResolver);

}
