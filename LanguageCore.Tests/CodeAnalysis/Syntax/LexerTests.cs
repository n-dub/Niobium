using System;
using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Syntax;
using LanguageCore.CodeAnalysis.Text;
using NUnit.Framework;

namespace LanguageCore.Tests.CodeAnalysis.Syntax
{
    [TestFixture]
    public partial class LexerTests
    {
        [Test]
        public void Lexer_Lexes_UnterminatedString()
        {
            const string text = "\"text";
            var tokens = SyntaxTree.ParseTokens(text, out var diagnostics);

            var token = tokens.First();
            Assert.AreEqual(SyntaxKind.StringToken, token.Kind);
            Assert.AreEqual(text, token.Text);

            var diagnostic = diagnostics.First();
            Assert.AreEqual(new TextSpan(0, 1), diagnostic.Location.Span);
            Assert.AreEqual("Unterminated string literal.", diagnostic.Message);
        }

        [Test]
        public void Lexer_Covers_AllTokens()
        {
            var tokenKinds = Enum.GetValues(typeof(SyntaxKind))
                .Cast<SyntaxKind>()
                .Where(SyntaxKindExtensions.IsToken);

            var testedTokenKinds = GetTokens().Concat(GetSeparators()).Select(t => t.kind);

            var untestedTokenKinds = new SortedSet<SyntaxKind>(tokenKinds);
            untestedTokenKinds.Remove(SyntaxKind.BadToken);
            untestedTokenKinds.Remove(SyntaxKind.EndOfFileToken);
            untestedTokenKinds.ExceptWith(testedTokenKinds);

            Assert.IsEmpty(untestedTokenKinds);
        }

        [TestCaseSource(nameof(GetSeparatorsData))]
        public void Lexer_Lexes_Separator(SyntaxKind kind, string text)
        {
            var tokens = SyntaxTree.ParseTokens(text, true);

            var token = tokens.First();
            var trivia = token.LeadingTrivia.First();
            Assert.AreEqual(kind, trivia.Kind);
            Assert.AreEqual(text, trivia.Text);
        }

        [TestCaseSource(nameof(GetTokensData))]
        public void Lexer_Lexes_Token(SyntaxKind kind, string text)
        {
            var tokens = SyntaxTree.ParseTokens(text);

            var token = tokens.First();
            Assert.AreEqual(kind, token.Kind);
            Assert.AreEqual(text, token.Text);
        }

        [TestCaseSource(nameof(GetTokenPairsData))]
        public void Lexer_Lexes_TokenPairs(SyntaxKind t1Kind, string t1Text,
            SyntaxKind t2Kind, string t2Text)
        {
            var text = t1Text + t2Text;
            var tokens = SyntaxTree.ParseTokens(text).ToArray();

            Assert.AreEqual(2, tokens.Length);
            Assert.AreEqual(t1Kind, tokens[0].Kind);
            Assert.AreEqual(t1Text, tokens[0].Text);
            Assert.AreEqual(t2Kind, tokens[1].Kind);
            Assert.AreEqual(t2Text, tokens[1].Text);
        }

        [TestCaseSource(nameof(GetTokenPairsWithSeparatorData))]
        public void Lexer_Lexes_TokenPairs_WithSeparators(SyntaxKind t1Kind, string t1Text,
            SyntaxKind separatorKind, string separatorText,
            SyntaxKind t2Kind, string t2Text)
        {
            var text = t1Text + separatorText + t2Text;
            var tokens = SyntaxTree.ParseTokens(text).ToArray();

            Assert.AreEqual(2, tokens.Length);
            Assert.AreEqual(t1Kind, tokens[0].Kind);
            Assert.AreEqual(t1Text, tokens[0].Text);

            var separator = tokens[0].TrailingTrivia.Single();
            Assert.AreEqual(separatorKind, separator.Kind);
            Assert.AreEqual(separatorText, separator.Text);

            Assert.AreEqual(t2Kind, tokens[1].Kind);
            Assert.AreEqual(t2Text, tokens[1].Text);
        }

        public static IEnumerable<object[]> GetTokensData()
        {
            return GetTokens()
                .Select(t => new object[] {t.kind, t.text});
        }

        public static IEnumerable<object[]> GetSeparatorsData()
        {
            return GetSeparators()
                .Select(t => new object[] {t.kind, t.text});
        }

        public static IEnumerable<object[]> GetTokenPairsData()
        {
            return GetTokenPairs().Select(t =>
                new object[] {t.t1Kind, t.t1Text, t.t2Kind, t.t2Text});
        }

        public static IEnumerable<object[]> GetTokenPairsWithSeparatorData()
        {
            return GetTokenPairsWithSeparator().Select(t =>
                new object[] {t.t1Kind, t.t1Text, t.separatorKind, t.separatorText, t.t2Kind, t.t2Text});
        }

        private static IEnumerable<(SyntaxKind kind, string text)> GetTokens()
        {
            var fixedTokens = Enum.GetValues(typeof(SyntaxKind))
                .Cast<SyntaxKind>()
                .Select(k => (kind: k, text: SyntaxFacts.GetText(k)))
                .Where(t => t.text != null)
                .Select(t => (t.kind, t.text!));


            var dynamicTokens = new[]
            {
                (SyntaxKind.NumberToken, "1"),
                (SyntaxKind.NumberToken, "123"),
                (SyntaxKind.IdentifierToken, "a"),
                (SyntaxKind.IdentifierToken, "abc"),
                (SyntaxKind.StringToken, "\"Test\""),
                (SyntaxKind.StringToken, "\"Te\\\"st\"")
            };

            return fixedTokens.Concat(dynamicTokens);
        }

        private static IEnumerable<(SyntaxKind kind, string text)> GetSeparators()
        {
            return new[]
            {
                (SyntaxKind.WhitespaceTrivia, " "),
                (SyntaxKind.WhitespaceTrivia, "  "),
                (SyntaxKind.LineBreakTrivia, "\r"),
                (SyntaxKind.LineBreakTrivia, "\n"),
                (SyntaxKind.LineBreakTrivia, "\r\n"),
                (SyntaxKind.MultiLineCommentTrivia, "/**/")
            };
        }

        private static IEnumerable<(SyntaxKind t1Kind, string t1Text, SyntaxKind t2Kind, string t2Text)> GetTokenPairs()
        {
            foreach (var t1 in GetTokens())
            {
                foreach (var t2 in GetTokens())
                {
                    if (!RequiresSeparator(t1.kind, t2.kind))
                    {
                        yield return (t1.kind, t1.text, t2.kind, t2.text);
                    }
                }
            }
        }

        private static IEnumerable<(SyntaxKind t1Kind, string t1Text,
            SyntaxKind separatorKind, string separatorText,
            SyntaxKind t2Kind, string t2Text)> GetTokenPairsWithSeparator()
        {
            foreach (var t1 in GetTokens())
            {
                foreach (var t2 in GetTokens())
                {
                    if (RequiresSeparator(t1.kind, t2.kind))
                    {
                        foreach (var s in GetSeparators())
                        {
                            if (!RequiresSeparator(t1.kind, s.kind) && !RequiresSeparator(s.kind, t2.kind))
                            {
                                yield return (t1.kind, t1.text, s.kind, s.text, t2.kind, t2.text);
                            }
                        }
                    }
                }
            }
        }
    }
}
