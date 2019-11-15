using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Utf8Json;
namespace RamType0.JsonRpc
{
    class Responser
    {
        public Responser(Stream output)
        {
            Output = output;
        }

        Stream Output { get; }
        
        public void Response<T>(ResultResponse<T> response)
        {
            JsonSerializer.Serialize(Output,response);
        }
    }
    public readonly struct ResultResponse<T>
    {
        public readonly JsonRpcVersion jsonrpc;
        public readonly T result;
        public readonly ID id;

        public ResultResponse(T result, ID id) : this()
        {
            this.result = result;
            this.id = id;
        }
    }
    public readonly struct ErrorResponse<T>
    {
        public readonly JsonRpcVersion jsonrpc;
        public readonly ID id;
        public readonly ErrorObject<T> error;
 
        
    }
    public readonly struct ErrorObject<T>
    {
        public readonly long code;
        public readonly string message;
        public readonly T data;

        public ErrorObject(long code, string message, T data)
        {
            this.code = code;
            this.message = message;
            this.data = data;
        }
    }
}
