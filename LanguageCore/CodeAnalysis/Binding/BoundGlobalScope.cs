using System.Collections.Generic;
using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal class BoundGlobalScope
    {
        public BoundGlobalScope Previous { get; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public IReadOnlyList<FunctionSymbol> Functions { get; }
        public IReadOnlyList<VariableSymbol> Variables { get; }
        public IReadOnlyList<BoundStatement> Statements { get; }

        public BoundGlobalScope(BoundGlobalScope previous, IReadOnlyList<Diagnostic> diagnostics,
            IReadOnlyList<FunctionSymbol> functions, IReadOnlyList<VariableSymbol> variables,
            IReadOnlyList<BoundStatement> statements)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            Functions = functions;
            Variables = variables;
            Statements = statements;
        }
    }
}
