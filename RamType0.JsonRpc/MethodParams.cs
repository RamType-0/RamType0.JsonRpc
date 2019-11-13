using System;
using System.Collections.Generic;
using System.Text;

namespace RamType0.JsonRpc
{
    readonly struct MethodParams
    {
        /// <summary>
        /// <see cref="Dictionary{string, object}"/>、または<see cref="object[]"/>
        /// </summary>
        IReadOnlyCollection<object> Value { get; }
        
    }
}
