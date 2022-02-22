using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LanguageCore.CodeAnalysis.Symbols
{
    internal static class BuiltinFunctions
    {
        public static readonly FunctionSymbol Print = new FunctionSymbol("print",
            new[] {new ParameterSymbol("message", TypeSymbol.String)}, TypeSymbol.Void);

        public static readonly FunctionSymbol ReadLine =
            new FunctionSymbol("readLine", Array.Empty<ParameterSymbol>(), TypeSymbol.String);

        internal static IEnumerable<FunctionSymbol> GetAll()
        {
            return typeof(BuiltinFunctions)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(FunctionSymbol))
                .Select(f => (FunctionSymbol) f.GetValue(null));
        }
    }
}
