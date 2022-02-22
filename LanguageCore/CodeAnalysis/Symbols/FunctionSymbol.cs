using System.Collections.Generic;

namespace LanguageCore.CodeAnalysis.Symbols
{
    public sealed class FunctionSymbol : Symbol
    {
        public override SymbolKind Kind => SymbolKind.Function;
        public IReadOnlyList<ParameterSymbol> Parameters { get; }
        public TypeSymbol Type { get; }

        public FunctionSymbol(string name, IReadOnlyList<ParameterSymbol> parameters, TypeSymbol type) : base(name)
        {
            Parameters = parameters;
            Type = type;
        }
    }
}
