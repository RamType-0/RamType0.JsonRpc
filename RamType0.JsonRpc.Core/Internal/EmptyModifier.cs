using System;
using Utf8Json;

namespace RamType0.JsonRpc.Internal
{
    public struct EmptyModifier<TParams> : IMethodParamsModifier<TParams>
    {

        public void Modify(ref TParams parameters, ArraySegment<byte> parametersSegment, ID? id, IJsonFormatterResolver formatterResolver)
        {
            return;
        }
    }
}
