using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using LanguageCore.CodeAnalysis.Binding;
using LanguageCore.CodeAnalysis.Emit;
using LanguageCore.CodeAnalysis.Symbols;
using LanguageCore.CodeAnalysis.Syntax;

namespace LanguageCore.CodeAnalysis
{
    public sealed class Compilation
    {
        public bool IsScript { get; }
        public Compilation? Previous { get; }

        public IReadOnlyList<SyntaxTree> SyntaxTrees { get; }
        public FunctionSymbol? MainFunction => GlobalScope.MainFunction;

        public IReadOnlyList<FunctionSymbol> Functions => GlobalScope.Functions;
        public IReadOnlyList<VariableSymbol> Variables => GlobalScope.Variables;

        internal BoundGlobalScope GlobalScope
        {
            get
            {
                if (globalScope == null)
                {
                    var newGlobalScope = Binder.BindGlobalScope(IsScript, Previous?.GlobalScope, SyntaxTrees);
                    Interlocked.CompareExchange(ref globalScope, newGlobalScope, null);
                }

                return globalScope;
            }
        }

        private BoundGlobalScope? globalScope;

        private Compilation(bool isScript, Compilation? previous, params SyntaxTree[] syntaxTrees)
        {
            IsScript = isScript;
            Previous = previous;
            SyntaxTrees = syntaxTrees;
        }

        public static Compilation Create(params SyntaxTree[] syntaxTrees)
        {
            return new Compilation(false, null, syntaxTrees);
        }

        public static Compilation CreateScript(Compilation? previous, params SyntaxTree[] syntaxTrees)
        {
            return new Compilation(true, previous, syntaxTrees);
        }

        public IEnumerable<Symbol> GetSymbols()
        {
            var submission = this;
            var seenSymbolNames = new HashSet<string>();

            var builtinFunctions = BuiltinFunctions.GetAll().ToArray();
            while (submission != null)
            {
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

                foreach (var builtin in builtinFunctions)
                {
                    if (seenSymbolNames.Add(builtin.Name))
                    {
                        yield return builtin;
                    }
                }

                submission = submission.Previous;
            }
        }

        private BoundProgram GetProgram()
        {
            var previous = Previous?.GetProgram();
            return Binder.BindProgram(IsScript, previous, GlobalScope);
        }

        public EvaluationResult Evaluate(Dictionary<VariableSymbol, object> variables)
        {
            if (GlobalScope.Diagnostics.HasErrors())
            {
                return new EvaluationResult(GlobalScope.Diagnostics, null, null, TypeSymbol.Error);
            }

            var program = GetProgram();

            // SaveControlFlowGraph(program);

            if (program.Diagnostics.HasErrors())
            {
                return new EvaluationResult(program.Diagnostics.ToArray(), null, null, TypeSymbol.Error);
            }

            var evaluator = new Evaluator(program, variables);
            var value = evaluator.Evaluate(out var type);
            return new EvaluationResult(program.Diagnostics, value, "_", type);
        }

        public void EmitTree(TextWriter writer)
        {
            if (GlobalScope.MainFunction != null)
            {
                EmitTree(GlobalScope.MainFunction, writer);
            }
            else if (GlobalScope.ScriptFunction != null)
            {
                EmitTree(GlobalScope.ScriptFunction, writer);
            }
        }

        public void EmitTree(FunctionSymbol symbol, TextWriter writer)
        {
            var program = GetProgram();
            symbol.WriteTo(writer);
            writer.WriteLine();

            if (!program.Functions.TryGetValue(symbol, out var body))
            {
                return;
            }

            body.WriteTo(writer);
        }

        public IReadOnlyList<Diagnostic> Emit(string moduleName, IReadOnlyList<string> references, string outputPath)
        {
            // TODO: References should be part of the compilation, not arguments for Emit
            var parseDiagnostics = SyntaxTrees.SelectMany(st => st.Diagnostics);

            var diagnostics = parseDiagnostics.Concat(GlobalScope.Diagnostics).ToArray();
            if (diagnostics.HasErrors())
            {
                return diagnostics;
            }

            var program = GetProgram();

            if (program.Diagnostics.HasErrors())
            {
                return program.Diagnostics;
            }

            return Emitter.Emit(program, moduleName, references, outputPath);
        }

#if false
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
#endif
    }
}
