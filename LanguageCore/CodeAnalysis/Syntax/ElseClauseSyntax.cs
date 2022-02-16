namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class ElseClauseSyntax : SyntaxNode
    {
        public override SyntaxKind Kind => SyntaxKind.ElseClause;
        public SyntaxToken ElseKeyword { get; }
        public BlockStatementSyntax ElseStatement { get; }

        public ElseClauseSyntax(SyntaxToken elseKeyword, BlockStatementSyntax elseStatement)
        {
            ElseKeyword = elseKeyword;
            ElseStatement = elseStatement;
        }
    }
}
