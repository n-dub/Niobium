namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundWhileStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;
        public BoundExpression Condition { get; }
        public BoundBlockStatement Body { get; }

        public BoundWhileStatement(BoundExpression condition, BoundBlockStatement body)
        {
            Condition = condition;
            Body = body;
        }
    }
}
