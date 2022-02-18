using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using LanguageCore.CodeAnalysis.Binding;
using LanguageCore.CodeAnalysis.Lowering;
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
            var name = "_";
            switch (GlobalScope.Statement)
            {
                case BoundExpressionStatement statement:
                    switch (statement.Expression)
                    {
                        case BoundVariableExpression variable:
                            name = variable.Variable.Name;
                            break;
                        case BoundAssignmentExpression assignment:
                            name = assignment.Variable.Name;
                            break;
                    }

                    break;
                case BoundVariableDeclarationStatement statement:
                    name = statement.Variable.Name;
                    break;
            }

            var diagnostics = SyntaxTree.Diagnostics.Concat(GlobalScope.Diagnostics).ToArray();
            if (diagnostics.Any())
            {
                return new EvaluationResult(diagnostics, null, null);
            }

            var evaluator = new Evaluator(GetStatement(), variables);
            var value = evaluator.Evaluate();
            return new EvaluationResult(Array.Empty<Diagnostic>(), value, name);
        }

        public void EmitTree(TextWriter writer)
        {
            var statement = GetStatement();
            statement.WriteTo(writer);
        }

        private BoundBlockStatement GetStatement()
        {
            var result = GlobalScope.Statement;
            return Lowerer.Lower(result);
        }
    }
}
