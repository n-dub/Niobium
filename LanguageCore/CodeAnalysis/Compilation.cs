using System;
using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Binding;
using LanguageCore.CodeAnalysis.Syntax;

namespace LanguageCore.CodeAnalysis
{
    public sealed class Compilation
    {
        public SyntaxTree SyntaxTree { get; }

        public Compilation(SyntaxTree syntaxTree)
        {
            SyntaxTree = syntaxTree;
        }

        public EvaluationResult Evaluate(Dictionary<string, object> variables)
        {
            var binder = new Binder(variables);
            var boundExpression = binder.BindExpression(SyntaxTree.Root);

            var name = "$temp";
            switch (boundExpression)
            {
                case BoundVariableExpression variable:
                    name = variable.Name;
                    break;
                case BoundAssignmentExpression assignment:
                    name = assignment.Name;
                    break;
            }

            var diagnostics = SyntaxTree.Diagnostics.Concat(binder.Diagnostics).ToArray();
            if (diagnostics.Any())
            {
                return new EvaluationResult(diagnostics, null, null);
            }

            var evaluator = new Evaluator(boundExpression, variables);
            var value = evaluator.Evaluate();
            return new EvaluationResult(Array.Empty<Diagnostic>(), value, name);
        }
    }
}
