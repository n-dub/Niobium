using System.Collections.Generic;
using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundProgram
    {
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public IReadOnlyDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }
        public BoundBlockStatement Statement { get; }

        public BoundProgram(IReadOnlyList<Diagnostic> diagnostics,
            IReadOnlyDictionary<FunctionSymbol, BoundBlockStatement> functions, BoundBlockStatement statement)
        {
            Diagnostics = diagnostics;
            Functions = functions;
            Statement = statement;
        }
    }
}
