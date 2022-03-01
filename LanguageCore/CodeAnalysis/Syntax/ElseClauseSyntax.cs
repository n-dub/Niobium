namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class ElseClauseSyntax : SyntaxNode
    {
        public override SyntaxKind Kind => SyntaxKind.ElseClause;
        public SyntaxToken ElseKeyword { get; }
        public BlockStatementSyntax ElseStatement { get; }

        public ElseClauseSyntax(SyntaxTree syntaxTree, SyntaxToken elseKeyword,
            BlockStatementSyntax elseStatement)
            : base(syntaxTree)
        {
            ElseKeyword = elseKeyword;
            ElseStatement = elseStatement;
        }
    }
}
