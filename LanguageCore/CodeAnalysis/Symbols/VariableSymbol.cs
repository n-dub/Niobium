namespace LanguageCore.CodeAnalysis.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        public bool IsImmutable { get; }
        public TypeSymbol Type { get; }

        internal VariableSymbol(string name, bool isImmutable, TypeSymbol type) : base(name)
        {
            IsImmutable = isImmutable;
            Type = type;
        }
    }
}
