using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
namespace RamType0.JsonRpc.Internal
{
    using Server;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
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
                if (field.GetCustomAttribute<RpcIDAttribute>() is null)
                {
                    continue;
                }
                else
                {
                    idInjectField = field;
                    break;
                }
            }
            if (idInjectField is null)
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
        public static RpcAsyncMethodEntryFactory<Func<TParams, TResult>> Instance { get; }
        static RpcExplicitParamsFuncDelegateEntryFactory()
        {
            if (typeof(TResult).IsConstructedGenericType)
            {
                var genericType = typeof(TResult).GetGenericTypeDefinition();
                if (genericType == typeof(Task<>))
                {
                    Instance = RpcMethodEntryFactoryHelper.CreateEntryFactory<Func<TParams, TResult>>(typeof(RpcAsyncDelegateEntryFactory<,,,,,>),
                                                                                                          typeof(TParams),
                                                                                                          typeof(TResult).GetGenericArguments()[0],
                                                                                                          typeof(ExplicitParamsObjectDeserializer<TParams>),
                                                                                                          ExplicitParamsModifierCache<TParams>.ModifierType,
                                                                                                          typeof(ExplicitParamsTaskFuncDelegateInvoker<TParams, TResult>));
                }
                else
                {
                   
                    if (genericType == typeof(ValueTask<>))
                    {
                        Instance = RpcMethodEntryFactoryHelper.CreateEntryFactory<Func<TParams, TResult>>(typeof(RpcAsyncDelegateEntryFactory<,,,,,>),
                                                                                                          typeof(TParams),
                                                                                                          typeof(TResult).GetGenericArguments()[0],
                                                                                                          typeof(ExplicitParamsObjectDeserializer<TParams>),
                                                                                                          ExplicitParamsModifierCache<TParams>.ModifierType,
                                                                                                          typeof(ExplicitParamsFuncDelegateInvoker<TParams, TResult>));
                    }

                    else
                    {
                        Instance = RpcMethodEntryFactoryHelper.CreateEntryFactory<Func<TParams, TResult>>(typeof(RpcDelegateEntryFactory<,,,,,>),
                                                                                                          typeof(TParams),
                                                                                                          typeof(TResult),
                                                                                                          typeof(ExplicitParamsObjectDeserializer<TParams>),
                                                                                                          ExplicitParamsModifierCache<TParams>.ModifierType,
                                                                                                          typeof(ExplicitParamsFuncDelegateInvoker<TParams, TResult>));
                    }
                }

            }
            else
            {
                if (typeof(TResult) == typeof(Task))
                {
                    Instance = RpcMethodEntryFactoryHelper.CreateAsyncActionEntryFactory<Func<TParams, TResult>>(typeof(RpcAsyncDelegateEntryFactory<,,,,>),
                                                                                                          typeof(TParams),
                                                                                                          typeof(ExplicitParamsObjectDeserializer<TParams>),
                                                                                                          ExplicitParamsModifierCache<TParams>.ModifierType,
                                                                                                          typeof(ExplicitParamsTaskActionDelegateInvoker<TParams>));

                }
                else
                {
                    
                    if (typeof(TResult) == typeof(ValueTask))
                    {
                        Instance = RpcMethodEntryFactoryHelper.CreateAsyncActionEntryFactory<Func<TParams, TResult>>(typeof(RpcAsyncDelegateEntryFactory<,,,,>),
                                                                                                          typeof(TParams),
                                                                                                          typeof(ExplicitParamsObjectDeserializer<TParams>),
                                                                                                          ExplicitParamsModifierCache<TParams>.ModifierType,
                                                                                                          typeof(ExplicitParamsFuncDelegateInvoker<TParams,ValueTask>));

                    }
                    else
                    {
                        Instance = RpcMethodEntryFactoryHelper.CreateEntryFactory<Func<TParams, TResult>>(typeof(RpcDelegateEntryFactory<,,,,,>),
                                                                                                          typeof(TParams),
                                                                                                          typeof(TResult),
                                                                                                          typeof(ExplicitParamsObjectDeserializer<TParams>),
                                                                                                          ExplicitParamsModifierCache<TParams>.ModifierType,
                                                                                                          typeof(ExplicitParamsFuncDelegateInvoker<TParams, TResult>));
                    }
                }

                
            }
        }
    }
        internal static class RpcExplicitParamsActionDelegateEntryFactory<TParams>
        {
            public static RpcAsyncMethodEntryFactory<Action<TParams>> Instance { get; }
            static RpcExplicitParamsActionDelegateEntryFactory()
            {
                Instance = RpcMethodEntryFactoryHelper.CreateEntryFactory<Action<TParams>>(typeof(RpcDelegateEntryFactory<,,,,,>),typeof(TParams), typeof(NullResult), typeof(ExplicitParamsObjectDeserializer<TParams>), ExplicitParamsModifierCache<TParams>.ModifierType, typeof(ExplicitParamsActionDelegateInvoker<TParams>));
            }
        }

        public struct ExplicitParamsFuncDelegateInvoker<TParams, TResult> : IRpcMethodBody<TParams, TResult>, IDelegateContainer<Delegate>
        {
            Func<TParams, TResult> func;

            public Delegate Delegate { set => func = Unsafe.As<Func<TParams, TResult>>(value); }

            public TResult Invoke(TParams parameters)
            {
                return func(parameters);
            }
        }


        public struct ExplicitParamsTaskFuncDelegateInvoker<TParams, TResult> : IRpcAsyncMethodBody<TParams, TResult>, IDelegateContainer<Delegate>
        {
            Func<TParams, Task<TResult>> func;

            public Delegate Delegate { set => func = Unsafe.As<Func<TParams, Task<TResult>>>(value); }

            public ValueTask<TResult> InvokeAsync(TParams parameters)
            {
                return new ValueTask<TResult>(func(parameters));
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


        public struct ExplicitParamsTaskActionDelegateInvoker<TParams> : IRpcAsyncMethodBody<TParams>, IDelegateContainer<Delegate>
        {
            Func<TParams, Task> action;
            public Delegate Delegate { set => action = Unsafe.As<Func<TParams, Task>>(value); }

            public ValueTask InvokeAsync(TParams parameters)
            {
                return new ValueTask(action(parameters));
            }
        }

    }
