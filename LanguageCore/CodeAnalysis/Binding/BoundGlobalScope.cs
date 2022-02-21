using System.Collections.Generic;
using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal class BoundGlobalScope
    {
        public BoundGlobalScope Previous { get; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public IReadOnlyList<VariableSymbol> Variables { get; }
        public BoundStatement Statement { get; }

        public BoundGlobalScope(BoundGlobalScope previous, IReadOnlyList<Diagnostic> diagnostics,
            IReadOnlyList<VariableSymbol> variables, BoundStatement statement)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            Variables = variables;
            Statement = statement;
        }
    }
}
