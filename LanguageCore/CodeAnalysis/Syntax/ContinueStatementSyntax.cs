namespace LanguageCore.CodeAnalysis.Syntax
{
    internal class ContinueStatementSyntax : StatementSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.ContinueStatement;
        public SyntaxToken Keyword { get; }

        public ContinueStatementSyntax(SyntaxToken keyword)
        {
            Keyword = keyword;
        }
    }
}
