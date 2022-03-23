namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundGotoStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.GotoStatement;
        public BoundLabel Label { get; }

        public BoundGotoStatement(BoundLabel label)
        {
            Label = label;
        }
    }

    internal static partial class BoundNodeFactory
    {
        public static BoundGotoStatement Goto(BoundLabelStatement label)
        {
            return new BoundGotoStatement(label.Label);
        }

        public static BoundGotoStatement Goto(BoundLabel label)
        {
            return new BoundGotoStatement(label);
        }
    }
}
