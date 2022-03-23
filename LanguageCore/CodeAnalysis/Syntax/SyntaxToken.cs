using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Text;

namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class SyntaxToken : SyntaxNode
    {
        public override SyntaxKind Kind { get; }
        public int Position { get; }
        public string Text { get; }
        public object? Value { get; }
        public IReadOnlyList<SyntaxTrivia> LeadingTrivia { get; }
        public IReadOnlyList<SyntaxTrivia> TrailingTrivia { get; }
        public override TextSpan Span => new TextSpan(Position, Text.Length);

        public override TextSpan FullSpan
        {
            get
            {
                var start = LeadingTrivia.Count == 0
                    ? Span.Start
                    : LeadingTrivia.First().Span.Start;
                var end = TrailingTrivia.Count == 0
                    ? Span.End
                    : TrailingTrivia.Last().Span.End;
                return TextSpan.FromBounds(start, end);
            }
        }

        public bool IsMissing { get; }

        public SyntaxToken(SyntaxTree syntaxTree, SyntaxKind kind, int position, string? text, object? value,
            IReadOnlyList<SyntaxTrivia> leadingTrivia, IReadOnlyList<SyntaxTrivia> trailingTrivia)
            : base(syntaxTree)
        {
            Kind = kind;
            Position = position;
            Text = text ?? string.Empty;
            IsMissing = text is null;
            Value = value;
            LeadingTrivia = leadingTrivia;
            TrailingTrivia = trailingTrivia;
        }
    }
}
