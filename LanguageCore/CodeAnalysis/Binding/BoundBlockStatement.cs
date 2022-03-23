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

    internal static partial class BoundNodeFactory
    {
        public static BoundBlockStatement Block(params BoundStatement[] statements)
        {
            return new BoundBlockStatement(statements);
        }
    }
}
