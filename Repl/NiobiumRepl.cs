using System;
using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis;
using LanguageCore.CodeAnalysis.Symbols;
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
                Console.ForegroundColor = GetConsoleColor(token.Kind, token.Text);
                Console.Write(token.Text);
                Console.ResetColor();
            }
        }

        private static ConsoleColor GetConsoleColor(SyntaxKind tokenKind, string text)
        {
            switch (tokenKind)
            {
                case SyntaxKind.NumberToken:
                    return ConsoleColor.DarkCyan;
                case SyntaxKind.StringToken:
                    return ConsoleColor.Yellow;
                case SyntaxKind.FalseKeyword:
                case SyntaxKind.TrueKeyword:
                    return ConsoleColor.Blue;
                case SyntaxKind.IdentifierToken:
                    return TypeSymbol.TryParse(text, out _)
                        ? ConsoleColor.DarkCyan
                        : Console.ForegroundColor;
                default:
                    return tokenKind.IsKeyword() ? ConsoleColor.Magenta : Console.ForegroundColor;
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

            return !syntaxTree.Root.Members.Last().GetLastToken().IsMissing;
        }

        protected override void EvaluateSubmission(string text)
        {
            var syntaxTree = SyntaxTree.Parse(text);

            var compilation = previous?.ContinueWith(syntaxTree) ?? new Compilation(syntaxTree);

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
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;

                foreach (var diagnostic in result.Diagnostics.OrderBy(x => x.Span.Start))
                {
                    var lineIndex = syntaxTree.SourceText.GetLineIndex(diagnostic.Span.Start);
                    var line = syntaxTree.SourceText.Lines[lineIndex];
                    var lineNumber = lineIndex + 1;
                    var character = diagnostic.Span.Start - line.Start + 1;

                    Console.WriteLine();

                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"<repl>:{lineNumber}:{character}: error: " + diagnostic);
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                    var prefixSpan = TextSpan.FromBounds(line.Start, diagnostic.Span.Start);

                    var prefix = syntaxTree.SourceText.ToString(prefixSpan);
                    var error = syntaxTree.SourceText.ToString(diagnostic.Span);

                    Console.Write($"{lineNumber,4} | ");
                    Console.ResetColor();
                    RenderLine(line.ToString());
                    Console.WriteLine();
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
