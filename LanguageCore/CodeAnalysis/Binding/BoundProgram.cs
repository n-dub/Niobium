using System.Collections.Generic;
using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundProgram
    {
        public BoundProgram(BoundGlobalScope globalScope, DiagnosticBag diagnostics, IReadOnlyDictionary<FunctionSymbol, BoundBlockStatement> functionBodies)
        {
            GlobalScope = globalScope;
            Diagnostics = diagnostics;
            FunctionBodies = functionBodies;
        }

        public BoundGlobalScope GlobalScope { get; }
        public DiagnosticBag Diagnostics { get; }
        public IReadOnlyDictionary<FunctionSymbol, BoundBlockStatement> FunctionBodies { get; }
    }
}
