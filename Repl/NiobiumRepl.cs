using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanguageCore.CodeAnalysis;
using LanguageCore.CodeAnalysis.IO;
using LanguageCore.CodeAnalysis.Symbols;
using LanguageCore.CodeAnalysis.Syntax;
using Repl.Authoring;

namespace Repl
{
    public sealed class NiobiumRepl : Repl
    {
        private static readonly Compilation emptyCompilation = Compilation.CreateScript(null);

        private bool loadingSubmission;
        private Compilation previous;
        private bool showParseTree;
        private bool showBoundTree;
        private readonly Dictionary<VariableSymbol, object> variables = new Dictionary<VariableSymbol, object>();

        public NiobiumRepl()
        {
            LoadSubmissions();
        }

        protected override object RenderLine(IReadOnlyList<string> lines, int lineIndex, object state)
        {
            SyntaxTree syntaxTree;

            if (state == null)
            {
                var text = string.Join(Environment.NewLine, lines);
                syntaxTree = SyntaxTree.Parse(text);
            }
            else
            {
                syntaxTree = (SyntaxTree) state;
            }

            // TODO: malformed expressions must be included in the SyntaxTree
            if (lines.Count == 1 && lines[0].FirstOrDefault() == ':')
            {
                return base.RenderLine(lines, lineIndex, state);
            }

            var lineSpan = syntaxTree.SourceText.Lines[lineIndex].Span;
            var classifiedSpans = Classifier.Classify(syntaxTree, lineSpan);

            foreach (var classifiedSpan in classifiedSpans)
            {
                var classifiedText = syntaxTree.SourceText.ToString(classifiedSpan.Span);

                switch (classifiedSpan.Classification)
                {
                    case Classification.Keyword:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        break;
                    case Classification.LiteralKeyword:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case Classification.Number:
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        break;
                    case Classification.String:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case Classification.Comment:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }

                Console.Write(classifiedText);
                Console.ResetColor();
            }

            return syntaxTree;
        }

        // ReSharper disable once UnusedMember.Local
        [MetaCommand(new[] {"reset"}, "Clear all previous submissions")]
        private void EvaluateReset()
        {
            previous = null;
            variables.Clear();
            ClearSubmissions();
        }

        // ReSharper disable once UnusedMember.Local
        [MetaCommand(new[] {"clear"}, "Clear console")]
        private void EvaluateClear()
        {
            Console.Clear();
        }

        // ReSharper disable once UnusedMember.Local
        [MetaCommand(new[] {"show-bound-tree"}, "Toggle showing bound tree of last expression")]
        private void EvaluateShowBoundTree()
        {
            showBoundTree = !showBoundTree;
            Console.WriteLine(showBoundTree ? "Showing bound trees." : "Not showing bound trees.");
        }

        // ReSharper disable once UnusedMember.Local
        [MetaCommand(new[] {"show-parse-tree"}, "Toggle showing parse tree of last expression")]
        private void EvaluateShowParseTree()
        {
            showParseTree = !showParseTree;
            Console.WriteLine(showParseTree ? "Showing parse trees." : "Not showing parse trees.");
        }

        // ReSharper disable once UnusedMember.Local
        [MetaCommand(new[] {"load"}, "Load a niobium source file")]
        private void EvaluateLoad(string path)
        {
            path = Path.GetFullPath(path);

            if (!File.Exists(path))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"error: file does not exist '{path}'");
                Console.ResetColor();
                return;
            }

            var text = File.ReadAllText(path);
            EvaluateSubmission(text);
        }

        // ReSharper disable once UnusedMember.Local
        [MetaCommand(new[] {"browse"}, "Show all declared symbols")]
        private void EvaluateBrowse()
        {
            var compilation = previous ?? emptyCompilation;

            var symbols = compilation
                .GetSymbols()
                .OrderBy(s => s.Kind)
                .ThenBy(s => s.Name);

            foreach (var symbol in symbols)
            {
                symbol.WriteTo(Console.Out);
                Console.WriteLine();
            }
        }

        // ReSharper disable once UnusedMember.Local
        [MetaCommand(new[] {"dump"}, "Show bound tree of a given function")]
        private void EvaluateDump(string functionName)
        {
            var compilation = previous ?? emptyCompilation;

            var symbol = compilation.GetSymbols().OfType<FunctionSymbol>().SingleOrDefault(f => f.Name == functionName);
            if (symbol == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"error: function '{functionName}' does not exist");
                Console.ResetColor();
                return;
            }

            compilation.EmitTree(symbol, Console.Out);
        }

        protected override bool IsCompleteSubmission(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }

            var lastTwoLinesAreBlank = text.Split(new[] {Environment.NewLine}, StringSplitOptions.None)
                .Reverse()
                .TakeWhile(string.IsNullOrEmpty)
                .Take(2)
                .Count() == 2;

            if (lastTwoLinesAreBlank)
            {
                return true;
            }

            var syntaxTree = SyntaxTree.Parse(text);
            var lastMember = syntaxTree.Root.Members.LastOrDefault();

            return lastMember != null && !lastMember.GetLastToken().IsMissing;
        }

        protected override void EvaluateSubmission(string text)
        {
            var syntaxTree = SyntaxTree.Parse(text);

            var compilation = Compilation.CreateScript(previous, syntaxTree);

            if (showParseTree)
            {
                syntaxTree.Root.WriteTo(Console.Out);
            }

            if (showBoundTree)
            {
                compilation.EmitTree(Console.Out);
            }

            var result = compilation.Evaluate(variables);

            if (!result.Diagnostics.Any())
            {
                if (result.Value != null)
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine(result);
                    Console.ResetColor();
                }

                previous = compilation;

                SaveSubmission(text);
            }
            else
            {
                Console.Out.WriteDiagnostics(result.Diagnostics);
            }
        }

        private static string GetSubmissionsDirectory()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var submissionsDirectory = Path.Combine(localAppData, "Niobium", "Submissions");
            return submissionsDirectory;
        }

        private void LoadSubmissions()
        {
            var submissionsDirectory = GetSubmissionsDirectory();
            if (!Directory.Exists(submissionsDirectory))
            {
                return;
            }

            var files = Directory.GetFiles(submissionsDirectory).OrderBy(f => f).ToArray();
            if (files.Length == 0)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Loaded {files.Length} submission(s)");
            Console.ResetColor();

            loadingSubmission = true;

            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                EvaluateSubmission(text);
            }

            loadingSubmission = false;
        }

        private static void ClearSubmissions()
        {
            var dir = GetSubmissionsDirectory();
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }

        private void SaveSubmission(string text)
        {
            if (loadingSubmission)
            {
                return;
            }

            var submissionsDirectory = GetSubmissionsDirectory();
            Directory.CreateDirectory(submissionsDirectory);
            var count = Directory.GetFiles(submissionsDirectory).Length;
            var name = $"submission{count:0000}";
            var fileName = Path.Combine(submissionsDirectory, name);
            File.WriteAllText(fileName, text);
        }
    }
}
