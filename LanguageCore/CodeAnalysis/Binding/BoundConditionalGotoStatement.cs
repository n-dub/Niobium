namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundConditionalGotoStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.ConditionalGotoStatement;
        public BoundLabel Label { get; }
        public BoundExpression Condition { get; }
        public bool JumpIfTrue { get; }

        public BoundConditionalGotoStatement(BoundLabel label, BoundExpression condition, bool jumpIfTrue = true)
        {
            Label = label;
            Condition = condition;
            JumpIfTrue = jumpIfTrue;
        }
    }

    internal static partial class BoundNodeFactory
    {
        public static BoundConditionalGotoStatement GotoIf(BoundLabelStatement label, BoundExpression condition,
            bool jumpIfTrue)
        {
            return new BoundConditionalGotoStatement(label.Label, condition, jumpIfTrue);
        }

        public static BoundConditionalGotoStatement GotoIf(BoundLabel label, BoundExpression condition,
            bool jumpIfTrue)
        {
            return new BoundConditionalGotoStatement(label, condition, jumpIfTrue);
        }

        public static BoundConditionalGotoStatement GotoTrue(BoundLabelStatement label, BoundExpression condition)
        {
            return GotoIf(label, condition, true);
        }

        public static BoundConditionalGotoStatement GotoFalse(BoundLabelStatement label, BoundExpression condition)
        {
            return GotoIf(label, condition, false);
        }
    }
}
