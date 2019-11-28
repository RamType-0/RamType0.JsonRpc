using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RamType0.JsonRpc.Server
{
    /// <summary>
    /// 手動でレスポンスを返すときにスローする例外です。
    /// </summary>
    class ManualResponseException : Exception
    {
        public ManualResponseException(Func<JsonRpcServer,ID?,ValueTask> responseMethod,bool responseAlsoOnNotification = false):base()
        {
            ResponseMethod = responseMethod;
            ResponseAlsoOnNotification = responseAlsoOnNotification;
        }
        public Func<JsonRpcServer,ID?,ValueTask> ResponseMethod { get; }
        public bool ResponseAlsoOnNotification { get; }
        
        public ValueTask Response(JsonRpcServer server, ID? id)
        {
            return ResponseMethod(server, id);
        }

    }
}
