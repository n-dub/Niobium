using System.Collections.Generic;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal class BoundGlobalScope
    {
        public BoundGlobalScope Previous { get; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public IReadOnlyList<VariableSymbol> Variables { get; }
        public BoundExpression Expression { get; }

        public BoundGlobalScope(BoundGlobalScope previous, IReadOnlyList<Diagnostic> diagnostics,
            IReadOnlyList<VariableSymbol> variables, BoundExpression expression)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            Variables = variables;
            Expression = expression;
        }
    }
}
