using System.Collections.Generic;
using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundProgram
    {
        public BoundProgram? Previous { get; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public FunctionSymbol? MainFunction { get; }
        public FunctionSymbol? ScriptFunction { get; }
        public IReadOnlyDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }

        public BoundProgram(BoundProgram? previous, IReadOnlyList<Diagnostic> diagnostics,
            FunctionSymbol? mainFunction, FunctionSymbol? scriptFunction,
            IReadOnlyDictionary<FunctionSymbol, BoundBlockStatement> functions)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            MainFunction = mainFunction;
            ScriptFunction = scriptFunction;
            Functions = functions;
        }
    }
}
