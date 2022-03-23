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

    internal static partial class BoundNodeFactory
    {
        public static BoundWhileStatement While(BoundExpression condition, BoundBlockStatement body,
            BoundLabel breakLabel, BoundLabel continueLabel)
        {
            return new BoundWhileStatement(condition, body, breakLabel, continueLabel);
        }
    }
}
