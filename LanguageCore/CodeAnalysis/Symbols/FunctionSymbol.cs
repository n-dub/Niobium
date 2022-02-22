using System.Collections.Generic;

namespace LanguageCore.CodeAnalysis.Symbols
{
    public sealed class FunctionSymbol : Symbol
    {
        public override SymbolKind Kind => SymbolKind.Function;
        public IReadOnlyList<ParameterSymbol> Parameter { get; }
        public TypeSymbol Type { get; }

        public FunctionSymbol(string name, IReadOnlyList<ParameterSymbol> parameter, TypeSymbol type) : base(name)
        {
            Parameter = parameter;
            Type = type;
        }
    }
}
