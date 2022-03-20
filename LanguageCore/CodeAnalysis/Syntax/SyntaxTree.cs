using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanguageCore.CodeAnalysis.Text;

namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class SyntaxTree
    {
        public SourceText SourceText { get; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public CompilationUnitSyntax Root { get; }
        private const string DefaultFileName = "<repl>";

        private SyntaxTree(SourceText sourceText, ParseHandler handler)
        {
            SourceText = sourceText;
            handler(this, out var root, out var diagnostics);
            Diagnostics = diagnostics;
            Root = root;
        }

        public static SyntaxTree Load(string fileName)
        {
            var text = File.ReadAllText(fileName);
            var sourceText = SourceText.From(text, fileName);
            return Parse(sourceText);
        }

        public static SyntaxTree Parse(string text)
        {
            return Parse(SourceText.From(text, DefaultFileName));
        }

        public static SyntaxTree Parse(SourceText text)
        {
            return new SyntaxTree(text, Parse);
        }

        public static IReadOnlyList<SyntaxToken> ParseTokens(string text, bool includeEndOfFile = false)
        {
            return ParseTokens(SourceText.From(text, DefaultFileName), includeEndOfFile);
        }

        public static IReadOnlyList<SyntaxToken> ParseTokens(string text, out IReadOnlyList<Diagnostic> diagnostics,
            bool includeEndOfFile = false)
        {
            return ParseTokens(SourceText.From(text, DefaultFileName), out diagnostics, includeEndOfFile);
        }

        public static IReadOnlyList<SyntaxToken> ParseTokens(SourceText text, bool includeEndOfFile = false)
        {
            return ParseTokens(text, out _, includeEndOfFile);
        }

        public static IReadOnlyList<SyntaxToken> ParseTokens(SourceText text, out IReadOnlyList<Diagnostic> diagnostics,
            bool includeEndOfFile = false)
        {
            var tokens = new List<SyntaxToken>();

            void ParseTokens(SyntaxTree st, out CompilationUnitSyntax root, out IReadOnlyList<Diagnostic> d)
            {
                root = null;

                var l = new Lexer(st);
                while (true)
                {
                    var token = l.Lex();
                    if (token.Kind == SyntaxKind.EndOfFileToken)
                    {
                        root = new CompilationUnitSyntax(st, Array.Empty<MemberSyntax>(), token);
                    }

                    if (token.Kind != SyntaxKind.EndOfFileToken || includeEndOfFile)
                    {
                        tokens.Add(token);
                    }

                    if (token.Kind == SyntaxKind.EndOfFileToken)
                    {
                        break;
                    }
                }

                d = l.Diagnostics.ToArray();
            }

            var syntaxTree = new SyntaxTree(text, ParseTokens);
            diagnostics = syntaxTree.Diagnostics;
            return tokens;
        }

        private static void Parse(SyntaxTree syntaxTree, out CompilationUnitSyntax root,
            out IReadOnlyList<Diagnostic> diagnostics)
        {
            var parser = new Parser(syntaxTree);
            root = parser.ParseCompilationUnit();
            diagnostics = parser.Diagnostics.ToArray();
        }

        private delegate void ParseHandler(SyntaxTree syntaxTree,
            out CompilationUnitSyntax root,
            out IReadOnlyList<Diagnostic> diagnostics);
    }
}
