using System;
using System.Collections.Generic;
using System.Text;

namespace RamType0.JsonRpc
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field,AllowMultiple = false, Inherited = true)]
    public sealed class CancelledByIDAttribute : Attribute
    {
        
    }
}
