using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using static RamType0.JsonRpc.JsonRpcMethodDictionary;

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

        #endregion
        internal static RpcEntry FromDelegate<T>(T d)
            where T:Delegate
        {
            MethodInfo method = d.Method;
            var returnType = method.ReturnType;
            bool isVoidMethod = returnType == typeof(void);
            bool isFuncOrAction = isVoidMethod ? IsAction<T>() : IsFunc<T>();//FuncまたはActionでないときは、デリゲート自体の引数の名前、属性を元にRpcEntryを生成します。
            var sourceMethod = isFuncOrAction ? method : typeof(T).GetMethod("Invoke")!;
            var generated = GeneratedFactories;
            lock (generated) {
                if(generated.TryGetValue(sourceMethod,out var factory))
                {
                    return factory.CreateNew(d);
                }
                var (paramsType, args, deserializedFields) = ParamsBuilder.FromMethod(sourceMethod);
                ReadOnlySpan<FieldInfo> fields = new ReadOnlySpan<FieldInfo>(deserializedFields.Array!, deserializedFields.Offset, deserializedFields.Count);
                var arrayDeserializerType = ParamsDeserializerBuilder.ArrayStyle.Create(paramsType, fields);
                var typeArray3 = TypeArray3;
                {
                    typeArray3[0] = paramsType;
                
                    var genericArgs = TypeArray1;
                    genericArgs[0] = paramsType;

                    typeArray3[1] = typeof(DefaultObjectStyleParamsDeserializer<>).MakeGenericType(genericArgs);

                    typeArray3[2] = arrayDeserializerType;
                }
                var deserializerType = typeof(ParamsDeserializer<,,>).MakeGenericType(typeArray3);
                ReadOnlySpan<FieldInfo> arguments = new ReadOnlySpan<FieldInfo>(args.Array!, args.Offset, args.Count);
                var invokerType = RpcDelegateInvokerBuilder.Create<T>(paramsType, arguments);
                Type responseCreaterType;
                bool isCancellable = typeof(ICancellableMethodParams).IsAssignableFrom(paramsType);
                if (isVoidMethod)
                {

                    typeArray3[0] = typeof(T);
                    typeArray3[1] = paramsType;
                    typeArray3[2] = invokerType;
                    var genericType = isCancellable ? typeof(DefaultCancellableActionProxy<,,>):typeof(DefaultActionProxy<,,>);
                    responseCreaterType = genericType.MakeGenericType(typeArray3);
                }
                else
                {
                    var funcProxyGenericArgs = TypeArray4;
                    funcProxyGenericArgs[0] = typeof(T);
                    funcProxyGenericArgs[1] = paramsType;
                    funcProxyGenericArgs[2] = returnType;
                    funcProxyGenericArgs[3] = invokerType;
                    var genericType = isCancellable? typeof(DefaultCancellableFunctionProxy<,,,>) : typeof(DefaultFunctionProxy<,,,>);
                    responseCreaterType = genericType.MakeGenericType(funcProxyGenericArgs);
                }
                var entryFactoryArgs = TypeArray4;
                entryFactoryArgs[0] = responseCreaterType;
                entryFactoryArgs[1] = typeof(T);
                entryFactoryArgs[2] = paramsType;
                entryFactoryArgs[3] = deserializerType;
                {
                    var newFactory = Unsafe.As<RpcEntryFactory>(Activator.CreateInstance(typeof(RpcEntryFactory<,,,>).MakeGenericType(entryFactoryArgs)));
                    generated.Add(sourceMethod, newFactory);
                    return newFactory.CreateNew(d);
                }
            }
        }

        static Dictionary<MethodInfo, RpcEntryFactory> GeneratedFactories { get; } = new Dictionary<MethodInfo, RpcEntryFactory>();

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

        internal abstract class RpcEntryFactory
        {
            public abstract RpcEntry CreateNew(Delegate rpcMethod);
        }
        internal sealed class RpcEntryFactory<TProxy, TDelegate, TParams, TDeserializer> : RpcEntryFactory
            where TProxy : struct, RpcResponseCreater<TDelegate, TParams>
            where TDelegate : Delegate
            where TParams : struct, IMethodParams
            where TDeserializer : struct, IParamsDeserializer<TParams>
        {
            public override RpcEntry CreateNew(Delegate rpcMethod)
            {
                try
                {
                    return new RpcEntry<TProxy, TDelegate, TParams, TDeserializer>(Unsafe.As<TDelegate>(rpcMethod));
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException();
                }
            }
        }
    }
}
