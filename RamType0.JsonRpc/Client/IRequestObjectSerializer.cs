using System.Threading.Tasks;

namespace RamType0.JsonRpc.Client
{
    public interface IRequestObjectOutput
    {
        ValueTask SendRequestAsync<T>(Client client, Request<T> request) where T : IMethodParams;
        void SendNotification<T>(Client client, Notification<T> notification) where T : IMethodParams;
    }

   
}
