using System;
using System.CodeDom.Compiler;
using System.IO;
using LanguageCore.CodeAnalysis.Syntax;

namespace LanguageCore.CodeAnalysis.IO
{
    internal static class TextWriterExtensions
    {
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
