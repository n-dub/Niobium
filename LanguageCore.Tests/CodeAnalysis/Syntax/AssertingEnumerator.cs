using System;
using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Syntax;
using NUnit.Framework;

namespace LanguageCore.Tests.CodeAnalysis.Syntax
{
    internal sealed class AssertingEnumerator : IDisposable
    {
        private readonly IEnumerator<SyntaxNode> enumerator;
        private bool hasErrors;

        public AssertingEnumerator(SyntaxNode node)
        {
            enumerator = Flatten(node).GetEnumerator();
        }

        private bool MarkFailed()
        {
            hasErrors = true;
            return false;
        }

        public void Dispose()
        {
            if (!hasErrors)
            {
                Assert.False(enumerator.MoveNext());
            }

            enumerator.Dispose();
        }

        private static IEnumerable<SyntaxNode> Flatten(SyntaxNode node)
        {
            var stack = new Stack<SyntaxNode>();
            stack.Push(node);

            while (stack.Count > 0)
            {
                var n = stack.Pop();
                yield return n;

                foreach (var child in n.GetChildren().Reverse())
                {
                    stack.Push(child);
                }
            }
        }

        public void AssertNode(SyntaxKind kind)
        {
            try
            {
                Assert.True(enumerator.MoveNext());
                Assert.AreEqual(kind, enumerator.Current.Kind);
                Assert.AreNotEqual(typeof(SyntaxToken), enumerator.Current.GetType());
            }
            catch when (MarkFailed())
            {
                throw;
            }
        }

        public void AssertToken(SyntaxKind kind, string text)
        {
            try
            {
                Assert.True(enumerator.MoveNext());
                Assert.AreEqual(kind, enumerator.Current.Kind);
                var token = (SyntaxToken) enumerator.Current;
                Assert.AreEqual(text, token.Text);
            }
            catch when (MarkFailed())
            {
                throw;
            }
        }
    }
}
