using System;
using System.Collections.Generic;
using System.Text;

namespace RamType0.JsonRpc
{
    public struct CancelParams
    {
        internal static EscapedUTF8String CancellationMethodName { get; } = EscapedUTF8String.FromUnEscaped("$/cancelRequest");
        public ID id;
    }
}
