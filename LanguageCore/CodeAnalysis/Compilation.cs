using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using LanguageCore.CodeAnalysis.Binding;
using LanguageCore.CodeAnalysis.Symbols;
using LanguageCore.CodeAnalysis.Syntax;
using Utilities;
using Binder = LanguageCore.CodeAnalysis.Binding.Binder;

namespace LanguageCore.CodeAnalysis
{
    public sealed class Compilation
    {
        public Compilation Previous { get; }

        public IReadOnlyList<SyntaxTree> SyntaxTrees { get; }

        public IReadOnlyList<FunctionSymbol> Functions => GlobalScope.Functions;
        public IReadOnlyList<VariableSymbol> Variables => GlobalScope.Variables;

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

        public IEnumerable<Symbol> GetSymbols()
        {
            var submission = this;
            var seenSymbolNames = new HashSet<string>();

            while (submission != null)
            {
                const BindingFlags bindingFlags = BindingFlags.Static |
                                                  BindingFlags.Public |
                                                  BindingFlags.NonPublic;
                var builtinFunctions = typeof(BuiltinFunctions)
                    .GetFields(bindingFlags)
                    .Where(x => x.FieldType == typeof(FunctionSymbol))
                    .Select(x => x.GetValue(null))
                    .Cast<FunctionSymbol>()
                    .ToList();

                foreach (var builtin in builtinFunctions)
                {
                    if (seenSymbolNames.Add(builtin.Name))
                    {
                        yield return builtin;
                    }
                }

                foreach (var function in submission.Functions)
                {
                    if (seenSymbolNames.Add(function.Name))
                    {
                        yield return function;
                    }
                }

                foreach (var variable in submission.Variables)
                {
                    if (seenSymbolNames.Add(variable.Name))
                    {
                        yield return variable;
                    }
                }

                submission = submission.Previous;
            }
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
                foreach (var functionSymbol in program.Functions.Keys)
                {
                    if (GlobalScope.Functions.Contains(functionSymbol))
                    {
                        EmitTree(functionSymbol, writer);
                    }
                }
            }
        }

        public void EmitTree(FunctionSymbol symbol, TextWriter writer)
        {
            var program = Binder.BindProgram(GlobalScope);
            symbol.WriteTo(writer);
            writer.WriteLine();

            if (!program.Functions.TryGetValue(symbol, out var body))
            {
                return;
            }

            body.WriteTo(writer);
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
