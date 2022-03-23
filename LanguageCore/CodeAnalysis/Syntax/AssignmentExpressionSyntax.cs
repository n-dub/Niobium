namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class AssignmentExpressionSyntax : ExpressionSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.AssignmentExpression;
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken AssignmentToken { get; }
        public ExpressionSyntax Expression { get; }

        public AssignmentExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken,
            SyntaxToken assignmentToken,
            ExpressionSyntax expression)
            : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
            AssignmentToken = assignmentToken;
            Expression = expression;
        }
    }
}
