using System.Collections.Generic;
using LanguageCore.CodeAnalysis.Text;

namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class SyntaxTree
    {
        public SourceText SourceText { get; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public ExpressionSyntax Root { get; }
        public SyntaxToken EndOfFileToken { get; }

        public SyntaxTree(SourceText sourceText, IReadOnlyList<Diagnostic> diagnostics, ExpressionSyntax root, SyntaxToken endOfFileToken)
        {
            SourceText = sourceText;
            Diagnostics = diagnostics;
            Root = root;
            EndOfFileToken = endOfFileToken;
        }

        public static SyntaxTree Parse(string text)
        {
            return Parse(SourceText.From(text));
        }

        public static SyntaxTree Parse(SourceText text)
        {
            return new Parser(text).Parse();
        }

        public static IEnumerable<SyntaxToken> ParseTokens(string text)
        {
            return ParseTokens(SourceText.From(text));
        }

        public static IEnumerable<SyntaxToken> ParseTokens(SourceText text)
        {
            var lexer = new Lexer(text);
            while (true)
            {
                var token = lexer.Lex();
                if (token.Kind == SyntaxKind.EndOfFileToken)
                {
                    break;
                }

                yield return token;
            }
        }
    }
}
