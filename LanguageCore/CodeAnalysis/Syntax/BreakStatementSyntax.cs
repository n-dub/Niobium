namespace LanguageCore.CodeAnalysis.Syntax
{
    internal class BreakStatementSyntax : StatementSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.BreakStatement;
        public SyntaxToken Keyword { get; }

        public BreakStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keyword) : base(syntaxTree)
        {
            Keyword = keyword;
        }
    }
}
