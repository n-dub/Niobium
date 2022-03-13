using LanguageCore.CodeAnalysis.Binding;

namespace LanguageCore.CodeAnalysis.Symbols
{
    public sealed class GlobalVariableSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.GlobalVariable;

        internal GlobalVariableSymbol(string name, bool isReadOnly, TypeSymbol type, BoundConstant constant)
            : base(name, isReadOnly, type, constant)
        {
        }
    }
}
