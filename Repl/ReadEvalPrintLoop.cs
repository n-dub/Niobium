﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LanguageCore.CodeAnalysis;
using LanguageCore.CodeAnalysis.Syntax;
using LanguageCore.CodeAnalysis.Text;
using Utilities;

namespace Repl
{
    public class ReadEvalPrintLoop
    {
        private bool showParseTrees;
        private readonly Dictionary<VariableSymbol, object> variables = new Dictionary<VariableSymbol, object>();
        private readonly StringBuilder textBuilder = new StringBuilder();

        public void Start()
        {
            Console.WriteLine($"Welcome to {LanguageInfo.Name}, v{LanguageInfo.ShortVersion}");
            Console.WriteLine("You can evaluate Niobium expressions and more (type :help or :? to get help).");

            for (var commandNumber = 1; ProcessCommand(out var evalResult, ref commandNumber);)
            {
                if (evalResult != null)
                {
                    Console.WriteLine(evalResult);
                }
            }
        }

        private bool ProcessCommand(out string evalResult, ref int commandNumber)
        {
            Console.Write($"{commandNumber++,3}{(textBuilder.Length == 0 ? '>' : '.')} ");
            
            var input = Console.ReadLine();
            evalResult = null;

            var isBlank = string.IsNullOrWhiteSpace(input);

            if (input == null)
            {
                return false;
            }

            if (textBuilder.Length == 0 && input.First() == ':')
            {
                return ProcessReplCommand(input);
            }

            textBuilder.AppendLine(input);
            var sourceLine = textBuilder.ToString();
            var syntaxTree = SyntaxTree.Parse(sourceLine);

            if (!isBlank && syntaxTree.Diagnostics.Any())
            {
                return true;
            }
            
            var compilation = new Compilation(syntaxTree);
            var result = compilation.Evaluate(variables);

            if (showParseTrees)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                syntaxTree.Root.WriteTo(Console.Out);
                Console.ResetColor();
            }

            textBuilder.Clear();
            commandNumber = 1;
            if (!result.Diagnostics.Any())
            {
                Console.WriteLine(result);
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
                    Console.WriteLine("^" + new string('~', error.Length - 1));
                    Console.ResetColor();
                }

                Console.WriteLine();
            }

            return true;
        }

        private bool ProcessReplCommand(string sourceLine)
        {
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
                    return true;
                case ":show-parse-tree":
                    showParseTrees = !showParseTrees;
                    Console.WriteLine(showParseTrees ? "Showing parse trees." : "Not showing parse trees");
                    return true;
                case ":quit":
                    return false;
                default:
                    Console.WriteLine($"Invalid REPL command: {sourceLine}");
                    return true;
            }
        }
    }
}
