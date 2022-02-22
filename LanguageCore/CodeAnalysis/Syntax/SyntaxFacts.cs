using System;
using System.Collections.Generic;
using System.Linq;

namespace LanguageCore.CodeAnalysis.Syntax
{
    public static class SyntaxFacts
    {
        public static int GetUnaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.BangToken:
                case SyntaxKind.TildeToken:
                    return 6;

                default:
                    return 0;
            }
        }

        public static int GetBinaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.StarToken:
                case SyntaxKind.SlashToken:
                    return 5;

                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return 4;

                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.BangEqualsToken:
                case SyntaxKind.LessToken:
                case SyntaxKind.LessOrEqualsToken:
                case SyntaxKind.GreaterToken:
                case SyntaxKind.GreaterOrEqualsToken:
                    return 3;

                case SyntaxKind.AmpersandToken:
                case SyntaxKind.AmpersandAmpersandToken:
                    return 2;

                case SyntaxKind.PipeToken:
                case SyntaxKind.PipePipeToken:
                case SyntaxKind.HatToken:
                    return 1;

                default:
                    return 0;
            }
        }

        public static SyntaxKind GetKeywordKind(string text)
        {
            // TODO: this is probably too inefficient
            foreach (var keywordKind in GetKeywordKinds())
            {
                if (GetText(keywordKind) == text)
                {
                    return keywordKind;
                }
            }

            return SyntaxKind.IdentifierToken;
        }

        public static IEnumerable<SyntaxKind> GetUnaryOperatorKinds()
        {
            return Enum.GetValues(typeof(SyntaxKind))
                .Cast<SyntaxKind>()
                .Where(k => k.GetUnaryOperatorPrecedence() > 0);
        }

        public static IEnumerable<SyntaxKind> GetBinaryOperatorKinds()
        {
            return Enum.GetValues(typeof(SyntaxKind))
                .Cast<SyntaxKind>()
                .Where(k => k.GetBinaryOperatorPrecedence() > 0);
        }

        public static string GetText(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusToken:
                    return "+";
                case SyntaxKind.MinusToken:
                    return "-";
                case SyntaxKind.StarToken:
                    return "*";
                case SyntaxKind.SlashToken:
                    return "/";
                case SyntaxKind.BangToken:
                    return "!";
                case SyntaxKind.EqualsToken:
                    return "=";
                case SyntaxKind.TildeToken:
                    return "~";
                case SyntaxKind.LessToken:
                    return "<";
                case SyntaxKind.LessOrEqualsToken:
                    return "<=";
                case SyntaxKind.GreaterToken:
                    return ">";
                case SyntaxKind.GreaterOrEqualsToken:
                    return ">=";
                case SyntaxKind.AmpersandToken:
                    return "&";
                case SyntaxKind.AmpersandAmpersandToken:
                    return "&&";
                case SyntaxKind.PipeToken:
                    return "|";
                case SyntaxKind.PipePipeToken:
                    return "||";
                case SyntaxKind.HatToken:
                    return "^";
                case SyntaxKind.EqualsEqualsToken:
                    return "==";
                case SyntaxKind.BangEqualsToken:
                    return "!=";
                case SyntaxKind.OpenParenthesisToken:
                    return "(";
                case SyntaxKind.CloseParenthesisToken:
                    return ")";
                case SyntaxKind.OpenBraceToken:
                    return "{";
                case SyntaxKind.CloseBraceToken:
                    return "}";
                case SyntaxKind.CommaToken:
                    return ",";
                case SyntaxKind.ElseKeyword:
                    return "else";
                case SyntaxKind.FalseKeyword:
                    return "false";
                case SyntaxKind.ForKeyword:
                    return "for";
                case SyntaxKind.IfKeyword:
                    return "if";
                case SyntaxKind.InKeyword:
                    return "in";
                case SyntaxKind.LetKeyword:
                    return "let";
                case SyntaxKind.RepeatKeyword:
                    return "repeat";
                case SyntaxKind.TrueKeyword:
                    return "true";
                case SyntaxKind.VarKeyword:
                    return "var";
                case SyntaxKind.WhileKeyword:
                    return "while";
                default:
                    return null;
            }
        }

        private static IEnumerable<SyntaxKind> GetKeywordKinds()
        {
            return Enum.GetValues(typeof(SyntaxKind))
                .Cast<SyntaxKind>()
                .Where(SyntaxKindExtensions.IsKeyword);
        }
    }
}
