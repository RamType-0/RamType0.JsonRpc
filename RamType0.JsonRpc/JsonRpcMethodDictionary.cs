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
        Dictionary<EscapedUTF8String, Delegate> Methods { get; } = new Dictionary<EscapedUTF8String, Delegate>();

       
        public ID? Invoke(EscapedUTF8String methodName,ref JsonReader reader)
        {
            throw new NotImplementedException();
        }

   
       abstract class InvokerBase<TParams>
            where TParams:struct,IMethodParamsObject
       {
            /// <summary>
            /// paramsとidを読み取ります。Requestオブジェクトの末端は読み取りません。
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="formatterResolver"></param>
            /// <returns></returns>
           protected static (TParams,ID?) ReadParamsAndID(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                TParams paramsObj;
                ID? id;
                switch (reader.GetCurrentJsonToken())
                {
                    case JsonToken.String:
                        {
                            var firstPropNameSegment = reader.ReadPropertyNameSegmentRaw().AsSpan();

                            if (firstPropNameSegment.SequenceEqual(stackalloc byte[] { (byte)'p', (byte)'a', (byte)'r', (byte)'a', (byte)'m', (byte)'s', }))
                            {
                                paramsObj = ParamsFormatter<TParams>.Instance.Deserialize(ref reader, formatterResolver);
                                switch (reader.GetCurrentJsonToken())
                                {
                                    case JsonToken.String:
                                        {
                                            if (reader.ReadPropertyNameSegmentRaw().AsSpan().SequenceEqual(stackalloc byte[] { (byte)'i', (byte)'d' }))
                                            {
                                                id = ID.Formatter.Instance.Deserialize(ref reader, formatterResolver);
                                                return (paramsObj, id);
                                            }
                                            else
                                            {
                                                throw new FormatException();
                                            }


                                        }
                                    case JsonToken.EndObject:
                                        {
                                            id = null;
                                            return (paramsObj, id);
                                        }
                                    default:
                                        {
                                            throw new FormatException();
                                        }
                                }

                            }
                            else
                            {
                                if (default(TParams) is IEmptyParamsObject)//JIT時解決！！
                                {
                                    paramsObj = default;
                                }
                                else
                                {
                                    throw new FormatException();
                                }

                                if (firstPropNameSegment.SequenceEqual(stackalloc byte[] { (byte)'i', (byte)'d' }))
                                {
                                    id = ID.Formatter.Instance.Deserialize(ref reader, formatterResolver);
                                    return (paramsObj, id);
                                }
                                else
                                {
                                    throw new FormatException();
                                }


                            }


                        }
                    case JsonToken.EndObject:
                        {
                            if (default(TParams) is IEmptyParamsObject)//JIT時解決！！
                            {
                                paramsObj = default;
                            }
                            else
                            {
                                throw new FormatException();
                            }
                            id = null;
                            return (paramsObj, id);
                        }
                    default:
                        {
                            throw new FormatException();
                        }
                }
            }
            public abstract Task Invoke(ref JsonReader reader);
            
       }
        sealed class VoidInvoker<T> : InvokerBase<T>
            where T: struct, IMethodParamsObject
        {
            public override Task Invoke(ref JsonReader reader)

            {
                var (parameters, id) = ReadParamsAndID(ref reader, JsonSerializer.DefaultResolver);
                reader.ReadIsEndObjectWithVerify();
                if(id is null)
                {
                    return Task.Run(parameters.Invoke);//TODO:ここで関数オブジェクト生成されてるがどうにもならなそう
                }
                else
                {
                    throw new NotImplementedException();
                    return Task.Run(parameters.Invoke);
                }

            }
        }


    }
}
