using LanguageCore.CodeAnalysis.Binding;

namespace LanguageCore.CodeAnalysis.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        public bool IsImmutable { get; }
        public TypeSymbol Type { get; }
        public BoundConstant Constant { get; }

        internal VariableSymbol(string name, bool isImmutable, TypeSymbol type, BoundConstant constant) : base(name)
        {
            IsImmutable = isImmutable;
            Type = type;
            Constant = isImmutable ? constant : null;
        }
    }
}
