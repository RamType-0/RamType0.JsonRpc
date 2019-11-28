using RamType0.JsonRpc.Server;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Utf8Json;

namespace RamType0.JsonRpc
{
    public static partial class Emit
    {

        public static class ParamsDeserializerBuilder
        {


            static MethodInfo ReadIsBeginArrayWithVerify { get; } = typeof(JsonReader).GetMethod("ReadIsBeginArrayWithVerify")!;
            static MethodInfo ReadIsEndArrayWithVerify { get; } = typeof(JsonReader).GetMethod("ReadIsEndArrayWithVerify")!;
            static MethodInfo ReadIsValueSeparatorWithVerify { get; } = typeof(JsonReader).GetMethod("ReadIsValueSeparatorWithVerify")!;
            static MethodInfo ReadJson { get; } = typeof(ParamsDeserializerBuilder).GetMethod(nameof(ReadJsonImpl), BindingFlags.Public | BindingFlags.Static)!;

            static Type[] DeserializeParams { get; } = new Type[] { typeof(JsonReader).MakeByRefType(), typeof(IJsonFormatterResolver) };

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T ReadJsonImpl<T>(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                return formatterResolver.GetFormatterWithVerify<T>().Deserialize(ref reader, formatterResolver);
            }
            public static class ArrayStyle
            {



                /// <summary>
                /// <paramref name="fields"/>内のフィールドに対して順番に配列要素をデシリアライズし、<paramref name="paramsType"/>のインスタンスを返す<see cref="IArrayStyleParamsDeserializer{T}"/>を実装した構造体の型を生成します。
                /// </summary>
                /// <param name="paramsType">配列全体からデシリアライズされる型。</param>
                /// <param name="fields">順番どおりにデシリアライズされる配列要素のデシリアライズ先のフィールド。</param>
                /// <returns></returns>
                public static Type Create(Type paramsType, ReadOnlySpan<FieldInfo> fields)
                {

                    var builder = ModuleBuilder.DefineType($"{paramsType.FullName}.ArrayStyleDeserializer", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(ValueType));
                    var typeArray1 = TypeArray1;
                    typeArray1[0] = paramsType;
                    Type interfaceType = typeof(IArrayStyleParamsDeserializer<>).MakeGenericType(typeArray1);
                    builder.AddInterfaceImplementation(interfaceType);
                    var deserializeMethod = builder.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, paramsType, DeserializeParams);
                    builder.DefineMethodOverride(deserializeMethod, typeof(IParamsDeserializer<>).MakeGenericType(typeArray1).GetMethod("Deserialize")!);
                    //T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver);
                    {
                        var il = deserializeMethod.GetILGenerator();
                        il.DeclareLocal(paramsType);
                        {
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Call, ReadIsBeginArrayWithVerify);
                            //var i = 0;

                            for (int i = 0; i < fields.Length; i++)
                            {
                                var field = fields[i];
                                il.Emit(OpCodes.Ldloca_S, (byte)0);

                                il.Emit(OpCodes.Ldarg_1);
                                il.Emit(OpCodes.Ldarg_2);
                                typeArray1[0] = field.FieldType;
                                il.Emit(OpCodes.Call, ReadJson.MakeGenericMethod(typeArray1));
                                il.Emit(OpCodes.Stfld, field);
                                if (i != fields.Length - 1)
                                {
                                    il.Emit(OpCodes.Ldarg_1);
                                    il.Emit(OpCodes.Call, ReadIsValueSeparatorWithVerify);
                                }
                            }
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Call, ReadIsEndArrayWithVerify);
                            il.Emit(OpCodes.Ldloc_0);
                            il.Emit(OpCodes.Ret);
                        }
                    }
                    return builder.CreateType()!;
                }



            }

            public static class ObjectStyle
            {
                public static Type Create(Type paramsType)
                {
                    var typeArray1 = TypeArray1;
                    typeArray1[0] = paramsType;
                    return typeof(DefaultObjectStyleParamsDeserializer<>).MakeGenericType(typeArray1);
                }
            }
        }
    }
}
