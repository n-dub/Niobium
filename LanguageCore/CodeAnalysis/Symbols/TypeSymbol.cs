using System;
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

        public override SymbolKind Kind => SymbolKind.Type;

        private TypeSymbol(string name) : base(name)
        {
        }

        public static bool TryParse(string name, out TypeSymbol type)
        {
            type = typeof(TypeSymbol)
                .GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(x => (TypeSymbol) x.GetValue(null))
                .FirstOrDefault(x => x.Name == name);
            return type != null;
        }

        public Type ToSystemType()
        {
            if (this == Bool)
            {
                return typeof(bool);
            }

            if (this == Int32)
            {
                return typeof(int);
            }

            if (this == String)
            {
                return typeof(string);
            }

            if (this == Any)
            {
                return typeof(object);
            }

            throw new Exception($"Unexpected type {this}");
        }
    }
}
