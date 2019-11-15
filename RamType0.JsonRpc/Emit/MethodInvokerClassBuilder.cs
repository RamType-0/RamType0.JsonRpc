using System;
using Utf8Json;
using System.Reflection.Emit;
using System.Reflection;
namespace RamType0.JsonRpc.Emit
{
    /// <summary>
    /// ILGeneratorのお時間です
    /// </summary>
    public static class MethodInvokerClassBuilder
    {
        static readonly ModuleBuilder moduleBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("JsonRpcMethodParamsResolvers"), AssemblyBuilderAccess.Run).DefineDynamicModule("JsonRpcMethodParamsResolvers");

        public abstract class ParamsFormatter<T>
        where T : struct, IMethodParamsObject
        {
            public static ParamsFormatter<T>? instance;
            public static ParamsFormatter<T> Instance
            {
                get
                {
                    return instance ?? throw new InvalidOperationException();
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
                            throw new FormatException();
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
        /// <summary>
        /// 戻り値を持った関数の引数を表現する<see cref="IMethodParamsObject"/>を示します。
        /// </summary>
        /// <typeparam name="T">関数の戻り値の型。</typeparam>
        public interface IMethodParamsObject<T> : IMethodParamsObject
        {
            public new T Invoke();
            void IMethodParamsObject.Invoke() => Invoke();
        }
        /// <summary>
        /// 関数の引数を表現するオブジェクトを示します。Disposeすると、以後これを実装する型の全てのインスタンスでのInvokeの結果は未定義になります。
        /// </summary>
        public interface IMethodParamsObject:IDisposable
        {
            public void Invoke();
        }

        /// <summary>
        /// インスタンスフィールドを持たない<see cref="IMethodParamsObject"/>を示します。
        /// </summary>
        public interface IEmptyParamsObject : IMethodParamsObject { }
        public static Type CreateType<T>(T jsonRpcFunction,string name)
            where T : Delegate
        {
            MethodInfo method = jsonRpcFunction.Method;
            var parameters = method.GetParameters();

            var (paramsObjType, paramsObjFields) = CreateParamsTypes(jsonRpcFunction, name, method, parameters);
            
            return paramsObjType;
        }

        private static Type CreateParamsFormatterType(string name, TypeInfo paramsObjType, FieldInfo[] paramsObjFields)
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

                var il = readFromArrayStyleMethod.GetILGenerator();
                var paramsObjLocal = il.DeclareLocal(paramsObjType);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, readBeginArrayVerify);
                if (paramsObjFields.Length != 0)
                {
                    for (int i = 0; i < paramsObjFields.Length - 1; i++)
                    {
                        FieldInfo field = paramsObjFields[i];
                        il.Emit(OpCodes.Ldloca_S, (byte)0);

                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Call, readObjectMethod.MakeGenericMethod(field.FieldType));
                        il.Emit(OpCodes.Stfld, field);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, readSeparatorVerify);
                    }

                    {
                        FieldInfo field = paramsObjFields[^1];
                        il.Emit(OpCodes.Ldloca_S, (byte)0);

                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Call, readObjectMethod.MakeGenericMethod(field.FieldType));
                        il.Emit(OpCodes.Stfld, field);
                        il.Emit(OpCodes.Ldarg_0);

                    }
                }
                il.Emit(OpCodes.Call, readEndArrayVerify);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ret);
                MethodInfo methodInfoDeclaration = baseParamsFormatterType.GetMethod("ReadParamsObjectFromArrayStyle")!;
                paramsFormatterType.DefineMethodOverride(readFromArrayStyleMethod, methodInfoDeclaration);
            
            Type createdParamsFormatterType = paramsFormatterType.CreateType()!;
            baseParamsFormatterType.GetField("instance")!.SetValue(null, Activator.CreateInstance(createdParamsFormatterType));
            return createdParamsFormatterType;
        }

        static Type[] IParamsObjectTypeArray { get; } = new Type[] { typeof(IMethodParamsObject) };
        static Type[] IEmptyParamsObjectTypeArray { get; } = new Type[] { typeof(IEmptyParamsObject) };
        private static (TypeInfo paramsObjType,FieldInfo[] paramsObjFields) CreateParamsTypes<T>(T jsonRpcFunction,string name, MethodInfo method, ParameterInfo[] parameters) where T : Delegate
        {

            Type returnType = method.ReturnType;
            bool isVoidMethod = returnType == typeof(void);
            bool isEmpty = parameters.Length == 0;
            TypeBuilder type;
            MethodInfo iParamsObjInvokeMethod;
            if (isVoidMethod)
            {
                var interfaces = isEmpty ? IEmptyParamsObjectTypeArray : IParamsObjectTypeArray;
                type = moduleBuilder.DefineType(name + "Params", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AutoLayout, typeof(ValueType), interfaces);
                iParamsObjInvokeMethod = typeof(IMethodParamsObject).GetMethod("Invoke")!;
            }
            else
            {
                var genericType = typeof(IMethodParamsObject<>).MakeGenericType(new Type[] { returnType });
                Type[] interfaces = isEmpty ? new Type[] { typeof(IEmptyParamsObject), genericType } : new Type[] { typeof(IMethodParamsObject), genericType };
                type = moduleBuilder.DefineType(name + "Params", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AutoLayout, typeof(ValueType), interfaces);
                iParamsObjInvokeMethod = genericType.GetMethod("Invoke")!;
            }


            var fields = isEmpty ? Array.Empty<FieldBuilder>() : new FieldBuilder[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                fields[i] = type.DefineField(parameter.Name!, parameter.ParameterType, FieldAttributes.Public);
            }
            

            var typeInfo = DefineInvokeAndCreateTypeInfo(jsonRpcFunction, method, returnType, type, iParamsObjInvokeMethod, fields);
            var formatterType = CreateParamsFormatterType(name, typeInfo, fields);
            var jsonFormatterAttrCtor = typeof(JsonFormatterAttribute).GetConstructor(new[] { typeof(Type) })!;

            type.SetCustomAttribute(new CustomAttributeBuilder(jsonFormatterAttrCtor, new[] { formatterType }));
            return (typeInfo, fields);
        }

        private static TypeInfo DefineInvokeAndCreateTypeInfo<T>(T jsonRpcFunction, MethodInfo method, Type returnType, TypeBuilder type, MethodInfo iParamsObjInvokeMethod, FieldBuilder[] fields) where T : Delegate
        {
            var invokeMethod = type.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, returnType, Type.EmptyTypes);
            type.DefineMethodOverride(invokeMethod, iParamsObjInvokeMethod);
            if (method.IsStatic)
            {
                var il = invokeMethod.GetILGenerator();

                foreach (var field in fields)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, field);
                }

                il.Emit(OpCodes.Call, method);
                il.Emit(OpCodes.Ret);
                var disposeMethod = type.DefineMethod("Dispose", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final);
                var disposeIL = disposeMethod.GetILGenerator();
                disposeIL.Emit(OpCodes.Ret);
                type.DefineMethodOverride(disposeMethod, typeof(IDisposable).GetMethod("Dispose")!);
                return type.CreateTypeInfo()!;
            }
            else
            {
                var targetObj = jsonRpcFunction.Target!;
                Type targetObjType = targetObj.GetType();
                const string targetObjStoredFieldName = "targetObj";
                var targetObjStoredField = type.DefineField(targetObjStoredFieldName, targetObjType, FieldAttributes.Public | FieldAttributes.Static);
                var il = invokeMethod.GetILGenerator();
                bool targetObjIsValueType = targetObjType.IsValueType;
                if (targetObjIsValueType)
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
                var disposeMethod = type.DefineMethod("Dispose", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final);
                var disposeIL = disposeMethod.GetILGenerator();
                if (targetObjIsValueType)
                {
                    disposeIL.Emit(OpCodes.Ldsflda, targetObjStoredField);
                    disposeIL.Emit(OpCodes.Initobj,targetObjType);
                }
                else
                {
                    disposeIL.Emit(OpCodes.Ldnull);
                    disposeIL.Emit(OpCodes.Stsfld, targetObjStoredField);
                }

                disposeIL.Emit(OpCodes.Ret);
                type.DefineMethodOverride(disposeMethod, typeof(IDisposable).GetMethod("Dispose")!);
                var createdType = type.CreateTypeInfo()!;
                createdType.GetField(targetObjStoredFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, targetObj);//TODO:targetObjが永久に開放されなくなるので、適当に開放できるようにする。
                return createdType;
            }
        }


    }

}
