using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LanguageCore.CodeAnalysis.Symbols
{
    public sealed class TypeSymbol : Symbol
    {
        public static readonly TypeSymbol Error = new TypeSymbol("?");

        public static readonly TypeSymbol Any = new TypeSymbol("Any");
        public static readonly TypeSymbol Bool = new TypeSymbol("Bool");
        public static readonly TypeSymbol Int32 = new TypeSymbol("Int32");
        public static readonly TypeSymbol String = new TypeSymbol("String");
        public static readonly TypeSymbol Void = new TypeSymbol("Void");

        public static IEnumerable<TypeSymbol> AllTypes => typeof(TypeSymbol)
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Select(x => (TypeSymbol) x.GetValue(null))
            .Where(x => x != Error);

        public override SymbolKind Kind => SymbolKind.Type;

        private TypeSymbol(string name) : base(name)
        {
        }

        public static bool TryParse(string name, out TypeSymbol type)
        {
            type = AllTypes.FirstOrDefault(x => x.Name == name);
            return type != null;
        }

        public static Type ToSystemType(TypeSymbol symbol)
        {
            if (symbol == Bool)
            {
                return typeof(bool);
            }

            if (symbol == Int32)
            {
                return typeof(int);
            }

            if (symbol == String)
            {
                return typeof(string);
            }

            if (symbol == Any)
            {
                return typeof(object);
            }

            if (symbol == Void)
            {
                return typeof(void);
            }

            throw new Exception($"Unexpected type {symbol}");
        }
    }
}
