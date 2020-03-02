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
using static RamType0.JsonRpc.Internal.Emit;
namespace RamType0.JsonRpc.Internal
{
    public abstract class RpcEntry : IRpcEntry
    {
        public static RpcEntry FromDelegate<T>(T d)
            where T:notnull, Delegate
        {
            if (d is MulticastDelegate multicast && multicast.GetInvocationList().Length > 1)
            {
                return RpcDelegateEntryFactory<T>.Instance.CreateEntry(d);
            }
            else if (d.Method.IsStatic)
            {
                return RpcStaticMethodEntryFactory<T>.Instance.CreateEntry(d);
            }
            else
            {
                return RpcInstanceMethodEntryFactory<T>.Instance.CreateEntry(d);
            }
            
        }
        public abstract ArraySegment<byte> ResolveRequest(ArraySegment<byte> serializedParameters, ID? id, IJsonFormatterResolver formatterResolver);
    }
    public class RpcMethodEntry<TMethod,TParams,TResult,TDeserializer,TModifier> : RpcEntry
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
            JsonReader reader;
            var array = parametersSegment.Array;
            if(array is null)
            {
                reader = new JsonReader(Array.Empty<byte>());
            }
            else
            {
                reader = new JsonReader(array, parametersSegment.Offset);
            }
            

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
    internal abstract class RpcEntryFactory<T>
    where T: notnull,Delegate
    {
        public abstract RpcEntry CreateEntry(T d);
    }

 
    internal static class RpcEntryFactoryCache<TDelegate>
        where TDelegate :notnull, Delegate
    {
        public static MethodInfo DelegateInvokeMethodInfo { get; } = typeof(TDelegate).GetMethod("Invoke")!;

        public static Type TParams { get; }
        public static Type TResult { get; }
        public static Type TDeserializer { get; }
        public static Type TModifier { get; }
        public static Type IRpcMethodBodyType { get; }
        public static MethodInfo IRpcMethodInvokeMethodInfo { get; }
        public static Type[] DelegateInvokeParameterTypes { get; }
        public static FieldInfo[] ParameterFields { get; }
        public static Type DelegateResultType { get; }
        static RpcEntryFactoryCache()
        {
            var methodInfo = DelegateInvokeMethodInfo;
            var methodParameters = methodInfo.GetParameters();
            var methodResultType = methodInfo.ReturnType;
            DelegateResultType = methodResultType;
            var tResult = methodResultType == typeof(void) ? typeof(NullResult) : methodResultType;
            TResult = tResult;
            var serializedParameterFields = GetTmpSerializedParametersList();
            FieldInfo? idInjectField = null;
            Type tParams;
            {
                var parameterFields = GetTmpParametersList();
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

                tParams = builder.CreateType()!;
                ParameterFields = parameterFields.ToArray();
                parameterFields.Clear();
            }
            TParams = tParams;
            


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
                tDeserializer = serializedParameterFields.Count == 0 ? typeof(EmptySerializedParamsDeserializer<,,>).MakeGenericType(type3) : typeof(ParamsDeserializer<,,>).MakeGenericType(type3);
            }

            TDeserializer = tDeserializer;

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

            TModifier = tModifier;


            {
                var type2 = TypeArray2;
                type2[0] = tParams;
                type2[1] = tResult;
                var iMethodBodyType = typeof(IRpcMethodBody<,>).MakeGenericType(type2);

                IRpcMethodBodyType = iMethodBodyType;
                var invokeMethodToOverride = iMethodBodyType.GetMethod("Invoke")!;
                IRpcMethodInvokeMethodInfo = invokeMethodToOverride;
                var parameterTypes = new Type[methodParameters.Length];
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    parameterTypes[i] = methodParameters[i].ParameterType;
                }

                DelegateInvokeParameterTypes = parameterTypes;

            }
        }

        private static Type CreateIdInjecter(string name, FieldInfo idInjectField, Type tParams)
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

        //static Type[] TDelegateType { get; } = new[] { typeof(TDelegate) };


    }

    internal static class RpcInstanceMethodEntryFactory<TDelegate>
        where TDelegate: notnull,Delegate
    {
        static RpcInstanceMethodEntryFactory()
        {
            var tParams = RpcEntryFactoryCache<TDelegate>.TParams;

            var tResult = RpcEntryFactoryCache<TDelegate>.TResult;
            var methodInfo = RpcEntryFactoryCache<TDelegate>.DelegateInvokeMethodInfo;
            var iMethodBodyType = RpcEntryFactoryCache<TDelegate>.IRpcMethodBodyType;
            var invokeMethodToOverride = RpcEntryFactoryCache<TDelegate>.IRpcMethodInvokeMethodInfo;
            var parameterFields = RpcEntryFactoryCache<TDelegate>.ParameterFields;
            var methodResultType = RpcEntryFactoryCache<TDelegate>.DelegateResultType;
            var parameterTypes = RpcEntryFactoryCache<TDelegate>.DelegateInvokeParameterTypes;
            var type1 = TypeArray1;
            type1[0] = tParams;

            {
                var v = BuildRpcMethodTypeSignature($"{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}.Invoker.Instance", iMethodBodyType, type1, tResult, invokeMethodToOverride);

                var builder = v.TypeBuilder;


                var fpField = DefineFunctionPointerProperty(builder, FunctionPointerSetter);
                var targetField = DefineTargetObjectProperty(builder, TargetObjectSetter);
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
                    il.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, methodResultType, parameterTypes, null);
                    if (methodResultType == typeof(void))
                    {
                        il.DeclareLocal(typeof(NullResult));
                        //il.Emit(OpCodes.Ldloca_S, (byte)0);
                        //il.Emit(OpCodes.Initobj,typeof(NullResult));
                        il.Emit(OpCodes.Ldloc_0);
                    }
                    il.Emit(OpCodes.Ret);
                }
                var tMethodBody = builder.CreateType()!;
                {
                    var type6 = TypeArray6;
                    type6[0] = typeof(TDelegate);
                    type6[1] = tMethodBody;
                    type6[2] = tParams;
                    type6[3] = tResult;
                    type6[4] = RpcEntryFactoryCache<TDelegate>.TDeserializer;
                    type6[5] = RpcEntryFactoryCache<TDelegate>.TModifier;
                    Instance = Unsafe.As<RpcEntryFactory<TDelegate>>(Activator.CreateInstance(typeof(RpcInstanceMethodEntryFactory<,,,,,>).MakeGenericType(type6)));
                }
            }
        }
        public static RpcEntryFactory<TDelegate> Instance { get; }
    }

    internal static class RpcStaticMethodEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
    {
        static RpcStaticMethodEntryFactory()
        {
            var tParams = RpcEntryFactoryCache<TDelegate>.TParams;

            var tResult = RpcEntryFactoryCache<TDelegate>.TResult;
            var methodInfo = RpcEntryFactoryCache<TDelegate>.DelegateInvokeMethodInfo;
            var iMethodBodyType = RpcEntryFactoryCache<TDelegate>.IRpcMethodBodyType;
            var invokeMethodToOverride = RpcEntryFactoryCache<TDelegate>.IRpcMethodInvokeMethodInfo;
            var parameterFields = RpcEntryFactoryCache<TDelegate>.ParameterFields;
            var methodResultType = RpcEntryFactoryCache<TDelegate>.DelegateResultType;
            var parameterTypes = RpcEntryFactoryCache<TDelegate>.DelegateInvokeParameterTypes;
            var type1 = TypeArray1;
            type1[0] = tParams;

            {
                var v = BuildRpcMethodTypeSignature($"{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}.Invoker.Static", iMethodBodyType, type1, tResult, invokeMethodToOverride);

                var builder = v.TypeBuilder;


                FieldBuilder fpField = DefineFunctionPointerProperty(builder, FunctionPointerSetter);
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
                    il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, methodResultType, parameterTypes, null);
                    if (methodResultType == typeof(void))
                    {
                        il.DeclareLocal(typeof(NullResult));
                        //il.Emit(OpCodes.Ldloca_S, (byte)0);
                        //il.Emit(OpCodes.Initobj,typeof(NullResult));
                        il.Emit(OpCodes.Ldloc_0);
                    }
                    il.Emit(OpCodes.Ret);
                }
                var tMethodBody = builder.CreateType()!;
                {
                    var type6 = TypeArray6;
                    type6[0] = typeof(TDelegate);
                    type6[1] = tMethodBody;
                    type6[2] = tParams;
                    type6[3] = tResult;
                    type6[4] = RpcEntryFactoryCache<TDelegate>.TDeserializer;
                    type6[5] = RpcEntryFactoryCache<TDelegate>.TModifier;
                    Instance = Unsafe.As<RpcEntryFactory<TDelegate>>(Activator.CreateInstance(typeof(RpcStaticMethodEntryFactory<,,,,,>).MakeGenericType(type6)));

                }
            }
        }
        public static RpcEntryFactory<TDelegate> Instance { get; }
    }

    internal static class RpcDelegateEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
    {

        static RpcDelegateEntryFactory()
        {
            var tParams = RpcEntryFactoryCache<TDelegate>.TParams;
            var tResult = RpcEntryFactoryCache<TDelegate>.TResult;
            var methodInfo = RpcEntryFactoryCache<TDelegate>.DelegateInvokeMethodInfo;
            var iMethodBodyType = RpcEntryFactoryCache<TDelegate>.IRpcMethodBodyType;
            var invokeMethodToOverride = RpcEntryFactoryCache<TDelegate>.IRpcMethodInvokeMethodInfo;
            var parameterFields = RpcEntryFactoryCache<TDelegate>.ParameterFields;
            var methodResultType = RpcEntryFactoryCache<TDelegate>.DelegateResultType;
            //var parameterTypes = RpcEntryFactoryDelegateTypeCache<TDelegate>.DelegateInvokeParameterTypes;
            var type1 = TypeArray1;
            type1[0] = tParams;
            {
                var v = BuildRpcMethodTypeSignature($"{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}.Invoker.Delegate", iMethodBodyType, type1, tResult, invokeMethodToOverride);

                var builder = v.TypeBuilder;
                var delegateField = DefineDelegateProperty(builder);
                var il = v.InvokeMethodBuilder.GetILGenerator();
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, delegateField);
                    foreach (var field in parameterFields)
                    {
                        //Push parameters
                        il.Emit(OpCodes.Ldarga_S, (byte)1);
                        //il.Emit(OpCodes.Constrained, tParams);
                        il.Emit(OpCodes.Ldfld, field);
                    }
                    //Push this
                    il.Emit(OpCodes.Callvirt, methodInfo);
                    if (methodResultType == typeof(void))
                    {
                        il.DeclareLocal(typeof(NullResult));
                        //il.Emit(OpCodes.Ldloca_S, (byte)0);
                        //il.Emit(OpCodes.Initobj,typeof(NullResult));
                        il.Emit(OpCodes.Ldloc_0);
                    }
                    il.Emit(OpCodes.Ret);
                }
                var tMethodBody = builder.CreateType()!;
                {
                    var type6 = TypeArray6;
                    type6[0] = typeof(TDelegate);
                    type6[1] = tMethodBody;
                    type6[2] = tParams;
                    type6[3] = tResult;
                    type6[4] = RpcEntryFactoryCache<TDelegate>.TDeserializer;
                    type6[5] = RpcEntryFactoryCache<TDelegate>.TModifier;
                    Instance = Unsafe.As<RpcEntryFactory<TDelegate>>(Activator.CreateInstance(typeof(RpcDelegateEntryFactory<,,,,,>).MakeGenericType(type6)));
                    
                }
            }
        }
        public static RpcEntryFactory<TDelegate> Instance { get; }
    }

    internal static class Emit
    {
        internal static CustomAttributeBuilder RpcIdAttributeBuilder { get; } = new CustomAttributeBuilder(typeof(RpcIDAttribute).GetConstructor(Type.EmptyTypes)!, Array.Empty<object>());
        internal static MethodInfo FunctionPointerSetter { get; } = typeof(IFunctionPointerContainer).GetMethod("set_FunctionPointer")!;
        internal static MethodInfo TargetObjectSetter { get; } = typeof(IObjectReferenceContainer).GetMethod("set_Target")!;
        internal static MethodInfo DelegateSetter { get; } = typeof(IMulticastDelegateContainer<Delegate>).GetMethod("set_Delegate")!;
        internal static Type[] IntPtrType { get; } = new[] { typeof(IntPtr) };
        internal static Type[] ObjectType { get; } = new[] { typeof(object) };

        internal static Type[] DelegateType { get; } = new[] { typeof(Delegate) };
        [ThreadStatic]
        static List<FieldInfo>? _parameters, _serializedParameters;

        internal static List<FieldInfo> GetTmpParametersList()
        {
            if (_parameters is null)
            {
                _parameters = new List<FieldInfo>();
            }
            else
            {
                _parameters.Clear();
            }
            return _parameters;
        }

        internal static List<FieldInfo> GetTmpSerializedParametersList()
        {
            if (_serializedParameters is null)
            {
                _serializedParameters = new List<FieldInfo>();
            }
            else
            {
                _serializedParameters.Clear();
            }
            return _serializedParameters;
        }


        internal static FieldBuilder DefineFunctionPointerProperty(TypeBuilder builder, MethodInfo setterToOverride)
        {
            var field = builder.DefineField("functionPointer", typeof(IntPtr), FieldAttributes.Public);
            builder.AddInterfaceImplementation(typeof(IFunctionPointerContainer));

            var setter = builder.DefineMethod("set_FunctionPointer", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), IntPtrType);
            var ilG = setter.GetILGenerator();
            {
                ilG.Emit(OpCodes.Ldarg_0);
                ilG.Emit(OpCodes.Ldarg_1);
                ilG.Emit(OpCodes.Stfld, field);
                ilG.Emit(OpCodes.Ret);
            }
            builder.DefineMethodOverride(setter, setterToOverride);
            return field;
        }
        internal static FieldBuilder DefineTargetObjectProperty(TypeBuilder builder, MethodInfo setterToOverride)
        {
            var field = builder.DefineField("target", typeof(object), FieldAttributes.Public);
            builder.AddInterfaceImplementation(typeof(IObjectReferenceContainer));

            var setter = builder.DefineMethod("set_Target", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), ObjectType);
            var ilG = setter.GetILGenerator();
            {
                ilG.Emit(OpCodes.Ldarg_0);
                ilG.Emit(OpCodes.Ldarg_1);
                ilG.Emit(OpCodes.Stfld, field);
                ilG.Emit(OpCodes.Ret);
            }
            builder.DefineMethodOverride(setter, setterToOverride);
            return field;
        }

        internal static FieldBuilder DefineDelegateProperty(TypeBuilder builder)
        {
            var field = builder.DefineField("Delegate", typeof(Delegate), FieldAttributes.Public);
            builder.AddInterfaceImplementation(typeof(IMulticastDelegateContainer<Delegate>));

            var setter = builder.DefineMethod("set_Delegate", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), DelegateType);
            var ilG = setter.GetILGenerator();
            {
                ilG.Emit(OpCodes.Ldarg_0);
                ilG.Emit(OpCodes.Ldarg_1);
                ilG.Emit(OpCodes.Stfld, field);
                ilG.Emit(OpCodes.Ret);
            }
            builder.DefineMethodOverride(setter, DelegateSetter);
            return field;
        }
        internal static (TypeBuilder TypeBuilder, MethodBuilder InvokeMethodBuilder) BuildRpcMethodTypeSignature(string name, Type iMethodBodyType, Type[] tParams, Type tResult, MethodInfo invokeMethodToOverride)
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


    }

    internal sealed class RpcInstanceMethodEntryFactory<TDelegate,TMethod, TParams, TResult, TDeserializer, TModifier> : RpcEntryFactory<TDelegate>

        where TDelegate : notnull,Delegate
        where TMethod : struct, IRpcMethodBody<TParams, TResult>, IFunctionPointerContainer,IObjectReferenceContainer
        where TParams : notnull
        where TDeserializer : notnull, IParamsDeserializer<TParams>
        where TModifier : notnull, IMethodParamsModifier<TParams>
    { 
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;
        
        

        public override RpcEntry CreateEntry(TDelegate d)
        {
            var method = this.method;
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            method.Target = d.Target!;
            return new RpcMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, deserializer, modifier);
        }
    }

    internal sealed class RpcStaticMethodEntryFactory<TDelegate,TMethod, TParams, TResult, TDeserializer, TModifier> : RpcEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcMethodBody<TParams, TResult>,IFunctionPointerContainer
        where TParams : notnull
        where TDeserializer : notnull, IParamsDeserializer<TParams>
        where TModifier : notnull, IMethodParamsModifier<TParams>
    {
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;



        public override RpcEntry CreateEntry(TDelegate d)
        {
            var method = this.method;
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            return new RpcMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, deserializer, modifier);
        }
    }

    internal sealed class RpcDelegateEntryFactory<TDelegate, TMethod, TParams, TResult, TDeserializer, TModifier> : RpcEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcMethodBody<TParams, TResult>,IMulticastDelegateContainer<Delegate>
        where TParams : notnull
        where TDeserializer : notnull, IParamsDeserializer<TParams>
        where TModifier : notnull, IMethodParamsModifier<TParams>
    {
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;
        public override RpcEntry CreateEntry(TDelegate d)
        {
            var method = this.method;
            method.Delegate = d;
            return new RpcMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, deserializer, modifier);
        }
    }
}
