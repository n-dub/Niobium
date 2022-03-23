using System.Collections.Generic;
using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal class BoundGlobalScope
    {
        public BoundGlobalScope? Previous { get; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public FunctionSymbol? MainFunction { get; }
        public FunctionSymbol? ScriptFunction { get; }
        public IReadOnlyList<FunctionSymbol> Functions { get; }
        public IReadOnlyList<VariableSymbol> Variables { get; }
        public IReadOnlyList<BoundStatement> Statements { get; }

        public BoundGlobalScope(BoundGlobalScope? previous, IReadOnlyList<Diagnostic> diagnostics,
            FunctionSymbol? mainFunction, FunctionSymbol? scriptFunction,
            IReadOnlyList<FunctionSymbol> functions, IReadOnlyList<VariableSymbol> variables,
            IReadOnlyList<BoundStatement> statements)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            MainFunction = mainFunction;
            ScriptFunction = scriptFunction;
            Functions = functions;
            Variables = variables;
            Statements = statements;
        }
    }
}
