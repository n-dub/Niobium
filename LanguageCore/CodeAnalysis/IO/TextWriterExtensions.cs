﻿using System;
using System.CodeDom.Compiler;
using System.IO;

namespace LanguageCore.CodeAnalysis.IO
{
    internal static class TextWriterExtensions
    {
        public static bool IsConsoleOut(this TextWriter writer)
        {
            if (writer == Console.Out)
            {
                return true;
            }

            return writer is IndentedTextWriter iw && iw.InnerWriter.IsConsoleOut();
        }

        public static void SetForeground(this TextWriter writer, ConsoleColor color)
        {
            if (writer.IsConsoleOut())
            {
                Console.ForegroundColor = color;
            }
        }

        public static void ResetColor(this TextWriter writer)
        {
            if (writer.IsConsoleOut())
            {
                Console.ResetColor();
            }
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

        public static void WritePunctuation(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.White);
            writer.Write(text);
            writer.ResetColor();
        }
    }
}
