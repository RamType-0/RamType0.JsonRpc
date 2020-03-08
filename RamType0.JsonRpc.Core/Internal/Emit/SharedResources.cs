
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace RamType0.JsonRpc.Internal.Emit
{
    static partial class SharedResources
    {
        internal static ModuleBuilder ModuleBuilder { get; } = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(nameof(RamType0.JsonRpc.Internal) + ".DynamicAssembly"), AssemblyBuilderAccess.Run).DefineDynamicModule("RamType0.JsonRpc.DynamicModule");
        #region 不毛なプーリング
        [ThreadStatic]
        static Type[]? typeArray1;
        internal static Type[] TypeArray1 => typeArray1 ??= new Type[1];
        [ThreadStatic]
        static Type[]? typeArray2;
        internal static Type[] TypeArray2 => typeArray2 ??= new Type[2];
        [ThreadStatic]
        static Type[]? typeArray3;
        internal static Type[] TypeArray3 => typeArray3 ??= new Type[3];
        [ThreadStatic]
        static Type[]? typeArray4;
        internal static Type[] TypeArray4 => typeArray4 ??= new Type[4];
        [ThreadStatic]
        static Type[]? typeArray5;
        internal static Type[] TypeArray5 => typeArray5 ??= new Type[5];
        [ThreadStatic]
        static Type[]? typeArray6;
        internal static Type[] TypeArray6 => typeArray6 ??= new Type[6];
        #endregion

    }
}
