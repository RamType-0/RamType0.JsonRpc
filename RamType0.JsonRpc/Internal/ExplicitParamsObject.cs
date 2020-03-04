using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
namespace RamType0.JsonRpc.Internal
{
    using Server;
    using System.Runtime.CompilerServices;
    using Utf8Json;
    public struct ExplicitParamsObjectDeserializer<T> : IParamsDeserializer<T>
    {
        public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            return formatterResolver.GetFormatterWithVerify<T>().Deserialize(ref reader, formatterResolver);
        }
    }

    internal static class ExplicitParamsModifierCache<TParams>
    {
        internal static Type ModifierType { get; }
        static ExplicitParamsModifierCache()
        {
            FieldInfo? idInjectField = null;
            foreach (var field in typeof(TParams).GetFields(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if(field.GetCustomAttribute<RpcIDAttribute>() is null)
                {
                    continue;
                }
                else
                {
                    idInjectField = field;
                    break;
                }
            }
            if(idInjectField is null)
            {
                ModifierType = typeof(EmptyModifier<TParams>);
            }
            else
            {
                ModifierType = RpcMethodEntryFactoryHelper.CreateIdInjecter(typeof(TParams).FullName + ".IdInjecter", idInjectField, typeof(TParams));
            }

        }
    }

    internal static class RpcExplicitParamsFuncDelegateEntryFactory<TParams, TResult>
    {
        public static RpcMethodEntryFactory<Func<TParams,TResult>> Instance { get; }
        static RpcExplicitParamsFuncDelegateEntryFactory()
        {
            Instance = RpcDelegateEntryFactory<Func<TParams, TResult>>.CreateDelegateEntryFactory(typeof(TParams), typeof(TResult), typeof(ExplicitParamsObjectDeserializer<TParams>), ExplicitParamsModifierCache<TParams>.ModifierType,typeof(ExplicitParamsFuncDelegateInvoker<TParams, TResult>));
        }
    }

    internal static class RpcExplicitParamsActionDelegateEntryFactory<TParams>
    {
        public static RpcMethodEntryFactory<Action<TParams>> Instance { get; }
        static RpcExplicitParamsActionDelegateEntryFactory()
        {
            Instance = RpcDelegateEntryFactory<Action<TParams>>.CreateDelegateEntryFactory(typeof(TParams), typeof(NullResult), typeof(ExplicitParamsObjectDeserializer<TParams>), ExplicitParamsModifierCache<TParams>.ModifierType, typeof(ExplicitParamsActionDelegateInvoker<TParams>));
        }
    }

    public struct ExplicitParamsFuncDelegateInvoker<TParams, TResult> : IRpcMethodBody<TParams, TResult>,IDelegateContainer<Delegate>
    {
        Func<TParams, TResult> func;

        public Delegate Delegate { set => func = Unsafe.As<Func<TParams, TResult>>(value); }

        public TResult Invoke(TParams parameters)
        {
            return func(parameters);
        }
    }

    public struct ExplicitParamsActionDelegateInvoker<TParams> : IRpcMethodBody<TParams, NullResult>, IDelegateContainer<Delegate>
    {
        Action<TParams> action;
        public Delegate Delegate { set => action = Unsafe.As<Action<TParams>>(value); }

        public NullResult Invoke(TParams parameters)
        {
            action(parameters);
            return new NullResult();
        }
    }
}
