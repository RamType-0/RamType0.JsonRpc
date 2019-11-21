using System;
using Utf8Json;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;
using System.Linq;
namespace RamType0.JsonRpc.Emit
{

    /// <summary>
    /// ILGeneratorのお時間です
    /// </summary>
    public static class MethodParamsTypeBuilder
    {
        static readonly ModuleBuilder moduleBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("JsonRpcMethodParamsResolvers"), AssemblyBuilderAccess.Run).DefineDynamicModule("JsonRpcMethodParamsResolvers");


        public static Type CreateParamsType<T>(T jsonRpcFunction, string name)
            where T : Delegate
        {
            MethodInfo method = jsonRpcFunction.Method;
            var parameters = method.GetParameters();

            foreach (var param in parameters)
            {
                if (param.ParameterType.IsByRef)
                {
                    throw new ArgumentException("JsonRpcMethod does not support ByRef parameter.");
                }
            }

            var (paramsObjType, _) = CreateParamsTypes(jsonRpcFunction, name, method, parameters);

            return paramsObjType;
        }

        private static Type CreateParamsFormatterType(string name, TypeInfo paramsObjType, FieldInfo[] paramsObjFields,int cancellByIDIndex = -1)
        {
            Type baseParamsFormatterType = typeof(ParamsFormatter<>).MakeGenericType(paramsObjType);
            var paramsFormatterType = moduleBuilder.DefineType(name + "ParamsFormatter", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class | TypeAttributes.AutoLayout, baseParamsFormatterType);
            //var paramsFormatterGenericParam = paramsFormatterType.DefineGenericParameters("T")[0];
            //paramsFormatterGenericParam.SetBaseTypeConstraint(typeof(ValueType));
            //paramsFormatterGenericParam.SetInterfaceConstraints(typeof(IMethodParamsObject));
            var readObjectMethod = baseParamsFormatterType.GetMethod("ReadObject")!;
            var readBeginArrayVerify = typeof(JsonReader).GetMethod("ReadIsBeginArrayWithVerify")!;
            var readSeparatorVerify = typeof(JsonReader).GetMethod("ReadIsValueSeparatorWithVerify")!;
            var readEndArrayVerify = typeof(JsonReader).GetMethod("ReadIsEndArrayWithVerify")!;

            var readFromArrayStyleMethod = paramsFormatterType.DefineMethod("ReadParamsObjectFromArrayStyle", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, paramsObjType, new[] { typeof(JsonReader).MakeByRefType(), typeof(IJsonFormatterResolver) });


            if (cancellByIDIndex != -1)
            {
                var array = new FieldInfo[paramsObjFields.Length - 1];
                for (int i = 0; i < cancellByIDIndex; i++)
                {
                    array[i] = paramsObjFields[i];
                }
                for (int i = cancellByIDIndex; i < array.Length; i++)
                {
                    array[i] = paramsObjFields[i + 1];
                }
                paramsObjFields = array;
            }

            var il = readFromArrayStyleMethod.GetILGenerator();
            var paramsObjLocal = il.DeclareLocal(paramsObjType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, readBeginArrayVerify);
            
            if (paramsObjFields.Length != 0)
            {
                for (int i = 0; i < paramsObjFields.Length - 1; i++)
                {
                    FieldInfo field = paramsObjFields[i];
                    il.Emit(OpCodes.Ldloca_S, (byte)0);

                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, readObjectMethod.MakeGenericMethod(field.FieldType));
                    il.Emit(OpCodes.Stfld, field);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, readSeparatorVerify);
                }

