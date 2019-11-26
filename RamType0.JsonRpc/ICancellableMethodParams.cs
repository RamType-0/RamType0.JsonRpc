using System.Threading;

namespace RamType0.JsonRpc
{
    public interface ICancellableMethodParams : IMethodParams
    {
        public CancellationToken CancellationToken { get; set; }
    }
}
