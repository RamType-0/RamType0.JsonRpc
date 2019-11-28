using System;
using System.Threading.Tasks;

namespace RamType0.JsonRpc.Server
{
    /// <summary>
    /// 手動でレスポンスを返すときにスローする例外です。
    /// </summary>
    class ManualResponseException : Exception
    {
        public ManualResponseException(Func<Server, ID?, ValueTask> responseMethod, bool responseAlsoOnNotification = false) : base()
        {
            ResponseMethod = responseMethod;
            ResponseAlsoOnNotification = responseAlsoOnNotification;
        }
        public Func<Server, ID?, ValueTask> ResponseMethod { get; }
        public bool ResponseAlsoOnNotification { get; }

        public ValueTask Response(Server server, ID? id)
        {
            return ResponseMethod(server, id);
        }

    }
}
