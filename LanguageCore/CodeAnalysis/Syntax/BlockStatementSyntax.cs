using System.Collections.Generic;

namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class BlockStatementSyntax : StatementSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.BlockStatement;
        public SyntaxToken OpenBraceToken { get; }
        public IReadOnlyList<StatementSyntax> Statements { get; }
        public SyntaxToken CloseBraceToken { get; }

        public BlockStatementSyntax(SyntaxTree syntaxTree, SyntaxToken openBraceToken,
            IReadOnlyList<StatementSyntax> statements, SyntaxToken closeBraceToken)
            : base(syntaxTree)
        {
            OpenBraceToken = openBraceToken;
            Statements = statements;
            CloseBraceToken = closeBraceToken;
        }
    }
}
