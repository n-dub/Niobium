namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class ExpressionStatementSyntax : StatementSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.ExpressionStatement;
        public ExpressionSyntax Expression { get; }

        public ExpressionStatementSyntax(ExpressionSyntax expression)
        {
            Expression = expression;
        }
    }
}
