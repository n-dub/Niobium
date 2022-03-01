namespace LanguageCore.CodeAnalysis.Syntax
{
    public class RepeatWhileStatementSyntax : StatementSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.RepeatWhileStatement;

        public SyntaxToken RepeatKeyword { get; }
        public BlockStatementSyntax Body { get; }
        public SyntaxToken WhileKeyword { get; }
        public ExpressionSyntax Condition { get; }

        public RepeatWhileStatementSyntax(SyntaxTree syntaxTree, SyntaxToken repeatKeyword, BlockStatementSyntax body,
            SyntaxToken whileKeyword, ExpressionSyntax condition)
            : base(syntaxTree)
        {
            RepeatKeyword = repeatKeyword;
            Body = body;
            WhileKeyword = whileKeyword;
            Condition = condition;
        }
    }
}
