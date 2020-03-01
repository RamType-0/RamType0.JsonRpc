using System;
using System.Collections.Generic;
using System.Text;
using Utf8Json;

namespace RamType0.JsonRpc.Internal
{
    public struct EmptyModifier<TParams> : IMethodParamsModifier<TParams>
        where TParams: notnull
    {
        
        public void Modify(ref TParams parameters, ArraySegment<byte> parametersSegment, ID? id, IJsonFormatterResolver formatterResolver)
        {
            return;
        }
    }
}
