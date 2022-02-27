namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundWhileStatement : BoundLoopStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;
        public BoundExpression Condition { get; }
        public BoundBlockStatement Body { get; }

        public BoundWhileStatement(BoundExpression condition, BoundBlockStatement body, BoundLabel breakLabel,
            BoundLabel continueLabel)
            : base(breakLabel, continueLabel)
        {
            Condition = condition;
            Body = body;
        }
    }
}
