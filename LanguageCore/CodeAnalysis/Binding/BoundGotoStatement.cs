namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundGotoStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.GotoStatement;
        public LabelSymbol Label { get; }

        public BoundGotoStatement(LabelSymbol label)
        {
            Label = label;
        }
    }
}
