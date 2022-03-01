namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class GlobalStatementSyntax : MemberSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.GlobalStatement;
        public StatementSyntax Statement { get; }

        public GlobalStatementSyntax(SyntaxTree syntaxTree, StatementSyntax statement)
            : base(syntaxTree)
        {
            Statement = statement;
        }
    }
}
