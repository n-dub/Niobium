using LanguageCore.CodeAnalysis.Text;

namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class SyntaxTrivia
    {
        public SyntaxTree SyntaxTree { get; }
        public SyntaxKind Kind { get; }
        public int Position { get; }
        public TextSpan Span => new TextSpan(Position, Text?.Length ?? 0);
        public string Text { get; }

        public SyntaxTrivia(SyntaxTree syntaxTree, SyntaxKind kind, int position, string text)
        {
            SyntaxTree = syntaxTree;
            Kind = kind;
            Position = position;
            Text = text;
        }
    }
}
