using System.Threading.Tasks;

namespace RamType0.JsonRpc.Client
{
    public interface IRequestOutput
    {
        ValueTask SendRequestAsync<T>(Client client, Request<T> request) where T : IMethodParams;
        ValueTask SendNotification<T>(Client client, Notification<T> notification) where T : IMethodParams;
    }


}
