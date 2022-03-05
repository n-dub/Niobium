using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanguageCore.CodeAnalysis;
using LanguageCore.CodeAnalysis.IO;
using LanguageCore.CodeAnalysis.Symbols;
using LanguageCore.CodeAnalysis.Syntax;
using Repl;
using Utilities;

namespace Niobium
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            if (!args.Any())
            {
                Console.WriteLine($"Welcome to {LanguageInfo.Name}, v{LanguageInfo.ShortVersion}");
                Console.WriteLine("You can evaluate Niobium expressions and more (type :help to get help).");
                var repl = new NiobiumRepl();
                repl.Start();
            }
            else
            {
                switch (args.First())
                {
                    case "--version":
                        Console.WriteLine(
                            $"{LanguageInfo.Name}, version {LanguageInfo.Name.ToLower()}-{LanguageInfo.FullVersion}");
                        Console.WriteLine(LanguageInfo.Description);
                        Console.WriteLine(LanguageInfo.Copyright);
                        break;
                    case "--help":
                        Console.WriteLine($"{LanguageInfo.Name}, v{LanguageInfo.ShortVersion}");
                        Console.WriteLine(
                            "Run this tool without arguments to enter Niobium REPL - Read Eval Print Loop");
                        break;
                    default:
                        return RunCompilation(args);
                }
            }

            return 0;
        }

        private static int RunCompilation(IEnumerable<string> arguments)
        {
            var syntaxTrees = new List<SyntaxTree>();
            var hasErrors = false;

            foreach (var path in GetFilePaths(arguments))
            {
                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"error: File '{path}' doesn't exist");
                    hasErrors = true;
                    continue;
                }

                syntaxTrees.Add(SyntaxTree.Load(path));
            }

            if (hasErrors)
            {
                return 1;
            }

            var compilation = Compilation.Create(syntaxTrees.ToArray());
            var result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());

            if (!result.Diagnostics.Any())
            {
                if (result.Value != null)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Last computed value: {result.Value}");
                }
            }
            else
            {
                Console.Error.WriteDiagnostics(result.Diagnostics);
            }

            return 0;
        }

        private static IEnumerable<string> GetFilePaths(IEnumerable<string> paths)
        {
            var result = new SortedSet<string>();

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    result.UnionWith(Directory.EnumerateFiles(path, "*.nb", SearchOption.AllDirectories));
                }
                else
                {
                    result.Add(path);
                }
            }

            return result;
        }
    }
}
