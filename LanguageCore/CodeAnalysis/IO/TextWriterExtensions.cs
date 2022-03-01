using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanguageCore.CodeAnalysis.Syntax;
using LanguageCore.CodeAnalysis.Text;

namespace LanguageCore.CodeAnalysis.IO
{
    public static class TextWriterExtensions
    {
        public static void WriteDiagnostics(this TextWriter writer, IEnumerable<Diagnostic> diagnostics)
        {
            foreach (var diagnostic in diagnostics.OrderBy(x => x.Location.Span.Start)
                .ThenBy(x => x.Location.Span.Length))
            {
                var text = diagnostic.Location.Text;
                var fileName = diagnostic.Location.FileName;
                var startLine = diagnostic.Location.StartLine + 1;
                var startCharacter = diagnostic.Location.StartCharacter + 1;
                var endLine = diagnostic.Location.EndLine + 1;
                var endCharacter = diagnostic.Location.EndCharacter + 1;

                var span = diagnostic.Location.Span;
                var lineIndex = text.GetLineIndex(span.Start);
                var line = text.Lines[lineIndex];

                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"{fileName}:{startLine}:{startCharacter}:{endLine}:{endCharacter}: error: " +
                                  diagnostic);
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkGray;

                var prefixSpan = TextSpan.FromBounds(line.Start, span.Start);

                var prefix = text.ToString(prefixSpan);
                var error = text.ToString(span);

                Console.Write($"{startLine,4} | ");
                Console.ResetColor();
                Console.WriteLine(line);
                Console.Write(new string(' ', 7 + prefix.Length));

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("^" + new string('~', Math.Max(0, error.Length - 1)));
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        public static void WriteKeyword(this TextWriter writer, SyntaxKind kind)
        {
            writer.WriteKeyword(SyntaxFacts.GetText(kind));
        }

        public static void WriteKeyword(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.Magenta);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteIdentifier(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.White);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteNumber(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.Cyan);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteString(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.Yellow);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteSpace(this TextWriter writer)
        {
            writer.WritePunctuation(" ");
        }

        public static void WritePunctuation(this TextWriter writer, SyntaxKind kind)
        {
            writer.WritePunctuation(SyntaxFacts.GetText(kind));
        }

        public static void WritePunctuation(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.White);
            writer.Write(text);
            writer.ResetColor();
        }

        private static bool IsConsoleOut(this TextWriter writer)
        {
            if (writer == Console.Out)
            {
                return true;
            }

            return writer is IndentedTextWriter iw && iw.InnerWriter.IsConsoleOut();
        }

        private static void SetForeground(this TextWriter writer, ConsoleColor color)
        {
            if (writer.IsConsoleOut())
            {
                Console.ForegroundColor = color;
            }
        }

        private static void ResetColor(this TextWriter writer)
        {
            if (writer.IsConsoleOut())
            {
                Console.ResetColor();
            }
        }
    }
}
