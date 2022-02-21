namespace LanguageCore.CodeAnalysis.Symbols
{
    public sealed class VariableSymbol : Symbol
    {
        public bool IsImmutable { get; }
        public TypeSymbol Type { get; }

        public override SymbolKind Kind => SymbolKind.Variable;

        internal VariableSymbol(string name, bool isImmutable, TypeSymbol type) : base(name)
        {
            IsImmutable = isImmutable;
            Type = type;
        }
    }
}
