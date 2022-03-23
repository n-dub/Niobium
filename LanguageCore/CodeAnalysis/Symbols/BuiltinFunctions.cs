using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LanguageCore.CodeAnalysis.Symbols
{
    internal static class BuiltinFunctions
    {
        public static readonly FunctionSymbol Print = new FunctionSymbol("print",
            new[] {new ParameterSymbol("value", TypeSymbol.Any, 0)}, TypeSymbol.Void);

        public static readonly FunctionSymbol ReadLine = new FunctionSymbol("readLine",
            Array.Empty<ParameterSymbol>(), TypeSymbol.String);

        public static readonly FunctionSymbol Random = new FunctionSymbol("random",
            new[] {new ParameterSymbol("min", TypeSymbol.Int32, 0), new ParameterSymbol("max", TypeSymbol.Int32, 1)},
            TypeSymbol.Int32);

        internal static IEnumerable<FunctionSymbol> GetAll()
        {
            return typeof(BuiltinFunctions)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(FunctionSymbol))
                .Select(f => (FunctionSymbol) f.GetValue(null)!);
        }
    }
}
