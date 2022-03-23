using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanguageCore.CodeAnalysis;
using LanguageCore.CodeAnalysis.IO;
using LanguageCore.CodeAnalysis.Syntax;
using Mono.Options;
using Repl;
using Utilities;

namespace Niobium
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            string? outputPath = null;
            string? moduleName = null;
            var referencePaths = new List<string>();
            var sourcePaths = new List<string>();
            var helpRequested = false;
            var versionRequested = false;

            var options = new OptionSet
            {
                $"usage: {LanguageInfo.Name} <source files> [options]\n" +
                "Run this tool without arguments to enter Niobium REPL",
                {"r=", "The {path} of an assembly to reference", referencePaths.Add},
                {"o=", "The output {path} of the assembly to create", v => outputPath = v},
                {"m=", "The {name} of the module", v => moduleName = v},
                {"h|help", "Prints this help message", _ => helpRequested = true},
                {"v|version", "Prints compiler's version", _ => versionRequested = true},
                {"<>", sourcePaths.Add}
            };

            options.Parse(args);

            if (helpRequested)
            {
                options.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            if (versionRequested)
            {
                Console.WriteLine(
                    $"{LanguageInfo.Name}, version {LanguageInfo.Name.ToLower()}-{LanguageInfo.FullVersion}");
                Console.WriteLine(LanguageInfo.Description);
                Console.WriteLine(LanguageInfo.Copyright);
                return 0;
            }

            if (!args.Any())
            {
                Console.WriteLine($"Welcome to {LanguageInfo.Name}, v{LanguageInfo.ShortVersion}");
                Console.WriteLine("You can evaluate Niobium expressions and more (type :help or :? to get help).");
                var repl = new NiobiumRepl();
                repl.Start();
            }
            else
            {
                return RunCompilation(sourcePaths, referencePaths, moduleName, outputPath);
            }

            return 0;
        }

        private static int RunCompilation(IReadOnlyList<string> sourcePaths, IReadOnlyList<string> referencePaths,
            string? moduleName, string? outputPath)
        {
            var syntaxTrees = new List<SyntaxTree>();

            if (!sourcePaths.Any())
            {
                Console.Error.Write("error: at least one source file is needed");
                return 1;
            }

            outputPath ??= sourcePaths.Count != 1
                ? Path.Combine(Path.GetDirectoryName(sourcePaths[0]) ?? ".", "a.exe")
                : Path.ChangeExtension(sourcePaths[0], ".exe");

            moduleName ??= Path.GetFileNameWithoutExtension(outputPath);

            var hasErrors = false;
            foreach (var sourcePath in sourcePaths)
            {
                if (!File.Exists(sourcePath))
                {
                    Console.Error.WriteLine($"error: file '{sourcePath}' doesn't exist");
                    hasErrors = true;
                    continue;
                }

                syntaxTrees.Add(SyntaxTree.Load(sourcePath));
            }

            foreach (var referencePath in referencePaths)
            {
                if (!File.Exists(referencePath))
                {
                    Console.Error.WriteLine($"error: file '{referencePath}' doesn't exist");
                    hasErrors = true;
                }
            }

            if (hasErrors)
            {
                return 1;
            }

            var compilation = Compilation.Create(syntaxTrees.ToArray());
            var diagnostics = compilation.Emit(moduleName, referencePaths, outputPath);

            if (diagnostics.Any())
            {
                Console.Error.WriteDiagnostics(diagnostics.Where(x => !x.Expired));
                return diagnostics.HasErrors() ? 1 : 0;
            }

            return 0;
        }
    }
}
