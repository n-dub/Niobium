using System;
using System.Linq;

namespace LanguageCore.CodeAnalysis.Syntax
{
    public static class SyntaxKindExtensions
    {
        private static readonly SyntaxKind[] keywords;
        private static readonly SyntaxKind[] tokens;
        private static readonly SyntaxKind[] trivia;

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

            trivia = Enum.GetValues(typeof(SyntaxKind))
                .Cast<SyntaxKind>()
                .Where(k => k.ToString().EndsWith("Trivia"))
                .ToArray();
        }

        public static bool IsKeyword(this SyntaxKind syntaxKind)
        {
            return keywords.Contains(syntaxKind);
        }

        public static bool IsLiteralKeyword(this SyntaxKind syntaxKind)
        {
            return syntaxKind == SyntaxKind.TrueKeyword ||
                   syntaxKind == SyntaxKind.FalseKeyword;
        }

        public static bool IsNonKeywordToken(this SyntaxKind syntaxKind)
        {
            return tokens.Contains(syntaxKind);
        }

        public static bool IsToken(this SyntaxKind syntaxKind)
        {
            return syntaxKind.IsNonKeywordToken() || syntaxKind.IsKeyword();
        }

        public static bool IsComment(this SyntaxKind syntaxKind)
        {
            return syntaxKind == SyntaxKind.MultiLineCommentTrivia ||
                   syntaxKind == SyntaxKind.SingleLineCommentTrivia;
        }

        public static bool IsAssignmentOperator(this SyntaxKind kind)
        {
            return SyntaxFacts.IsAssignmentOperator(kind);
        }

        public static bool IsTrivia(this SyntaxKind syntaxKind)
        {
            return trivia.Contains(syntaxKind);
        }
    }
}
