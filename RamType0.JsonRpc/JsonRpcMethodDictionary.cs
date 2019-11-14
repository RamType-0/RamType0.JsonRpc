using System;
using System.Collections.Generic;
using System.Text;
using Utf8Json;
using System.Reflection.Emit;
using System.Reflection;
namespace RamType0.JsonRpc
{
    public class JsonRpcMethodDictionary
    {
        static readonly ModuleBuilder moduleBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("JsonRpcMethodParamsResolvers"), AssemblyBuilderAccess.Run).DefineDynamicModule("JsonRpcMethodParamsResolvers");
        public readonly struct Element
        {
            public Element(Delegate function)
            {
                Method = function;
                Formatter = CreateFormatter(function);
            }
            static IJsonFormatter CreateFormatter(Delegate function)
            {
                MethodInfo method = function.Method;
                var returnType = method.ReturnType;
                var parameters = method.GetParameters();
                throw null!;
                
            }

            public static Type CreateParamsType(MethodInfo method)
            {
                var type = moduleBuilder.DefineType(method.Name + "Params", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AutoLayout, typeof(ValueType));
                var parameters = method.GetParameters();
                var fields = new FieldBuilder[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    fields[i] = type.DefineField(parameter.Name!, parameter.ParameterType, FieldAttributes.Public);
                }
                var invokeMethodParams = method.IsStatic ? Array.Empty<Type>() : new Type[] { method.DeclaringType! };
                var invokeMethod = type.DefineMethod("Invoke", MethodAttributes.Public, method.ReturnType, invokeMethodParams);
                var il = invokeMethod.GetILGenerator();
                
                foreach (var field in fields)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, field);
                }
                if (!method.IsStatic)
                    il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, method);
                return type.CreateType()!;
                

            }

            

            public Delegate Method { get; }
            public IJsonFormatter Formatter { get; }
        }
        Dictionary<EscapedUTF8String, Delegate> Methods { get; } = new Dictionary<EscapedUTF8String, Delegate>();
        
    }
}
