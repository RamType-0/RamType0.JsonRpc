using System;
using System.Buffers;
using System.Reflection;
using System.Reflection.Emit;

namespace RamType0.JsonRpc
{
    static partial class Emit
    {
        static class ParamsBuilder
        {

            public static (Type paramsType, ArraySegment<FieldBuilder> argumentFields, ArraySegment<FieldBuilder> deserializedFields) FromMethod(MethodInfo method)
            {


                var builder = ModuleBuilder.DefineType($"{method.DeclaringType?.FullName}.{method.Name}.Parameters", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(ValueType));

                var parameters = method.GetParameters();
                bool isEmpty = parameters.Length == 0;
                if (isEmpty)
                {
                    builder.AddInterfaceImplementation(typeof(IEmptyParams));

                }
                else
                {
                    builder.AddInterfaceImplementation(typeof(IMethodParams));


                }
                var _fields = ArrayPool<FieldBuilder>.Shared.Rent(parameters.Length);
                var fields = _fields.AsSpan(0, parameters.Length);
                var cancellByIDIndex = -1;
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo parameter = parameters[i];
                    var field = fields[i] = builder.DefineField(parameter.Name!, parameter.ParameterType, FieldAttributes.Public);
                    if (parameter.GetCustomAttribute(typeof(RpcIDAttribute)) != null)
                    {
                        if (parameter.ParameterType == typeof(ID?))
                        {

                            if (parameters.Length == 1)
                            {
                                builder.AddInterfaceImplementation(typeof(IEmptyParams));
                            }
                            field.SetCustomAttribute(idCancellationTokenAttributeBuilder);
                            builder.AddInterfaceImplementation(typeof(IMethodParamsInjectID));
                            var property = builder.DefineProperty("ID", PropertyAttributes.HasDefault, typeof(ID?), Type.EmptyTypes);
                            property.SetGetMethod(DefineCancellationTokenGetter(builder, field));
                            property.SetSetMethod(DefineCancellationTokenSetter(builder, field));
                            cancellByIDIndex = i;
                        }
                    }
                }
                var type = builder.CreateType()!;
                ArraySegment<FieldBuilder> argFields = new ArraySegment<FieldBuilder>(_fields, 0, parameters.Length);


                if (cancellByIDIndex != -1)
                {
                    var deserializedFields = new FieldBuilder[parameters.Length - 1];
                    fields[..cancellByIDIndex].CopyTo(deserializedFields);
                    fields[(cancellByIDIndex + 1)..].CopyTo(deserializedFields.AsSpan(cancellByIDIndex));

                    return (type, argFields, deserializedFields);
                }
                else
                {
                    return (type, argFields, argFields);
                }

            }



            readonly static CustomAttributeBuilder idCancellationTokenAttributeBuilder = new CustomAttributeBuilder(typeof(RpcIDAttribute).GetConstructor(Type.EmptyTypes)!, Array.Empty<object>());
            private static MethodBuilder DefineCancellationTokenGetter(TypeBuilder type, FieldInfo field)
            {
                var getter = type.DefineMethod("get_ID", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(ID?), Type.EmptyTypes);
                type.DefineMethodOverride(getter, typeof(IMethodParamsInjectID).GetMethod("get_ID")!);
                var il = getter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ret);
                return getter;
            }

            private static MethodBuilder DefineCancellationTokenSetter(TypeBuilder type, FieldInfo field)
            {
                var typeArray1 = TypeArray1;
                typeArray1[0] = typeof(ID?);
                var setter = type.DefineMethod("set_ID", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), typeArray1);
                type.DefineMethodOverride(setter, typeof(IMethodParamsInjectID).GetMethod("set_ID")!);
                var il = setter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, field);
                il.Emit(OpCodes.Ret);
                return setter;
            }
        }
    }
}
