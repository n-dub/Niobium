namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class ReturnStatementSyntax : StatementSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.ReturnStatement;
        public SyntaxToken ReturnKeyword { get; }
        public ExpressionSyntax Expression { get; }

        public ReturnStatementSyntax(SyntaxToken returnKeyword, ExpressionSyntax expression)
        {
            ReturnKeyword = returnKeyword;
            Expression = expression;
        }
    }
}
