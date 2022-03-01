namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class WhileStatementSyntax : StatementSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.WhileStatement;
        public SyntaxToken WhileKeyword { get; }
        public ExpressionSyntax Condition { get; }
        public BlockStatementSyntax Body { get; }

        public WhileStatementSyntax(SyntaxTree syntaxTree, SyntaxToken whileKeyword, ExpressionSyntax condition,
            BlockStatementSyntax body)
            : base(syntaxTree)
        {
            WhileKeyword = whileKeyword;
            Condition = condition;
            Body = body;
        }
    }
}
