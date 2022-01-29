using System;
using System.Linq;
using LanguageCore.CodeAnalysis;
using LanguageCore.CodeAnalysis.Binding;
using LanguageCore.CodeAnalysis.Syntax;
using Utilities;

namespace Repl
{
    public class ReadEvalPrintLoop
    {
        private bool showParseTrees;

        public void Start()
        {
            Console.WriteLine($"Welcome to {LanguageInfo.Name}, v{LanguageInfo.ShortVersion}");
            Console.WriteLine("You can evaluate Niobium expressions and more (type :help or :? to get help).");

            for (var i = 1; ProcessCommand(out var evalResult, i); ++i)
                if (evalResult != null)
                    Console.WriteLine(evalResult);
        }

        private bool ProcessCommand(out string evalResult, int commandNumber)
        {
            Console.Write($"{commandNumber,3}>> ");
            var sourceLine = Console.ReadLine();
            evalResult = null;

            if (string.IsNullOrEmpty(sourceLine)) return string.Empty == sourceLine;

            if (sourceLine.First() != ':')
            {
                var syntaxTree = SyntaxTree.Parse(sourceLine);
                var binder = new Binder();
                var boundExpression = binder.BindExpression(syntaxTree.Root);
                var diagnostics = binder.Diagnostics.Concat(syntaxTree.Diagnostics).ToArray();

                if (showParseTrees)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    PrettyPrint(syntaxTree.Root);
                    Console.ResetColor();
                }

                if (!diagnostics.Any())
                {
                    var e = new Evaluator(boundExpression);
                    var result = e.Evaluate();
                    Console.WriteLine(result);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;

                    foreach (var diagnostic in diagnostics) Console.WriteLine(diagnostic);

                    Console.ResetColor();
                }

                return true;
            }

            switch (sourceLine)
            {
                case ":help":
                case ":?":
                    Console.WriteLine("Useful help message about REPL commands...");
                    break;
                case ":show-parse-tree":
                    showParseTrees = !showParseTrees;
                    Console.WriteLine(showParseTrees ? "Showing parse trees." : "Not showing parse trees");
                    break;
                case ":exit":
                    return false;
                default:
                    Console.WriteLine($"Invalid REPL command: {sourceLine}");
                    break;
            }

            return true;
        }

        private static void PrettyPrint(SyntaxNode node, string indent = "", bool isLast = true)
        {
            var marker = isLast ? "└──" : "├──";

            Console.Write(indent);
            Console.Write(marker);
            Console.Write(node.Kind);

            if (node is SyntaxToken t && t.Value != null)
            {
                Console.Write(" ");
                Console.Write(t.Value);
            }

            Console.WriteLine();

            indent += isLast ? "   " : "│   ";

            var lastChild = node.GetChildren().LastOrDefault();

            foreach (var child in node.GetChildren())
                PrettyPrint(child, indent, child == lastChild);
        }
    }
}
