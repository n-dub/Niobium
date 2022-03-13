using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanguageCore.CodeAnalysis.Symbols;
using LanguageCore.CodeAnalysis.Syntax;
using LanguageCore.CodeAnalysis.Text;

namespace LanguageCore.CodeAnalysis.IO
{
    public static class TextWriterExtensions
    {
        public static void WriteDiagnostics(this TextWriter writer, IEnumerable<Diagnostic> diagnostics)
        {
            var splitDiagnostics = diagnostics
                .GroupBy(x => x.Location.Text is null)
                .ToArray();
            var sourceFileDiagnostics = splitDiagnostics
                .Where(x => !x.Key)
                .SelectMany(x => x).OrderBy(x => x.Location.Span.Start)
                .ThenBy(x => x.Location.Span.Length);
            var otherDiagnostics = splitDiagnostics
                .Where(x => x.Key)
                .SelectMany(x => x);

            foreach (var diagnostic in sourceFileDiagnostics)
            {
                var text = diagnostic.Location.Text;
                var fileName = diagnostic.Location.FileName;
                var startLine = diagnostic.Location.StartLine + 1;
                var startChar = diagnostic.Location.StartCharacter + 1;
                var endLine = diagnostic.Location.EndLine + 1;
                var endChar = diagnostic.Location.EndCharacter + 1;

                var span = diagnostic.Location.Span;
                var lineIndex = text.GetLineIndex(span.Start);
                var line = text.Lines[lineIndex];

                writer.WriteLine();

                writer.SetForeground(ConsoleColor.DarkRed);
                writer.WriteLine($"{fileName}:{startLine}:{startChar}:{endLine}:{endChar}: error: {diagnostic}");
                writer.ResetColor();
                writer.SetForeground(ConsoleColor.DarkGray);

                var prefixSpan = TextSpan.FromBounds(line.Start, span.Start);

                var prefix = text.ToString(prefixSpan);
                var error = text.ToString(span);

                writer.Write($"{startLine,4} | ");
                writer.ResetColor();
                writer.WriteLine(line);
                writer.Write(new string(' ', 7 + prefix.Length));

                writer.SetForeground(ConsoleColor.DarkRed);
                writer.WriteLine("^" + new string('~', Math.Max(0, error.Length - 1)));
                writer.ResetColor();
            }

            foreach (var diagnostic in otherDiagnostics)
            {
                writer.SetForeground(ConsoleColor.DarkRed);
                writer.WriteLine($"error: {diagnostic}");
                writer.ResetColor();
            }

            writer.WriteLine();
        }

        public static void WriteKeyword(this TextWriter writer, SyntaxKind kind)
        {
            writer.WriteKeyword(SyntaxFacts.GetText(kind));
        }

        public static void WriteKeyword(this TextWriter writer, string text)
        {
            var color = TypeSymbol.TryParse(text, out _)
                ? ConsoleColor.DarkCyan
                : ConsoleColor.Magenta;

            writer.SetForeground(color);
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

        private static bool IsConsole(this TextWriter writer)
        {
            if (writer == Console.Out)
            {
                return !Console.IsOutputRedirected;
            }

            if (writer == Console.Error)
            {
                return !Console.IsErrorRedirected && !Console.IsOutputRedirected;
            }

            return writer is IndentedTextWriter iw && iw.InnerWriter.IsConsole();
        }

        private static void SetForeground(this TextWriter writer, ConsoleColor color)
        {
            if (writer.IsConsole())
            {
                Console.ForegroundColor = color;
            }
        }

        private static void ResetColor(this TextWriter writer)
        {
            if (writer.IsConsole())
            {
                Console.ResetColor();
            }
        }
    }
}
