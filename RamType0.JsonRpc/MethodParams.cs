using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Utf8Json;
namespace RamType0.JsonRpc
{
    readonly struct MethodParams
    {
        /// <summary>
        /// <see cref="Dictionary{string, object}"/>、または<see cref="object[]"/>
        /// </summary>
        IReadOnlyCollection<object> Value { get; }

    }

    readonly struct MethodParamsInfo
    {
        /// <summary>
        /// null配列を避けるためReadOnlyMemory
        /// </summary>
        ReadOnlyMemory<Parameter> Parameters { get; }
        readonly struct Parameter
        {
            public Parameter(Type type, EscapedUTF8String name)
            {
                Type = type;
                Name = name;
            }

            Type Type { get; }
            EscapedUTF8String Name { get; }

        }
        void CreateParamObject()
        {
            
        }
    }
}
