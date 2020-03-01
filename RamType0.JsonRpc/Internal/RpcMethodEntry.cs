using RamType0.JsonRpc.Server;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Utf8Json;
using static RamType0.JsonRpc.Emit;
namespace RamType0.JsonRpc.Internal
{
    public abstract class RpcMethodEntry : IRpcMethodEntry
    {
        public static RpcMethodEntry FromDelegate<T>(T d)
            where T:notnull, Delegate
        {
            if (d.Method.IsStatic)
            {
                return RpcMethodEntryFactoryDelegateTypeCache<T>.StaticMethodFactory.CreateEntry(d);
            }
            else
            {
                return RpcMethodEntryFactoryDelegateTypeCache<T>.InstanceMethodFactory.CreateEntry(d);
            }
            
        }
        public abstract ArraySegment<byte> ResolveRequest(ArraySegment<byte> serializedParameters, ID? id, IJsonFormatterResolver formatterResolver);
    }
    public class RpcMethodEntry<TMethod,TParams,TResult,TDeserializer,TModifier> : RpcMethodEntry
        where TMethod : notnull,IRpcMethodBody<TParams, TResult>
        where TParams : notnull
        where TDeserializer : notnull,IParamsDeserializer<TParams>
        where TModifier : notnull,IMethodParamsModifier<TParams>
    {
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;

        public RpcMethodEntry(TMethod method, TDeserializer deserializer, TModifier modifier)
        {
            this.method = method;
            this.deserializer = deserializer;
            this.modifier = modifier;
        }

        public override ArraySegment<byte> ResolveRequest(ArraySegment<byte> parametersSegment, ID? id,IJsonFormatterResolver formatterResolver)
        {

            TParams parameters;
            var reader = new JsonReader(parametersSegment.Array!, parametersSegment.Offset);

            try
            {
                parameters = deserializer.Deserialize(ref reader, formatterResolver);
            }
            catch (JsonParsingException)
            {
                if (id is ID reqID)
                {
                    return JsonSerializer.SerializeUnsafe(ErrorResponse.InvalidParams(reqID, Encoding.UTF8.GetString(parametersSegment)), formatterResolver);
                }
                else
                {
                    return ArraySegment<byte>.Empty;
                }

            }
            TResult result;
            try
            {
                modifier.Modify(ref parameters,parametersSegment,id,formatterResolver);
                result = method.Invoke(parameters);
            }
            catch(Exception e)
            {
                if(id is ID requestID)
                {
                    return JsonSerializer.SerializeUnsafe(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e));
                }
                else
                {
                    return ArraySegment<byte>.Empty;
                }
                
            }
            {
                if (id is ID requestID)
                {
                    if(result is null)
                    {
                        return JsonSerializer.SerializeUnsafe(ResultResponse.Create(requestID, new NullResult()), formatterResolver);
                    }
                    else
                    {
                        return JsonSerializer.SerializeUnsafe(ResultResponse.Create(requestID, result), formatterResolver);
                    }
                    

                }
                else
                {
                    return ArraySegment<byte>.Empty;
                }
                
            }

        }
    }
    internal abstract class RpcInstanceMethodEntryFactory<T>
        where T: notnull ,Delegate
    {
        public abstract RpcMethodEntry CreateEntry(T d);
    }

    internal abstract class RpcStaticMethodEntryFactory<T>
        where T : notnull, Delegate
    {
        public abstract RpcMethodEntry CreateEntry(T d);
    }
    internal static class RpcMethodEntryFactoryDelegateTypeCache<T>
        where T :notnull, Delegate
    {
        public static RpcInstanceMethodEntryFactory<T> InstanceMethodFactory { get; }
        public static RpcStaticMethodEntryFactory<T> StaticMethodFactory { get; }
        static RpcMethodEntryFactoryDelegateTypeCache()
        {
            var methodInfo = typeof(T).GetMethod("Invoke")!;
            var methodParameters = methodInfo.GetParameters();
            var methodResultType = methodInfo.ReturnType;
            var tResult = methodResultType == typeof(void) ? typeof(NullResult) : methodResultType;
            var parameterFields = GetTmpParametersList();
            var serializedParameterFields = GetTmpSerializedParametersList();
            FieldInfo? idInjectField = null;
            TypeInfo tParams;
            {
                var builder = JsonRpc.Emit.ModuleBuilder.DefineType($"{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}.Parameters", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(ValueType));
                foreach (var param in methodParameters)
                {
                    var name = param.Name;
                    if(name is null)
                    {
                        throw new FormatException("anonymous parameter is not supported.");
                    }
                    else
                    {
                        var type = param.ParameterType;
                        var field = builder.DefineField(name, type, FieldAttributes.Public);
                        var rpcIDAttribute = param.GetCustomAttribute<RpcIDAttribute>();
                        if (rpcIDAttribute is null | type != typeof(ID?))
                        {
                            serializedParameterFields.Add(field);
                        }
                        else
                        {
                            idInjectField = field;
                            field.SetCustomAttribute(RpcIdAttributeBuilder);
                            
                        }
                        parameterFields.Add(field);
                    }
                    
                }

                tParams = builder.CreateTypeInfo()!;
            }

            TypeInfo tStaticMethodBody, tInstanceMethodBody;
            {
                var type2 = TypeArray2;
                type2[0] = tParams;
                type2[1] = tResult;
                var iMethodBodyType = typeof(IRpcMethodBody<,>).MakeGenericType(type2);
                var type1 = TypeArray1;
                type1[0] = tParams;
                var invokeMethodToOverride = iMethodBodyType.GetMethod("Invoke")!;
                var fpSetterToOverride = typeof(IFunctionPointerContainer).GetMethod("set_FunctionPointer")!;
                var objectSetterToOverride = typeof(IObjectReferenceContainer).GetMethod("set_Target")!;
                var parameterTypes = new Type[methodParameters.Length];
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    parameterTypes[i] = methodParameters[i].ParameterType;
                }

                {
                    var v = BuildRpcMethodTypeSignature($"{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}.Invoker.Static", iMethodBodyType, type1, tResult, invokeMethodToOverride);

                    var builder = v.TypeBuilder;


                    FieldBuilder fpField = DefineFunctionPointerProperty(builder, fpSetterToOverride);
                    var il = v.InvokeMethodBuilder.GetILGenerator();
                    {
                        foreach (var field in parameterFields)
                        {
                            //Push parameters
                            il.Emit(OpCodes.Ldarga_S, (byte)1);
                            //il.Emit(OpCodes.Constrained, tParams);
                            il.Emit(OpCodes.Ldfld, field);
                        }
                        //Push this
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, fpField);
                        il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, methodResultType, parameterTypes, Type.EmptyTypes);
                        if (methodResultType == typeof(void))
                        {
                            il.DeclareLocal(typeof(NullResult));
                            il.Emit(OpCodes.Ldloca_S, (byte)0);
                            il.Emit(OpCodes.Initobj);
                            il.Emit(OpCodes.Ldloc_0);
                        }
                        il.Emit(OpCodes.Ret);
                    }
                    tStaticMethodBody = builder.CreateTypeInfo()!;
                }
                {
                    var v = BuildRpcMethodTypeSignature($"{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}.Invoker.Instance", iMethodBodyType, type1, tResult, invokeMethodToOverride);

                    var builder = v.TypeBuilder;


                    var fpField = DefineFunctionPointerProperty(builder, fpSetterToOverride);
                    var targetField = DefineTargetObjectProperty(builder, objectSetterToOverride);
                    var il = v.InvokeMethodBuilder.GetILGenerator();
                    {
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, targetField);
                        foreach (var field in parameterFields)
                        {
                            //Push parameters
                            il.Emit(OpCodes.Ldarga_S, (byte)1);
                            //il.Emit(OpCodes.Constrained, tParams);
                            il.Emit(OpCodes.Ldfld, field);
                        }
                        //Push this
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, fpField);
                        il.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, methodResultType, parameterTypes, Type.EmptyTypes);
                        if (methodResultType == typeof(void))
                        {
                            il.DeclareLocal(typeof(NullResult));
                            il.Emit(OpCodes.Ldloca_S, (byte)0);
                            il.Emit(OpCodes.Initobj);
                            il.Emit(OpCodes.Ldloc_0);
                        }
                        il.Emit(OpCodes.Ret);
                    }
                    tInstanceMethodBody = builder.CreateTypeInfo()!;
                }




            }
            Type tDeserializer;
            {
                var tObjStyle = ParamsDeserializerBuilder.ObjectStyle.Create(tParams);
                var buffer = ArrayPool<FieldInfo>.Shared.Rent(serializedParameterFields.Count);
                serializedParameterFields.CopyTo(buffer);
                var tArrayStyle = ParamsDeserializerBuilder.ArrayStyle.Create(tParams, buffer.AsSpan(..serializedParameterFields.Count));
                ArrayPool<FieldInfo>.Shared.Return(buffer);
                var type3 = TypeArray3;
                type3[0] = tParams;
                type3[1] = tObjStyle;
                type3[2] = tArrayStyle;
                tDeserializer = typeof(ParamsDeserializer<,,>).MakeGenericType(type3);
            }

            Type tModifier; 
            {
                if(idInjectField is null)
                {
                    var type1 = TypeArray1;
                    type1[0] = tParams;
                    tModifier = typeof(EmptyModifier<>).MakeGenericType(type1);

                }
                else
                {
                    tModifier = CreateIdInjecter($"{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}.Parameters.RpcIdInjecter", idInjectField, tParams);
                }

            }
            {
                var type6 = TypeArray6;
                type6[0] = typeof(T);
                type6[1] = tInstanceMethodBody;
                type6[2] = tParams;
                type6[3] = tResult;
                type6[4] = tDeserializer;
                type6[5] = tModifier;
                InstanceMethodFactory = Unsafe.As<RpcInstanceMethodEntryFactory<T>>(Activator.CreateInstance(typeof(RpcInstanceMethodEntryFactory<,,,,,>).MakeGenericType(type6)));
                StaticMethodFactory = Unsafe.As<RpcStaticMethodEntryFactory<T>>(Activator.CreateInstance(typeof(RpcStaticMethodEntryFactory<,,,,,>).MakeGenericType(type6)));
            }
            


        }

        private static Type CreateIdInjecter(string name, FieldInfo idInjectField, TypeInfo tParams)
        {
            Type tModifier;
            var builder = JsonRpc.Emit.ModuleBuilder.DefineType(name, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(ValueType));

            var type1 = TypeArray1;
            type1[0] = tParams;
            var iModifierType = typeof(IMethodParamsModifier<>).MakeGenericType(type1);
            builder.AddInterfaceImplementation(iModifierType);
            var type4 = TypeArray4;
            type4[0] = tParams.MakeByRefType();
            type4[1] = typeof(ArraySegment<byte>);
            type4[2] = typeof(ID?);
            type4[3] = typeof(IJsonFormatterResolver);
            var modifyMethod = builder.DefineMethod("Modify", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), type4);
            var il = modifyMethod.GetILGenerator();
            {
                il.Emit(OpCodes.Ldarg_1);
                if (!tParams.IsValueType)
                {
                    il.Emit(OpCodes.Ldind_Ref);
                }

                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Stfld, idInjectField);
                il.Emit(OpCodes.Ret);
            }
            builder.DefineMethodOverride(modifyMethod, iModifierType.GetMethod("Modify")!);
            tModifier = builder.CreateType()!;
            return tModifier;
        }

        private static FieldBuilder DefineFunctionPointerProperty(TypeBuilder builder, MethodInfo setterToOverride)
        {
            var field = builder.DefineField("functionPointer", typeof(IntPtr), FieldAttributes.Public);
            builder.AddInterfaceImplementation(typeof(IFunctionPointerContainer));

            var fpSetter = builder.DefineMethod("set_FunctionPointer", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), IntPtrType);
            var ilG = fpSetter.GetILGenerator();
            {
                ilG.Emit(OpCodes.Ldarg_0);
                ilG.Emit(OpCodes.Ldarg_1);
                ilG.Emit(OpCodes.Stfld, field);
                ilG.Emit(OpCodes.Ret);
            }
            builder.DefineMethodOverride(fpSetter, setterToOverride);
            return field;
        }
        private static FieldBuilder DefineTargetObjectProperty(TypeBuilder builder, MethodInfo setterToOverride)
        {
            var field = builder.DefineField("target", typeof(object), FieldAttributes.Public);
            builder.AddInterfaceImplementation(typeof(IObjectReferenceContainer));

            var fpSetter = builder.DefineMethod("set_Target", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), ObjectType);
            var ilG = fpSetter.GetILGenerator();
            {
                ilG.Emit(OpCodes.Ldarg_0);
                ilG.Emit(OpCodes.Ldarg_1);
                ilG.Emit(OpCodes.Stfld, field);
                ilG.Emit(OpCodes.Ret);
            }
            builder.DefineMethodOverride(fpSetter, setterToOverride);
            return field;
        }
        private static (TypeBuilder TypeBuilder, MethodBuilder InvokeMethodBuilder) BuildRpcMethodTypeSignature(string name, Type iMethodBodyType, Type[] tParams, Type tResult,MethodInfo invokeMethodToOverride)
        {
            var builder = JsonRpc.Emit.ModuleBuilder.DefineType(name, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(ValueType));
            //var instanceMB = JsonRpc.Emit.ModuleBuilder.DefineType($"{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}.Invoker.Instance", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(ValueType));

            
            //instanceMB.DefineField("methodPtr", typeof(IntPtr), FieldAttributes.Public);

            //instanceMB.DefineField("target", typeof(object), FieldAttributes.Public);



            builder.AddInterfaceImplementation(iMethodBodyType);



            var invokeMethodBody = builder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig, tResult, tParams);
          
            

            builder.DefineMethodOverride(invokeMethodBody, invokeMethodToOverride);
            return (builder, invokeMethodBody);
        }

        [ThreadStatic]
        static List<FieldInfo>? _parameters, _serializedParameters;

        private static List<FieldInfo> GetTmpParametersList()
        {
            if(_parameters is null)
            {
                _parameters = new List<FieldInfo>();
            }
            else
            {
                _parameters.Clear();
            }
            return _parameters;
        }

        private static List<FieldInfo> GetTmpSerializedParametersList()
        {
            if(_serializedParameters is null)
            {
                _serializedParameters = new List<FieldInfo>();
            }
            else
            {
                _serializedParameters.Clear();
            }
            return _serializedParameters;
        }

        static CustomAttributeBuilder RpcIdAttributeBuilder { get; } = new CustomAttributeBuilder(typeof(RpcIDAttribute).GetConstructor(Type.EmptyTypes)!,Array.Empty<object>());
        static MethodInfo MethodBodyInvoke { get; } = typeof(IRpcMethodBody<,>).GetMethod("Invoke")!;

        static Type[] IntPtrType { get; } = new[] { typeof(IntPtr) };
        static Type[] ObjectType { get; } = new[] { typeof(object) };
    }

    internal sealed class RpcInstanceMethodEntryFactory<TDelegate,TMethod, TParams, TResult, TDeserializer, TModifier> : RpcInstanceMethodEntryFactory<TDelegate> 
    
        where TDelegate : notnull,Delegate
        where TMethod : struct, IRpcMethodBody<TParams, TResult>, IFunctionPointerContainer,IObjectReferenceContainer
        where TParams : notnull
        where TDeserializer : notnull, IParamsDeserializer<TParams>
        where TModifier : notnull, IMethodParamsModifier<TParams>
    { 
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;
        
        

        public override RpcMethodEntry CreateEntry(TDelegate d)
        {
            var method = this.method;
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            method.Target = d.Target!;
            return new RpcMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, deserializer, modifier);
        }
    }

    internal sealed class RpcStaticMethodEntryFactory<TDelegate,TMethod, TParams, TResult, TDeserializer, TModifier> : RpcStaticMethodEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcMethodBody<TParams, TResult>,IFunctionPointerContainer
        where TParams : notnull
        where TDeserializer : notnull, IParamsDeserializer<TParams>
        where TModifier : notnull, IMethodParamsModifier<TParams>
    {
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;



        public override RpcMethodEntry CreateEntry(TDelegate d)
        {
            var method = this.method;
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            return new RpcMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, deserializer, modifier);
        }
    }

    
}
