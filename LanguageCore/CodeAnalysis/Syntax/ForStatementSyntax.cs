namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class ForStatementSyntax : StatementSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.ForStatement;
        public SyntaxToken ForKeyword { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax LowerBound { get; }
        public SyntaxToken InKeyword { get; }
        public ExpressionSyntax UpperBound { get; }
        public BlockStatementSyntax Body { get; }

        public ForStatementSyntax(SyntaxToken forKeyword, SyntaxToken identifier, SyntaxToken equalsToken,
            ExpressionSyntax lowerBound, SyntaxToken inKeyword, ExpressionSyntax upperBound, BlockStatementSyntax body)
        {
            // We use 'in' keyword here instead of 'to', because later this will be changed:
            // The for loop will take a kind of IEnumerable<T> object and act like foreach.
            // Now:
            //   for i = 0 in 10 { }
            // In future:
            //   for i in range(from: 0, to: 10) { }
            ForKeyword = forKeyword;
            Identifier = identifier;
            EqualsToken = equalsToken;
            LowerBound = lowerBound;
            InKeyword = inKeyword;
            UpperBound = upperBound;
            Body = body;
        }
    }
}
