using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using LanguageCore.CodeAnalysis.Binding;
using LanguageCore.CodeAnalysis.Symbols;
using LanguageCore.CodeAnalysis.Syntax;
using Utilities;

namespace LanguageCore.CodeAnalysis
{
    public sealed class Compilation
    {
        public Compilation Previous { get; }

        public IReadOnlyList<SyntaxTree> SyntaxTrees { get; }

        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (globalScope == null)
                {
                    var newGlobalScope = Binder.BindGlobalScope(Previous?.GlobalScope, SyntaxTrees);
                    Interlocked.CompareExchange(ref globalScope, newGlobalScope, null);
                }

                return globalScope;
            }
        }

        private BoundGlobalScope globalScope;

        public Compilation(params SyntaxTree[] syntaxTrees)
            : this(null, syntaxTrees)
        {
        }

        private Compilation(Compilation previous, params SyntaxTree[] syntaxTrees)
        {
            Previous = previous;
            SyntaxTrees = syntaxTrees;
        }

        public Compilation ContinueWith(SyntaxTree syntaxTree)
        {
            return new Compilation(this, syntaxTree);
        }

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
        {
            var parseDiagnostics = SyntaxTrees.SelectMany(st => st.Diagnostics);

            var diagnostics = parseDiagnostics.Concat(GlobalScope.Diagnostics).ToArray();
            if (diagnostics.Any())
            {
                return new EvaluationResult(diagnostics, null, null, TypeSymbol.Error);
            }

            var program = Binder.BindProgram(GlobalScope);

            SaveControlFlowGraph(program);

            if (program.Diagnostics.Any())
            {
                return new EvaluationResult(program.Diagnostics.ToArray(), null, null, TypeSymbol.Error);
            }

            var evaluator = new Evaluator(program, variables);
            var value = evaluator.Evaluate(out var type);
            var name = GetEvaluationVariableName(program.Statement.Statements.LastOrDefault());
            return new EvaluationResult(Array.Empty<Diagnostic>(), value, name, type);
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

        private static void SaveControlFlowGraph(BoundProgram program)
        {
            // This causes problems with multiple threads writing to a single file.
            // Also not needed in unit tests anyway.
            if (UnitTestDetector.IsRunningFromNUnit)
            {
                return;
            }

            var appPath = Environment.GetCommandLineArgs()[0];
            var appDirectory = Path.GetDirectoryName(appPath) ?? string.Empty;
            var cfgPath = Path.Combine(appDirectory, "cfg.dot");
            var cfgStatement = !program.Statement.Statements.Any() && program.Functions.Any()
                ? program.Functions.Last().Value
                : program.Statement;

            var cfg = ControlFlowGraph.Create(cfgStatement);
            using (var streamWriter = new StreamWriter(cfgPath))
            {
                cfg.WriteTo(streamWriter);
            }
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
    }
}
