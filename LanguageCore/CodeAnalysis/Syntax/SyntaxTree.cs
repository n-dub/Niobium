using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Text;

namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class SyntaxTree
    {
        public SourceText SourceText { get; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public CompilationUnitSyntax Root { get; }

        private SyntaxTree(SourceText sourceText)
        {
            var parser = new Parser(sourceText);
            var root = parser.ParseCompilationUnit();
            var diagnostics = parser.Diagnostics.ToArray();

            SourceText = sourceText;
            Diagnostics = diagnostics;
            Root = root;
        }

        public static SyntaxTree Parse(string text)
        {
            return Parse(SourceText.From(text));
        }

        public static SyntaxTree Parse(SourceText text)
        {
            return new SyntaxTree(text);
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
