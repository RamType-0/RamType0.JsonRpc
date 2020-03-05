using RamType0.JsonRpc.Server;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;

namespace RamType0.JsonRpc.Internal
{
    using static Emit;
    using static RpcMethodEntryFactoryHelper;
    internal abstract class RpcAsyncMethodEntryFactory<T>
   where T : notnull, Delegate
    {
        public abstract RpcAsyncMethodEntry CreateAsyncMethodEntry(T d, IExceptionHandler exceptionHandler);
    }
    internal abstract class RpcMethodEntryFactory<T> : RpcAsyncMethodEntryFactory<T>
   where T : notnull, Delegate
    {
        public abstract RpcMethodEntry CreateMethodEntry(T d, IExceptionHandler exceptionHandler);
        public override RpcAsyncMethodEntry CreateAsyncMethodEntry(T d, IExceptionHandler exceptionHandler)
        {
            return CreateMethodEntry(d, exceptionHandler);
        }
    }
    internal static class RpcMethodEntryFactoryCache<TDelegate>
        where TDelegate : notnull, Delegate
    {
        public static MethodInfo DelegateInvokeMethodInfo { get; }
        public static Type? ValueTaskType { get; }
        public static ConstructorInfo? ValueTaskConstructor { get; }
        public static Type TParams { get; }
        public static Type TResult { get; }
        public static Type IMethodBodyReturnType { get; }
        public static Type ReturnType { get; }
        public static Type TDeserializer { get; }
        public static Type TModifier { get; }
        public static Type IRpcMethodBodyType { get; }
        public static MethodInfo IRpcMethodInvokeMethodInfo { get; }
        public static Type[] DelegateInvokeParameterTypes { get; }
        public static FieldInfo[] ParameterFields { get; }
        static RpcMethodEntryFactoryCache()
        {
            var methodInfo = typeof(TDelegate).GetMethod("Invoke");
            if(methodInfo is null)
            {
                throw new ArgumentException($"{typeof(TDelegate)} does not have Invoke method.");
            }
            DelegateInvokeMethodInfo = methodInfo;
            var methodParameters = methodInfo.GetParameters();
            var returnType = methodInfo.ReturnType;
            ReturnType = returnType;
            Type tResult,iMethodBodyReturnType;
            Type? valueTaskType;
            ConstructorInfo? ctor;
            if (returnType.IsConstructedGenericType)
            {
                var genericType = returnType.GetGenericTypeDefinition();
                if(genericType == typeof(Task<>))
                {
                    var genericArgs = returnType.GetGenericArguments();
                    valueTaskType = typeof(ValueTask<>).MakeGenericType(genericArgs);
                    var type1 = TypeArray1;
                    type1[0] = returnType;
                    ctor = valueTaskType.GetConstructor(type1)!;
                    tResult = genericArgs[0];
                    iMethodBodyReturnType = valueTaskType;
                }
                else
                {
                    ctor = null;
                    iMethodBodyReturnType = returnType;
                    if (genericType == typeof(ValueTask<>))
                    {
                        valueTaskType = returnType;
                        tResult = returnType.GetGenericArguments()[0];
                    }

                    else
                    {
                        valueTaskType = null;
                        tResult = returnType;
                    }
                }
                
            }
            else
            {
                if (returnType == typeof(Task))
                {
                    valueTaskType = typeof(ValueTask);
                    var type1 = TypeArray1;
                    type1[0] = typeof(Task);
                    ctor = valueTaskType.GetConstructor(type1);
                    tResult = typeof(void);
                    iMethodBodyReturnType = valueTaskType;
                    
                }else
                {
                    ctor = null;
                    if (returnType == typeof(ValueTask))
                    {
                        valueTaskType = typeof(ValueTask);
                        tResult = typeof(void);
                        iMethodBodyReturnType = valueTaskType;
                    }
                    else
                    {
                        valueTaskType = null;
                        iMethodBodyReturnType = returnType;
                        if (returnType == typeof(void))
                        {
                            tResult = typeof(NullResult);
                        }

                        else
                        {
                            tResult = returnType;
                        }
                    }
                }
                
            }


            TResult = tResult;
            ValueTaskType = valueTaskType;
            ValueTaskConstructor = ctor;
            IMethodBodyReturnType = iMethodBodyReturnType;
            var (tParams, parameters, serializedParameters, idInjectField) = CreateParamsType(methodInfo, methodParameters);
            TParams = tParams;
            ParameterFields = parameters;

            Type tDeserializer;
            tDeserializer = CreateDeserializerType(tParams, serializedParameters);
            ArrayPool<FieldInfo>.Shared.Return(serializedParameters.Array!);
            TDeserializer = tDeserializer;

            Type tModifier;
            {
                if (idInjectField is null)
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
                Type iMethodBodyType;
                if (valueTaskType is null)
                {
                    if (returnType == typeof(void))
                    {
                        var type1 = TypeArray1;
                        type1[0] = tParams;
                        iMethodBodyType = typeof(IRpcMethodBody<>).MakeGenericType(type1);
                    }
                    else
                    {
                        var type2 = TypeArray2;
                        type2[0] = tParams;
                        type2[1] = returnType;
                        iMethodBodyType = typeof(IRpcMethodBody<,>).MakeGenericType(type2);

                    }
                }
                else
                {
                    if (tResult == typeof(void))
                    {
                        var type1 = TypeArray1;
                        type1[0] = tParams;
                        iMethodBodyType = typeof(IRpcAsyncMethodBody<>).MakeGenericType(type1);
                    }
                    else
                    {
                        var type2 = TypeArray2;
                        type2[0] = tParams;
                        type2[1] = tResult;
                        iMethodBodyType = typeof(IRpcAsyncMethodBody<,>).MakeGenericType(type2);
                    }
                }



                IRpcMethodBodyType = iMethodBodyType;
                var methodName = valueTaskType is null ? "Invoke" : "InvokeAsync";
                var invokeMethodToOverride = iMethodBodyType.GetMethod(methodName)!;
                IRpcMethodInvokeMethodInfo = invokeMethodToOverride;
                var parameterTypes = new Type[methodParameters.Length];
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    parameterTypes[i] = methodParameters[i].ParameterType;
                }

                DelegateInvokeParameterTypes = parameterTypes;

            }
        }

      
        //static Type[] TDelegateType { get; } = new[] { typeof(TDelegate) };


    }

    internal static class RpcInstanceMethodEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
    {
        static RpcInstanceMethodEntryFactory()
        {
            var tParams = RpcMethodEntryFactoryCache<TDelegate>.TParams;
            var tResult = RpcMethodEntryFactoryCache<TDelegate>.TResult;
            var valueTaskType = RpcMethodEntryFactoryCache<TDelegate>.ValueTaskType;
            var valueTaskCtor = RpcMethodEntryFactoryCache<TDelegate>.ValueTaskConstructor;
            var iMethodBodyReturnType = RpcMethodEntryFactoryCache<TDelegate>.IMethodBodyReturnType;
            var returnType = RpcMethodEntryFactoryCache<TDelegate>.ReturnType;
            var methodInfo = RpcMethodEntryFactoryCache<TDelegate>.DelegateInvokeMethodInfo;
            var iMethodBodyType = RpcMethodEntryFactoryCache<TDelegate>.IRpcMethodBodyType;
            var invokeMethodToOverride = RpcMethodEntryFactoryCache<TDelegate>.IRpcMethodInvokeMethodInfo;
            var parameterFields = RpcMethodEntryFactoryCache<TDelegate>.ParameterFields;
            var parameterTypes = RpcMethodEntryFactoryCache<TDelegate>.DelegateInvokeParameterTypes;
            var tDeserializer = RpcMethodEntryFactoryCache<TDelegate>.TDeserializer;
            var tModifier = RpcMethodEntryFactoryCache<TDelegate>.TModifier;
            var type1 = TypeArray1;
            type1[0] = tParams;

            {
                var v = BuildRpcMethodTypeSignature($"{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}.Invoker.Instance", iMethodBodyType, type1, iMethodBodyReturnType, invokeMethodToOverride);

                var builder = v.TypeBuilder;


                var fpField = DefineFunctionPointerProperty(builder, FunctionPointerSetter);
                var targetField = DefineTargetObjectProperty(builder, TargetObjectSetter);
                var il = v.InvokeMethodBuilder.GetILGenerator();
                {
                    if (!(valueTaskCtor is null))
                    {
                        il.DeclareLocal(valueTaskType!);
                        il.Emit(OpCodes.Ldloca_S, (byte)0);

                    }
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
                    il.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, returnType, parameterTypes, null);
                    if (!(valueTaskCtor is null))
                    {
                        il.Emit(OpCodes.Call, valueTaskCtor);
                        il.Emit(OpCodes.Ldloc_0);
                    }
                        il.Emit(OpCodes.Ret);
                }
                var tMethodBody = builder.CreateType()!;
                RpcAsyncMethodEntryFactory<TDelegate> factory;
                if(valueTaskType is null)
                {
                    factory = CreateEntryFactory<TDelegate>(typeof(RpcInstanceMethodEntryFactory<,,,,,>),tParams, tResult, tDeserializer, tModifier, tMethodBody);
                }
                else
                {
                    if(tResult == typeof(void))
                    {
                        factory = CreateAsyncActionEntryFactory<TDelegate>(typeof(RpcInstanceAsyncMethodEntryFactory<,,,,>), tParams, tDeserializer, tModifier, tMethodBody);
                    }
                    else
                    {
                        factory = CreateEntryFactory<TDelegate>(typeof(RpcInstanceAsyncMethodEntryFactory<,,,,,>), tParams, tResult, tDeserializer, tModifier, tMethodBody);
                    }
                }

                Instance = factory;
            }
        }


        public static RpcAsyncMethodEntryFactory<TDelegate> Instance { get; }
    }

    internal static class RpcStaticMethodEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
    {
        static RpcStaticMethodEntryFactory()
        {
            var tParams = RpcMethodEntryFactoryCache<TDelegate>.TParams;
            var tResult = RpcMethodEntryFactoryCache<TDelegate>.TResult;
            var valueTaskType = RpcMethodEntryFactoryCache<TDelegate>.ValueTaskType;
            var valueTaskCtor = RpcMethodEntryFactoryCache<TDelegate>.ValueTaskConstructor;
            var iMethodBodyReturnType = RpcMethodEntryFactoryCache<TDelegate>.IMethodBodyReturnType;
            var returnType = RpcMethodEntryFactoryCache<TDelegate>.ReturnType;
            var methodInfo = RpcMethodEntryFactoryCache<TDelegate>.DelegateInvokeMethodInfo;
            var iMethodBodyType = RpcMethodEntryFactoryCache<TDelegate>.IRpcMethodBodyType;
            var invokeMethodToOverride = RpcMethodEntryFactoryCache<TDelegate>.IRpcMethodInvokeMethodInfo;
            var parameterFields = RpcMethodEntryFactoryCache<TDelegate>.ParameterFields;
            var parameterTypes = RpcMethodEntryFactoryCache<TDelegate>.DelegateInvokeParameterTypes;
            var tDeserializer = RpcMethodEntryFactoryCache<TDelegate>.TDeserializer;
            var tModifier = RpcMethodEntryFactoryCache<TDelegate>.TModifier;
            var type1 = TypeArray1;
            type1[0] = tParams;

            {
                var v = BuildRpcMethodTypeSignature($"{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}.Invoker.Static", iMethodBodyType, type1, iMethodBodyReturnType, invokeMethodToOverride);

                var builder = v.TypeBuilder;


                FieldBuilder fpField = DefineFunctionPointerProperty(builder, FunctionPointerSetter);
                var il = v.InvokeMethodBuilder.GetILGenerator();
                {
                    if (!(valueTaskCtor is null))
                    {
                        il.DeclareLocal(valueTaskType!);
                        il.Emit(OpCodes.Ldloca_S, (byte)0);

                    }
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
                    il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, returnType, parameterTypes, null);
                    if (!(valueTaskCtor is null))
                    {
                        il.Emit(OpCodes.Call, valueTaskCtor);
                        il.Emit(OpCodes.Ldloc_0);
                    }
                    il.Emit(OpCodes.Ret);
                }
                var tMethodBody = builder.CreateType()!;
                RpcAsyncMethodEntryFactory<TDelegate> factory;
                if (valueTaskType is null)
                {
                    factory = CreateEntryFactory<TDelegate>(typeof(RpcStaticMethodEntryFactory<,,,,,>), tParams, tResult, tDeserializer, tModifier, tMethodBody);
                }
                else
                {
                    if (tResult == typeof(void))
                    {
                        factory = CreateAsyncActionEntryFactory<TDelegate>(typeof(RpcStaticAsyncMethodEntryFactory<,,,,>), tParams, tDeserializer, tModifier, tMethodBody);
                    }
                    else
                    {
                        factory = CreateEntryFactory<TDelegate>(typeof(RpcStaticAsyncMethodEntryFactory<,,,,,>), tParams, tResult, tDeserializer, tModifier, tMethodBody);
                    }
                }

                Instance = factory;
            }
        }

        public static RpcAsyncMethodEntryFactory<TDelegate> Instance { get; }
    }

    internal static class RpcDelegateEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
    {

        static RpcDelegateEntryFactory()
        {
            var tParams = RpcMethodEntryFactoryCache<TDelegate>.TParams;
            var tResult = RpcMethodEntryFactoryCache<TDelegate>.TResult;
            var valueTaskType = RpcMethodEntryFactoryCache<TDelegate>.ValueTaskType;
            var valueTaskCtor = RpcMethodEntryFactoryCache<TDelegate>.ValueTaskConstructor;
            var iMethodBodyReturnType = RpcMethodEntryFactoryCache<TDelegate>.IMethodBodyReturnType;
            var returnType = RpcMethodEntryFactoryCache<TDelegate>.ReturnType;
            var methodInfo = RpcMethodEntryFactoryCache<TDelegate>.DelegateInvokeMethodInfo;
            var iMethodBodyType = RpcMethodEntryFactoryCache<TDelegate>.IRpcMethodBodyType;
            var invokeMethodToOverride = RpcMethodEntryFactoryCache<TDelegate>.IRpcMethodInvokeMethodInfo;
            var parameterFields = RpcMethodEntryFactoryCache<TDelegate>.ParameterFields;
            //var parameterTypes = RpcEntryFactoryDelegateTypeCache<TDelegate>.DelegateInvokeParameterTypes;
            var tDeserializer = RpcMethodEntryFactoryCache<TDelegate>.TDeserializer;
            var tModifier = RpcMethodEntryFactoryCache<TDelegate>.TModifier;
            var type1 = TypeArray1;
            type1[0] = tParams;
            {
                var v = BuildRpcMethodTypeSignature($"{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}.Invoker.Delegate", iMethodBodyType, type1, iMethodBodyReturnType, invokeMethodToOverride);

                var builder = v.TypeBuilder;
                var delegateField = DefineDelegateProperty(builder);
                var il = v.InvokeMethodBuilder.GetILGenerator();
                {
                    if (!(valueTaskCtor is null))
                    {
                        il.DeclareLocal(valueTaskType!);
                        il.Emit(OpCodes.Ldloca_S, (byte)0);

                    }
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, delegateField);
                    foreach (var field in parameterFields)
                    {
                        //Push parameters
                        il.Emit(OpCodes.Ldarga_S, (byte)1);
                        //il.Emit(OpCodes.Constrained, tParams);
                        il.Emit(OpCodes.Ldfld, field);
                    }
                    il.Emit(OpCodes.Callvirt, methodInfo);
                    if (!(valueTaskCtor is null))
                    {
                        il.Emit(OpCodes.Call, valueTaskCtor);
                        il.Emit(OpCodes.Ldloc_0);
                    }
                    il.Emit(OpCodes.Ret);
                }
                var tMethodBody = builder.CreateType()!;
                RpcAsyncMethodEntryFactory<TDelegate> factory;
                if (valueTaskType is null)
                {
                    factory = CreateEntryFactory<TDelegate>(typeof(RpcDelegateEntryFactory<,,,,,>), tParams, tResult, tDeserializer, tModifier, tMethodBody);
                }
                else
                {
                    if (tResult == typeof(void))
                    {
                        factory = CreateAsyncActionEntryFactory<TDelegate>(typeof(RpcAsyncDelegateEntryFactory<,,,,>), tParams, tDeserializer, tModifier, tMethodBody);
                    }
                    else
                    {
                        factory = CreateEntryFactory<TDelegate>(typeof(RpcAsyncDelegateEntryFactory<,,,,,>), tParams, tResult, tDeserializer, tModifier, tMethodBody);
                    }
                }

                Instance = factory;
            }
        }

        public static RpcAsyncMethodEntryFactory<TDelegate> Instance { get; }
    }

    internal static class RpcMethodEntryFactoryHelper
    {
        

        internal static CustomAttributeBuilder RpcIdAttributeBuilder { get; } = new CustomAttributeBuilder(typeof(RpcIDAttribute).GetConstructor(Type.EmptyTypes)!, Array.Empty<object>());

        private const string FuncitonPointerSetterName = "set_FunctionPointer";
        private const string TargetObjectSetterName = "set_Target";
        private const string DelegateSetterName = "set_Delegate";
        private const string ModifyMethodName = "Modify";

        internal static MethodInfo FunctionPointerSetter { get; } = typeof(IFunctionPointerContainer).GetMethod(FuncitonPointerSetterName)!;
        internal static MethodInfo TargetObjectSetter { get; } = typeof(IObjectReferenceContainer).GetMethod(TargetObjectSetterName)!;
        internal static MethodInfo DelegateSetter { get; } = typeof(IDelegateContainer<Delegate>).GetMethod(DelegateSetterName)!;
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

            var setter = builder.DefineMethod(FuncitonPointerSetterName, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), IntPtrType);
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

            var setter = builder.DefineMethod(TargetObjectSetterName, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), ObjectType);
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
            builder.AddInterfaceImplementation(typeof(IDelegateContainer<Delegate>));

            var setter = builder.DefineMethod(DelegateSetterName, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), DelegateType);
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
        internal static (TypeBuilder TypeBuilder, MethodBuilder InvokeMethodBuilder) BuildRpcMethodTypeSignature(string name, Type iMethodBodyType, Type[] tParams, Type iMethodBodyReturnType, MethodInfo invokeMethodToOverride)
        {
            var builder = JsonRpc.Emit.ModuleBuilder.DefineType(name, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(ValueType));
            //var instanceMB = JsonRpc.Emit.ModuleBuilder.DefineType($"{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}.Invoker.Instance", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(ValueType));


            //instanceMB.DefineField("methodPtr", typeof(IntPtr), FieldAttributes.Public);

            //instanceMB.DefineField("target", typeof(object), FieldAttributes.Public);



            builder.AddInterfaceImplementation(iMethodBodyType);



            var invokeMethodBody = builder.DefineMethod(invokeMethodToOverride.Name, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig, iMethodBodyReturnType, tParams);



            builder.DefineMethodOverride(invokeMethodBody, invokeMethodToOverride);
            return (builder, invokeMethodBody);
        }
        internal static Type CreateDeserializerType(Type tParams, ArraySegment<FieldInfo> serializedParameters)
        {
            Type tDeserializer;
            {
                var tObjStyle = ParamsDeserializerBuilder.ObjectStyle.Create(tParams);
                var tArrayStyle = ParamsDeserializerBuilder.ArrayStyle.Create(tParams, serializedParameters);

                var type3 = TypeArray3;
                type3[0] = tParams;
                type3[1] = tObjStyle;
                type3[2] = tArrayStyle;
                tDeserializer = serializedParameters.Count == 0 ? typeof(EmptySerializedParamsDeserializer<,,>).MakeGenericType(type3) : typeof(ParamsDeserializer<,,>).MakeGenericType(type3);

            }

            return tDeserializer;
        }

        internal static (Type Type, FieldInfo[] ParameterFields, ArraySegment<FieldInfo> SerializedFields, FieldInfo? IdInjectField) CreateParamsType(MethodInfo methodInfo, ParameterInfo[] methodParameters)
        {

            {
                var serializedParameterFields = GetTmpSerializedParametersList();
                var parameterFields = GetTmpParametersList();
                FieldInfo? idInjectField = null;
                var builder = ModuleBuilder.DefineType($"{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}.Parameters", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(ValueType));
                foreach (var param in methodParameters)
                {
                    var name = param.Name;
                    if (name is null)
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

                var tParams = builder.CreateType()!;
                var _parameterFields = parameterFields.ToArray();
                parameterFields.Clear();
                var buffer = ArrayPool<FieldInfo>.Shared.Rent(serializedParameterFields.Count);
                serializedParameterFields.CopyTo(buffer);
                var serializedFields = new ArraySegment<FieldInfo>(buffer, 0, serializedParameterFields.Count);
                return (tParams, _parameterFields, serializedFields, idInjectField);
            }


        }

        internal static Type CreateIdInjecter(string name, FieldInfo idInjectField, Type tParams)
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
            var modifyMethod = builder.DefineMethod(ModifyMethodName, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), type4);
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
            builder.DefineMethodOverride(modifyMethod, iModifierType.GetMethod(ModifyMethodName)!);
            tModifier = builder.CreateType()!;
            return tModifier;
        }

        internal static RpcAsyncMethodEntryFactory<TDelegate> CreateEntryFactory<TDelegate>(Type genericFactoryType,Type tParams, Type tResult, Type tDeserializer, Type tModifier, Type tMethodBody)
        where TDelegate : notnull,Delegate
        {
            var type6 = TypeArray6;
            type6[0] = typeof(TDelegate);
            type6[1] = tMethodBody;
            type6[2] = tParams;
            type6[3] = tResult;
            type6[4] = tDeserializer;
            type6[5] = tModifier;
            return Unsafe.As<RpcAsyncMethodEntryFactory<TDelegate>>(Activator.CreateInstance(genericFactoryType.MakeGenericType(type6)));

        }

        internal static RpcAsyncMethodEntryFactory<TDelegate> CreateAsyncActionEntryFactory<TDelegate>(Type genericFactoryType, Type tParams, Type tDeserializer, Type tModifier, Type tMethodBody)
        where TDelegate : notnull, Delegate
        {
            var type5 = TypeArray5;
            type5[0] = typeof(TDelegate);
            type5[1] = tMethodBody;
            type5[2] = tParams;
            type5[3] = tDeserializer;
            type5[4] = tModifier;
            return Unsafe.As<RpcAsyncMethodEntryFactory<TDelegate>>(Activator.CreateInstance(genericFactoryType.MakeGenericType(type5)));

        }
    }

    internal sealed class RpcInstanceMethodEntryFactory<TDelegate, TMethod, TParams, TResult, TDeserializer, TModifier> : RpcMethodEntryFactory<TDelegate>

        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcMethodBody<TParams, TResult>, IFunctionPointerContainer, IObjectReferenceContainer
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;



        public override RpcMethodEntry CreateMethodEntry(TDelegate d,IExceptionHandler exceptionHandler)
        {
            var method = this.method;
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            method.Target = d.Target!;
            return new RpcMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, deserializer, modifier,exceptionHandler);
        }
    }

    internal sealed class RpcInstanceAsyncMethodEntryFactory<TDelegate, TMethod, TParams, TResult, TDeserializer, TModifier> : RpcAsyncMethodEntryFactory<TDelegate>

        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcAsyncMethodBody<TParams, TResult>, IFunctionPointerContainer, IObjectReferenceContainer
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;



        public override RpcAsyncMethodEntry CreateAsyncMethodEntry(TDelegate d, IExceptionHandler exceptionHandler)
        {
            var method = this.method;
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            method.Target = d.Target!;
            return new RpcAsyncMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, deserializer, modifier, exceptionHandler);
        }
    }

    internal sealed class RpcInstanceAsyncMethodEntryFactory<TDelegate, TMethod, TParams, TDeserializer, TModifier> : RpcAsyncMethodEntryFactory<TDelegate>

        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcAsyncMethodBody<TParams>, IFunctionPointerContainer, IObjectReferenceContainer
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;



        public override RpcAsyncMethodEntry CreateAsyncMethodEntry(TDelegate d, IExceptionHandler exceptionHandler)
        {
            var method = this.method;
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            method.Target = d.Target!;
            return new RpcAsyncMethodEntry<TMethod, TParams,TDeserializer, TModifier>(method, deserializer, modifier, exceptionHandler);
        }
    }

    internal sealed class RpcStaticMethodEntryFactory<TDelegate, TMethod, TParams, TResult, TDeserializer, TModifier> : RpcMethodEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcMethodBody<TParams, TResult>, IFunctionPointerContainer
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;



        public override RpcMethodEntry CreateMethodEntry(TDelegate d,IExceptionHandler exceptionHandler)
        {
            var method = this.method;
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            return new RpcMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, deserializer, modifier,exceptionHandler);
        }
    }

    internal sealed class RpcStaticAsyncMethodEntryFactory<TDelegate, TMethod, TParams, TResult, TDeserializer, TModifier> : RpcAsyncMethodEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcAsyncMethodBody<TParams, TResult>, IFunctionPointerContainer
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;



        public override RpcAsyncMethodEntry CreateAsyncMethodEntry(TDelegate d, IExceptionHandler exceptionHandler)
        {
            var method = this.method;
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            return new RpcAsyncMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, deserializer, modifier, exceptionHandler);
        }
    }
    internal sealed class RpcStaticAsyncMethodEntryFactory<TDelegate, TMethod, TParams, TDeserializer, TModifier> : RpcAsyncMethodEntryFactory<TDelegate>
       where TDelegate : notnull, Delegate
       where TMethod : struct, IRpcAsyncMethodBody<TParams>, IFunctionPointerContainer
       where TParams : notnull
       where TDeserializer : struct, IParamsDeserializer<TParams>
       where TModifier : struct, IMethodParamsModifier<TParams>
    {
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;



        public override RpcAsyncMethodEntry CreateAsyncMethodEntry(TDelegate d, IExceptionHandler exceptionHandler)
        {
            var method = this.method;
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            return new RpcAsyncMethodEntry<TMethod, TParams,  TDeserializer, TModifier>(method, deserializer, modifier, exceptionHandler);
        }
    }

    internal sealed class RpcDelegateEntryFactory<TDelegate, TMethod, TParams, TResult, TDeserializer, TModifier> : RpcMethodEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcMethodBody<TParams, TResult>, IDelegateContainer<Delegate>
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;
        public override RpcMethodEntry CreateMethodEntry(TDelegate d,IExceptionHandler exceptionHandler)
        {
            var method = this.method;
            method.Delegate = d;
            return new RpcMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, deserializer, modifier,exceptionHandler);
        }
    }

    internal sealed class RpcAsyncDelegateEntryFactory<TDelegate, TMethod, TParams, TResult, TDeserializer, TModifier> : RpcAsyncMethodEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcAsyncMethodBody<TParams, TResult>, IDelegateContainer<Delegate>
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;
        public override RpcAsyncMethodEntry CreateAsyncMethodEntry(TDelegate d, IExceptionHandler exceptionHandler)
        {
            var method = this.method;
            method.Delegate = d;
            return new RpcAsyncMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, deserializer, modifier, exceptionHandler);
        }
    }

    internal sealed class RpcAsyncDelegateEntryFactory<TDelegate, TMethod, TParams,  TDeserializer, TModifier> : RpcAsyncMethodEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcAsyncMethodBody<TParams>, IDelegateContainer<Delegate>
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;
        public override RpcAsyncMethodEntry CreateAsyncMethodEntry(TDelegate d, IExceptionHandler exceptionHandler)
        {
            var method = this.method;
            method.Delegate = d;
            return new RpcAsyncMethodEntry<TMethod, TParams,  TDeserializer, TModifier>(method, deserializer, modifier, exceptionHandler);
        }
    }

}
