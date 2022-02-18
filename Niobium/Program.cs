using System;
using System.Linq;
using Repl;
using Utilities;

namespace Niobium
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            if (!args.Any())
            {
                Console.WriteLine($"Welcome to {LanguageInfo.Name}, v{LanguageInfo.ShortVersion}");
                Console.WriteLine("You can evaluate Niobium expressions and more (type :help or :? to get help).");
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
                }
            }
        }
    }
}
