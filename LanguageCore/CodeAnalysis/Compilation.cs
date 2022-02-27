using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using LanguageCore.CodeAnalysis.Binding;
using LanguageCore.CodeAnalysis.Lowering;
using LanguageCore.CodeAnalysis.Symbols;
using LanguageCore.CodeAnalysis.Syntax;

namespace LanguageCore.CodeAnalysis
{
    public sealed class Compilation
    {
        public Compilation Previous { get; }

        public SyntaxTree SyntaxTree { get; }

        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (globalScope == null)
                {
                    var newGlobalScope = Binder.BindGlobalScope(Previous?.GlobalScope, SyntaxTree.Root);
                    Interlocked.CompareExchange(ref globalScope, newGlobalScope, null);
                }

                return globalScope;
            }
        }

        private BoundGlobalScope globalScope;

        public Compilation(SyntaxTree syntaxTree)
            : this(null, syntaxTree)
        {
        }

        private Compilation(Compilation previous, SyntaxTree syntaxTree)
        {
            Previous = previous;
            SyntaxTree = syntaxTree;
        }

        public Compilation ContinueWith(SyntaxTree syntaxTree)
        {
            return new Compilation(this, syntaxTree);
        }

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
        {
            var diagnostics = SyntaxTree.Diagnostics.Concat(GlobalScope.Diagnostics).ToArray();
            if (diagnostics.Any())
            {
                return new EvaluationResult(diagnostics, null, null, TypeSymbol.Error);
            }

            var program = Binder.BindProgram(GlobalScope);
            if (program.Diagnostics.Any())
            {
                return new EvaluationResult(program.Diagnostics.ToArray(), null, null, TypeSymbol.Error);
            }

            var evaluator = new Evaluator(program, variables);
            var value = evaluator.Evaluate(out var type);
            var name = GetEvaluationVariableName(program.Statement.Statements.LastOrDefault());
            return new EvaluationResult(Array.Empty<Diagnostic>(), value, name, type);
        }

        private static string GetEvaluationVariableName(BoundStatement statement)
        {
            switch (statement)
            {
                case BoundExpressionStatement s:
                    switch (s.Expression)
                    {
                        case BoundVariableExpression variable:
                            return variable.Variable.Name;
                        case BoundAssignmentExpression assignment:
                            return assignment.Variable.Name;
                        default:
                            return "_";
                    }
                case BoundVariableDeclarationStatement s:
                    return s.Variable.Name;
                default:
                    return "_";
            }
        }

        public void EmitTree(TextWriter writer)
        {
            var program = Binder.BindProgram(GlobalScope);

            if (program.Statement.Statements.Any())
            {
                program.Statement.WriteTo(writer);
            }
            else
            {
                foreach (var functionBody in program.Functions)
                {
                    if (!GlobalScope.Functions.Contains(functionBody.Key))
                    {
                        continue;
                    }

                    functionBody.Key.WriteTo(writer);
                    functionBody.Value.WriteTo(writer);
                }
            }
        }
    }
}
