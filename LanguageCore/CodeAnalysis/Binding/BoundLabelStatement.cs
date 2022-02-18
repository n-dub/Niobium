namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundLabelStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.LabelStatement;
        public LabelSymbol Label { get; }

        public BoundLabelStatement(LabelSymbol label)
        {
            Label = label;
        }
    }
}
