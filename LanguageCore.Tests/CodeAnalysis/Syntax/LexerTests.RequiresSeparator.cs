using LanguageCore.CodeAnalysis.Syntax;

namespace LanguageCore.Tests.CodeAnalysis.Syntax
{
    public partial class LexerTests
    {
        private static bool RequiresSeparator(SyntaxKind t1Kind, SyntaxKind t2Kind)
        {
            var t1IsKeyword = t1Kind.IsKeyword();
            var t2IsKeyword = t2Kind.IsKeyword();

            if (t1Kind == SyntaxKind.IdentifierToken && t2Kind == SyntaxKind.IdentifierToken)
            {
                return true;
            }

            if (t1IsKeyword && t2IsKeyword)
            {
                return true;
            }

            if ((t1IsKeyword || t1Kind == SyntaxKind.IdentifierToken) && t2Kind == SyntaxKind.NumberToken)
            {
                return true;
            }

            if (t1IsKeyword && t2Kind == SyntaxKind.IdentifierToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.IdentifierToken && t2IsKeyword)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.NumberToken && t2Kind == SyntaxKind.NumberToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.BangToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.BangToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.EqualsToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.EqualsToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.PlusToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.PlusToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.StarToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.StarToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.LessToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.LessToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.GreaterToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.GreaterToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandAmpersandToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipeToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipePipeToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipeEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.HatToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.HatToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.StringToken && t2Kind == SyntaxKind.StringToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.GreaterToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.GreaterOrEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.SlashToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.StarToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.SingleLineCommentTrivia)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.MultiLineCommentTrivia)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.SlashEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.StarEqualsToken)
            {
                return true;
            }

            return false;
        }
    }
}
