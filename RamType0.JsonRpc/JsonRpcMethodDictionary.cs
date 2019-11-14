using System;
using System.Collections.Generic;
using System.Text;
using Utf8Json;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading.Tasks;
using RamType0.JsonRpc.Emit;
using static RamType0.JsonRpc.Emit.MethodInvokerClassBuilder;

namespace RamType0.JsonRpc
{
    public class JsonRpcMethodDictionary
    {
              public readonly struct Element
        {
            public Element(Delegate function)
            {
                Method = function;
                Formatter = CreateFormatter(function);
            }
            static IJsonFormatter CreateFormatter(Delegate function)
            {
                MethodInfo method = function.Method;
                throw null!;
                
            }

            
            

            public Delegate Method { get; }
            public IJsonFormatter Formatter { get; }
        }
        Dictionary<EscapedUTF8String, Delegate> Methods { get; } = new Dictionary<EscapedUTF8String, Delegate>();

       
        public void InvokeAsync<T>(EscapedUTF8String methodName,ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            where T:struct,IParamsObject
        {
            switch (reader.GetCurrentJsonToken())
            {
                case JsonToken.String:
                    {
                        var propNameSegment = reader.ReadPropertyNameSegmentRaw().AsSpan();
                        if (propNameSegment.SequenceEqual(stackalloc byte[] {(byte)'p',(byte)'a',(byte)'r',(byte)'a',(byte)'m',(byte)'s', }))
                        {
                            
                        }
                        else if(propNameSegment.SequenceEqual(stackalloc byte[] {(byte)'i',(byte)'d' }))
                        {
                            //TODO:ReadIDAndInvokeVoidAsyncWithResponse
                        }
                        else
                        {
                            throw new FormatException();
                            
                        }
                    }
                case JsonToken.EndObject:
                    {
                        //TODO:InvokeNonParameterMethodWithoutResponse
                    }
            }
            //TODO:params、idを全て読み進めてからreturnし、後続の処理は非同期実行
        }
    }
}
