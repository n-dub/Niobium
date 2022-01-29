using System;
using System.Linq;
using Utilities;

namespace Repl
{
    public class ReadEvalPrintLoop
    {
        public void Start()
        {
            Console.WriteLine($"Welcome to {LanguageInfo.Name}, version {LanguageInfo.Version}");
            Console.WriteLine("You can evaluate Niobium expressions and more (type :help or :? to get help).");

            for (var i = 1; ProcessCommand(out var evalResult, i); ++i)
            {
                if (evalResult != null)
                {
                    Console.WriteLine(evalResult);
                }
            }
        }

        private static bool ProcessCommand(out string evalResult, int commandNumber)
        {
            Console.Write($"{commandNumber,3}>> ");
            var sourceLine = Console.ReadLine();
            evalResult = null;

            if (string.IsNullOrEmpty(sourceLine))
            {
                return string.Empty == sourceLine;
            }

            if (sourceLine.First() != ':')
            {
                evalResult = sourceLine;
                return true;
            }

            switch (sourceLine)
            {
                case ":help":
                case ":?":
                    Console.WriteLine("Useful help message about REPL commands...");
                    break;
                case ":exit":
                    return false;
            }

            return true;
        }
    }
}
