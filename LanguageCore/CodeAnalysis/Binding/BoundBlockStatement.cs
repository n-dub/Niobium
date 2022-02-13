using System.Collections.Generic;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundBlockStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.BlockStatement;
        public IReadOnlyList<BoundStatement> Statements { get; }

        public BoundBlockStatement(IReadOnlyList<BoundStatement> statements)
        {
            Statements = statements;
        }
    }
}
