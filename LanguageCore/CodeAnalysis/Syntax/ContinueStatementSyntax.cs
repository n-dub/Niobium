namespace LanguageCore.CodeAnalysis.Syntax
{
    internal class ContinueStatementSyntax : StatementSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.ContinueStatement;
        public SyntaxToken Keyword { get; }

        public ContinueStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keyword)
            : base(syntaxTree)
        {
            Keyword = keyword;
        }
    }
}
