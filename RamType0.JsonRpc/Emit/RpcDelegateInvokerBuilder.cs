using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Reflection.Emit;
using static RamType0.JsonRpc.JsonRpcMethodDictionary;

namespace RamType0.JsonRpc
{
    public static partial class Emit
    {

        
        static class RpcDelegateInvokerBuilder
        {
            public static Type Create<T>(Type paramsType, ReadOnlySpan<FieldInfo> argumentFields)
                where T:Delegate
            {
                VerifyTIsAccessible<T>();

                var builder = ModuleBuilder.DefineType($"{typeof(T).FullName}({paramsType.FullName}).Proxy", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(ValueType));
                var invokeMethod = typeof(T).GetMethod("Invoke")!;
                var returnType = invokeMethod.ReturnType;

                Type interfaceType;
                MethodInfo overriden;

                if (returnType == typeof(void))
                {
                    var typeArray = TypeArray2;
                    typeArray[0] = typeof(T);
                    typeArray[1] = paramsType;
                    interfaceType = typeof(IRpcActionInvoker<,>).MakeGenericType(typeArray);
                    overriden = typeof(IRpcDelegateInvoker<,>).MakeGenericType(typeArray).GetMethod("Invoke")!;
                }
                else
                {
                    var typeArray = TypeArray3;
                    typeArray[0] = typeof(T);
                    typeArray[1] = paramsType;
                    typeArray[2] = returnType;
                    interfaceType = typeof(IRpcFunctionInvoker<,,>).MakeGenericType(typeArray);
                    overriden = interfaceType.GetMethod("Invoke")!;
                }


                builder.AddInterfaceImplementation(interfaceType);
                {
                    var method = builder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, returnType, new[] { typeof(T), paramsType });
                    builder.DefineMethodOverride(method, overriden);
                    //(TResult | void) Invoke(TDelegate invokedDelegate, TParams parameters);
                    {
                        var il = method.GetILGenerator();
                        {
                            il.Emit(OpCodes.Ldarg_1);
                            foreach (var argumentField in argumentFields)
                            {
                                il.Emit(OpCodes.Ldarga_S, (byte)2);
                                il.Emit(OpCodes.Ldfld, argumentField);
                            }
                            il.Emit(OpCodes.Call, invokeMethod);
                            il.Emit(OpCodes.Ret);
                        }
                    }
                }
                return builder.CreateType()!;
            }

            private static void VerifyTIsAccessible<T>() where T : Delegate
            {
                var type = typeof(T);
                while (true)
                {
                    if (type.IsNotPublic)
                    {
                        throw new ArgumentException($"{typeof(T).Name} is inaccessible. mark it delegate and it parent class as public.");
                    }
                    type = type.DeclaringType!;
                    if (type is null)
                    {
                        break;
                    }
                }
            }
        }
    }
}
