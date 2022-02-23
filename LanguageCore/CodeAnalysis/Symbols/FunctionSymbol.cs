using System.Collections.Generic;
using LanguageCore.CodeAnalysis.Syntax;

namespace LanguageCore.CodeAnalysis.Symbols
{
    public sealed class FunctionSymbol : Symbol
    {
        public override SymbolKind Kind => SymbolKind.Function;
        public IReadOnlyList<ParameterSymbol> Parameters { get; }
        public TypeSymbol Type { get; }
        public FunctionDeclarationSyntax Declaration { get; }

        public FunctionSymbol(string name, IReadOnlyList<ParameterSymbol> parameters, TypeSymbol type,
            FunctionDeclarationSyntax declaration = null) : base(name)
        {
            Parameters = parameters;
            Type = type;
            Declaration = declaration;
        }
    }
}
