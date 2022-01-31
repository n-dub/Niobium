namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class AssignmentExpressionSyntax : ExpressionSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.AssignmentExpression;
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Expression { get; }

        public AssignmentExpressionSyntax(SyntaxToken identifierToken, SyntaxToken equalsToken,
            ExpressionSyntax expression)
        {
            IdentifierToken = identifierToken;
            EqualsToken = equalsToken;
            Expression = expression;
        }
    }
}
