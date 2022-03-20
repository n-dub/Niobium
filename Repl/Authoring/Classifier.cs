using System;
using System.Collections.Generic;
using LanguageCore.CodeAnalysis.Syntax;
using LanguageCore.CodeAnalysis.Text;

namespace Repl.Authoring
{
    public static class Classifier
    {
        public static IReadOnlyList<ClassifiedSpan> Classify(SyntaxTree syntaxTree, TextSpan span)
        {
            var result = new List<ClassifiedSpan>();
            ClassifyNode(syntaxTree.Root, span, result);
            return result;
        }

        private static void ClassifyNode(SyntaxNode node, TextSpan span, List<ClassifiedSpan> result)
        {
            if (node == null || !node.FullSpan.OverlapsWith(span))
            {
                return;
            }

            if (node is SyntaxToken token)
            {
                ClassifyToken(token, span, result);
            }

            foreach (var child in node.GetChildren())
            {
                ClassifyNode(child, span, result);
            }
        }

        private static void ClassifyToken(SyntaxToken token, TextSpan span, List<ClassifiedSpan> result)
        {
            foreach (var leadingTrivia in token.LeadingTrivia)
            {
                ClassifyTrivia(leadingTrivia, span, result);
            }

            AddClassification(token.Kind, token.Span, span, result);

            foreach (var trailingTrivia in token.TrailingTrivia)
            {
                ClassifyTrivia(trailingTrivia, span, result);
            }
        }

        private static void ClassifyTrivia(SyntaxTrivia trivia, TextSpan span, List<ClassifiedSpan> result)
        {
            AddClassification(trivia.Kind, trivia.Span, span, result);
        }

        private static void AddClassification(SyntaxKind elementKind, TextSpan elementSpan, TextSpan span,
            List<ClassifiedSpan> result)
        {
            if (!elementSpan.OverlapsWith(span))
            {
                return;
            }

            var adjustedStart = Math.Max(elementSpan.Start, span.Start);
            var adjustedEnd = Math.Min(elementSpan.End, span.End);
            var adjustedSpan = TextSpan.FromBounds(adjustedStart, adjustedEnd);
            var classification = GetClassification(elementKind);

            var classifiedSpan = new ClassifiedSpan(adjustedSpan, classification);
            result.Add(classifiedSpan);
        }

        private static Classification GetClassification(SyntaxKind kind)
        {
            if (kind.IsLiteralKeyword())
            {
                return Classification.LiteralKeyword;
            }

            if (kind.IsKeyword())
            {
                return Classification.Keyword;
            }

            if (kind.IsComment())
            {
                return Classification.Comment;
            }

            switch (kind)
            {
                case SyntaxKind.IdentifierToken:
                    return Classification.Identifier;
                case SyntaxKind.NumberToken:
                    return Classification.Number;
                case SyntaxKind.StringToken:
                    return Classification.String;
            }

            return Classification.Text;
        }
    }
}
