using System;
using Utf8Json;
using System.Reflection.Emit;
using System.Reflection;
namespace RamType0.JsonRpc.Emit
{
    
        public static class MethodInvokerClassBuilder
        {
            static readonly ModuleBuilder moduleBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("JsonRpcMethodParamsResolvers"), AssemblyBuilderAccess.Run).DefineDynamicModule("JsonRpcMethodParamsResolvers");

            public abstract class ParamsFormatter<T>
            where T : struct, IParamsObject
            {
                static ParamsFormatter<T>? instance;
                public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
                {
                    T paramsObj;
                    switch (reader.GetCurrentJsonToken())
                    {
                        case JsonToken.BeginObject:
                            {
                                paramsObj = ReadObject<T>(ref reader, formatterResolver);
                                break;
                            }
                        case JsonToken.BeginArray:
                            {
                                paramsObj = ReadParamsObjectFromArrayStyle(ref reader, formatterResolver);
                                break;
                            }
                        default:
                            {
                                throw new FormatException();
                            }
                    }
                    return paramsObj;
                }

                protected static TRead ReadObject<TRead>(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
                {
                    return formatterResolver.GetFormatter<TRead>().Deserialize(ref reader, formatterResolver);
                }

                protected abstract T ReadParamsObjectFromArrayStyle(ref JsonReader reader, IJsonFormatterResolver formatterResolver);

            }
            public interface IParamsObject<T> : IParamsObject
            {
                public new T Invoke();
                void IParamsObject.Invoke() => Invoke();
            }
            public interface IParamsObject
            {
                public void Invoke();
                public bool IsEmpty { get; }
            }
            public static Type CreateType<T>(T jsonRpcFunction)
                where T : Delegate
        {
            MethodInfo method = jsonRpcFunction.Method;
            var parameters = method.GetParameters();
            
            return CreateParamsObjectType(jsonRpcFunction, method,parameters);

        }
        static Type[] IParamsObjectTypeArray { get; } = new Type[] { typeof(IParamsObject) };
        private static Type CreateParamsObjectType<T>(T jsonRpcFunction, MethodInfo method, ParameterInfo[] parameters) where T : Delegate
        {

            Type returnType = method.ReturnType;
            bool isVoidMethod = returnType == typeof(void);
            Type typea = typeof(IParamsObject<>).MakeGenericType(new Type[] { returnType });
            Type[] interfaces = isVoidMethod ? IParamsObjectTypeArray : new Type[] { typeof(IParamsObject), typea };
            var overRideTarget = isVoidMethod ? ;
            var type = moduleBuilder.DefineType(method.Name + "Params", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AutoLayout, typeof(ValueType), interfaces);

            var fields = new FieldBuilder[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                fields[i] = type.DefineField(parameter.Name!, parameter.ParameterType, FieldAttributes.Public);
            }
            var invokeMethod = type.DefineMethod("Invoke", MethodAttributes.Public, returnType, Type.EmptyTypes);

            if (!method.IsStatic)
            {
                var targetObj = jsonRpcFunction.Target!;
                Type targetObjType = targetObj.GetType();
                const string targetObjStoredFieldName = "targetObj";
                var targetObjStoredField = type.DefineField(targetObjStoredFieldName, targetObjType, FieldAttributes.Public | FieldAttributes.Static);
                var il = invokeMethod.GetILGenerator();
                if (targetObjType.IsValueType)
                {
                    il.Emit(OpCodes.Ldsflda, targetObjStoredField);
                }
                else
                {
                    il.Emit(OpCodes.Ldsfld, targetObjStoredField);
                }
                foreach (var field in fields)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, field);
                }

                il.Emit(OpCodes.Call, method);
                il.Emit(OpCodes.Ret);
                var createdType = type.CreateTypeInfo()!;
                createdType.GetField(targetObjStoredFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, targetObj);
                return createdType;
            }
            else
            {
                var il = invokeMethod.GetILGenerator();

                foreach (var field in fields)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, field);
                }

                il.Emit(OpCodes.Call, method);
                il.Emit(OpCodes.Ret);
                return type.CreateType()!;
            }
        }
    }
    
}
