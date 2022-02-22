namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundRepeatWhileStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.RepeatWhileStatement;
        public BoundExpression Condition { get; }
        public BoundBlockStatement Body { get; }

        public BoundRepeatWhileStatement(BoundExpression condition, BoundBlockStatement body)
        {
            Condition = condition;
            Body = body;
        }
    }
}
