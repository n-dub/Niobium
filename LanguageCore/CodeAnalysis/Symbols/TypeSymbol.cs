namespace LanguageCore.CodeAnalysis.Symbols
{
    public sealed class TypeSymbol : Symbol
    {
        public static readonly TypeSymbol Error = new TypeSymbol("Error");

        public static readonly TypeSymbol Bool = new TypeSymbol("Bool");
        public static readonly TypeSymbol Int32 = new TypeSymbol("Int32");
        public static readonly TypeSymbol String = new TypeSymbol("String");

        private TypeSymbol(string name) : base(name)
        {
        }

        public override SymbolKind Kind => SymbolKind.Type;
    }
}
