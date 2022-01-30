using System;
using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis;
using LanguageCore.CodeAnalysis.Syntax;
using Utilities;

namespace Repl
{
    public class ReadEvalPrintLoop
    {
        private bool showParseTrees;
        private readonly Dictionary<VariableSymbol, object> variables = new Dictionary<VariableSymbol, object>();

        public void Start()
        {
            Console.WriteLine($"Welcome to {LanguageInfo.Name}, v{LanguageInfo.ShortVersion}");
            Console.WriteLine("You can evaluate Niobium expressions and more (type :help or :? to get help).");

            for (var i = 1; ProcessCommand(out var evalResult, i); ++i)
            {
                if (evalResult != null)
                {
                    Console.WriteLine(evalResult);
                }
            }
        }

        private bool ProcessCommand(out string evalResult, int commandNumber)
        {
            Console.Write($"{commandNumber,3}> ");
            var sourceLine = Console.ReadLine();
            evalResult = null;

            if (string.IsNullOrEmpty(sourceLine))
            {
                return string.Empty == sourceLine;
            }

            if (sourceLine.First() != ':')
            {
                var syntaxTree = SyntaxTree.Parse(sourceLine);
                var compilation = new Compilation(syntaxTree);
                var result = compilation.Evaluate(variables);

                if (showParseTrees)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    PrettyPrint(syntaxTree.Root);
                    Console.ResetColor();
                }

                if (!result.Diagnostics.Any())
                {
                    Console.WriteLine(result);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;

                    foreach (var diagnostic in result.Diagnostics)
                    {
                        Console.WriteLine();

                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write("ERROR");
                        Console.ResetColor();
                        Console.WriteLine(": " + diagnostic);
                        Console.ForegroundColor = ConsoleColor.DarkCyan;

                        var prefix = sourceLine.Substring(0, diagnostic.Span.Start);
                        var error = sourceLine.Substring(diagnostic.Span.Start, diagnostic.Span.Length);

                        Console.WriteLine(new string(' ', 4) + sourceLine);
                        Console.Write(new string(' ', 4 + prefix.Length));

                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine(new string('~', error.Length));
                        Console.ResetColor();
                    }

                    Console.WriteLine();
                }

                return true;
            }

            switch (sourceLine)
            {
                case ":help":
                case ":?":
                    Console.WriteLine(
                        @"Commands available in Niobium REPL:
   <niobium-expression>         Evaluate Niobium expression
   :help, :?                    Show this message with the list of commands
   :show-parse-tree             Toggle showing parse tree of last expression
   :quit                        Exit the REPL");
                    break;
                case ":show-parse-tree":
                    showParseTrees = !showParseTrees;
                    Console.WriteLine(showParseTrees ? "Showing parse trees." : "Not showing parse trees");
                    break;
                case ":quit":
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
            {
                PrettyPrint(child, indent, child == lastChild);
            }
        }
    }
}
