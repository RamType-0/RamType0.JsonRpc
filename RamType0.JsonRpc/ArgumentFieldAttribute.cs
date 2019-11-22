using System;
using System.Collections.Generic;
using System.Text;

namespace RamType0.JsonRpc
{
    [AttributeUsage(AttributeTargets.Field,AllowMultiple = false,Inherited = true)]
    class ArgumentFieldAttribute : Attribute
    {
        public ushort Order {get;}
        public ArgumentFieldAttribute(ushort order)
        {
            Order = order;
        }
    }
}
