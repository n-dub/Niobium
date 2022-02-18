using System;
using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis;
using LanguageCore.CodeAnalysis.Syntax;
using LanguageCore.CodeAnalysis.Text;

namespace Repl
{
    public sealed class NiobiumRepl : Repl
    {
        private Compilation previous;
        private bool showParseTree;
        private bool showBoundTree;
        private readonly Dictionary<VariableSymbol, object> variables = new Dictionary<VariableSymbol, object>();

        protected override void RenderLine(string line)
        {
            var tokens = SyntaxTree.ParseTokens(line);
            foreach (var token in tokens)
            {
                var isKeyword = token.Kind.IsKeyword();
                var isNumber = token.Kind == SyntaxKind.NumberToken;

                if (isKeyword)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                }
                else if (!isNumber)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                }

                Console.Write(token.Text);

                Console.ResetColor();
            }
        }

        protected override void EvaluateMetaCommand(string input)
        {
            switch (input)
            {
                case ":help":
                case ":?":
                    Console.WriteLine(
                        @"In Niobium REPL you can use:
Enter to evaluate an expression, Ctrl+Enter to break the line.
Arrows (↑ and ↓) to navigate within a multi-line submission.
PageUp and PageDown to navigate through submission history.
Home and End to move cursor to the start and to the end of the current line respectively.
Esc to clear the current line.

Meta-commands available:
   :help, :?                    Show this message with the list of commands
   :show-parse-tree             Toggle showing parse tree of last expression
   :quit                        Exit the REPL
   :clear                       Clear console
   :reset                       Clear all previously declared variables");
                    break;
                case ":show-parse-tree":
                    showParseTree = !showParseTree;
                    Console.WriteLine(showParseTree ? "Showing parse trees." : "Not showing parse trees.");
                    break;
                case ":show-bound-tree":
                    showBoundTree = !showBoundTree;
                    Console.WriteLine(showBoundTree ? "Showing bound trees." : "Not showing bound trees.");
                    break;
                case ":clear":
                    Console.Clear();
                    break;
                case ":reset":
                    previous = null;
                    variables.Clear();
                    break;
                default:
                    base.EvaluateMetaCommand(input);
                    break;
            }
        }

        protected override bool IsCompleteSubmission(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }

            var syntaxTree = SyntaxTree.Parse(text);

            return syntaxTree.Diagnostics.Count == 0;
        }

        protected override void EvaluateSubmission(string text)
        {
            var syntaxTree = SyntaxTree.Parse(text);

            var compilation = previous == null
                ? new Compilation(syntaxTree)
                : previous.ContinueWith(syntaxTree);

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
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(result);
                Console.ResetColor();
                previous = compilation;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;

                foreach (var diagnostic in result.Diagnostics)
                {
                    var lineIndex = syntaxTree.SourceText.GetLineIndex(diagnostic.Span.Start);
                    var line = syntaxTree.SourceText.Lines[lineIndex];
                    var lineNumber = lineIndex + 1;
                    var character = diagnostic.Span.Start - line.Start + 1;

                    Console.WriteLine();

                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"<repl>:{lineNumber}:{character}: error: " + diagnostic);
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.DarkCyan;

                    var prefixSpan = TextSpan.FromBounds(line.Start, diagnostic.Span.Start);

                    var prefix = syntaxTree.SourceText.ToString(prefixSpan);
                    var error = syntaxTree.SourceText.ToString(diagnostic.Span);

                    Console.WriteLine($"{lineNumber,4} | {line}");
                    Console.Write(new string(' ', 7 + prefix.Length));

                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("^" + new string('~', Math.Max(0, error.Length - 1)));
                    Console.ResetColor();
                }

                Console.WriteLine();
            }
        }
    }
}
