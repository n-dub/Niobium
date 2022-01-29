﻿using System;
using System.Linq;
using Repl;
using Utilities;

namespace Niobium
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (!args.Any())
            {
                var repl = new ReadEvalPrintLoop();
                repl.Start();
            }
            else switch (args.First())
            {
                case "--version":
                    Console.WriteLine($"{LanguageInfo.Name}, v{LanguageInfo.Version}");
                    Console.WriteLine(LanguageInfo.Description);
                    Console.WriteLine(LanguageInfo.Copyright);
                    break;
                case "--help":
                    Console.WriteLine($"{LanguageInfo.Name}, v{LanguageInfo.Version}");
                    Console.WriteLine("Run this tool without arguments to enter Niobium REPL - Read Eval Print Loop");
                    break;
            }
        }
    }
}
