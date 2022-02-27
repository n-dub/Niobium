namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundRepeatWhileStatement : BoundLoopStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.RepeatWhileStatement;
        public BoundExpression Condition { get; }
        public BoundBlockStatement Body { get; }

        public BoundRepeatWhileStatement(BoundExpression condition, BoundBlockStatement body,
            BoundLabel breakLabel, BoundLabel continueLabel)
            : base(breakLabel, continueLabel)
        {
            Condition = condition;
            Body = body;
        }
    }
}
