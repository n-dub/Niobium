using System.Collections.Generic;
using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis
{
    public sealed class EvaluationResult
    {
        // TODO: create an extension method over IEnumerable<Diagnostic> to get errors and warnings
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public object? Value { get; }
        public string? Name { get; }
        public TypeSymbol? TypeSymbol { get; }

        public EvaluationResult(IReadOnlyList<Diagnostic> diagnostics, object? value, string? name, TypeSymbol? typeSymbol)
        {
            Diagnostics = diagnostics;
            TypeSymbol = typeSymbol;
            Value = value;
            Name = name;
        }

        public override string ToString()
        {
            return $"{Name}: {TypeSymbol} = {Value}";
        }
    }
}
