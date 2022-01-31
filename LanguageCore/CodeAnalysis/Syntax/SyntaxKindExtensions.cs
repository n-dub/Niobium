using System;
using System.Linq;

namespace LanguageCore.CodeAnalysis.Syntax
{
    public static class SyntaxKindExtensions
    {
        private static readonly SyntaxKind[] keywords;
        private static readonly SyntaxKind[] tokens;

        static SyntaxKindExtensions()
        {
            keywords = Enum.GetValues(typeof(SyntaxKind))
                .Cast<SyntaxKind>()
                .Where(k => k.ToString().EndsWith("Keyword"))
                .ToArray();

            tokens = Enum.GetValues(typeof(SyntaxKind))
                .Cast<SyntaxKind>()
                .Where(k => k.ToString().EndsWith("Token"))
                .ToArray();
        }

        public static bool IsKeyword(this SyntaxKind syntaxKind)
        {
            return keywords.Contains(syntaxKind);
        }

        public static bool IsToken(this SyntaxKind syntaxKind)
        {
            return tokens.Contains(syntaxKind);
        }
    }
}
