using System;
using System.Collections.Generic;
using System.Text;

namespace RamType0.JsonRpc
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    internal sealed class IDCancellationTokenAttribute : Attribute
    {
    }
}
