using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace RamType0.JsonRpc
{
    static partial class Emit
    {
        public static ModuleBuilder ModuleBuilder { get; } = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("RamType0.JsonRpc.DynamicAssembly"), AssemblyBuilderAccess.Run).DefineDynamicModule("RamType0.JsonRpc.DynamicModule");
        #region 不毛なプーリング
        [ThreadStatic]
        static Type[]? typeArray1;
        static Type[] TypeArray1 => typeArray1 ??= new Type[1];
        [ThreadStatic]
        static Type[]? typeArray2;
        static Type[] TypeArray2 => typeArray2 ??= new Type[2];
        [ThreadStatic]
        static Type[]? typeArray3;
        static Type[] TypeArray3 => typeArray3 ??= new Type[3];
        [ThreadStatic]
        static Type[]? typeArray4;
        static Type[] TypeArray4 => typeArray4 ??= new Type[4];
        [ThreadStatic]
        static object[]? objArray1;
        static object[] ObjArray1 => objArray1 ??= new object[1];

        #endregion
        internal static JsonRpcMethodDictionary.RpcInvoker FromDelegate<T>(T d)
            where T:Delegate
        {
            MethodInfo method = d.Method;
            var returnType = method.ReturnType;
            bool isVoidMethod = returnType == typeof(void);
            bool isFuncOrAction = isVoidMethod ? IsAction<T>() : IsFunc<T>();
            var sourceMethod = isFuncOrAction ? method : typeof(T).GetMethod("Invoke")!;
            var generated = GeneratedRpcInvokers;
            lock (generated) {
                if(generated.TryGetValue(sourceMethod,out var invoker))
                {
                    return invoker;
                }
                var (paramsType, args, deserializedFields) = ParamsBuilder.FromMethod(sourceMethod);
                ReadOnlySpan<FieldInfo> fields = new ReadOnlySpan<FieldInfo>(deserializedFields.Array!, deserializedFields.Offset, deserializedFields.Count);
                var arrayDeserializerType = ParamsDeserializerBuilder.ArrayStyle.Create(paramsType, fields);
                var typeArray3 = TypeArray3;
                typeArray3[0] = paramsType;
                {
                    var genericArgs = TypeArray1;
                    genericArgs[0] = paramsType;

                    typeArray3[1] = typeof(DefaultObjectStyleParamsDeserializer<>).MakeGenericType(genericArgs);
                }
                typeArray3[2] = arrayDeserializerType;
                var deserializerType = typeof(ParamsDeserializer<,,>).MakeGenericType(typeArray3);
                ReadOnlySpan<FieldInfo> arguments = new ReadOnlySpan<FieldInfo>(args.Array!, args.Offset, args.Count);
                var invokerType = RpcDelegateInvokerBuilder.Create<T>(paramsType, arguments);
                Type responseCreaterType;
                if (isVoidMethod)
                {

                    typeArray3[0] = typeof(T);
                    typeArray3[1] = paramsType;
                    typeArray3[2] = invokerType;
                    responseCreaterType = typeof(DefaultActionProxy<,,>).MakeGenericType(typeArray3);
                }
                else
                {
                    var funcProxyGenericArgs = TypeArray4;
                    funcProxyGenericArgs[0] = typeof(T);
                    funcProxyGenericArgs[1] = paramsType;
                    funcProxyGenericArgs[2] = returnType;
                    funcProxyGenericArgs[3] = invokerType;
                    responseCreaterType = typeof(DefaultFunctionProxy<,,,>).MakeGenericType(funcProxyGenericArgs);
                }
                var rpcInvokerArgs = TypeArray4;
                rpcInvokerArgs[0] = responseCreaterType;
                rpcInvokerArgs[1] = typeof(T);
                rpcInvokerArgs[2] = paramsType;
                rpcInvokerArgs[3] = deserializerType;
                var ctorArgs = ObjArray1;
                ctorArgs[0] = d;
                var rpcInvoker = Unsafe.As<JsonRpcMethodDictionary.RpcInvoker>(Activator.CreateInstance(typeof(JsonRpcMethodDictionary.RpcInvoker<,,,>).MakeGenericType(rpcInvokerArgs), BindingFlags.NonPublic, null, ctorArgs, null));
                generated.Add(sourceMethod, rpcInvoker);
                return rpcInvoker; 
            }
        }

        static Dictionary<MethodInfo, JsonRpcMethodDictionary.RpcInvoker> GeneratedRpcInvokers { get; } = new Dictionary<MethodInfo, JsonRpcMethodDictionary.RpcInvoker>();

        public static bool IsAction<T>()
            where T : Delegate
        {
            if (typeof(T) == typeof(Action))
            {
                return true;
            }
            if (!typeof(T).IsConstructedGenericType)
            {
                return false;
            }
            var genericType = typeof(T).GetGenericTypeDefinition();
            //FUCK
            if (genericType == typeof(Action<>) || genericType == typeof(Action<,>) || genericType == typeof(Action<,,>)
                || genericType == typeof(Action<,,,>) || genericType == typeof(Action<,,,,>) || genericType == typeof(Action<,,,,,>)
                || genericType == typeof(Action<,,,,,,>) || genericType == typeof(Action<,,,,,,,>) || genericType == typeof(Action<,,,,,,,,>)
                || genericType == typeof(Action<,,,,,,,,,>) || genericType == typeof(Action<,,,,,,,,,,>) || genericType == typeof(Action<,,,,,,,,,,,>)
                || genericType == typeof(Action<,,,,,,,,,,,,>) || genericType == typeof(Action<,,,,,,,,,,,,,>) || genericType == typeof(Action<,,,,,,,,,,,,,,>)
                || genericType == typeof(Action<,,,,,,,,,,,,,,,>))
            {

                return true;
            }
            return false;

        }
        static bool IsFunc<T>()
            where T : Delegate
        {
            if (!typeof(T).IsConstructedGenericType)
            {
                return false;
            }
            var genericType = typeof(T).GetGenericTypeDefinition();
            //FUCK
            if (genericType == typeof(Func<>) || genericType == typeof(Func<,>) || genericType == typeof(Func<,,>)
                || genericType == typeof(Func<,,,>) || genericType == typeof(Func<,,,,>) || genericType == typeof(Func<,,,,,>)
                || genericType == typeof(Func<,,,,,,>) || genericType == typeof(Func<,,,,,,,>) || genericType == typeof(Func<,,,,,,,,>)
                || genericType == typeof(Func<,,,,,,,,,>) || genericType == typeof(Func<,,,,,,,,,,>) || genericType == typeof(Func<,,,,,,,,,,,>)
                || genericType == typeof(Func<,,,,,,,,,,,,>) || genericType == typeof(Func<,,,,,,,,,,,,,>) || genericType == typeof(Func<,,,,,,,,,,,,,,>)
                || genericType == typeof(Func<,,,,,,,,,,,,,,,>) || genericType == typeof(Func<,,,,,,,,,,,,,,,,>))
            {
                return true;
            }
            return false;
        }
    }
}
