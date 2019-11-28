using RamType0.JsonRpc.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace RamType0.JsonRpc
{
    static partial class Emit
    {
        static ModuleBuilder ModuleBuilder { get; } = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("RamType0.JsonRpc.DynamicAssembly"), AssemblyBuilderAccess.Run).DefineDynamicModule("RamType0.JsonRpc.DynamicModule");
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
        internal static RpcEntryFactory FromDelegate<T>(T d)
            where T : Delegate
        {
            if (!IsFuncOrAction<T>())//FuncまたはActionでないときは、デリゲート自体の引数の名前、属性を元にRpcEntryFactoryを生成します。
            {
                return DelegateTypeBasedRpcEntryFactoryCache<T>.Instance;
            }
            return FromFuncOrAction(d);
        }

        private static RpcEntryFactory FromFuncOrAction<T>(T d) where T : Delegate
        {
            var generated = GeneratedFuncActionFactories;
            var method = d.Method;
            if (generated.TryGetValue(method, out var factory))
            {
                return factory;
            }
            lock (generated)
            {
                if (generated.TryGetValue(method, out factory))
                {
                    return factory;
                }
                factory = BuildFactory<T>(method);
                generated.TryAdd(method, factory);
                return factory;

            }
        }

        /// <summary>
        /// このメソッド自体はスレッドセーフでありません。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <returns></returns>
        private static RpcEntryFactory<T> BuildFactory<T>(MethodInfo method)
            where T : Delegate
        {
            var (paramsType, args, deserializedFields) = ParamsBuilder.FromMethod(method);
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
            bool isCancellable = typeof(IMethodParamsInjectID).IsAssignableFrom(paramsType);
            var returnType = method.ReturnType;
            if (returnType == typeof(void))
            {

                typeArray3[0] = typeof(T);
                typeArray3[1] = paramsType;
                typeArray3[2] = invokerType;
                var genericType = isCancellable ? typeof(DefaultIDInjectedActionProxy<,,>) : typeof(DefaultActionProxy<,,>);
                responseCreaterType = genericType.MakeGenericType(typeArray3);
            }
            else
            {
                var funcProxyGenericArgs = TypeArray4;
                funcProxyGenericArgs[0] = typeof(T);
                funcProxyGenericArgs[1] = paramsType;
                funcProxyGenericArgs[2] = returnType;
                funcProxyGenericArgs[3] = invokerType;
                var genericType = isCancellable ? typeof(DefaultIDInjectedFunctionProxy<,,,>) : typeof(DefaultFunctionProxy<,,,>);
                responseCreaterType = genericType.MakeGenericType(funcProxyGenericArgs);
            }
            var entryFactoryArgs = TypeArray4;
            entryFactoryArgs[0] = responseCreaterType;
            entryFactoryArgs[1] = typeof(T);
            entryFactoryArgs[2] = paramsType;
            entryFactoryArgs[3] = deserializerType;
            {
                var newFactory = Unsafe.As<RpcEntryFactory<T>>(Activator.CreateInstance(typeof(RpcEntryFactory<,,,>).MakeGenericType(entryFactoryArgs)));

                return newFactory;//.CreateNew(d);
            }
        }



        static ConcurrentDictionary<MethodInfo, RpcEntryFactory> GeneratedFuncActionFactories { get; } = new ConcurrentDictionary<MethodInfo, RpcEntryFactory>();
        static HashSet<Type> GenericFuncActionTypes { get; } = new HashSet<Type>()
        {
            typeof(Action<>) ,  typeof(Action<,>) ,  typeof(Action<,,>),
            typeof(Action<,,,>) ,  typeof(Action<,,,,>) ,  typeof(Action<,,,,,>) ,
            typeof(Action<,,,,,,>) ,  typeof(Action<,,,,,,,>) ,  typeof(Action<,,,,,,,,>),
            typeof(Action<,,,,,,,,,>) ,  typeof(Action<,,,,,,,,,,>) ,  typeof(Action<,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,>) ,  typeof(Action<,,,,,,,,,,,,,>) ,  typeof(Action<,,,,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,,,,>),

            typeof(Func<>) ,  typeof(Func<,>) ,  typeof(Func<,,>),
            typeof(Func<,,,>) ,  typeof(Func<,,,,>) ,  typeof(Func<,,,,,>) ,
            typeof(Func<,,,,,,>) ,  typeof(Func<,,,,,,,>) ,  typeof(Func<,,,,,,,,>),
            typeof(Func<,,,,,,,,,>) ,  typeof(Func<,,,,,,,,,,>) ,  typeof(Func<,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,>) ,  typeof(Func<,,,,,,,,,,,,,>) ,  typeof(Func<,,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,,>),typeof(Func<,,,,,,,,,,,,,,,,>)

        };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsFuncOrAction<T>()
            where T : Delegate
        {
            if (typeof(T) == typeof(Action))//ここはJIT時解決
            {
                return true;
            }
            if (!typeof(T).IsConstructedGenericType)
            {
                return false;
            }
            var genericType = typeof(T).GetGenericTypeDefinition();
            return GenericFuncActionTypes.Contains(genericType);

        }
        internal abstract class RpcEntryFactory
        {
            public abstract RpcMethodEntry NewEntry(Delegate rpcMethod);
        }
        internal static class DelegateTypeBasedRpcEntryFactoryCache<T>
            where T : Delegate
        {
            public static RpcEntryFactory<T> Instance { get; }
            static DelegateTypeBasedRpcEntryFactoryCache()//静的コンストラクタはJITコンパイラがスレッドセーフで1回しか呼ばれないことを保証するため最強の排他処理となる
            {
                var method = typeof(T).GetMethod("Invoke")!;
                Instance = BuildFactory<T>(method);
            }
        }
        internal abstract class RpcEntryFactory<TDelegate> : RpcEntryFactory where TDelegate : Delegate { }
        internal sealed class RpcEntryFactory<TProxy, TDelegate, TParams, TDeserializer> : RpcEntryFactory<TDelegate>
            where TProxy : struct, IRpcMethodProxy<TDelegate, TParams>
            where TDelegate : Delegate
            where TParams : struct, IMethodParams
            where TDeserializer : struct, IParamsDeserializer<TParams>
        {
            public override sealed RpcMethodEntry NewEntry(Delegate rpcMethod)
            {

                return new RpcEntry<TProxy, TDelegate, TParams, TDeserializer>(default, (TDelegate)(rpcMethod), default);

            }
        }
    }
}
