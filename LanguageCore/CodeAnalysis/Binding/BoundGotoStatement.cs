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
}