                {
                    FieldInfo field = paramsObjFields[^1];
                    il.Emit(OpCodes.Ldloca_S, (byte)0);

                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, readObjectMethod.MakeGenericMethod(field.FieldType));
                    il.Emit(OpCodes.Stfld, field);


                }
            }
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, readEndArrayVerify);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);
            MethodInfo methodInfoDeclaration = baseParamsFormatterType.GetMethod("ReadParamsObjectFromArrayStyle")!;
            paramsFormatterType.DefineMethodOverride(readFromArrayStyleMethod, methodInfoDeclaration);

            Type createdParamsFormatterType = paramsFormatterType.CreateType()!;
            baseParamsFormatterType.GetField("instance")!.SetValue(null, Activator.CreateInstance(createdParamsFormatterType));
            return createdParamsFormatterType;
        }

        static Type[] IActionParamsObjectTypeArray { get; } = new Type[] { typeof(IActionParamsObject) };
        static Type[] IEmptyActionParamsObjectTypeArray { get; } = new Type[] {typeof(IActionParamsObject) , typeof(IEmptyParamsObject) };
        private static (TypeInfo paramsType, Type paramsFormatterType) CreateParamsTypes<T>(T jsonRpcFunction, string name, MethodInfo method, ParameterInfo[] parameters) where T : Delegate
        {

            Type returnType = method.ReturnType;
            
            bool isEmpty = parameters.Length == 0;

            MethodInfo iParamsObjInvokeMethod;
            TypeBuilder type;

            {
                Type[] interfaces;
                if (returnType == typeof(void))
                {
                    interfaces = isEmpty ? IEmptyActionParamsObjectTypeArray : IActionParamsObjectTypeArray;
                    iParamsObjInvokeMethod = typeof(IMethodParamsObject).GetMethod("Invoke")!;
                }
                else
                {
                    var genericType = typeof(IFunctionParamsObject<>).MakeGenericType(new Type[] { returnType });
                    interfaces = isEmpty ? new Type[] { typeof(IEmptyParamsObject), genericType } : new Type[] { typeof(IMethodParamsObject), genericType };
                    iParamsObjInvokeMethod = genericType.GetMethod("Invoke")!;


                }
                type = moduleBuilder.DefineType(name + "Params", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AutoLayout, typeof(ValueType), interfaces);

            }
            var fields = DefineParamsTypeFields(parameters, isEmpty, type,out var cancellByIDIndex);

            var typeInfo = DefineMethodsAndCreateTypeInfo(jsonRpcFunction, returnType, type, iParamsObjInvokeMethod, fields);
            var formatterType = CreateParamsFormatterType(name, typeInfo, fields,cancellByIDIndex);
            //var jsonFormatterAttrCtor = typeof(JsonFormatterAttribute).GetConstructor(new[] { typeof(Type) })!;

            //type.SetCustomAttribute(new CustomAttributeBuilder(jsonFormatterAttrCtor, new[] { formatterType }));
            return (typeInfo, formatterType);
        }


        static CustomAttributeBuilder idCancellationTokenAttributeBuilder = new CustomAttributeBuilder(typeof(CancelledByIDAttribute).GetConstructor(Type.EmptyTypes)!, Array.Empty<object>());
        private static FieldBuilder[] DefineParamsTypeFields(ParameterInfo[] parameters, bool isEmpty, TypeBuilder type,out int cancellByIDIndex)
        {
            cancellByIDIndex = -1;
            var fields = isEmpty ? Array.Empty<FieldBuilder>() : new FieldBuilder[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var field = fields[i] = type.DefineField(parameter.Name!, parameter.ParameterType, FieldAttributes.Public);
                if (parameter.GetCustomAttribute(typeof(CancelledByIDAttribute)) != null)
                {
                    if(parameter.ParameterType == typeof(CancellationToken))
                    {
                        field.SetCustomAttribute(idCancellationTokenAttributeBuilder);
                        type.AddInterfaceImplementation(typeof(ICancellableMethodParamsObject));
                        var property = type.DefineProperty("CancellationToken", PropertyAttributes.HasDefault, typeof(CancellationToken), Type.EmptyTypes);
                        property.SetGetMethod(DefineCancellationTokenGetter(type, field));
                        property.SetSetMethod(DefineCancellationTokenSetter(type, field));
                        cancellByIDIndex = i;
                    }
                }
            }

            return fields;
        }

        private static MethodBuilder DefineCancellationTokenGetter(TypeBuilder type,FieldInfo field)
        {
            var getter = type.DefineMethod("get_CancellationToken", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig,typeof(CancellationToken),Type.EmptyTypes);
            type.DefineMethodOverride(getter, typeof(ICancellableMethodParamsObject).GetMethod("get_CancellationToken")!);
            var il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return getter;
        }

        private static MethodBuilder DefineCancellationTokenSetter(TypeBuilder type,FieldInfo field)
        {
            var setter = type.DefineMethod("set_CancellationToken", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig,typeof(void),new[] { typeof(CancellationToken)});
            type.DefineMethodOverride(setter, typeof(ICancellableMethodParamsObject).GetMethod("set_CancellationToken")!);
            var il = setter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return setter;
        }

        private static TypeInfo DefineMethodsAndCreateTypeInfo<T>(T jsonRpcFunction, Type returnType, TypeBuilder type, MethodInfo iParamsObjInvokeMethod, FieldBuilder[] fields) where T : Delegate
        {

            var delegateInvokeMethod = typeof(T).GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance)!;
            const string targetObjStoredFieldName = "invokedDelegate";
            var delegateStoredField = type.DefineField(targetObjStoredFieldName, typeof(T), FieldAttributes.Public | FieldAttributes.Static);
            DefineInvoke(returnType, type, iParamsObjInvokeMethod, fields, delegateInvokeMethod, delegateStoredField);
            DefineDispose(type, delegateStoredField);
            TypeInfo createdType = CreateTypeAndStoreDelegate(jsonRpcFunction, type, targetObjStoredFieldName);
            return createdType;
        }

        private static void DefineInvoke(Type returnType, TypeBuilder type, MethodInfo iParamsObjInvokeMethod, FieldBuilder[] fields, MethodInfo delegateInvokeMethod, FieldBuilder delegateStoredField)
        {
            var invokeMethod = type.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, returnType, Type.EmptyTypes);
            type.DefineMethodOverride(invokeMethod, iParamsObjInvokeMethod);
            var il = invokeMethod.GetILGenerator();
            {
                il.Emit(OpCodes.Ldsfld, delegateStoredField);
            }
            foreach (var field in fields)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
            }

            il.Emit(OpCodes.Call, delegateInvokeMethod);
            il.Emit(OpCodes.Ret);
        }

        private static TypeInfo CreateTypeAndStoreDelegate<T>(T jsonRpcFunction, TypeBuilder type, string targetObjStoredFieldName) where T : Delegate
        {
            var createdType = type.CreateTypeInfo()!;
            createdType.GetField(targetObjStoredFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, jsonRpcFunction);//TODO:targetObjが永久に開放されなくなるので、適当に開放できるようにする。
            return createdType;
        }

        private static void DefineDispose(TypeBuilder type, FieldBuilder delegateStoredField)
        {
            var disposeMethod = type.DefineMethod("Dispose", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final);
            type.DefineMethodOverride(disposeMethod, typeof(IDisposable).GetMethod("Dispose")!);
            var disposeIL = disposeMethod.GetILGenerator();
            {
                disposeIL.Emit(OpCodes.Ldnull);
                disposeIL.Emit(OpCodes.Stsfld, delegateStoredField);
            }

            disposeIL.Emit(OpCodes.Ret);
        }
    }


   
    public abstract class ParamsFormatter<T>
        where T : struct, IMethodParamsObject
    {
        public static ParamsFormatter<T>? instance;
        public static ParamsFormatter<T> Instance
        {
            get
            {
                return instance ?? throw new FormatterNotRegisteredException($"Formatter of { typeof(T).Name} not registered.");
            }
        }

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
                        throw new JsonParsingException("ParamsObject was not array, neither object.");
                    }
            }
            return paramsObj;
        }

        public static TRead ReadObject<TRead>(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            return formatterResolver.GetFormatter<TRead>().Deserialize(ref reader, formatterResolver);
        }

        public abstract T ReadParamsObjectFromArrayStyle(ref JsonReader reader, IJsonFormatterResolver formatterResolver);

    }

    public struct DefaultObjectStyleParamsDeserializationProxy<T> : IObjectStyleParamsDeserializationProxy<T>
        where T : struct, IMethodParams
    {
        public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            return formatterResolver.GetFormatterWithVerify<T>().Deserialize(ref reader, formatterResolver);
        }
    }

    public interface IParamsDeserializationProxy<T>
        where T : struct, IMethodParams
    {
        T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver);
    }


    public readonly struct ParamsDeserializationProxy<T, TObjectStyle, TArrayStyle> : IParamsDeserializationProxy<T>
        where T : struct, IMethodParams
        where TObjectStyle : struct, IObjectStyleParamsDeserializationProxy<T>
        where TArrayStyle : struct, IArrayStyleParamsDeserializationProxy<T>
    {
        public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            T paramsObj;
            switch (reader.GetCurrentJsonToken())
            {
                case JsonToken.BeginObject:
                    {
                        paramsObj = default(TObjectStyle).Deserialize(ref reader, formatterResolver);
                        break;
                    }
                case JsonToken.BeginArray:
                    {
                        paramsObj = default(TArrayStyle).Deserialize(ref reader, formatterResolver);
                        break;
                    }
                default:
                    {
                        throw new JsonParsingException("ParamsObject was not array, neither object.");
                    }
            }
            return paramsObj;
        }

        public static TRead ReadObject<TRead>(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            return formatterResolver.GetFormatter<TRead>().Deserialize(ref reader, formatterResolver);
        }

    }

    public interface IArrayStyleParamsDeserializationProxy<T> : IParamsDeserializationProxy<T>
        where T:struct,IMethodParams
    {
        //T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver);
    }
    public interface IObjectStyleParamsDeserializationProxy<T> : IParamsDeserializationProxy<T>
        where T : struct, IMethodParams
    {
        //T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver);
    }

}
