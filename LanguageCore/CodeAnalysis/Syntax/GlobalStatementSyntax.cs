namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class GlobalStatementSyntax : MemberSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.GlobalStatement;
        public StatementSyntax Statement { get; }

        public GlobalStatementSyntax(StatementSyntax statement)
        {
            Statement = statement;
        }
    }
}
