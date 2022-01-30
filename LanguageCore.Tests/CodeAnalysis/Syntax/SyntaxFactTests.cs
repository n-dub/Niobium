using System;
using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Syntax;
using NUnit.Framework;

namespace LanguageCore.Tests.CodeAnalysis.Syntax
{
    public class SyntaxFactTests
    {
        [TestCaseSource(nameof(GetSyntaxKindData))]
        public void SyntaxFact_GetText_RoundTrips(SyntaxKind kind)
        {
            var text = SyntaxFacts.GetText(kind);
            if (text == null)
                return;

            var tokens = SyntaxTree.ParseTokens(text);
            var token = tokens.First();
            Assert.AreEqual(kind, token.Kind);
            Assert.AreEqual(text, token.Text);
        }

        public static IEnumerable<object[]> GetSyntaxKindData()
        {
            return Enum.GetValues(typeof(SyntaxKind))
                .Cast<SyntaxKind>()
                .Select(k => new object[] {k});
        }
    }
}
